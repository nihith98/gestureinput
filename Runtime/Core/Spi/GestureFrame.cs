namespace GestureInput.Core
{
    /// <summary>
    /// The per-tick, backend-agnostic perception snapshot every recognizer reads.
    /// Passed as <c>in GestureFrame</c> so many recognizers can share it cheaply
    /// and none can mutate it.
    /// </summary>
    public readonly struct GestureFrame
    {
        /// <summary>Monotonic frame timestamp in milliseconds.</summary>
        public long TimestampMs { get; }

        public HandData Hand { get; }
        public PoseData Pose { get; }

        /// <summary>The backend's built-in static-gesture classification for this frame.</summary>
        public BuiltinGesture Builtin { get; }

        public GestureFrame(long timestampMs, HandData hand, PoseData pose, BuiltinGesture builtin)
        {
            TimestampMs = timestampMs;
            Hand = hand;
            Pose = pose;
            Builtin = builtin;
        }
    }
}
