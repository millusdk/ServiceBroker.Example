﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ServiceBroker.Example.Core;
using ServiceBroker.Example.Models;

namespace ServiceBroker.Example
{
    public class ServiceBrokerService : IServiceBrokerService
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly IDynamicService _dynamicService;
        private readonly ICachedService _cachedService;
        private readonly ITaskScheduler _taskScheduler;
        private readonly ICache _cache;

        public ServiceBrokerService(IServiceRepository serviceRepository, IDynamicService dynamicService, ICachedService cachedService, ITaskScheduler taskScheduler, ICache cache)
        {
            _serviceRepository = serviceRepository;
            _dynamicService = dynamicService;
            _cachedService = cachedService;
            _taskScheduler = taskScheduler;
            _cache = cache;
        }

        public ServiceBrokerResponse CallServices(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout)
        {
            return CallServicesAsync(serviceOrTokenIds, cacheRegion, timeout).GetAwaiter().GetResult();
        }

        public ServiceResponse CallService(Guid serviceId, string cacheRegion, TimeSpan timeout, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            return CallServiceAsync(serviceId, cacheRegion, timeout, additionalParameters).GetAwaiter().GetResult();
        }

        public async Task<ServiceResponse> CallServiceAsync(Guid serviceId, string cacheRegion, TimeSpan timeout, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            ServiceBrokerResponse serviceBrokerResponse = await CallServicesAsyncInternal(new[] { serviceId }, cacheRegion, timeout, additionalParameters);

            var serviceResponse = serviceBrokerResponse.ServiceResponses.FirstOrDefault();

            return serviceResponse ?? new ServiceResponse
            {
                Id = serviceId,
                Status = ServiceResponseStatus.Error
            };
        }

        public async Task<ServiceBrokerResponse> CallServicesAsync(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout)
        {
            return await CallServicesAsyncInternal(serviceOrTokenIds, cacheRegion, timeout, null);
        }

        public void StartBackgroundServiceCalls(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion,
            TimeSpan timeout)
        {
            _taskScheduler.QueueBackgroundWorkItem(async token =>
                {
                    await CallServicesAsync(serviceOrTokenIds, cacheRegion, timeout);
                });
        }

        public XDocument GetUserProfile(string cacheRegion)
        {
            var doc = new XDocument();
            doc.Add(new XElement("user"));

            IEnumerable<CachedServiceInfo> cachedServices = _serviceRepository.GetCachedServices();

            var tokens = new XElement("tokens");

            foreach (CachedServiceInfo cachedService in cachedServices)
            {
                CacheEntry<string> cacheEntry = _cache.Get<string>(cacheRegion, cachedService.CacheKey);

                if (cacheEntry != null && cacheEntry.Value != null)
                {
                    var serviceNode = new XElement(cachedService.Name);

                    LoadValue(cacheEntry.Value, serviceNode);

                    // ReSharper disable once PossibleNullReferenceException
                    doc.Root.Add(serviceNode);

                    foreach (TokenInfo token in cachedService.Tokens)
                    {
                        CacheEntry<string> tokenCacheEntry = _cache.Get<string>(cacheRegion, token.CacheKey);

                        if (tokenCacheEntry?.Value != null)
                        {
                            var tokenNode = new XElement(token.Name);

                            LoadValue(tokenCacheEntry.Value, tokenNode);
                            tokens.Add(tokenNode);
                        }
                    }
                }
            }


            if (tokens.HasElements)
            {
                // ReSharper disable once PossibleNullReferenceException
                doc.Root.Add(tokens);
            }

            return doc;
        }

        private static void LoadValue(string content, XElement parentNode)
        {
            try
            {
                XElement contentNode = XElement.Parse(content);
                parentNode.Add(contentNode);
            }
            catch (Exception)
            {
                parentNode.Add(content);
            }
        }

        private async Task<ServiceBrokerResponse> CallServicesAsyncInternal(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            IEnumerable<Guid> serviceAndTokenIds = serviceOrTokenIds as Guid[] ?? serviceOrTokenIds?.ToArray();
            if (!serviceAndTokenIds?.Any() ?? true)
            {
                return null;
            }

            IEnumerable<ServiceInfo> services = _serviceRepository.GetServicesAndTokens(serviceAndTokenIds);

            var cancellationTokenSource = new CancellationTokenSource();

            IEnumerable<Task<ServiceResponse>> serviceTasks = GetServiceTasks(services, cacheRegion, cancellationTokenSource.Token, additionalParameters).ToArray();

            Task<ServiceResponse[]> combinedTask = Task.WhenAll(serviceTasks);
            Task sleepTask = Task.Delay(timeout, cancellationTokenSource.Token);

            await Task.WhenAny(combinedTask, sleepTask);

            cancellationTokenSource.Cancel(true);

            IEnumerable<ServiceResponse> serviceResponses = GetServiceResponses(serviceTasks).ToArray();

            return new ServiceBrokerResponse
            {
                ServiceResponses = serviceResponses
            };
        }

        private static IEnumerable<ServiceResponse> GetServiceResponses(IEnumerable<Task<ServiceResponse>> serviceTasks)
        {
            foreach (Task<ServiceResponse> serviceTask in serviceTasks)
            {
                switch (serviceTask.Status)
                {
                    case TaskStatus.RanToCompletion:
                        ServiceResponse result = serviceTask.GetAwaiter().GetResult();

                        if (result != null)
                        {
                            yield return result;
                        }
                        break;
                    case TaskStatus.Faulted:
                        yield return new ServiceResponse
                        {
                            Status = ServiceResponseStatus.Error,
                            TokenResponses = new TokenResponse[0]
                        };
                        break;
                    default:
                        yield return new ServiceResponse
                        {
                            Status = ServiceResponseStatus.Timeout,
                            TokenResponses = new TokenResponse[0]
                        };
                        break;
                }
            }
        }

        private IEnumerable<Task<ServiceResponse>> GetServiceTasks(IEnumerable<ServiceInfo> services,
            string cacheRegion, CancellationToken cancellationToken,
            IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            var taskList = new List<Task<ServiceResponse>>();

            foreach (ServiceInfo service in services)
            {
                Task<ServiceResponse> task = null;

                // ReSharper disable once PossibleMultipleEnumeration
                IEnumerable<KeyValuePair<string, string>> additionalParams = additionalParameters as KeyValuePair<string, string>[];
                if (additionalParams == null)
                {
                    if (additionalParameters != null)
                    {
                        // ReSharper disable once PossibleMultipleEnumeration
                        additionalParams = additionalParameters.ToArray();
                    }
                }

                switch (service)
                {
                    case DynamicServiceInfo dynamicServiceInfo:
                        {
                            task = _dynamicService.CallService(dynamicServiceInfo, cacheRegion, cancellationToken, additionalParams);
                            break;
                        }
                    case CachedServiceInfo cachedServiceInfo:
                        task = _cachedService.CallService(cachedServiceInfo, cacheRegion, cancellationToken, additionalParams);
                        break;
                }

                if (task != null)
                {
                    taskList.Add(task);
                }
            }

            return taskList;
        }
    }
}