using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Nexus.Sequences
{
    public class Sequence : MonoBehaviour
    {
        [SerializeField] private bool _resetContextOnStart = true;
        [SerializeField] private bool _debugMode = true;
        
        private BaseStep[] sequenceSteps;
        private int currentIndex = -1;
        private bool isRunning;
        public bool IsPaused { get; private set; }
        public bool IsComplete => currentIndex >= sequenceSteps.Length;
        public bool IsRunning => isRunning;
        
        public event Action OnCompleteEvent;
        public UnityEvent OnComplete;
        
        private StepContext context = new();
        
        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        internal void Initialize()
        {
            if (_isInitialized)
                return;
            
            _isInitialized = true;
            
            sequenceSteps = GetComponentsInChildren<BaseStep>()
                .OrderBy(seq => seq.transform.GetSiblingIndex())
                .ToArray();
            
            if (_debugMode)
                Debug.Log($"Sequence {this.name}: initialize with {sequenceSteps.Length} steps");

            foreach (var sequence in sequenceSteps)
            {
                sequence.InitStep();
            }
        }

        public void StartSequencer()
        {
            Initialize();
            if (_debugMode)
            {
                Debug.Log($"Sequence {name}: starting sequencer");
            }
            if (!isRunning)
            {
                if (_debugMode)
                {
                    Debug.Log($"Sequence {name}: started");
                }
                isRunning = true;
                IsPaused = false;
                if (_resetContextOnStart)
                    context = new StepContext();
                StartNextStep();
            }
            else if (_debugMode)
            {
                Debug.LogWarning($"Sequence {name}: already running");
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
                if (_debugMode)
                {
                    Debug.Log($"Sequence {name}: completed");
                }
                OnCompleteEvent?.Invoke();
                OnComplete?.Invoke();
                return;
            }

            var nextStep = sequenceSteps[currentIndex];
            if (_debugMode)
            {
                Debug.Log($"Sequence {name}: starting step {nextStep.name}");
            }
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