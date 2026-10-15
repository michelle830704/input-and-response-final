# Adjusted original Godot C# project

Open Godot 4.5 .NET, Import project.godot, Import & Edit, F5.
Use the .NET 8 SDK if Godot reports an SDK missing.

## Controls
- WASD / arrows: camera-relative movement.
- Q/E: orbit the third-person camera.
- 1/2/3: immediate / linear / ease movement modes.
- Space: jump with 0.03s wait + 0.06s squash, then launch/stretch.
- C (hold): crouch, shorten collider and slow movement; release stands only with headroom.
- [ / ]: reduce/increase apex hover time; initial 0.18s.
- R: reset to original start; falling below Y=-8 also resets.

Stop, select CharacterBody3D, edit exported settings in Inspector, save and run.
Mode and hover changes during play are temporary. The GFX turns smoothly using radians
while the CharacterBody3D stays unrotated. Gravity runs once each physics frame.
The 0.12s coyote window allows a jump shortly after stepping off a ledge.
Jump eligibility is reserved on press, so anticipation can finish after the window.
Crouching reduces Y scale (not a negative mirror) and keeps feet at the same height.

## Appearance and scope
Original materials, colours, character, model and existing level objects are unchanged; a crouch passage is added.
The existing Camera3D is reused as a follow/orbit camera; its near clip is corrected
from 10m to 0.1m at runtime. The view therefore differs from the old fixed camera.
A low passage has been added at X=0, Z=-6: yellow roof and cyan supports
reuse your original materials. Its 1.25m clearance blocks the standing 2m
capsule but admits the crouched 1m capsule. Other existing scene objects are unchanged.

## Test crouching
From the starting point, move towards the middle of the green floor, then towards
its far half (negative Z) to find the yellow roof. Try walking underneath standing:
you should be blocked. Hold C and walk through. Release C while underneath:
you should stay crouched until clear of the roof. Then release to stand normally.
R resets. WASD/arrows move, Q/E orbit, Space jumps; 1/2/3 select movement modes.

## Changes
- Game1/Scenes/Player.cs: rewritten controller, preserving scene bindings.
- project.godot: explicit main scene path for reliable ZIP import.
- game1.csproj: excludes debug-draw demo C# from game compilation.
- README-Adjusted.md: this guide.
Editor/build caches are omitted; Godot regenerates them when importing.

## Validation
Compared original material blocks and model bytes; preserved. Original scene nodes retained, new passage added.
Checked local scene resource references, project XML and ZIP integrity.
Godot/.NET unavailable here: compilation and gameplay NOT verified in this environment.
Timing defaults are starting values for tuning, not verified textbook prescriptions.
Compare against your course videos before submission.
