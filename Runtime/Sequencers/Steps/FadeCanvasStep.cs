using System;
using System.Collections;
using UnityEngine;

namespace Nexus.Sequencers
{
    public class FadeCanvasStep : CoroutineStep
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float targetAlpha = 1f;
        [SerializeField] private float duration = 1f;

        protected override IEnumerator StepRoutine()
        {
            if (canvasGroup == null)
                throw new InvalidOperationException("CanvasGroup not assigned!");

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
            
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        protected override void HandleStepError(Exception error)
        {
            Debug.LogError($"Fade step failed: {error.Message}");
            base.HandleStepError(error);
        }
    }
}