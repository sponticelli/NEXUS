using System;
using UnityEngine;

namespace Nexus.Sequences
{
    public abstract class BaseStep : MonoBehaviour, IStep
    {
        protected bool isInitialized;
        protected bool isComplete;
        protected bool isFinished;

        public bool IsInitialized => isInitialized;
        public bool IsComplete => isComplete;
        public bool IsFinished => isFinished;

        public event Action OnComplete;
        public event Action OnFinished;

        protected virtual void Awake()
        {
            isInitialized = false;
            isComplete = false;
            isFinished = false;
        }

        public virtual void InitStep()
        {
            isInitialized = true;
            isComplete = false;
            isFinished = false;
        }

        public virtual void StartStep()
        {
            if (!isInitialized)
                InitStep();
        }

        public virtual void UpdateStep() { }

        public virtual void CleanupStep()
        {
            isInitialized = false;
            isComplete = false;
            isFinished = false;
        }

        protected virtual void Complete()
        {
            if (!isComplete)
            {
                isComplete = true;
                OnComplete?.Invoke();
            }
        }

        protected virtual void Finish()
        {
            if (!isFinished)
            {
                isFinished = true;
                OnFinished?.Invoke();
            }
        }
    }
}