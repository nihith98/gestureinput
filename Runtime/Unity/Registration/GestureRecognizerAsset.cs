using UnityEngine;
using GestureInput.Core;

namespace GestureInput.Unity.Registration
{
    /// <summary>
    /// ScriptableObject-based recognizer registration: subclass this, expose your
    /// thresholds as serialized fields so designers can tune them in the Inspector,
    /// create asset instances, and add them to GestureRuntime's recognizer list.
    ///
    /// <code>
    /// [CreateAssetMenu(menuName = "GestureInput/Wave Recognizer")]
    /// public class WaveRecognizerAsset : GestureRecognizerAsset
    /// {
    ///     [SerializeField] private int windowSamples = 30;
    ///     [SerializeField] private int minReversals = 4;
    ///     public override IGestureRecognizer CreateRecognizer() =>
    ///         new WaveRecognizer(windowSamples, minReversals);
    /// }
    /// </code>
    /// </summary>
    public abstract class GestureRecognizerAsset : ScriptableObject
    {
        /// <summary>Build the recognizer instance this asset configures.</summary>
        public abstract IGestureRecognizer CreateRecognizer();
    }
}
