using System.Collections.Generic;
using UnityEngine;

namespace GestureInput.Core
{
    public enum Handedness
    {
        Unknown,
        Left,
        Right
    }

    /// <summary>
    /// Backend-agnostic hand snapshot for one frame. Coordinates are normalized
    /// image space: x and y in [0, 1] with the origin at the top-left, matching
    /// MediaPipe's convention; z is relative depth.
    /// </summary>
    public readonly struct HandData
    {
        public bool IsPresent { get; }
        public Handedness Handedness { get; }

        /// <summary>Palm reference point (normalized), e.g. the wrist landmark or MCP centroid.</summary>
        public Vector2 Palm { get; }

        /// <summary>The 21 hand landmarks, or null when no hand is present.</summary>
        public IReadOnlyList<Vector3> Landmarks { get; }

        public HandData(Handedness handedness, Vector2 palm, IReadOnlyList<Vector3> landmarks)
        {
            IsPresent = true;
            Handedness = handedness;
            Palm = palm;
            Landmarks = landmarks;
        }

        public static HandData Absent => default;
    }
}
