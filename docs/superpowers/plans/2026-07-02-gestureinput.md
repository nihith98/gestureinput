# GestureInput Unity Library — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `com.nihith.gestureinput`, an MIT-licensed UPM package that turns MediaPipe hand/pose perception into an extensible gesture-recognizer SPI surfaced through Unity's Input System, per SCOPE.md.

**Architecture:** Three runtime assemblies — `GestureInput.Core` (pure C# SPI + toolkit + built-in recognizers, references only UnityEngine math types), `GestureInput.Unity` (Input System device, runtime, registration; no MediaPipe), `GestureInput.Mediapipe` (driver, `#if GESTUREINPUT_HAS_MEDIAPIPE`-guarded via asmdef versionDefines so the package compiles without the plugin). Core + all EditMode tests also compile in a `DevTests~/` dotnet project against a tiny UnityEngine shim, giving red-green TDD and license-free CI on this machine.

**Tech Stack:** C# 9 (Unity 2022.3-compatible), NUnit, Unity Input System 1.7+, homuler/MediaPipeUnityPlugin (consumer-installed), .NET SDK for the dev harness, GitHub Actions.

**Repo root = package root** (`E:\Fable5\gesturify` is `com.nihith.gestureinput`; embed under `Packages/` of a host project by cloning).

---

## File structure

```
/ (package root, git repo)
├── package.json  README.md  CHANGELOG.md  LICENSE.md  .gitignore
├── Runtime/
│   ├── Core/
│   │   ├── GestureInput.Core.asmdef
│   │   ├── Spi/            GestureKind.cs GesturePhase.cs GestureDescriptor.cs GestureEvent.cs
│   │   │                   HandData.cs PoseData.cs BuiltinGesture.cs GestureFrame.cs
│   │   │                   IGestureSink.cs IGestureRecognizer.cs
│   │   ├── Toolkit/        RingBuffer.cs Hysteresis.cs Cooldown.cs Motion.cs TimedVector2.cs
│   │   ├── Recognizers/    StaticGestureRecognizer.cs SwipeRecognizer.cs
│   │   ├── Fixtures/       FixtureCodec.cs
│   │   ├── GestureRegistry.cs
│   │   └── FrameInbox.cs   (ConcurrentQueue<GestureFrame> wrapper, drop-oldest)
│   ├── Unity/
│   │   ├── GestureInput.Unity.asmdef        (refs Core, Unity.InputSystem)
│   │   ├── IGestureFrameSource.cs GestureRuntime.cs GestureDevice.cs GestureDeviceBridge.cs
│   │   └── Registration/   GestureRecognizerAsset.cs GestureRecognizerAttribute.cs AttributeDiscovery.cs
│   ├── Mediapipe/
│   │   ├── GestureInput.Mediapipe.asmdef    (refs Core, Unity asm, Mediapipe.Runtime; versionDefines)
│   │   └── MediapipeGestureDriver.cs        (all code inside #if GESTUREINPUT_HAS_MEDIAPIPE)
│   └── Editor/  GestureInput.Editor.asmdef  MediapipeDependencyCheck.cs
├── Tests/
│   ├── EditMode/
│   │   ├── GestureInput.Tests.EditMode.asmdef
│   │   ├── TestSink.cs FixturePaths.cs
│   │   ├── Spi/  Toolkit/  Recognizers/  (unit tests)
│   │   ├── Conformance/RecognizerConformanceSuite.cs (+ concrete subclasses)
│   │   └── Fixtures/*.gframes (checked-in fixture files)
│   └── PlayMode/
│       ├── GestureInput.Tests.PlayMode.asmdef
│       ├── MockFrameSource.cs GestureDeviceTests.cs GestureRuntimeTests.cs
├── Samples~/
│   ├── LiveWebcamDemo/   DemoBootstrap.cs LandmarkOverlay.cs GestureDebugHud.cs FixtureRecorder.cs README.md
│   └── CustomRecognizerExample/  WaveRecognizer.cs WaveRegistration.cs README.md
├── Documentation~/authoring-a-recognizer.md
├── DevTests~/            (dotnet harness; tilde ⇒ invisible to Unity)
│   ├── GestureInput.DevTests.csproj   (links ../Runtime/Core/**/*.cs, ../Tests/EditMode/**/*.cs,
│   │                                    Samples~/CustomRecognizerExample/WaveRecognizer.cs)
│   └── UnityShim/  Vector2.cs Vector3.cs Mathf.cs
└── .github/workflows/ci.yml           (dotnet test DevTests~)
```

Rule enforced throughout: `Runtime/Core` and `Tests/EditMode` use **only** `Vector2`/`Vector3`/`Mathf` from UnityEngine (shimmed in DevTests~), plus BCL. Anything needing the engine goes in `Runtime/Unity`+`Tests/PlayMode`.

---

## Key SPI contracts (single source of truth for all tasks)

```csharp
public enum GestureKind { Discrete, Continuous1D, Continuous2D }
public enum GesturePhase { Began, Updated, Ended }
public enum Handedness { Unknown, Left, Right }
public enum BuiltinGestureType { None, OpenPalm, ClosedFist, ThumbUp, ThumbDown, Victory, PointingUp, ILoveYou }

public readonly struct GestureDescriptor {
    public string Id { get; }            // ^[a-zA-Z][a-zA-Z0-9]*$ — Input control name safe
    public GestureKind Kind { get; }
    public GestureDescriptor(string id, GestureKind kind); // throws ArgumentException on bad id
}

public readonly struct GestureEvent {
    public string Id { get; } public GesturePhase Phase { get; }
    public float Value { get; } public Vector2 Value2 { get; }
    public float Confidence { get; } public long TimestampMs { get; }
    // ctors: (id, phase, value, confidence=1, ts=0) and (id, phase, value2, confidence=1, ts=0)
}

public readonly struct HandData {
    public bool IsPresent { get; } public Handedness Handedness { get; }
    public Vector2 Palm { get; }                 // normalized [0,1]²
    public IReadOnlyList<Vector3> Landmarks { get; }  // 21 pts or null when absent
    public static HandData Absent { get; }
}
public readonly struct PoseData { bool IsPresent; IReadOnlyList<Vector3> Landmarks; static PoseData Absent; }
public readonly struct BuiltinGesture { BuiltinGestureType Type; float Confidence; }
public readonly struct GestureFrame { long TimestampMs; HandData Hand; PoseData Pose; BuiltinGesture Builtin; }

public interface IGestureSink { void Emit(in GestureEvent e); }
public interface IGestureRecognizer {
    IReadOnlyList<GestureDescriptor> Descriptors { get; }
    void Reset();
    void Process(in GestureFrame frame, IGestureSink sink);
}
```

Built-in gesture ids: `openPalm closedFist thumbUp thumbDown victory pointingUp iLoveYou`;
swipe ids: `swipeLeft swipeRight swipeUp swipeDown`; example id: `wave`.

---

## Tasks

### Task 0: Repo + package skeleton
- [ ] `git init`; write `.gitignore` (Library/ Temp/ Obj/ Logs/ Build/ bin/ obj/ *.csproj.user *.sln.DotSettings.user); note: DevTests~ csproj IS tracked.
- [ ] `package.json` (name com.nihith.gestureinput, version 0.1.0, unity 2022.3, dependency `com.unity.inputsystem: 1.7.0`, samples entries), `LICENSE.md` (MIT), stub `README.md`/`CHANGELOG.md`.
- [ ] Four asmdefs + tests asmdefs exactly as in file structure (Core: noEngineReferences=false, references []; Unity: [Core, Unity.InputSystem]; Mediapipe: adds Mediapipe.Runtime + versionDefines `{expression:"", define:"GESTUREINPUT_HAS_MEDIAPIPE", name:"com.github.homuler.mediapipe"}`; EditMode tests: [Core] + nunit; PlayMode tests: [Core, Unity, Unity.InputSystem] + nunit).
- [ ] Commit.

### Task 1: DevTests~ harness with UnityEngine shim
- [ ] `DevTests~/GestureInput.DevTests.csproj`: net8.0, LangVersion 9, NUnit + NUnit3TestAdapter + Microsoft.NET.Test.Sdk; `<Compile Include>` links for Core, EditMode tests, WaveRecognizer sample.
- [ ] `UnityShim/Vector2.cs`, `Vector3.cs`, `Mathf.cs` in namespace `UnityEngine` (fields x/y/z, arithmetic operators, magnitude/sqrMagnitude/normalized, Dot, Distance; Mathf.Abs/Min/Max/Clamp/Clamp01/Approximately/Sqrt/Epsilon).
- [ ] Smoke test `ShimSmokeTests.cs` (in DevTests~ itself, not linked into Unity) asserting Vector2 math; `dotnet test` → PASS. Commit.

### Task 2: SPI types (TDD)
- [ ] Tests `Tests/EditMode/Spi/SpiTypeTests.cs`: descriptor rejects null/empty/`"bad id!"`/leading digit; accepts `wave`, `swipeLeft2`; GestureEvent ctors populate fields; HandData.Absent.IsPresent false; GestureFrame round-trips fields. Run → FAIL.
- [ ] Implement all Spi/ files per contracts above. Run → PASS. Commit.

### Task 3: Toolkit (TDD, one component at a time)
- [ ] `RingBuffer<T>`: Add/Count/Capacity/IsFull/Clear/this[int] (0 = oldest)/Latest; wraparound test (capacity 3, add 5, expect last 3 in order).
- [ ] `Hysteresis(enter, exit)`: `bool Update(float)` + `IsActive` + `Reset`; test no-flicker: value oscillating between exit and enter stays active once entered; ctor validates enter > exit.
- [ ] `Cooldown(long ms)`: `Ready(ts)`, `Trigger(ts)`, `Reset`; test one-event-per-window; ready again after ms elapsed; Reset makes immediately ready.
- [ ] `TimedVector2` + static `Motion`: `Displacement(RingBuffer<TimedVector2>)`, `Velocity(...)` (units/sec, zero when <2 samples or dt 0), `DominantDirection(Vector2, deadZone)` → `SwipeDirection {None,Left,Right,Up,Down}`, `CountReversals(RingBuffer<float>, minDelta)` (wave helper). Tests for each incl. degenerate inputs.
- [ ] Commit after each component passes.

### Task 4: FixtureCodec + FixturePaths
- [ ] Line-based, culture-invariant text format `.gframes` v1: header `gframes 1`; per frame `ts|handPresent|handedness|palm.x,palm.y|builtinType|builtinConf|landmarks(x,y,z;...)|posePresent|poseLandmarks`. `FixtureCodec.Write(IEnumerable<GestureFrame>) : string` and `Read(string) : List<GestureFrame>`; round-trip test incl. absent hand, empty stream, version rejection.
- [ ] `FixturePaths.Resolve(name)`: probe upward from `AppContext.BaseDirectory` and from `Packages/com.nihith.gestureinput/` for `Tests/EditMode/Fixtures/<name>` so it works in both dotnet and Unity. Test resolves a checked-in `smoke.gframes`. Commit.

### Task 5: GestureRegistry + FrameInbox
- [ ] Registry: `Register` (throws on null, on duplicate descriptor Id across recognizers), `Remove`, `Clear`, `Recognizers`, `AllDescriptors`, `ProcessFrame(in frame, IGestureSink)` (iterates recognizers; a throwing recognizer is caught, reported via `event Action<IGestureRecognizer,Exception> OnRecognizerError`, others still run), `ResetAll()`. Tests for each behavior.
- [ ] `FrameInbox(int capacity=4)`: `Enqueue(in GestureFrame)` (drop-oldest when full, increment `DroppedFrames`), `TryDequeue(out GestureFrame)`; thread-safety test (producer thread + consumer loop, no loss beyond capacity accounting). Commit.

### Task 6: TestSink + conformance suite
- [ ] `TestSink : IGestureSink` — `Events` list, `Contains(id, phase)`, `Count(id)`, `Clear()`.
- [ ] Abstract `RecognizerConformanceSuite`: `protected abstract IGestureRecognizer CreateRecognizer();` `protected virtual IEnumerable<GestureFrame> GetSampleFrames()` (default: 60 empty frames). Tests: descriptors non-empty/unique/valid ids; emitted ids ⊆ descriptors over sample frames; Reset determinism (replay sample twice with Reset between ⇒ identical event sequences); no throw on hand-absent frames; Process before any real frame doesn't throw. Verified first against a deliberately-broken inline recognizer (assert suite catches it) and a trivial good one. Commit.

### Task 7: StaticGestureRecognizer (TDD + conformance)
- [ ] Behavior: per built-in type, Hysteresis on confidence (defaults enter .7/exit .5, ctor-configurable); emits Began (value 1) on activation, Updated each frame held (value = confidence), Ended (value 0) on deactivation or hand loss; only one static gesture active at a time (highest-confidence wins); Reset clears active state (emits nothing). Descriptors = the 7 ids, Discrete.
- [ ] Tests: rising confidence ⇒ Began once then Updated; mid-band flicker ⇒ no re-Began; hand lost while active ⇒ Ended; switch OpenPalm→Fist ⇒ Ended(openPalm)+Began(closedFist). Conformance subclass with synthetic sample frames. Commit.

### Task 8: SwipeRecognizer (TDD + conformance + fixtures)
- [ ] Behavior: RingBuffer<TimedVector2> of palm positions (window default 250 ms ⇒ capacity ~16); fires when window displacement magnitude ≥ minDistance (default 0.25 normalized) AND mean speed ≥ minVelocity (default 1.0 /s) AND straightness (|disp|/pathLen ≥ 0.8); direction via Motion.DominantDirection; Began-only discrete event (value 1, confidence = straightness); Cooldown 400 ms; hand loss ⇒ Reset.
- [ ] Tests: synthetic right-swipe stream ⇒ exactly one swipeRight; slow drift ⇒ nothing; zig-zag ⇒ nothing (straightness); cooldown suppresses immediate second event; opposite swipe after cooldown works.
- [ ] Generator writes `swipe_right.gframes`, `wave_left_right.gframes` (synthetic, checked in); fixture-driven tests replay via FixtureCodec. Conformance subclass. Commit.

### Task 9: GestureInput.Unity — runtime, device, registration (not dotnet-compiled; Unity-verified later)
- [ ] `IGestureFrameSource { bool TryDequeue(out GestureFrame); void Reset(); }`.
- [ ] `GestureDeviceBridge`: builds `InputControlLayout` from descriptor union via `InputSystem.RegisterLayoutBuilder` — Button control (format FLT) per Discrete/Continuous1D at sequential 4-byte offsets ("Axis" for Continuous1D), Vector2 control (two floats) per Continuous2D; `AddDevice`, `Push(in GestureEvent)` → `InputSystem.QueueDeltaStateEvent(control, value)`; `RemoveDevice()` on teardown.
- [ ] `GestureRuntime : MonoBehaviour`: singleton-ish `Instance`; `Registry` property; `FrameSource` settable; `static event Action<GestureEvent> OnGesture`; Update() drains source (all queued frames), calls `Registry.ProcessFrame` with internal collector sink, then per event: raise OnGesture + bridge.Push; `CreateDevice()` explicit call after registration phase; OnDestroy → remove device, dispose driver if IDisposable; hand-loss (N frames without hand) ⇒ `Registry.ResetAll()`.
- [ ] Registration: abstract `GestureRecognizerAsset : ScriptableObject { public abstract IGestureRecognizer CreateRecognizer(); }` + `GestureRuntime.recognizerAssets` serialized list auto-registered in Awake; `[GestureRecognizer]` attribute + `AttributeDiscovery.RegisterAll(registry)` (opt-in, try/catch per type, documented IL2CPP caveat). Commit.

### Task 10: MediaPipe driver (guarded)
- [ ] `MediapipeGestureDriver : MonoBehaviour, IGestureFrameSource` wrapped in `#if GESTUREINPUT_HAS_MEDIAPIPE`; `#else` stub that logs setup error. WebCamTexture start → per-Update `Image` build → `GestureRecognizer` (Tasks API, LIVE_STREAM) `RecognizeAsync`; result callback (native thread) normalizes to GestureFrame (landmarks, palm = wrist/mean of MCPs, builtin classification map by category name) → `FrameInbox.Enqueue`; `TryDequeue` delegates to inbox; OnDestroy disposes recognizer + stops camera; `using` on Image. Category-name→BuiltinGestureType map ("Open_Palm"→OpenPalm etc.). Note in code + README: verify exact API names against installed plugin sample (SCOPE §11).
- [ ] Editor check `MediapipeDependencyCheck` ([InitializeOnLoadMethod]): if define absent, one clear console warning with install link. Commit.

### Task 11: PlayMode tests (run in Unity only)
- [ ] `MockFrameSource : IGestureFrameSource` (queue you preload). Tests ([UnityTest]): registering recognizer + CreateDevice ⇒ `<GestureDevice>/wave` control exists; preload frames ⇒ bound InputAction fires within 2 frames; OnGesture raised on main thread; device removed on runtime destroy. Commit.

### Task 12: Samples
- [ ] `CustomRecognizerExample`: `WaveRecognizer` exactly per SCOPE §5.2 ergonomics (RingBuffer + Cooldown + Motion.CountReversals) — this file is linked into DevTests~ and covered by unit + conformance tests (wave fixture from Task 8); `WaveRegistration` MonoBehaviour showing `registry.Register(new WaveRecognizer())`; README.
- [ ] `LiveWebcamDemo`: `DemoBootstrap` (wires driver+runtime+device), `LandmarkOverlay` (draws hand skeleton via GL/LineRenderer from latest frame), `GestureDebugHud` (OnGUI: active gestures, confidences, FPS, dropped frames), `FixtureRecorder` (record toggle → FixtureCodec.Write to `Application.persistentDataPath`); README with setup steps. Commit.

### Task 13: Docs + CI + release polish
- [ ] `Documentation~/authoring-a-recognizer.md`: SPI walkthrough (wave as worked example), toolkit reference, registration options, conformance-suite usage, fixture record/replay, packaging a third-party recognizer against Core only.
- [ ] `README.md`: features, install (git URL + prerequisite MediaPipe plugin pinned release), quickstart, binding `<GestureDevice>/openPalm`, architecture summary, Unity-verification checklist (what must be validated in-editor: layout builder, delta events, plugin API names), license matrix.
- [ ] `CHANGELOG.md` 0.1.0. `.github/workflows/ci.yml`: dotnet test on push/PR (documented note re optional game-ci EditMode job needing Unity license).
- [ ] Final: `dotnet test` full suite green; self-review vs SCOPE §1.2 checklist; commit.

---

## Verification matrix

| Layer | Verified here by | Deferred to Unity editor |
|---|---|---|
| Core SPI/toolkit/recognizers/registry/codec | dotnet test (NUnit, same sources as EditMode) | re-run as EditMode in Unity (should be identical) |
| Conformance suite | dotnet test | same |
| Unity runtime/device | code review vs Input System docs | PlayMode tests (Task 11) |
| MediaPipe driver | compile-guard design | live sample scene (SCOPE §9.8) |
