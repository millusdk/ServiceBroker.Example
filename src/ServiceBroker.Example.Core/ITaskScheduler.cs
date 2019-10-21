using System;
using System.Threading;

namespace ServiceBroker.Example.Core
{
    /// <summary>
    /// An interface that represents any service that is able to schedule an action on a background thread.
    /// </summary>
    public interface ITaskScheduler
    {
        /// <summary>
        /// Executes the specified action on a background thread.
        /// </summary>
        /// <param name="workItem">The action to be executed on the background thread.</param>
        void QueueBackgroundWorkItem(Action<CancellationToken> workItem);
    }
}
