using System;
using System.Threading;

namespace ServiceBroker.Example.Core
{
    public interface ITaskScheduler
    {
        void QueueBackgroundWorkItem(Action<CancellationToken> workItem);
    }
}
