using System.Collections.Generic;
using System.Linq;
using GestureInput.Core;

namespace GestureInput.Tests
{
    /// <summary>Collecting sink for recognizer tests.</summary>
    public sealed class TestSink : IGestureSink
    {
        public List<GestureEvent> Events { get; } = new List<GestureEvent>();

        public void Emit(in GestureEvent e) => Events.Add(e);

        public bool Contains(string id, GesturePhase phase) =>
            Events.Any(e => e.Id == id && e.Phase == phase);

        public int Count(string id) => Events.Count(e => e.Id == id);

        public void Clear() => Events.Clear();
    }
}
