# Custom Recognizer Example

A complete third-party gesture — **wave** — added without touching the library core.

## Files

| File | Shows |
|------|-------|
| `WaveRecognizer.cs` | The recognizer itself: pure C#, only `GestureInput.Core` types + toolkit (`RingBuffer`, `Cooldown`, `MotionMath.CountReversals`). |
| `WaveRegistration.cs` | Registration by code: `runtime.Registry.Register(new WaveRecognizer())`. |
| `WaveRecognizerAsset.cs` | Registration by ScriptableObject asset with Inspector-tunable thresholds. |

## Try it

1. Import this sample plus the *Live Webcam Demo* sample (Package Manager ▸ Gesture Input ▸ Samples).
2. Add `WaveRegistration` to the demo's GestureRuntime object.
3. Play, wave at the camera — the debug HUD shows `wave` firing, and it is bindable
   in any `.inputactions` asset as `<GestureDevice>/wave`.

## Testing your own recognizer

Subclass the conformance suite (in `Tests/EditMode/Conformance`) and feed it a
recorded fixture:

```csharp
public class WaveRecognizerConformance : RecognizerConformanceSuite
{
    protected override IGestureRecognizer CreateRecognizer() => new WaveRecognizer();
    protected override IEnumerable<GestureFrame> GetSampleFrames() =>
        FixtureCodec.Read(File.ReadAllText("path/to/wave.gframes"));
}
```

See `Documentation~/authoring-a-recognizer.md` for the full authoring guide.
