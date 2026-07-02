using System;
using System.Collections.Generic;

namespace GestureInput.Core
{
    /// <summary>
    /// Holds the active set of recognizers and runs them over each frame.
    /// Descriptor ids must be unique across all registered recognizers because
    /// they become Input System control names. A recognizer that throws during
    /// <see cref="ProcessFrame"/> is isolated (reported via
    /// <see cref="OnRecognizerError"/>) so one bad extension cannot take down
    /// the pipeline. Main-thread only.
    /// </summary>
    public sealed class GestureRegistry
    {
        private readonly List<IGestureRecognizer> _recognizers = new List<IGestureRecognizer>();
        private readonly List<GestureDescriptor> _descriptors = new List<GestureDescriptor>();
        private readonly HashSet<string> _ids = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Raised when a recognizer throws inside Process; processing continues.</summary>
        public event Action<IGestureRecognizer, Exception> OnRecognizerError;

        public IReadOnlyList<IGestureRecognizer> Recognizers => _recognizers;

        /// <summary>Union of all registered descriptors, in registration order.</summary>
        public IReadOnlyList<GestureDescriptor> AllDescriptors => _descriptors;

        public void Register(IGestureRecognizer recognizer)
        {
            if (recognizer == null) throw new ArgumentNullException(nameof(recognizer));

            var descriptors = recognizer.Descriptors;
            if (descriptors == null || descriptors.Count == 0)
                throw new ArgumentException("Recognizer declares no descriptors.", nameof(recognizer));

            foreach (var d in descriptors)
            {
                if (_ids.Contains(d.Id))
                    throw new ArgumentException(
                        $"Gesture id '{d.Id}' is already registered by another recognizer. Ids must be unique.",
                        nameof(recognizer));
            }

            _recognizers.Add(recognizer);
            foreach (var d in descriptors)
            {
                _ids.Add(d.Id);
                _descriptors.Add(d);
            }
        }

        public bool Remove(IGestureRecognizer recognizer)
        {
            if (recognizer == null || !_recognizers.Remove(recognizer)) return false;
            foreach (var d in recognizer.Descriptors)
            {
                _ids.Remove(d.Id);
                _descriptors.RemoveAll(x => x.Id == d.Id);
            }
            return true;
        }

        public void Clear()
        {
            _recognizers.Clear();
            _descriptors.Clear();
            _ids.Clear();
        }

        /// <summary>Run every recognizer over <paramref name="frame"/>, emitting into <paramref name="sink"/>.</summary>
        public void ProcessFrame(in GestureFrame frame, IGestureSink sink)
        {
            for (int i = 0; i < _recognizers.Count; i++)
            {
                var recognizer = _recognizers[i];
                try
                {
                    recognizer.Process(in frame, sink);
                }
                catch (Exception e)
                {
                    OnRecognizerError?.Invoke(recognizer, e);
                }
            }
        }

        public void ResetAll()
        {
            for (int i = 0; i < _recognizers.Count; i++)
                _recognizers[i].Reset();
        }
    }
}
