using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceBroker.Example.Models
{
    /// <summary>
    /// Contains information about a static service which writes data directly to the cache during another operation, ie. the login system writing claims or properties about the user.
    /// </summary>
    public class StaticServiceInfo : ServiceInfo
    {
        public string Data { get; set; }
    }
}
