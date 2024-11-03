using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Nexus.Sequencers
{
    public class Sequence : MonoBehaviour
    {
        [SerializeField] private bool _resetContextOnStart = true;
        
        private BaseStep[] sequenceSteps;
        private int currentIndex = -1;
        private bool isRunning;
        public bool IsPaused { get; private set; }
        public bool IsComplete => currentIndex >= sequenceSteps.Length;
        public bool IsRunning => isRunning;
        
        public event Action OnCompleteEvent;
        public UnityEvent OnComplete;
        
        private StepContext context = new();

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
                if (_resetContextOnStart)
                    context = new StepContext();
                StartNextStep();
            }
        }

        private void Update()
        {
            if (!isRunning || IsPaused || currentIndex >= sequenceSteps.Length)
                return;

            var currentSequence = sequenceSteps[currentIndex];
            currentSequence.UpdateStep();

            if (currentSequence.IsComplete)
                StartNextStep();
        }

        private void StartNextStep()
        {
            currentIndex++;
            
            if (currentIndex >= sequenceSteps.Length)
            {
                OnCompleteEvent?.Invoke();
                OnComplete?.Invoke();
                return;
            }

            var nextStep = sequenceSteps[currentIndex];
            var stepWithContext = nextStep as IStepWithContext;
            stepWithContext?.SetContext(context);
            nextStep.StartStep();
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
                StartNextStep();
        }
    }
}