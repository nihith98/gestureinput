using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GestureInput.Core;

namespace GestureInput.Tests
{
    [TestFixture]
    public class FixtureCodecTests
    {
        private static GestureFrame HandFrame(long ts, float px, float py,
            BuiltinGestureType builtin = BuiltinGestureType.None, float conf = 0f)
        {
            var landmarks = new Vector3[21];
            for (int i = 0; i < landmarks.Length; i++)
                landmarks[i] = new Vector3(px + i * 0.001f, py - i * 0.002f, i * 0.0005f);
            return new GestureFrame(ts,
                new HandData(Handedness.Right, new Vector2(px, py), landmarks),
                PoseData.Absent,
                new BuiltinGesture(builtin, conf));
        }

        private static GestureFrame EmptyFrame(long ts) =>
            new GestureFrame(ts, HandData.Absent, PoseData.Absent, BuiltinGesture.None);

        [Test]
        public void RoundTrip_PreservesFrames()
        {
            var frames = new List<GestureFrame>
            {
                EmptyFrame(0),
                HandFrame(33, 0.25f, 0.5f, BuiltinGestureType.OpenPalm, 0.91f),
                HandFrame(66, 0.30f, 0.52f),
                EmptyFrame(99)
            };

            var text = FixtureCodec.Write(frames);
            var read = FixtureCodec.Read(text);

            Assert.AreEqual(frames.Count, read.Count);

            Assert.IsFalse(read[0].Hand.IsPresent);
            Assert.AreEqual(0, read[0].TimestampMs);

            var f1 = read[1];
            Assert.AreEqual(33, f1.TimestampMs);
            Assert.IsTrue(f1.Hand.IsPresent);
            Assert.AreEqual(Handedness.Right, f1.Hand.Handedness);
            Assert.AreEqual(0.25f, f1.Hand.Palm.x, 1e-4f);
            Assert.AreEqual(0.5f, f1.Hand.Palm.y, 1e-4f);
            Assert.AreEqual(21, f1.Hand.Landmarks.Count);
            Assert.AreEqual(0.25f + 5 * 0.001f, f1.Hand.Landmarks[5].x, 1e-4f);
            Assert.AreEqual(BuiltinGestureType.OpenPalm, f1.Builtin.Type);
            Assert.AreEqual(0.91f, f1.Builtin.Confidence, 1e-4f);

            Assert.AreEqual(BuiltinGestureType.None, read[2].Builtin.Type);
        }

        [Test]
        public void RoundTrip_PreservesPose()
        {
            var pose = new Vector3[3] { new Vector3(0.1f, 0.2f, 0f), new Vector3(0.3f, 0.4f, 0.1f), new Vector3(0.5f, 0.6f, -0.1f) };
            var frame = new GestureFrame(10, HandData.Absent, new PoseData(pose), BuiltinGesture.None);

            var read = FixtureCodec.Read(FixtureCodec.Write(new[] { frame }));

            Assert.IsTrue(read[0].Pose.IsPresent);
            Assert.AreEqual(3, read[0].Pose.Landmarks.Count);
            Assert.AreEqual(0.4f, read[0].Pose.Landmarks[1].y, 1e-4f);
        }

        [Test]
        public void EmptyStream_RoundTrips()
        {
            var read = FixtureCodec.Read(FixtureCodec.Write(new GestureFrame[0]));
            Assert.AreEqual(0, read.Count);
        }

        [Test]
        public void Read_RejectsUnknownVersionOrGarbage()
        {
            Assert.Throws<FormatException>(() => FixtureCodec.Read("gframes 999\n"));
            Assert.Throws<FormatException>(() => FixtureCodec.Read("not a fixture"));
            Assert.Throws<FormatException>(() => FixtureCodec.Read(""));
        }

        [Test]
        public void Write_IsCultureInvariant()
        {
            var text = FixtureCodec.Write(new[] { HandFrame(1, 0.5f, 0.25f) });
            StringAssert.DoesNotContain(",5", text.Split('\n')[1].Split('|')[3]); // no decimal commas
            StringAssert.Contains("0.5", text);
        }
    }

    [TestFixture]
    public class FixturePathsTests
    {
        [Test]
        public void Resolve_FindsCheckedInFixture()
        {
            var path = FixturePaths.Resolve("smoke.gframes");
            Assert.IsTrue(System.IO.File.Exists(path), $"expected fixture at {path}");
            var frames = FixtureCodec.Read(System.IO.File.ReadAllText(path));
            Assert.Greater(frames.Count, 0);
        }
    }
}
