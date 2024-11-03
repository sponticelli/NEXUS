using System;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Events;

namespace Nexus.Sequencers
{
    /// <summary>
    /// Configuration class to define how text should be displayed
    /// </summary>
    [System.Serializable]
    public class TextDisplayConfig
    {
        [Header("Text Settings")]
        [Tooltip("The message to display. Leave empty to keep current text")]
        public string message;

        [Tooltip("Font size. Set to 0 to keep current size")]
        public float fontSize;

        [Tooltip("Text color. Alpha will be animated")]
        public Color color = Color.white;

        [Header("Timing")]
        [Tooltip("Time to fade in")]
        public float fadeInDuration = 0.5f;

        [Tooltip("Time to hold the text visible")]
        public float holdDuration = 2f;

        [Tooltip("Time to fade out")]
        public float fadeOutDuration = 0.5f;

        [Header("Effects")]
        [Tooltip("Easing curve for fade in")]
        public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("Easing curve for fade out")]
        public AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Tooltip("Scale multiplier during display")]
        public float scaleMultiplier = 1f;

        [Tooltip("Scale easing curve")]
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.8f, 1, 1f);

        [Header("Character Effects")]
        [Tooltip("Fade in each character separately")]
        public bool useCharacterFading;

        [Tooltip("Delay between each character fade")]
        public float characterFadeDelay = 0.05f;
    }

    /// <summary>
    /// Step that handles fading text with various effects
    /// </summary>
    public class TextFadeStep : CoroutineStep
    {
        [Header("Target")]
        [SerializeField] private TMP_Text targetText;

        [Header("Display Settings")]
        [SerializeField] private TextDisplayConfig config = new TextDisplayConfig();

        [Header("Events")]
        public UnityEvent onFadeInComplete;
        public UnityEvent onFadeOutComplete;

        private Vector3 originalScale;
        private float originalFontSize;
        private string originalText;
        private Color originalColor;

        #region Public Properties
        public TMP_Text TargetText
        {
            get => targetText;
            set => targetText = value;
        }

        public TextDisplayConfig Config
        {
            get => config;
            set => config = value;
        }
        #endregion

        private void Awake()
        {
            if (targetText != null)
            {
                StoreOriginalValues();
            }
        }

        private void StoreOriginalValues()
        {
            originalScale = targetText.transform.localScale;
            originalFontSize = targetText.fontSize;
            originalText = targetText.text;
            originalColor = targetText.color;
        }

        private void RestoreOriginalValues()
        {
            if (targetText == null) return;

            targetText.transform.localScale = originalScale;
            targetText.fontSize = originalFontSize;
            targetText.text = originalText;
            targetText.color = originalColor;
        }

        protected override void HandleStepError(Exception error)
        {
            RestoreOriginalValues();
            base.HandleStepError(error);
        }

        public override void CleanupStep()
        {
            RestoreOriginalValues();
            base.CleanupStep();
        }

        protected override IEnumerator StepRoutine()
        {
            if (targetText == null)
                throw new InvalidOperationException("No target text component assigned!");

            // Store original values if not already stored
            StoreOriginalValues();

            // Set initial state
            SetupInitialState();

            // Fade In
            yield return StartCoroutine(FadeIn());
            onFadeInComplete?.Invoke();

            // Hold
            yield return new WaitForSeconds(config.holdDuration);

            // Fade Out
            yield return StartCoroutine(FadeOut());
            onFadeOutComplete?.Invoke();
        }

        private void SetupInitialState()
        {
            if (!string.IsNullOrEmpty(config.message))
            {
                targetText.text = config.message;
            }

            if (config.fontSize > 0)
            {
                targetText.fontSize = config.fontSize;
            }

            // Set initial color with 0 alpha
            Color startColor = config.color;
            startColor.a = 0f;
            targetText.color = startColor;

            // Set initial scale
            if (config.scaleMultiplier != 1f)
            {
                targetText.transform.localScale = originalScale * (config.scaleCurve.Evaluate(0) * config.scaleMultiplier);
            }
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;

            if (config.useCharacterFading)
            {
                yield return CharacterFade(true);
            }
            else
            {
                while (elapsed < config.fadeInDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / config.fadeInDuration;
                    
                    // Fade color
                    Color currentColor = config.color;
                    currentColor.a = config.fadeInCurve.Evaluate(t);
                    targetText.color = currentColor;

                    // Scale effect
                    if (config.scaleMultiplier != 1f)
                    {
                        float scale = config.scaleCurve.Evaluate(t) * config.scaleMultiplier;
                        targetText.transform.localScale = originalScale * scale;
                    }

                    yield return null;
                }
            }

            // Ensure we reach final state
            Color finalColor = config.color;
            finalColor.a = 1f;
            targetText.color = finalColor;
            targetText.transform.localScale = originalScale * config.scaleMultiplier;
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;

            if (config.useCharacterFading)
            {
                yield return CharacterFade(false);
            }
            else
            {
                while (elapsed < config.fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / config.fadeOutDuration;

                    // Fade color
                    Color currentColor = config.color;
                    currentColor.a = config.fadeOutCurve.Evaluate(t);
                    targetText.color = currentColor;

                    // Scale effect
                    if (config.scaleMultiplier != 1f)
                    {
                        float scale = config.scaleCurve.Evaluate(1 - t) * config.scaleMultiplier;
                        targetText.transform.localScale = originalScale * scale;
                    }

                    yield return null;
                }
            }

            // Ensure we reach final state
            Color finalColor = config.color;
            finalColor.a = 0f;
            targetText.color = finalColor;
            targetText.transform.localScale = originalScale;
        }

        private IEnumerator CharacterFade(bool fadeIn)
        {
            // Enable per-character transparency
            targetText.enableVertexGradient = true;
            
            int characterCount = targetText.textInfo.characterCount;
            float[] characterAlphas = new float[characterCount];
            
            float duration = fadeIn ? config.fadeInDuration : config.fadeOutDuration;
            AnimationCurve fadeCurve = fadeIn ? config.fadeInCurve : config.fadeOutCurve;

            for (int i = 0; i < characterCount; i++)
            {
                float startTime = Time.time + (i * config.characterFadeDelay);
                int charIndex = fadeIn ? i : (characterCount - 1 - i);
                
                while (Time.time < startTime + duration)
                {
                    float t = (Time.time - startTime) / duration;
                    characterAlphas[charIndex] = fadeCurve.Evaluate(t);
                    
                    // Update mesh colors
                    targetText.ForceMeshUpdate();
                    var meshInfo = targetText.textInfo.meshInfo[0];
                    
                    for (int j = 0; j < characterCount; j++)
                    {
                        var charInfo = targetText.textInfo.characterInfo[j];
                        if (!charInfo.isVisible) continue;

                        var vertexIndex = charInfo.vertexIndex;
                        Color32 c = config.color;
                        c.a = (byte)(255 * characterAlphas[j]);

                        meshInfo.colors32[vertexIndex] = c;
                        meshInfo.colors32[vertexIndex + 1] = c;
                        meshInfo.colors32[vertexIndex + 2] = c;
                        meshInfo.colors32[vertexIndex + 3] = c;
                    }

                    targetText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                    yield return null;
                }
            }
        }
    }
}