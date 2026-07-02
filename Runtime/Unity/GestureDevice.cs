using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace GestureInput.Unity
{
    /// <summary>
    /// The custom Input Device whose controls are gestures. It has no fixed
    /// layout — <see cref="GestureDeviceBridge"/> builds one at runtime from the
    /// union of registered gesture descriptors, so a third-party "wave" gesture
    /// becomes bindable as <c>&lt;GestureDevice&gt;/wave</c> exactly like a
    /// built-in. The class itself is an empty shell; all state lives in the
    /// generated layout's controls and is pushed via delta state events.
    /// </summary>
    [InputControlLayout(displayName = "Gesture Device", isGenericTypeOfDevice = false)]
    public class GestureDevice : InputDevice
    {
        /// <summary>The layout name the bridge registers. Bind as &lt;GestureDevice&gt;/…</summary>
        public const string LayoutName = "GestureDevice";
    }
}
