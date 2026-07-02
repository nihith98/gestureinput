using System.Collections.Generic;
using GestureInput.Core;

namespace GestureInput.Samples
{
    /// <summary>
    /// A complete third-party gesture recognizer: detects a hand "wave" —
    /// several horizontal direction reversals within a short window.
    ///
    /// This is the reference for extending GestureInput. Note what it needs:
    /// only the SPI types and the toolkit from GestureInput.Core. No Unity
    /// scenes, no Input System, no camera, no MediaPipe — which is also why it
    /// can be fully unit-tested (see WaveRecognizerTests and its conformance
    /// subclass in Tests/EditMode).
    /// </summary>
    public sealed class WaveRecognizer : IGestureRecognizer
    {
        public const string Wave = "wave";

        private readonly RingBuffer<float> _xs;
        private readonly Cooldown _cooldown;
        private readonly int _minReversals;
        private readonly float _minDelta;

        /// <param name="windowSamples">Sliding window length in frames (~0.5 s at 30 fps).</param>
        /// <param name="minReversals">Direction reversals required inside the window.</param>
        /// <param name="minDelta">Minimum horizontal movement (normalized) counted as motion, filters jitter.</param>
        /// <param name="cooldownMs">Refractory period between wave events.</param>
        public WaveRecognizer(int windowSamples = 30, int minReversals = 4, float minDelta = 0.03f, long cooldownMs = 600)
        {
            _xs = new RingBuffer<float>(windowSamples);
            _cooldown = new Cooldown(cooldownMs);
            _minReversals = minReversals;
            _minDelta = minDelta;

            Descriptors = new[] { new GestureDescriptor(Wave, GestureKind.Discrete) };
        }

        public IReadOnlyList<GestureDescriptor> Descriptors { get; }

        public void Reset()
        {
            _xs.Clear();
            _cooldown.Reset();
        }

        public void Process(in GestureFrame frame, IGestureSink sink)
        {
            if (!frame.Hand.IsPresent)
            {
                _xs.Clear();
                return;
            }

            _xs.Add(frame.Hand.Palm.x);

            // a wave = several direction reversals in x within the window
            if (MotionMath.CountReversals(_xs, _minDelta) >= _minReversals && _cooldown.Ready(frame.TimestampMs))
            {
                _cooldown.Trigger(frame.TimestampMs);
                _xs.Clear();
                sink.Emit(new GestureEvent(Wave, GesturePhase.Began, 1f, timestampMs: frame.TimestampMs));
            }
        }
    }
}
