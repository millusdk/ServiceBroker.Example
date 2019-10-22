using System;

namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Contains information needed by the circuit breaker
    /// </summary>
    public class CircuitBreakerInfo
    {
        /// <summary>
        /// Gets or sets the amount consecutive exceptions to count before the circuit is opened
        /// </summary>
        public int ExceptionCount { get; set; }

        /// <summary>
        /// How log the circuit should remain open before changing to closed or half-open
        /// </summary>
        public TimeSpan BreakDuration { get; set; }
    }
}