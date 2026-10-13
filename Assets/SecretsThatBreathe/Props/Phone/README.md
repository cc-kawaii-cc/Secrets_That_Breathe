# Phone with flashlight screen states

Use `Prefabs/Phone_Blank.prefab`. Size is 78 × 166.8 × 9 mm (Unity units are metres), with a centred pivot. The screen faces local +Z and the top points toward +Y.

Optimized geometry: **264 triangles / 239 vertices**, down from 2,220 triangles (about 88% fewer). The unused rear camera is removed. The body and screen use four segments per rounded corner; small buttons, earpiece, and flash LED use simple boxes. Screen textures, collider, grip anchor, and flashlight switching are retained.

The separate `Screen` mesh is 72 × 155.65 mm, matching the supplied 853 × 1844 portrait image. Its UVs span 0–1, with the image top at +Y. Rounded corners crop only the outermost corners of the image. Both supplied images are included in `Textures`: `Phone_Flashlight_Off.png` (first image) and `Phone_Flashlight_On.png` (second image). Their separate URP Unlit materials display the screen in dark rooms without relying on room lighting.

The prefab includes a BoxCollider for interaction raycasts, a `GripAnchor` at the centre, and a `FlashlightAnchor` on the back pointing toward local -Z. Its Spot Light starts disabled, with the first image visible. The `PhoneFlashlightScreen` component switches the screen and Spot Light together. Toggle **Flashlight On** in the Inspector to preview both states, or use the component context menu **Flashlight > Toggle / Turn On / Turn Off**.

For gameplay, connect a pickup/input controller or UI button to `ToggleFlashlight()`, `TurnOn()`, `TurnOff()`, or `SetFlashlightOn(bool)`. When an existing controller changes this phone's assigned Spot Light directly, the screen follows in LateUpdate. If using `FirstPersonFlashlight`, assign this phone's Light as its source. Each phone has independent state; shared material assets are never modified by runtime switching. Keyboard input and pickup are not implemented by this component. No scene or existing phone is replaced.

Meshes and neutral URP materials are separate assets. `Models/Phone_Blank.obj` is an additional editable geometry export in metres. The Unity prefab is the primary asset with materials, collider, and anchors.

Rebuild through **Tools > Phone > Build Blank Phone**. Body materials are preserved; the two screen materials are configured from the supplied images. Previews: `Preview/Phone_Flashlight_Off.png` and `Preview/Phone_Flashlight_On.png`. The original blank preview is retained as an earlier reference.
