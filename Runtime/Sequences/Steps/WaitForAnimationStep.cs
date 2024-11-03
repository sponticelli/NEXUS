using System.Collections;
using UnityEngine;

namespace Nexus.Sequences
{
    public class WaitForAnimationStep : CoroutineStep
    {
        public Animator animator;
        public string stateName;
        public int layer = 0;
    
        protected override IEnumerator StepRoutine()
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
            {
                Debug.LogError("Animator or state name not set");
                
                yield break;
            }

            // Play the animation
            animator.Play(stateName, layer);
        
            // Wait for current state info to be valid
            yield return null;

            // Wait until the animation is complete
            while (animator.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1f ||
                   !animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
            {
                yield return null;
            }
        }
    }
}