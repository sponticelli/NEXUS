using System;

namespace Nexus.Core.Rx
{
    /// <summary>
    /// Enhanced error handling with detailed context
    /// </summary>
    public class RxException : Exception
    {
        public string OperatorName { get; }
        public object LastValue { get; }
        public string Context { get; }

        public RxException(
            string operatorName, 
            object lastValue, 
            Exception inner,
            string context = null) 
            : base($"Error in {operatorName}: {inner.Message}\nContext: {context ?? "None"}", inner)
        {
            OperatorName = operatorName;
            LastValue = lastValue;
            Context = context;
        }
    }
}