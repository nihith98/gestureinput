# Changelog

All notable changes to this package are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this
project adheres to [Semantic Versioning](https://semver.org/).

## [0.1.0] - 2026-07-02

### Added

- **Recognizer SPI** (`GestureInput.Core`): `IGestureRecognizer`, `GestureFrame`
  (backend-agnostic perception snapshot), `GestureDescriptor`, `GestureEvent`,
  `IGestureSink` — one interface to add any gesture.
- **Toolkit**: `RingBuffer<T>`, `Hysteresis`, `Cooldown`, `TimedVector2`, and
  `Motion` helpers (displacement, velocity, path length, dominant direction,
  reversal counting).
- **Built-in recognizers**: `StaticGestureRecognizer` (MediaPipe's seven static
  gestures with confidence hysteresis and Began/Updated/Ended phases) and
  `SwipeRecognizer` (four cardinal swipes with distance/velocity/straightness
  gates and cooldown).
- **Registry + runtime**: `GestureRegistry` (unique-id enforcement, per-recognizer
  error isolation), `GestureRuntime` MonoBehaviour (frame draining, hand-loss
  reset, `OnGesture`/`OnFrame` event streams), thread-safe `FrameInbox`.
- **Input System integration**: descriptor-driven `GestureDevice` layout built at
  runtime (`Discrete`→Button, `Continuous1D`→Axis, `Continuous2D`→Vector2);
  any registered gesture binds as `<GestureDevice>/{id}`.
- **MediaPipe driver** (`GestureInput.Mediapipe`): webcam → GestureRecognizer
  task (LIVE_STREAM) → normalized frames; compiled only when
  `com.github.homuler.mediapipe` is installed (asmdef versionDefines); editor
  dependency check with install guidance.
- **Registration**: code, `GestureRecognizerAsset` (ScriptableObject, Inspector
  tunable), and opt-in `[GestureRecognizer]` attribute discovery (documented
  IL2CPP caveat).
- **Testing**: `.gframes` record-and-replay fixture codec; reusable
  `RecognizerConformanceSuite` contract tests (with self-tests); 127-test
  EditMode suite runnable both in Unity and license-free via
  `dotnet test DevTests~` (also the CI backbone); PlayMode device/runtime
  integration tests with a mocked frame source.
- **Samples**: Live Webcam Demo (landmark overlay, debug HUD, fixture recorder)
  and Custom Recognizer Example (complete `wave` gesture with all registration
  options).
- **Docs**: README, `Documentation~/authoring-a-recognizer.md` authoring guide.
