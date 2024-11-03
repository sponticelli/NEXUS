using UnityEngine;

namespace Nexus.Sequences
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
}