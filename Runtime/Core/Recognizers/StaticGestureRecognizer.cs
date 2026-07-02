using System.Collections.Generic;

namespace GestureInput.Core
{
    /// <summary>
    /// Surfaces the perception backend's built-in static hand gestures (open palm,
    /// fist, thumbs, victory, pointing, "I love you") through the SPI, adding what
    /// the raw single-frame classification lacks: confidence hysteresis (no
    /// flicker at the threshold) and Began/Updated/Ended phase tracking.
    /// At most one static gesture is active at a time — a classification switch
    /// ends the old gesture before the new one can begin.
    /// </summary>
    public sealed class StaticGestureRecognizer : IGestureRecognizer
    {
        public const string OpenPalm = "openPalm";
        public const string ClosedFist = "closedFist";
        public const string ThumbUp = "thumbUp";
        public const string ThumbDown = "thumbDown";
        public const string Victory = "victory";
        public const string PointingUp = "pointingUp";
        public const string ILoveYou = "iLoveYou";

        private static readonly Dictionary<BuiltinGestureType, string> Ids =
            new Dictionary<BuiltinGestureType, string>
            {
                { BuiltinGestureType.OpenPalm, OpenPalm },
                { BuiltinGestureType.ClosedFist, ClosedFist },
                { BuiltinGestureType.ThumbUp, ThumbUp },
                { BuiltinGestureType.ThumbDown, ThumbDown },
                { BuiltinGestureType.Victory, Victory },
                { BuiltinGestureType.PointingUp, PointingUp },
                { BuiltinGestureType.ILoveYou, ILoveYou }
            };

        private readonly Hysteresis _gate;
        private BuiltinGestureType _active = BuiltinGestureType.None;

        public StaticGestureRecognizer(float enterConfidence = 0.7f, float exitConfidence = 0.5f)
        {
            _gate = new Hysteresis(enterConfidence, exitConfidence);

            var descriptors = new List<GestureDescriptor>(Ids.Count);
            foreach (var id in Ids.Values)
                descriptors.Add(new GestureDescriptor(id, GestureKind.Discrete));
            Descriptors = descriptors;
        }

        public IReadOnlyList<GestureDescriptor> Descriptors { get; }

        public void Reset()
        {
            _active = BuiltinGestureType.None;
            _gate.Reset();
        }

        public void Process(in GestureFrame frame, IGestureSink sink)
        {
            var type = frame.Hand.IsPresent ? frame.Builtin.Type : BuiltinGestureType.None;
            float confidence = frame.Builtin.Confidence;
            long ts = frame.TimestampMs;

            // Classification changed away from the active gesture — end it first.
            if (_active != BuiltinGestureType.None && type != _active)
            {
                sink.Emit(new GestureEvent(Ids[_active], GesturePhase.Ended, 0f, confidence, ts));
                _active = BuiltinGestureType.None;
                _gate.Reset();
            }

            if (type == BuiltinGestureType.None || !Ids.ContainsKey(type))
                return;

            bool wasActive = _active == type;
            bool nowActive = _gate.Update(confidence);

            if (nowActive && !wasActive)
            {
                _active = type;
                sink.Emit(new GestureEvent(Ids[type], GesturePhase.Began, 1f, confidence, ts));
            }
            else if (nowActive)
            {
                sink.Emit(new GestureEvent(Ids[type], GesturePhase.Updated, 1f, confidence, ts));
            }
            else if (wasActive)
            {
                _active = BuiltinGestureType.None;
                sink.Emit(new GestureEvent(Ids[type], GesturePhase.Ended, 0f, confidence, ts));
            }
        }
    }
}
