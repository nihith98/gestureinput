using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using GestureInput.Core;
using GestureInput.Unity;

namespace GestureInput.Tests.PlayMode
{
    /// <summary>
    /// PlayMode integration tests: runtime + device bridge in a live scene with a
    /// mocked frame source (no camera, no MediaPipe). Run these inside the Unity
    /// Test Runner; they cannot execute in the DevTests~ harness.
    /// </summary>
    public class GestureRuntimeDeviceTests
    {
        private GameObject _go;
        private GestureRuntime _runtime;
        private MockFrameSource _source;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("GestureRuntimeUnderTest");
            _go.SetActive(false); // configure before Awake
            _runtime = _go.AddComponent<GestureRuntime>();
            _source = new MockFrameSource();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private void Activate()
        {
            _go.SetActive(true); // Awake runs: registers built-ins
            _runtime.FrameSource = _source;
        }

        [UnityTest]
        public IEnumerator Device_IsCreated_WithControlsForAllRegisteredGestures()
        {
            Activate();
            yield return null; // first Update creates the device

            var device = InputSystem.GetDevice(GestureDevice.LayoutName);
            Assert.IsNotNull(device, "GestureDevice was not added to the Input System");
            Assert.IsNotNull(device.TryGetChildControl("openPalm"));
            Assert.IsNotNull(device.TryGetChildControl("swipeLeft"));
            Assert.IsNotNull(device.TryGetChildControl("swipeRight"));
        }

        [UnityTest]
        public IEnumerator CustomRecognizer_SurfacesAsNamedControl()
        {
            _runtime.Registry.Register(new FixedEmitRecognizer("customPinch"));
            Activate();
            yield return null;

            var device = InputSystem.GetDevice(GestureDevice.LayoutName);
            Assert.IsNotNull(device.TryGetChildControl("customPinch"),
                "custom gesture did not become a bindable control");
        }

        [UnityTest]
        public IEnumerator BuiltinGestureFrames_DriveBoundInputAction()
        {
            Activate();
            yield return null; // device exists

            var action = new InputAction(binding: "<GestureDevice>/openPalm");
            int performed = 0;
            action.performed += _ => performed++;
            action.Enable();

            for (int i = 0; i < 5; i++)
                _source.EnqueueBuiltin(i * 33, BuiltinGestureType.OpenPalm, 0.95f);

            yield return null; // runtime processes + queues state events
            yield return null; // input system updates actions

            Assert.Greater(performed, 0, "bound InputAction never performed");
            action.Disable();
            action.Dispose();
        }

        [UnityTest]
        public IEnumerator OnGesture_Stream_ReceivesEvents()
        {
            Activate();
            var received = new List<GestureEvent>();
            _runtime.OnGesture += e => received.Add(e);
            yield return null;

            for (int i = 0; i < 5; i++)
                _source.EnqueueBuiltin(i * 33, BuiltinGestureType.Victory, 0.95f);
            yield return null;

            Assert.IsTrue(received.Exists(e => e.Id == "victory" && e.Phase == GesturePhase.Began));
        }

        [UnityTest]
        public IEnumerator RuntimeDestroy_RemovesDevice()
        {
            Activate();
            yield return null;
            Assert.IsNotNull(InputSystem.GetDevice(GestureDevice.LayoutName));

            Object.DestroyImmediate(_go);
            _go = null;
            yield return null;

            Assert.IsNull(InputSystem.GetDevice(GestureDevice.LayoutName),
                "GestureDevice should be removed when the runtime is destroyed");
        }

        private sealed class FixedEmitRecognizer : IGestureRecognizer
        {
            public FixedEmitRecognizer(string id) =>
                Descriptors = new[] { new GestureDescriptor(id, GestureKind.Discrete) };

            public IReadOnlyList<GestureDescriptor> Descriptors { get; }
            public void Reset() { }
            public void Process(in GestureFrame frame, IGestureSink sink) { }
        }
    }
}
