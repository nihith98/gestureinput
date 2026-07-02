# Live Webcam Demo

Manual QA harness + fixture recorder for GestureInput.

## Prerequisites

- The **MediaPipe Unity Plugin** installed (see the package README — it comes from
  GitHub Releases and cannot be auto-installed).
- The `gesture_recognizer.task` model bundle in `Assets/StreamingAssets/`
  (download link in the package README).
- A webcam.

## Setup

1. Create a new empty scene.
2. Create an empty GameObject and add **DemoBootstrap** — it wires up the
   runtime, driver, HUD, skeleton overlay, and recorder.
3. Press Play.

## What you get

- **Green skeleton** — the 21 hand landmarks the recognizers actually see.
- **Left panel** — live `<GestureDevice>` control values for every registered
  gesture, recent Began/Ended events with confidence, submitted/dropped frame counts.
- **Right panel** — the **Record** button. Perform a gesture, stop, and a
  `.gframes` fixture is written to `Application.persistentDataPath`. Copy good
  takes into `Tests/EditMode/Fixtures/` and replay them with `FixtureCodec.Read`
  in EditMode tests — that is how live behavior gets locked into CI.

## Binding gestures in your game

Any registered gesture is a normal Input System control:

```
<GestureDevice>/openPalm     <GestureDevice>/swipeLeft
<GestureDevice>/closedFist   <GestureDevice>/swipeRight
<GestureDevice>/thumbUp      <GestureDevice>/swipeUp
<GestureDevice>/thumbDown    <GestureDevice>/swipeDown
<GestureDevice>/victory      <GestureDevice>/pointingUp
<GestureDevice>/iLoveYou
```

or subscribe in code: `GestureRuntime.Instance.OnGesture += e => ...;`
