# Evening drive hitch investigation — 2026-11-07

Reported: short freezes while seated in the moving FD, in a Windows build.

Evidence checked:
- Latest Player.log: no repeated exceptions. Startup warnings concern the convex FD hull and RoadsideGrass_Mixed tree shader; neither proves the recurring freeze cause.
- Tenkoku module is disabled by the Evening scene override; its periodic reflection updates are not an established cause in this scene.
- CanvasTextStackLayout is authored in Lawyer Room, not Evening. No speculative layout change was made.
- Ride route is sampled once; Evaluate uses BinarySearch without new lists each frame.
- Incremental GC already enabled. Texture mip streaming disabled. Waiting for streaming counters alone does not warm newly visible geometry/shader passes.

Changes:
- EveningDriveIntro renders route viewpoints to a temporary target while still black, before the text starts. Default 12 positions × left/forward/right. Uses the existing camera and restores its transform immediately; does not move the car/player or fire distance triggers.
- Warm Up Drive Views and Drive Warmup Positions are editable on the existing EveningDriveIntro component under SeatingCanvas. Disabling warmup provides an A/B comparison. Intro startup can take longer.
- Explicit temporary-target cleanup on F1/F2 cancellation and normal completion.
- RideFrameDiagnostics buffers up to 16,384 drive frames; no per-frame file output or text formatting. On quit/scene unload writes RideFrameTrace-MainScene_2_Evening.csv under Application.persistentDataPath. Includes wall frame delta, distance remaining, GC count/bytes, and main/render-thread counters if available (-1 otherwise). Frame counters represent the preceding completed frame and cannot independently establish a GPU cause.

Validation:
- Full current Assembly-CSharp response-file compilation passed against Unity 6000.3.8f1 references. Only existing warnings.
- Actual Player run, timing improvement, GPU shader warmup coverage, and live F1/F2 behavior have NOT been verified. First-use preparation is a suspected contributor, not a confirmed root cause. A new build and its frame trace are needed to establish the remaining cause if freezing persists.

Windows trace location:
`%USERPROFILE%/AppData/LocalLow/DefaultCompany/Secrets_That_Breathe/RideFrameTrace-MainScene_2_Evening.csv`

Compare identical routes in two fresh Player launches, warmup enabled/disabled, with no alt-tabbing. Inspect >33 ms frames and whether GC changes coincide. For exact CPU/GPU attribution, capture a Development Build in Unity Profiler.
