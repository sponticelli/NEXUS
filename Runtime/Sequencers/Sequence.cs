using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Nexus.Sequencers
{
    public class Sequence : MonoBehaviour
    {
        private BaseStep[] sequenceSteps;
        private int currentIndex = -1;
        private bool isRunning;
        public bool IsPaused { get; private set; }
        public bool IsComplete => currentIndex >= sequenceSteps.Length;
        public bool IsRunning => isRunning;
        
        public event Action OnCompleteEvent;
        public UnityEvent OnComplete;

        private void Awake()
        {
            Initialize();
        }

        internal void Initialize()
        {
            sequenceSteps = GetComponentsInChildren<BaseStep>()
                .OrderBy(seq => seq.transform.GetSiblingIndex())
                .ToArray();

            foreach (var sequence in sequenceSteps)
                sequence.InitStep();
        }

        public void StartSequencer()
        {
            if (!isRunning)
            {
                isRunning = true;
                IsPaused = false;
                StartNextSequence();
            }
        }

        private void Update()
        {
            if (!isRunning || IsPaused || currentIndex >= sequenceSteps.Length)
                return;

            var currentSequence = sequenceSteps[currentIndex];
            currentSequence.UpdateStep();

            if (currentSequence.IsComplete)
                StartNextSequence();
        }

        private void StartNextSequence()
        {
            currentIndex++;
            
            if (currentIndex >= sequenceSteps.Length)
            {
                OnCompleteEvent?.Invoke();
                OnComplete?.Invoke();
                return;
            }

            sequenceSteps[currentIndex].StartStep();
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void Reset()
        {
            isRunning = false;
            IsPaused = false;
            currentIndex = -1;
            
            foreach (var sequence in sequenceSteps)
                sequence.CleanupStep();
        }

        public void SkipCurrentSequence()
        {
            if (currentIndex >= 0 && currentIndex < sequenceSteps.Length)
                StartNextSequence();
        }
    }
}