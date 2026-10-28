# Smooth arrival braking

FD's latest cruise speed (8 m/s), destination `(186.5, 5.09909, 330.94418)` and
maximum driving volume (0.45) are preserved. `Braking Distance` is 12 m, giving
approximately a 3-second stop at this speed. Zero disables arrival braking.

`SmoothRideStop` analytically integrates a smooth velocity profile from cruise
speed to zero. Acceleration starts and ends at zero; the car stops in finite
time without creeping toward the endpoint. This applies to both rounded routes
and legacy straight routes. Restart resets braking; pausing freezes its progress.

Driving volume multiplies the existing screen-reveal gain by `CurrentSpeed / speed`.
It fades with braking, reaches zero at rest, and then pauses the source. This
avoids the previous abrupt full-volume cutoff.

Checks passed: exact 12 m braking distance, nonincreasing speed, finite stop,
zero final speed, long frames and 30/60/144 FPS, alongside existing route checks.
Runtime scripts compiled successfully. No live visual/audio Play Mode check was run.
