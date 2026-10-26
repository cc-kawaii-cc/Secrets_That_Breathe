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
