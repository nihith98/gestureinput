using UnityEditor;
using UnityEngine;

namespace GestureInput.Editor
{
    /// <summary>
    /// Fails fast (with instructions) when the MediaPipe Unity Plugin is missing.
    /// The plugin ships via GitHub Releases, not a package registry, so UPM cannot
    /// auto-install it as a dependency (SCOPE §4.4) — this check is the guard rail.
    /// </summary>
    internal static class MediapipeDependencyCheck
    {
        [InitializeOnLoadMethod]
        private static void CheckForMediapipePlugin()
        {
#if !GESTUREINPUT_HAS_MEDIAPIPE
            Debug.LogWarning(
                "[GestureInput] The MediaPipe Unity Plugin (com.github.homuler.mediapipe) is not installed, " +
                "so live webcam gesture recognition is disabled. Core recognizers, tests, and mock-driven " +
                "PlayMode work are unaffected.\n" +
                "Install it from https://github.com/homuler/MediaPipeUnityPlugin/releases (tested release is " +
                "pinned in the GestureInput README), then restart the editor.");
#endif
        }
    }
}
