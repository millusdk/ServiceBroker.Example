using System;
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
    /// <summary>
    /// A service broker wrapping calls to multiple external services, running them in parallel, handling timeouts for the service calls, and returning a combined result.
    /// </summary>
    public class ServiceBrokerService : IServiceBrokerService
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly IDynamicService _dynamicService;
        private readonly ICachedService _cachedService;
        private readonly IStaticService _staticService;
        private readonly ITaskScheduler _taskScheduler;
        private readonly ICache _cache;

        public ServiceBrokerService(IServiceRepository serviceRepository, IDynamicService dynamicService,
            ICachedService cachedService, IStaticService staticService, ITaskScheduler taskScheduler, ICache cache)
        {
            _serviceRepository = serviceRepository;
            _dynamicService = dynamicService;
            _cachedService = cachedService;
            _taskScheduler = taskScheduler;
            _cache = cache;
            _staticService = staticService;
        }

        /// <summary>
        /// Calls the services specified by the service and token ids, and returns the results of these calls.
        /// </summary>
        /// <param name="serviceOrTokenIds">Ids for the services and/or tokens to retrieve information for.</param>
        /// <param name="cacheRegion">Cache region to use for storing and retrieving values, if required by the services called.</param>
        /// <param name="timeout">Maximum time the services should wait for answers from external services.</param>
        /// <returns>An object representing the result of the calls to the services specified by the service and token ids.</returns>
        public ServiceBrokerResponse CallServices(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout)
        {
            return CallServicesAsync(serviceOrTokenIds, cacheRegion, timeout).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Calls the services specified by the service and token ids, and returns the results of these calls.
        /// </summary>
        /// <param name="serviceOrTokenIds">Ids for the services and/or tokens to retrieve information for.</param>
        /// <param name="cacheRegion">Cache region to use for storing and retrieving values, if required by the services called.</param>
        /// <param name="timeout">Maximum time the service should wait for answers from external services.</param>
        /// <returns>An object representing the result of the calls to the services specified by the service and token ids.</returns>
        public async Task<ServiceBrokerResponse> CallServicesAsync(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion, TimeSpan timeout)
        {
            return await CallServicesAsyncInternal(serviceOrTokenIds, cacheRegion, timeout, null);
        }

        /// <summary>
        /// Calls the specified service, and returns the result of the call.
        /// </summary>
        /// <param name="serviceId">The id of the service to call.</param>
        /// <param name="cacheRegion">he region to use for storing and retrieving values, if required by the service called.</param>
        /// <param name="timeout">Maximum time the service should wait for answers from service.</param>
        /// <param name="additionalParameters">Specification of additional parameters to include in the body of any outgoing requests performed as part of the processing of the service.</param>
        /// <returns>An object representing the result of the call to the external service.</returns>
        public ServiceResponse CallService(Guid serviceId, string cacheRegion, TimeSpan timeout, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            return CallServiceAsync(serviceId, cacheRegion, timeout, additionalParameters).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Calls the specified service, and returns the result of the call.
        /// </summary>
        /// <param name="serviceId">The id of the service to call.</param>
        /// <param name="cacheRegion">he region to use for storing and retrieving values, if required by the service called.</param>
        /// <param name="timeout">Maximum time the service should wait for answers from service.</param>
        /// <param name="additionalParameters">Specification of additional parameters to include in the body of any outgoing requests performed as part of the processing of the service.</param>
        /// <returns>An object representing the result of the call to the external service.</returns>
        public async Task<ServiceResponse> CallServiceAsync(Guid serviceId, string cacheRegion, TimeSpan timeout, IEnumerable<KeyValuePair<string, string>> additionalParameters)
        {
            ServiceBrokerResponse serviceBrokerResponse = await CallServicesAsyncInternal(new[] { serviceId }, cacheRegion, timeout, additionalParameters);

            var serviceResponse = serviceBrokerResponse.ServiceResponses.FirstOrDefault();

            return serviceResponse ?? new ServiceResponse
            {
                ServiceId = serviceId,
                Status = ServiceResponseStatus.Error
            };
        }

        /// <summary>
        /// Starts call to the services specified by the service and token ids on a background thread.
        /// </summary>
        /// <param name="serviceOrTokenIds">Ids for the services and/or tokens to retrieve information for.</param>
        /// <param name="cacheRegion">Cache region to use for storing and retrieving values, if required by the services called.</param>
        /// <param name="timeout">Maximum time the services should wait for answers from external services.</param>
        public void StartBackgroundServiceCalls(IEnumerable<Guid> serviceOrTokenIds, string cacheRegion,
            TimeSpan timeout)
        {
            _taskScheduler.QueueBackgroundWorkItem(async token =>
                {
                    await CallServicesAsync(serviceOrTokenIds, cacheRegion, timeout);
                });
        }

        /// <summary>
        /// Generates an xml document containing all the data returned from external services
        /// </summary>
        /// <param name="cacheRegion">The cache region to look for service and token data under</param>
        /// <param name="forVisualization"></param>
        /// <returns>The user profile containing the responses from the services</returns>
        public XDocument GetUserProfile(string cacheRegion, bool forVisualization)
        {
            var doc = new XDocument();
            doc.Add(new XElement("user"));

            IEnumerable<ServiceInfo> cachedServices = _serviceRepository.GetCachedServices();
            IEnumerable<ServiceInfo> staticServices = _serviceRepository.GetStaticServices();

            IEnumerable<ServiceInfo> services = cachedServices.Union(staticServices);
            IEnumerable<ServiceInfo> filteredServices = services.Where(service => !(forVisualization && !service.ExcludeFromVisibleUserProfile));
            IOrderedEnumerable<ServiceInfo> orderedServices = filteredServices.OrderBy(service => service.Name);

            var tokens = new XElement("tokens");

            foreach (ServiceInfo service in orderedServices)
            {
                CacheEntry<string> cacheEntry = _cache.Get<string>(cacheRegion, service.CacheKey);

                if (cacheEntry != null && cacheEntry.Value != null)
                {
                    var serviceNode = new XElement(service.Name);

                    LoadValue(cacheEntry.Value, serviceNode);

                    // ReSharper disable once PossibleNullReferenceException
                    doc.Root.Add(serviceNode);

                    foreach (TokenInfo token in service.Tokens)
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
                    case StaticServiceInfo staticServiceInfo:
                        task = _staticService.CallService(staticServiceInfo, cacheRegion, cancellationToken, additionalParams);
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
