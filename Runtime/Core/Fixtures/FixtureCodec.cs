using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace GestureInput.Core
{
    /// <summary>
    /// Serializes <see cref="GestureFrame"/> streams to the `.gframes` text format —
    /// the record-and-replay fixture format used by the sample recorder and the
    /// EditMode tests. Line-based, pipe-delimited, culture-invariant, and
    /// dependency-free so it works identically inside Unity and in plain .NET.
    ///
    /// Format (version 1):
    ///   line 0:  "gframes 1"
    ///   frame:   ts|hand(0/1)|handedness|palmX,palmY|builtinType|builtinConf|handLandmarks|pose(0/1)|poseLandmarks
    ///   landmark list: "x,y,z;x,y,z;..." (empty when absent)
    /// </summary>
    public static class FixtureCodec
    {
        private const string Header = "gframes 1";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Write(IEnumerable<GestureFrame> frames)
        {
            var sb = new StringBuilder();
            sb.Append(Header).Append('\n');
            foreach (var f in frames)
            {
                sb.Append(f.TimestampMs.ToString(Inv)).Append('|');
                sb.Append(f.Hand.IsPresent ? '1' : '0').Append('|');
                sb.Append(f.Hand.Handedness).Append('|');
                sb.Append(F(f.Hand.Palm.x)).Append(',').Append(F(f.Hand.Palm.y)).Append('|');
                sb.Append(f.Builtin.Type).Append('|');
                sb.Append(F(f.Builtin.Confidence)).Append('|');
                AppendLandmarks(sb, f.Hand.IsPresent ? f.Hand.Landmarks : null);
                sb.Append('|');
                sb.Append(f.Pose.IsPresent ? '1' : '0').Append('|');
                AppendLandmarks(sb, f.Pose.IsPresent ? f.Pose.Landmarks : null);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public static List<GestureFrame> Read(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new FormatException("Empty fixture.");

            var lines = text.Replace("\r\n", "\n").Split('\n');
            if (lines[0].Trim() != Header)
                throw new FormatException($"Unsupported fixture header '{lines[0]}'. Expected '{Header}'.");

            var frames = new List<GestureFrame>();
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;
                frames.Add(ParseFrame(line, i + 1));
            }
            return frames;
        }

        private static GestureFrame ParseFrame(string line, int lineNumber)
        {
            var parts = line.Split('|');
            if (parts.Length != 9)
                throw new FormatException($"Line {lineNumber}: expected 9 fields, found {parts.Length}.");

            try
            {
                long ts = long.Parse(parts[0], Inv);
                bool handPresent = parts[1] == "1";
                var handedness = (Handedness)Enum.Parse(typeof(Handedness), parts[2]);
                var palmParts = parts[3].Split(',');
                var palm = new Vector2(float.Parse(palmParts[0], Inv), float.Parse(palmParts[1], Inv));
                var builtinType = (BuiltinGestureType)Enum.Parse(typeof(BuiltinGestureType), parts[4]);
                float builtinConf = float.Parse(parts[5], Inv);
                var handLandmarks = ParseLandmarks(parts[6]);
                bool posePresent = parts[7] == "1";
                var poseLandmarks = ParseLandmarks(parts[8]);

                var hand = handPresent ? new HandData(handedness, palm, handLandmarks) : HandData.Absent;
                var pose = posePresent ? new PoseData(poseLandmarks) : PoseData.Absent;
                return new GestureFrame(ts, hand, pose, new BuiltinGesture(builtinType, builtinConf));
            }
            catch (Exception e) when (!(e is FormatException))
            {
                throw new FormatException($"Line {lineNumber}: malformed frame ({e.Message}).");
            }
        }

        private static void AppendLandmarks(StringBuilder sb, IReadOnlyList<Vector3> landmarks)
        {
            if (landmarks == null) return;
            for (int i = 0; i < landmarks.Count; i++)
            {
                if (i > 0) sb.Append(';');
                var p = landmarks[i];
                sb.Append(F(p.x)).Append(',').Append(F(p.y)).Append(',').Append(F(p.z));
            }
        }

        private static Vector3[] ParseLandmarks(string field)
        {
            if (string.IsNullOrEmpty(field)) return null;
            var points = field.Split(';');
            var result = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var c = points[i].Split(',');
                result[i] = new Vector3(float.Parse(c[0], Inv), float.Parse(c[1], Inv), float.Parse(c[2], Inv));
            }
            return result;
        }

        private static string F(float value) => value.ToString("R", Inv);
    }
}
