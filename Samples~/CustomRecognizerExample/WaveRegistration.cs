using UnityEngine;
using GestureInput.Unity;

namespace GestureInput.Samples
{
    /// <summary>
    /// Registration option 1 — plain code. Add this next to a GestureRuntime and
    /// the wave gesture becomes bindable as &lt;GestureDevice&gt;/wave.
    /// Registration must happen before the device is created (the runtime creates
    /// it on its first Update), hence Awake.
    /// </summary>
    [RequireComponent(typeof(GestureRuntime))]
    public sealed class WaveRegistration : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<GestureRuntime>().Registry.Register(new WaveRecognizer());
        }
    }
}
