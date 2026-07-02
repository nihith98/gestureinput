using UnityEngine;
using GestureInput.Core;
using GestureInput.Unity.Registration;

namespace GestureInput.Samples
{
    /// <summary>
    /// Registration option 2 — ScriptableObject asset. Create one via
    /// Assets ▸ Create ▸ GestureInput ▸ Wave Recognizer, tune the thresholds in
    /// the Inspector, and add it to GestureRuntime's "Recognizer Assets" list.
    /// </summary>
    [CreateAssetMenu(menuName = "GestureInput/Wave Recognizer", fileName = "WaveRecognizer")]
    public sealed class WaveRecognizerAsset : GestureRecognizerAsset
    {
        [SerializeField, Tooltip("Sliding window length in frames (~0.5s at 30fps).")]
        private int windowSamples = 30;

        [SerializeField, Tooltip("Direction reversals required inside the window.")]
        private int minReversals = 4;

        [SerializeField, Tooltip("Minimum horizontal movement (normalized) counted as motion.")]
        private float minDelta = 0.03f;

        [SerializeField, Tooltip("Refractory period between wave events (ms).")]
        private long cooldownMs = 600;

        public override IGestureRecognizer CreateRecognizer() =>
            new WaveRecognizer(windowSamples, minReversals, minDelta, cooldownMs);
    }
}
