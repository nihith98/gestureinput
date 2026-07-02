using UnityEngine;

namespace GestureInput.Core
{
    /// <summary>
    /// A single gesture occurrence emitted by a recognizer: which gesture,
    /// what lifecycle phase, and its scalar and/or 2D value.
    /// A small value type so events can be passed and queued without allocation.
    /// </summary>
    public readonly struct GestureEvent
    {
        /// <summary>Descriptor id of the gesture this event belongs to.</summary>
        public string Id { get; }

        public GesturePhase Phase { get; }

        /// <summary>Scalar value (button/axis). For 2D events this is the magnitude of <see cref="Value2"/>.</summary>
        public float Value { get; }

        /// <summary>2D value for <see cref="GestureKind.Continuous2D"/> gestures; zero otherwise.</summary>
        public Vector2 Value2 { get; }

        /// <summary>Recognizer confidence in [0, 1].</summary>
        public float Confidence { get; }

        /// <summary>Timestamp of the perception frame that produced this event (ms).</summary>
        public long TimestampMs { get; }

        public GestureEvent(string id, GesturePhase phase, float value, float confidence = 1f, long timestampMs = 0)
        {
            Id = id;
            Phase = phase;
            Value = value;
            Value2 = Vector2.zero;
            Confidence = confidence;
            TimestampMs = timestampMs;
        }

        public GestureEvent(string id, GesturePhase phase, Vector2 value2, float confidence = 1f, long timestampMs = 0)
        {
            Id = id;
            Phase = phase;
            Value = value2.magnitude;
            Value2 = value2;
            Confidence = confidence;
            TimestampMs = timestampMs;
        }

        public override string ToString() => $"{Id} {Phase} v={Value:F2} v2={Value2} c={Confidence:F2} t={TimestampMs}";
    }
}
