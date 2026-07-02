# GestureInput

**Webcam gestures as bindable Unity Input System controls — with an open SPI so
any developer can add their own gestures.**

MediaPipe (via [homuler/MediaPipeUnityPlugin](https://github.com/homuler/MediaPipeUnityPlugin))
already solves *perception*: hand landmarks and a fixed set of static gestures.
GestureInput is the missing layer above it:

- **Temporal / dynamic gestures** — swipes, waves, holds — tracked over time, not
  single-frame classification.
- **Input System integration** — every gesture is a control on a custom
  `GestureDevice`; game code binds `<GestureDevice>/swipeLeft` in a
  `.inputactions` asset exactly like a gamepad button.
- **Extension-first** — the built-ins implement the same one-interface SPI
  (`IGestureRecognizer`) any third party uses. Adding a gesture means
  implementing one interface; no forking, no core changes.

MIT licensed. Unity **2022.3+**.

## Installation

1. **Install this package** (Package Manager ▸ *Add package from git URL…*):

   ```
   https://github.com/nihith98/gestureinput.git
   ```

   The Input System dependency (`com.unity.inputsystem`) resolves automatically.
   Enable it under *Project Settings ▸ Player ▸ Active Input Handling* →
   **Input System Package** (or *Both*), then restart the editor.

2. **Install the MediaPipe Unity Plugin** (required for live webcam input only —
   everything else works without it). It ships native binaries via
   [GitHub Releases](https://github.com/homuler/MediaPipeUnityPlugin/releases),
   not a package registry, so UPM cannot auto-install it. Tested against
   **v0.16.x**; follow its installation guide, then verify its
   *GestureRecognizer* sample scene runs with your webcam **before** blaming
   this package. An editor check warns if the plugin is missing.

3. **Download the model**: place `gesture_recognizer.task` (from the
   [MediaPipe model zoo](https://ai.google.dev/edge/mediapipe/solutions/vision/gesture_recognizer))
   in `Assets/StreamingAssets/`.

## Quickstart

```csharp
// scene setup (or use the DemoBootstrap component from the sample)
var go = new GameObject("Gestures");
go.AddComponent<GestureRuntime>();                              // registry + device + event stream
go.AddComponent<GestureInput.Mediapipe.MediapipeGestureDriver>(); // webcam -> frames
```

Bind in the Input Actions editor: `<GestureDevice>/openPalm`,
`<GestureDevice>/swipeRight`, … — or subscribe in code:

```csharp
GestureRuntime.Instance.OnGesture += e =>
{
    if (e.Id == "swipeLeft" && e.Phase == GesturePhase.Began)
        NextPage();
};
```

**Built-in gestures** — static (from MediaPipe, with hysteresis + phase
tracking): `openPalm`, `closedFist`, `thumbUp`, `thumbDown`, `victory`,
`pointingUp`, `iLoveYou`. Dynamic: `swipeLeft/Right/Up/Down`.

**Adding your own** is one interface + a toolkit of tested helpers
(ring buffers, hysteresis, cooldowns, motion analysis):
see [Documentation~/authoring-a-recognizer.md](Documentation~/authoring-a-recognizer.md)
and the *Custom Recognizer Example* sample (a complete `wave` gesture).

## Architecture

```
webcam ──► MediaPipe (native, LIVE_STREAM)
                │ result callback (non-Unity thread!)
                ▼
        GestureFrame normalization ──► FrameInbox (thread-safe, drop-oldest)
                                            │ main thread, Update()
                                            ▼
                              GestureRuntime ── GestureRegistry
                                            │ runs all IGestureRecognizers
                            ┌───────────────┴───────────────┐
                            ▼                               ▼
                 GestureDevice (Input System)      OnGesture (C# events)
                 bind <GestureDevice>/wave         dynamic escape hatch
```

- Recognizers see only backend-agnostic `GestureFrame`s — pure C#, fully
  unit-testable without a camera, GPU, or scene.
- The `GestureDevice` layout is built at startup from the union of registered
  descriptors: `Discrete` → Button, `Continuous1D` → Axis, `Continuous2D` → Vector2.
  Controls are fixed once the device exists; late registrations use the event stream.
- Assembly split: `GestureInput.Core` (SPI + toolkit + built-in recognizers,
  no engine/plugin deps) ← third-party recognizer packages depend on this alone;
  `GestureInput.Unity` (runtime, device, registration);
  `GestureInput.Mediapipe` (driver, compiled only when the plugin is present).

## Samples

| Sample | Contents |
|--------|----------|
| **Live Webcam Demo** | landmark skeleton overlay, gesture/control debug HUD, FPS + dropped frames, one-click **fixture recorder** |
| **Custom Recognizer Example** | complete `wave` recognizer + code/asset registration |

## Testing

- **EditMode / dotnet** — the entire core (SPI, toolkit, built-in recognizers,
  registry, fixture codec) is covered by NUnit tests that run both in Unity's
  Test Runner and standalone: `dotnet test DevTests~/GestureInput.DevTests.csproj`
  (no Unity license needed — this is what CI runs).
- **Conformance suite** — subclass `RecognizerConformanceSuite` to verify any
  recognizer honors the SPI contract in one line.
- **Record-and-replay** — record `.gframes` fixtures in the live demo, replay
  them deterministically in EditMode tests.
- **PlayMode** — device/runtime integration tests with a mocked frame source
  (`Tests/PlayMode`, run in the Unity Test Runner).

### Verified-in-editor checklist

This package's core is fully machine-verified. The Unity/plugin glue must be
validated once per certified editor/plugin version (SCOPE §11):

- [ ] `InputSystem.RegisterLayoutBuilder` + `QueueDeltaStateEvent` path (run the PlayMode tests)
- [ ] MediaPipe Tasks API names in `MediapipeGestureDriver` against the installed plugin's sample
- [ ] LIVE_STREAM callback threading (the inbox makes this safe either way)
- [ ] Unity 6 LTS compatibility of the native plugin

## License

MIT (this package). Transitive: MediaPipe is Apache-2.0, the Unity plugin is MIT.
Consumers obtain the plugin and models separately; this package ships neither.
