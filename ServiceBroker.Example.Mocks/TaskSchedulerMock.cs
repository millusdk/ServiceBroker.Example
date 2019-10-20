using System;
using System.Threading;
using ServiceBroker.Example.Core;

namespace ServiceBroker.Example.Mocks
{
    public class TaskSchedulerMock : ITaskScheduler
    {
        public void QueueBackgroundWorkItem(Action<CancellationToken> workItem)
        {
            workItem.Invoke(CancellationToken.None);
        }
    }
}
