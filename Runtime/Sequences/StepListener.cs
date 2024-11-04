using UnityEngine;
using UnityEngine.Events;

namespace Nexus.Sequences
{
    public class StepListener : MonoBehaviour, IStepListener
    {
        [SerializeField] private UnityEvent _onStepComplete;
        [SerializeField] private UnityEvent _onStepFinished;
        
        
        public void OnStepComplete()
        {
            _onStepComplete.Invoke();
        }

        public void OnStepFinished()
        {
            _onStepFinished.Invoke();
        }
    }
}