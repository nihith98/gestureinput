using System.Collections.Generic;
using UnityEngine;

namespace GestureInput.Core
{
    /// <summary>
    /// Backend-agnostic body-pose snapshot for one frame (normalized image space).
    /// Optional — frames from a hands-only pipeline carry <see cref="Absent"/>.
    /// </summary>
    public readonly struct PoseData
    {
        public bool IsPresent { get; }

        /// <summary>The 33 pose landmarks, or null when no pose is available.</summary>
        public IReadOnlyList<Vector3> Landmarks { get; }

        public PoseData(IReadOnlyList<Vector3> landmarks)
        {
            IsPresent = true;
            Landmarks = landmarks;
        }

        public static PoseData Absent => default;
    }
}
