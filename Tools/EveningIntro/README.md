# Evening drive opening

Scene: `Assets/Nemo_Upgrade/MainScene_2_Evening.unity`

`EveningDriveIntroCanvas` is saved inactive, like the Lawyer Room intro. Its
scene-loaded callback activates it before the first rendered frame in this
specific scene. It uses the same Kanit-Medium SDF, material, white text, 40 pt,
center alignment, Thai text preprocessor and 0.05 s character reveal as Lawyer
Room's second intro. The text area is taller to accommodate the longer sentence.
The authored sentence remains editable in `EveningStoryText`.

Sequence (unscaled time): prepare three frames behind black; black wait 2 s;
type the sentence; hold the completed text 2 s; allow texture streaming to settle
for three frames (up to 10 extra seconds); text fade out 1 s; gameplay reveal 2 s.
There is no key prompt. Car movement resumes when story text has faded by 70%
(text alpha <= 0.3), while the screen is still black. The text then finishes
fading and the screen reveals over 2 s. Input resumes only after black opacity
reaches zero. The ride's direct mouse/gamepad look input explicitly checks this
intro lock, independently of PlayerInput being disabled.

FD interior driving ambience is multiplied by the screen's revealed fraction
(`1 - black alpha`): silent under black, gradually increasing with the picture,
and at its configured 0.45 volume when the screen is completely revealed.

`Time.timeScale` is zero until that movement threshold. The FD ride initializes its camera behind
black but cannot advance or process look input while paused. PlayerInput and
the phone flashlight are suspended; controls owned by the car ride are left to
that controller, so finishing the intro does not enable walking inside the car.
Input values are cleared and prior time scale restored on completion or abort.

Validation: Roslyn compilation against this project's Unity assemblies passed;
scene references and exact sentence checked; saved TMP styling matches Lawyer
Room; existing scene objects were not changed, apart from appending the new root.
No live Play Mode or visual rendering check was performed.

This covers initialization and texture streaming after scene activation. It is
not an asynchronous scene loader and cannot display before a synchronous scene
load itself completes. Later loading transitions can use a separate loading scene.

The original scene is backed up beside this file as `BeforeIntro-*.unity`.
`Tools/InstallEveningDriveIntro.ps1` reproduces the one-use scene setup.
