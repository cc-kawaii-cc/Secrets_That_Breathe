# Final Gameplay

This folder contains the production Lawyer Room gameplay. It deliberately does
not depend on the legacy Main1 player, managers, or interaction scripts.

`LawyerRoomRuntimeInstaller` connects the scene from exact hierarchy paths when
Lawyer Room loads, so it never guesses between similarly named objects. The UI
and gameplay components are created automatically and only for that scene.

For a permanently serialized copy of the same setup in the scene, run
`Tools > Secrets That Breathe > Setup Lawyer Room Gameplay`. Re-run that command
after changing any of the prepared scene objects or marker names.
