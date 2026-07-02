namespace GestureInput.Core
{
    /// <summary>The static gesture classes MediaPipe's canned gesture model can report.</summary>
    public enum BuiltinGestureType
    {
        None,
        OpenPalm,
        ClosedFist,
        ThumbUp,
        ThumbDown,
        Victory,
        PointingUp,
        ILoveYou
    }

    /// <summary>
    /// The perception backend's single-frame static-gesture classification,
    /// normalized into our own type so recognizers never see backend types.
    /// </summary>
    public readonly struct BuiltinGesture
    {
        public BuiltinGestureType Type { get; }
        public float Confidence { get; }

        public BuiltinGesture(BuiltinGestureType type, float confidence)
        {
            Type = type;
            Confidence = confidence;
        }

        public static BuiltinGesture None => default;
    }
}
