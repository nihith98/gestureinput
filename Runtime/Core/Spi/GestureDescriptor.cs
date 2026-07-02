using System;

namespace GestureInput.Core
{
    /// <summary>
    /// Static metadata a recognizer publishes for each gesture it can emit.
    /// The <see cref="Id"/> becomes the Input System control name
    /// (<c>&lt;GestureDevice&gt;/{Id}</c>), so it must be a stable, control-name-safe
    /// identifier: ASCII letters and digits, starting with a letter.
    /// </summary>
    public readonly struct GestureDescriptor : IEquatable<GestureDescriptor>
    {
        public string Id { get; }
        public GestureKind Kind { get; }

        public GestureDescriptor(string id, GestureKind kind)
        {
            if (!IsValidId(id))
                throw new ArgumentException(
                    $"Gesture id '{id}' is invalid. Ids must be non-empty, contain only ASCII letters and digits, and start with a letter.",
                    nameof(id));
            Id = id;
            Kind = kind;
        }

        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            char first = id[0];
            if (!(first >= 'a' && first <= 'z') && !(first >= 'A' && first <= 'Z')) return false;
            for (int i = 1; i < id.Length; i++)
            {
                char c = id[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
                if (!ok) return false;
            }
            return true;
        }

        public bool Equals(GestureDescriptor other) => Id == other.Id && Kind == other.Kind;
        public override bool Equals(object obj) => obj is GestureDescriptor d && Equals(d);
        public override int GetHashCode() => (Id != null ? Id.GetHashCode() : 0) * 397 ^ (int)Kind;
        public override string ToString() => $"{Id} ({Kind})";
    }
}
