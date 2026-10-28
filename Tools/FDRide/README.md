# FD opening ride

Updated `Assets/Nemo_Upgrade/MainScene_2_Evening.unity` directly without screen/input control.

- Enabled FD's copy of the existing `PorschePassengerRide` controller. The class is vehicle-independent despite its original name.
- Copied the existing route, speed (4), camera, player, view limits and suspended-control references.
- Added `FD/DriverSeat` at local `(-0.3143174, 1.13, -0.0620665)`, 10 cm above the original eye height. X/Z are based on the FD steering-wheel mesh bounds, with the eyes 55 cm behind the wheel.
- Placed FD at the existing route start. Disabled the Porsche root and its old ride component, retaining both for recovery.
- Structural validation passed: unique IDs, valid new references, exactly one enabled ride, and copied route/player settings unchanged.
- Play Mode and the rendered seating view have not been verified. Unity must import the changed scene when it next regains focus.

Original saved scene backup: `BeforeFD-20261026-230600.unity`.
`validation.json` contains the edit checks. `SwitchToFD.ps1` is the one-use scene edit and refuses to apply twice.
The unused editor-based alternative is archived as `FDPassengerRideSetup.cs.txt`; it does not compile or run.

## Extended smooth left turn

The active FD in `Assets/MainScenes/From_PM/MainScene_2_Evening.unity` now drives
past the old stop, across the bridge, toward the T junction. The Roads parent is
at `(212.2, 5.09909, 240.5)` and the horizontal road centers are at world
`z = 331.944176`. The westbound lane uses `z = 330.94418`.

- Start: `(209.3862, 5.09909, 127.37)`.
- Corner waypoint: `(209.3862, 5.09909, 330.94418)`.
- Rounded corner trim: 5 m; heading response: 6.
- Final point: `(140, 5.09909, 330.94418)`, safely before the west road edge.
- Distance: about 271.08 m. Speed remains the user's 4 m/s.
- The vehicle stops at the final point; this is a finite route, not an infinite loop.

`PassengerRidePath` replaces sharp corners with sampled quadratic Bezier curves.
Distance lookup maintains speed through curves; interpolated tangents and
frame-rate-independent rotation smoothing turn the car and attached camera.
Empty waypoint lists retain the original straight-route behavior.
Select FD to see the route gizmo; edit Intermediate Route Points, Corner Radius,
Heading Response and End Position on its ride component. Restart Ride applies
route edits during Play Mode.

The route geometry checks in `PassengerRidePathChecks.cs` ran successfully outside
Unity: straight fallback, distance clamping, westbound final heading, constant
travel increments, continuous turn headings, bounded corner geometry, short
segments and repeated points. Runtime scripts compile against the project's
Unity references. No live Play Mode or collision/visual drive-through was run.
