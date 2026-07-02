using System;
using NUnit.Framework;
using UnityEngine;
using GestureInput.Core;

namespace GestureInput.Tests
{
    [TestFixture]
    public class GestureDescriptorTests
    {
        [TestCase("wave")]
        [TestCase("swipeLeft2")]
        [TestCase("OpenPalm")]
        public void ValidIds_AreAccepted(string id)
        {
            var d = new GestureDescriptor(id, GestureKind.Discrete);
            Assert.AreEqual(id, d.Id);
            Assert.AreEqual(GestureKind.Discrete, d.Kind);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("bad id")]
        [TestCase("bad-id!")]
        [TestCase("2fast")]
        [TestCase("under_score")]
        public void InvalidIds_Throw(string id)
        {
            Assert.Throws<ArgumentException>(() => new GestureDescriptor(id, GestureKind.Discrete));
        }
    }

    [TestFixture]
    public class GestureEventTests
    {
        [Test]
        public void ScalarCtor_PopulatesFields()
        {
            var e = new GestureEvent("wave", GesturePhase.Began, 1f, confidence: 0.9f, timestampMs: 42);
            Assert.AreEqual("wave", e.Id);
            Assert.AreEqual(GesturePhase.Began, e.Phase);
            Assert.AreEqual(1f, e.Value);
            Assert.AreEqual(0.9f, e.Confidence);
            Assert.AreEqual(42, e.TimestampMs);
            Assert.AreEqual(Vector2.zero, e.Value2);
        }

        [Test]
        public void Vector2Ctor_PopulatesValue2_AndValueIsMagnitude()
        {
            var e = new GestureEvent("point", GesturePhase.Updated, new Vector2(3f, 4f), timestampMs: 7);
            Assert.AreEqual(new Vector2(3f, 4f), e.Value2);
            Assert.AreEqual(5f, e.Value, 1e-4f);
            Assert.AreEqual(GesturePhase.Updated, e.Phase);
            Assert.AreEqual(1f, e.Confidence);
        }
    }

    [TestFixture]
    public class GestureFrameTests
    {
        [Test]
        public void HandDataAbsent_IsNotPresent()
        {
            Assert.IsFalse(HandData.Absent.IsPresent);
            Assert.IsNull(HandData.Absent.Landmarks);
            Assert.AreEqual(Handedness.Unknown, HandData.Absent.Handedness);
        }

        [Test]
        public void PoseDataAbsent_IsNotPresent()
        {
            Assert.IsFalse(PoseData.Absent.IsPresent);
        }

        [Test]
        public void Frame_RoundTripsFields()
        {
            var landmarks = new[] { new Vector3(0.1f, 0.2f, 0.3f) };
            var hand = new HandData(Handedness.Right, new Vector2(0.5f, 0.6f), landmarks);
            var builtin = new BuiltinGesture(BuiltinGestureType.OpenPalm, 0.85f);
            var frame = new GestureFrame(123, hand, PoseData.Absent, builtin);

            Assert.AreEqual(123, frame.TimestampMs);
            Assert.IsTrue(frame.Hand.IsPresent);
            Assert.AreEqual(Handedness.Right, frame.Hand.Handedness);
            Assert.AreEqual(new Vector2(0.5f, 0.6f), frame.Hand.Palm);
            Assert.AreSame(landmarks, frame.Hand.Landmarks);
            Assert.AreEqual(BuiltinGestureType.OpenPalm, frame.Builtin.Type);
            Assert.AreEqual(0.85f, frame.Builtin.Confidence);
            Assert.IsFalse(frame.Pose.IsPresent);
        }

        [Test]
        public void DefaultFrame_IsSafe()
        {
            var frame = default(GestureFrame);
            Assert.IsFalse(frame.Hand.IsPresent);
            Assert.IsFalse(frame.Pose.IsPresent);
            Assert.AreEqual(BuiltinGestureType.None, frame.Builtin.Type);
        }
    }
}
