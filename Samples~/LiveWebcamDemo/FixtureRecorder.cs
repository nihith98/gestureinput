using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GestureInput.Core;
using GestureInput.Unity;

namespace GestureInput.Samples.LiveDemo
{
    /// <summary>
    /// Records the live GestureFrame stream to a `.gframes` fixture file —
    /// the record-and-replay loop (SCOPE §7.3): perform a gesture on camera,
    /// press Record twice, and the saved file becomes a deterministic EditMode
    /// test input via <see cref="FixtureCodec"/>.
    /// Files land in Application.persistentDataPath.
    /// </summary>
    [RequireComponent(typeof(GestureRuntime))]
    public sealed class FixtureRecorder : MonoBehaviour
    {
        private readonly List<GestureFrame> _frames = new List<GestureFrame>();
        private GestureRuntime _runtime;
        private bool _recording;
        private string _lastSavedPath;

        private void Awake()
        {
            _runtime = GetComponent<GestureRuntime>();
            _runtime.OnFrame += OnFrame;
        }

        private void OnDestroy()
        {
            if (_runtime != null) _runtime.OnFrame -= OnFrame;
        }

        private void OnFrame(GestureFrame frame)
        {
            if (_recording) _frames.Add(frame);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 210, 10, 200, 110), GUI.skin.box);

            if (!_recording)
            {
                if (GUILayout.Button("● Record fixture")) StartRecording();
            }
            else
            {
                GUILayout.Label($"recording… {_frames.Count} frames");
                if (GUILayout.Button("■ Stop & save")) StopAndSave();
            }

            if (_lastSavedPath != null) GUILayout.Label($"saved:\n{_lastSavedPath}");
            GUILayout.EndArea();
        }

        private void StartRecording()
        {
            _frames.Clear();
            _recording = true;
        }

        private void StopAndSave()
        {
            _recording = false;
            var path = Path.Combine(Application.persistentDataPath,
                $"gesture_{System.DateTime.Now:yyyyMMdd_HHmmss}.gframes");
            File.WriteAllText(path, FixtureCodec.Write(_frames));
            _lastSavedPath = path;
            Debug.Log($"[GestureInput] Fixture saved: {path} ({_frames.Count} frames)");
        }
    }
}
