using UnityEngine;
using System.Collections;
using System;

namespace Nexus.Sequencers
{
    public abstract class CoroutineStep : BaseStep
    {
        private Coroutine currentCoroutine;
        private Coroutine timeoutCoroutine;
        private bool isRoutineRunning;

        [Tooltip("Optional timeout duration in seconds. Set to 0 or negative for no timeout.")]
        public float timeoutDuration = -1f;

        [Tooltip("Whether to automatically complete the step when the coroutine finishes.")]
        public bool autoCompleteOnFinish = true;

        public event Action OnTimeout;

        protected abstract IEnumerator StepRoutine();

        public override void StartStep()
        {
            base.StartStep();
            StartStepRoutine();
        }

        public override void UpdateStep()
        {
            // Optional - override if you need per-frame updates in addition to coroutine
        }

        protected virtual void StartStepRoutine()
        {
            if (isRoutineRunning)
            {
                StopStepRoutine();
            }

            isRoutineRunning = true;
            currentCoroutine = StartCoroutine(RunMainRoutine());

            if (timeoutDuration > 0)
            {
                timeoutCoroutine = StartCoroutine(TimeoutRoutine());
            }
        }

        protected virtual void StopStepRoutine()
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
                currentCoroutine = null;
            }

            if (timeoutCoroutine != null)
            {
                StopCoroutine(timeoutCoroutine);
                timeoutCoroutine = null;
            }

            isRoutineRunning = false;
        }

        private IEnumerator RunMainRoutine()
        {
            bool hadError = false;
            
            IEnumerator routine = StepRoutine();
            while (true)
            {
                try
                {
                    if (!routine.MoveNext())
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in {GetType().Name} coroutine: {e}");
                    hadError = true;
                    HandleStepError(e);
                    break;
                }

                yield return routine.Current;
            }

            if (!hadError && autoCompleteOnFinish && !isComplete)
            {
                Complete();
                Finish();
            }

            isRoutineRunning = false;
            currentCoroutine = null;
        }

        private IEnumerator TimeoutRoutine()
        {
            yield return new WaitForSeconds(timeoutDuration);
            
            if (isRoutineRunning)
            {
                OnTimeout?.Invoke();
                HandleTimeout();
            }
            
            timeoutCoroutine = null;
        }

        protected virtual void HandleTimeout()
        {
            Debug.LogWarning($"{GetType().Name} timed out after {timeoutDuration} seconds");
            StopStepRoutine();
            Complete();
            Finish();
        }

        protected virtual void HandleStepError(Exception error)
        {
            StopStepRoutine();
            Complete();
            Finish();
        }

        public override void CleanupStep()
        {
            StopStepRoutine();
            base.CleanupStep();
        }

        private void OnDisable()
        {
            StopStepRoutine();
        }
    }
}