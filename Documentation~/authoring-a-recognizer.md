# Authoring a Gesture Recognizer

This guide walks through adding your own gesture to GestureInput — no fork, no
core changes, no Unity scene required until the very end.

## 1. The contract

You implement one interface from `GestureInput.Core`:

```csharp
public interface IGestureRecognizer
{
    IReadOnlyList<GestureDescriptor> Descriptors { get; }   // what you can emit
    void Reset();                                           // clear temporal state
    void Process(in GestureFrame frame, IGestureSink sink); // once per frame, main thread
}
```

Three data types flow through it:

- **`GestureFrame`** (input) — timestamp, `Hand` (presence, handedness, palm point,
  21 landmarks, all normalized `[0,1]²`, +y down), optional `Pose`, and `Builtin`
  (the backend's static-gesture classification). You never see MediaPipe types.
- **`GestureDescriptor`** (metadata) — a stable `Id` (letters/digits, starts with a
  letter — it becomes the `<GestureDevice>/{id}` control name) and a `Kind`:
  `Discrete` (button), `Continuous1D` (axis), `Continuous2D` (vector2).
- **`GestureEvent`** (output) — `Id`, `Phase` (`Began`/`Updated`/`Ended`), scalar
  `Value` and/or `Value2`, `Confidence`, `TimestampMs`.

Rules the conformance suite enforces:

1. Descriptors are non-empty, unique, valid, and identical across instances.
2. Every emitted event `Id` is one of your declared descriptors.
3. `Reset()` clears **all** temporal state — replaying the same frames after a
   `Reset()` must produce identical events. Use frame timestamps, never wall
   clock or `Time.time`, or replay determinism breaks.
4. Hand-absent and default frames must not throw, and nothing may `Began` while
   no hand is present.

## 2. Write it with the toolkit

A complete "wave" recognizer (this exact file ships as the
*Custom Recognizer Example* sample):

```csharp
public sealed class WaveRecognizer : IGestureRecognizer
{
    public IReadOnlyList<GestureDescriptor> Descriptors { get; } =
        new[] { new GestureDescriptor("wave", GestureKind.Discrete) };

    private readonly RingBuffer<float> _xs = new RingBuffer<float>(30); // ~0.5s window
    private readonly Cooldown _cooldown = new Cooldown(600);

    public void Reset() { _xs.Clear(); _cooldown.Reset(); }

    public void Process(in GestureFrame frame, IGestureSink sink)
    {
        if (!frame.Hand.IsPresent) { _xs.Clear(); return; }
        _xs.Add(frame.Hand.Palm.x);

        // a wave = several direction reversals in x within the window
        if (Motion.CountReversals(_xs, minDelta: 0.03f) >= 4 &&
            _cooldown.Ready(frame.TimestampMs))
        {
            _cooldown.Trigger(frame.TimestampMs);
            _xs.Clear();
            sink.Emit(new GestureEvent("wave", GesturePhase.Began, 1f,
                timestampMs: frame.TimestampMs));
        }
    }
}
```

Toolkit pieces (`GestureInput.Core`, each unit-tested):

| Type | Use for |
|------|---------|
| `RingBuffer<T>` | sliding window of recent samples (index 0 = oldest, `Latest` = newest) |
| `Hysteresis(enter, exit)` | de-flicker a thresholded value (used by StaticGestureRecognizer) |
| `Cooldown(ms)` | one event per gesture: `Ready(ts)` / `Trigger(ts)` — timestamp-driven |
| `TimedVector2` | position + timestamp sample for motion windows |
| `Motion.Displacement / Velocity / PathLength` | net movement, speed, traveled distance over a window |
| `Motion.DominantDirection` | displacement → `SwipeDirection` cardinal |
| `Motion.CountReversals` | oscillation counting for wave-like gestures |

Dynamic gestures generally follow the state shape *Idle → Tracking →
Accumulating → Fired → Cooldown* (SCOPE §6.3); `Hysteresis` and `Cooldown` are
those transitions, so recognizers stay short.

## 3. Test it — before any Unity glue

Your recognizer is pure C#. Unit-test it with synthetic frames, then lock in
real behavior with recorded fixtures:

```csharp
[Test]
public void Wave_Fixture_EmitsWave()
{
    var r = new WaveRecognizer();
    var sink = new TestSink();
    foreach (var f in FixtureCodec.Read(File.ReadAllText("wave_left_right.gframes")))
        r.Process(f, sink);
    Assert.IsTrue(sink.Contains("wave", GesturePhase.Began));
}
```

And run the whole SPI contract in one line by subclassing the conformance suite:

```csharp
public class WaveConformance : RecognizerConformanceSuite
{
    protected override IGestureRecognizer CreateRecognizer() => new WaveRecognizer();
    protected override IEnumerable<GestureFrame> GetSampleFrames() =>
        FixtureCodec.Read(File.ReadAllText("wave_left_right.gframes"));
}
```

**Recording fixtures:** open the *Live Webcam Demo* sample, press **Record**,
perform the gesture, stop — a `.gframes` file lands in
`Application.persistentDataPath`. Check good takes into your test folder.

## 4. Register it

Three ways, in increasing order of magic:

1. **Code** (always works):
   `GestureRuntime.Instance.Registry.Register(new WaveRecognizer());`
2. **ScriptableObject asset** — subclass `GestureRecognizerAsset`, expose your
   thresholds as `[SerializeField]`s, create the asset, add it to
   GestureRuntime's *Recognizer Assets* list. Designers tune it in the Inspector.
3. **Attribute discovery** (opt-in) — mark the class `[GestureRecognizer]` and
   call `AttributeDiscovery.RegisterAll(registry)` at startup.
   ⚠ Reflection-based: IL2CPP/AOT stripping can remove unreferenced types on
   mobile. Options 1–2 are the guaranteed paths.

Register **before** the runtime's first `Update` (i.e. in `Awake`) to get a
named Input System control; the layout is fixed when the device is created.
Gestures registered later still reach `GestureRuntime.OnGesture` — the C#
event stream is the escape hatch for fully dynamic cases.

## 5. Bind it

After registration, `"wave"` is a normal Input System control:
add a binding to `<GestureDevice>/wave` in any `.inputactions` asset —
indistinguishable from a built-in gesture or a gamepad button.

## 6. Ship it as a package

A third-party recognizer package needs to reference **only the
`GestureInput.Core` assembly** — no Input System, no MediaPipe, no engine
runtime beyond math types. In your package's asmdef:

```jsonc
{ "name": "YourName.CoolGestures", "references": [ "GestureInput.Core" ] }
```

Ship a registration helper (option 1 or 2 above) so consumers can wire your
recognizers into their `GestureRuntime`, and run the conformance suite in your
own CI — it is the compatibility contract between us.
