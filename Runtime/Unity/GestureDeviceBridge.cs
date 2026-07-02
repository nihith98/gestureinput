using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using GestureInput.Core;

namespace GestureInput.Unity
{
    /// <summary>
    /// Builds the <see cref="GestureDevice"/> layout from a set of gesture
    /// descriptors and pushes gesture events into the Input System as delta
    /// state events.
    ///
    /// The control set is fixed at <see cref="CreateDevice"/> time (an Input
    /// System constraint — layouts are static once a device is created).
    /// Gestures registered afterwards still flow through the C# event stream
    /// (<see cref="GestureRuntime.OnGesture"/>); recreate the device to get
    /// controls for them.
    /// </summary>
    public sealed class GestureDeviceBridge : IDisposable
    {
        private readonly Dictionary<string, InputControl> _controls =
            new Dictionary<string, InputControl>(StringComparer.Ordinal);
        private readonly Dictionary<string, GestureKind> _kinds =
            new Dictionary<string, GestureKind>(StringComparer.Ordinal);

        private GestureDescriptor[] _layoutDescriptors;
        private GestureDevice _device;

        public InputDevice Device => _device;
        public bool HasDevice => _device != null;

        /// <summary>
        /// Register the layout (built from <paramref name="descriptors"/>) and add
        /// the device. Call once, after all startup recognizers are registered.
        /// </summary>
        public void CreateDevice(IEnumerable<GestureDescriptor> descriptors)
        {
            if (_device != null)
                throw new InvalidOperationException("GestureDevice already exists. Call RemoveDevice first.");

            var list = new List<GestureDescriptor>(descriptors);
            if (list.Count == 0)
                throw new InvalidOperationException("Cannot create a GestureDevice with no gestures registered.");

            _layoutDescriptors = list.ToArray();

            // The builder delegate is re-invoked whenever the Input System needs
            // the layout, so it must read the captured descriptor snapshot.
            InputSystem.RegisterLayoutBuilder(BuildLayout, GestureDevice.LayoutName);

            _device = (GestureDevice)InputSystem.AddDevice(GestureDevice.LayoutName);

            _controls.Clear();
            _kinds.Clear();
            foreach (var d in _layoutDescriptors)
            {
                _controls[d.Id] = _device.GetChildControl(d.Id);
                _kinds[d.Id] = d.Kind;
            }
        }

        private InputControlLayout BuildLayout()
        {
            var builder = new InputControlLayout.Builder()
                .WithType<GestureDevice>()
                .WithFormat(new FourCC('G', 'S', 'T', 'R'));

            uint offset = 0;
            foreach (var d in _layoutDescriptors)
            {
                switch (d.Kind)
                {
                    case GestureKind.Discrete:
                        builder.AddControl(d.Id)
                            .WithLayout("Button")
                            .WithFormat(InputStateBlock.FormatFloat)
                            .WithByteOffset(offset)
                            .WithSizeInBits(32);
                        offset += 4;
                        break;

                    case GestureKind.Continuous1D:
                        builder.AddControl(d.Id)
                            .WithLayout("Axis")
                            .WithFormat(InputStateBlock.FormatFloat)
                            .WithByteOffset(offset)
                            .WithSizeInBits(32);
                        offset += 4;
                        break;

                    case GestureKind.Continuous2D:
                        builder.AddControl(d.Id)
                            .WithLayout("Vector2")
                            .WithFormat(InputStateBlock.FormatVector2)
                            .WithByteOffset(offset)
                            .WithSizeInBits(64);
                        offset += 8;
                        break;
                }
            }

            return builder.Build();
        }

        /// <summary>Push one gesture event into the device's control state.</summary>
        public void Push(in GestureEvent e)
        {
            if (_device == null || !_controls.TryGetValue(e.Id, out var control))
                return; // gesture registered after device creation — event stream only

            switch (_kinds[e.Id])
            {
                case GestureKind.Discrete:
                case GestureKind.Continuous1D:
                    float value = e.Phase == GesturePhase.Ended ? 0f : e.Value;
                    InputSystem.QueueDeltaStateEvent((InputControl<float>)control, value);
                    break;

                case GestureKind.Continuous2D:
                    Vector2 value2 = e.Phase == GesturePhase.Ended ? Vector2.zero : e.Value2;
                    InputSystem.QueueDeltaStateEvent((InputControl<Vector2>)control, value2);
                    break;
            }
        }

        public void RemoveDevice()
        {
            if (_device != null)
            {
                InputSystem.RemoveDevice(_device);
                _device = null;
            }
            _controls.Clear();
            _kinds.Clear();
        }

        public void Dispose() => RemoveDevice();
    }
}
