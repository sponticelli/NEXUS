using System;
using UnityEngine;

namespace Nexus.Core.Services
{
    [ServiceImplementation]
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Debug.Log(message);
        }
    }
}