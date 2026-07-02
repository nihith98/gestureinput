using UnityEngine;
using GestureInput.Core;
using GestureInput.Unity;

namespace GestureInput.Samples.LiveDemo
{
    /// <summary>
    /// Draws the 21-point hand skeleton over the screen with GL lines,
    /// mirroring what the recognizers actually see (normalized landmarks),
    /// which makes perception vs. recognition problems easy to tell apart.
    /// </summary>
    [RequireComponent(typeof(GestureRuntime))]
    public sealed class LandmarkOverlay : MonoBehaviour
    {
        // MediaPipe hand topology: bone pairs between landmark indices.
        private static readonly int[,] Bones =
        {
            {0,1},{1,2},{2,3},{3,4},        // thumb
            {0,5},{5,6},{6,7},{7,8},        // index
            {5,9},{9,10},{10,11},{11,12},   // middle
            {9,13},{13,14},{14,15},{15,16}, // ring
            {13,17},{17,18},{18,19},{19,20},// pinky
            {0,17}                          // palm edge
        };

        private GestureRuntime _runtime;
        private GestureFrame _latest;
        private Material _lineMaterial;

        private void Awake()
        {
            _runtime = GetComponent<GestureRuntime>();
            _runtime.OnFrame += OnFrame;

            _lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void OnFrame(GestureFrame frame) => _latest = frame;

        private void OnRenderObject()
        {
            if (!_latest.Hand.IsPresent || _latest.Hand.Landmarks == null) return;

            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.LINES);
            GL.Color(Color.green);

            var pts = _latest.Hand.Landmarks;
            for (int i = 0; i < Bones.GetLength(0); i++)
            {
                if (Bones[i, 0] >= pts.Count || Bones[i, 1] >= pts.Count) continue;
                var a = pts[Bones[i, 0]];
                var b = pts[Bones[i, 1]];
                // normalized image space (origin top-left) -> GL ortho (origin bottom-left)
                GL.Vertex3(a.x, 1f - a.y, 0f);
                GL.Vertex3(b.x, 1f - b.y, 0f);
            }

            GL.End();
            GL.PopMatrix();
        }

        private void OnDestroy()
        {
            if (_runtime != null) _runtime.OnFrame -= OnFrame;
            if (_lineMaterial != null) DestroyImmediate(_lineMaterial);
        }
    }
}
