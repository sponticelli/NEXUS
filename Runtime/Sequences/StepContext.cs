using System;
using System.Collections.Generic;

namespace Nexus.Sequences
{
    [Serializable]
    public class StepContext
    {
        private Dictionary<Type, IStepData> dataStore = new();
    
        public void SetData<T>(T data) where T : IStepData
        {
            dataStore[typeof(T)] = data;
        }
    
        public T GetData<T>() where T : IStepData
        {
            return (T)dataStore[typeof(T)];
        }
    }
}