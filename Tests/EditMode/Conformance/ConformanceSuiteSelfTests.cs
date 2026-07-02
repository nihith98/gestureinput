using System.Collections.Generic;
using NUnit.Framework;
using GestureInput.Core;

namespace GestureInput.Tests
{
    /// <summary>
    /// Meta-tests: prove the conformance suite actually catches contract
    /// violations, using deliberately broken recognizers.
    /// </summary>
    [TestFixture]
    public class ConformanceSuiteSelfTests
    {
        private sealed class UndeclaredEmitter : IGestureRecognizer
        {
            public IReadOnlyList<GestureDescriptor> Descriptors { get; } =
                new[] { new GestureDescriptor("declared", GestureKind.Discrete) };

            public void Reset() { }

            public void Process(in GestureFrame frame, IGestureSink sink) =>
                sink.Emit(new GestureEvent("undeclared", GesturePhase.Began, 1f));
        }

        private sealed class StatefulNoReset : IGestureRecognizer
        {
            private int _frames;

            public IReadOnlyList<GestureDescriptor> Descriptors { get; } =
                new[] { new GestureDescriptor("sticky", GestureKind.Discrete) };

            public void Reset() { /* deliberately forgets to clear _frames */ }

            public void Process(in GestureFrame frame, IGestureSink sink)
            {
                // fires exactly once in the instance's lifetime => replay after Reset differs
                _frames++;
                if (_frames == 10)
                    sink.Emit(new GestureEvent("sticky", GesturePhase.Began, 1f, timestampMs: frame.TimestampMs));
            }
        }

        private sealed class Compliant : IGestureRecognizer
        {
            public IReadOnlyList<GestureDescriptor> Descriptors { get; } =
                new[] { new GestureDescriptor("compliant", GestureKind.Discrete) };

            public void Reset() { }
            public void Process(in GestureFrame frame, IGestureSink sink) { }
        }

        // NUnit also discovers this nested fixture and runs it directly, so the
        // default factory must be a compliant recognizer; meta-tests swap it.
        public sealed class SuiteFor : RecognizerConformanceSuite
        {
            private readonly System.Func<IGestureRecognizer> _factory;
            public SuiteFor() : this(() => new Compliant()) { }
            public SuiteFor(System.Func<IGestureRecognizer> factory) => _factory = factory;
            protected override IGestureRecognizer CreateRecognizer() => _factory();
        }

        [Test]
        public void Suite_Catches_UndeclaredEventIds()
        {
            var suite = new SuiteFor(() => new UndeclaredEmitter());
            Assert.Throws<AssertionException>(suite.EmittedIds_AreSubsetOfDescriptors);
        }

        [Test]
        public void Suite_Catches_NonDeterministicReset()
        {
            var suite = new SuiteFor(() => new StatefulNoReset());
            Assert.Throws<AssertionException>(suite.Reset_MakesReplayDeterministic);
        }

        [Test]
        public void Suite_Catches_BeganWhileHandAbsent()
        {
            var suite = new SuiteFor(() => new UndeclaredEmitter());
            Assert.Throws<AssertionException>(suite.HandAbsentFrames_DoNotThrow_AndEmitNoBegan);
        }
    }

    /// <summary>A trivially correct recognizer must pass the whole suite.</summary>
    public class NullRecognizerConformance : RecognizerConformanceSuite
    {
        private sealed class NullRecognizer : IGestureRecognizer
        {
            public IReadOnlyList<GestureDescriptor> Descriptors { get; } =
                new[] { new GestureDescriptor("nothing", GestureKind.Discrete) };

            public void Reset() { }
            public void Process(in GestureFrame frame, IGestureSink sink) { }
        }

        protected override IGestureRecognizer CreateRecognizer() => new NullRecognizer();
    }
}
