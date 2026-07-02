using UnityEngine;
using GestureInput.Unity;

namespace GestureInput.Samples.LiveDemo
{
    /// <summary>
    /// One-component scene setup: drop this on an empty GameObject in a new scene
    /// and press Play. It adds the GestureRuntime, the MediaPipe driver (when the
    /// plugin is installed), the landmark overlay, the debug HUD, and the fixture
    /// recorder.
    /// </summary>
    public sealed class DemoBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var runtime = gameObject.AddComponent<GestureRuntime>();
            gameObject.AddComponent<GestureInput.Mediapipe.MediapipeGestureDriver>();
            gameObject.AddComponent<GestureDebugHud>();
            gameObject.AddComponent<LandmarkOverlay>();
            gameObject.AddComponent<FixtureRecorder>();
            _ = runtime;
        }
    }
}
