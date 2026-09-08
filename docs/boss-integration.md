# Residential Baxter integration research

The opt-in profile keeps the native Stage 12 scene, boss cutscenes, Baxter,
six laser objects, collision, camera path, and stage completion infrastructure.
It changes the visual presentation separately and transfers three existing Foot
Soldier actors into one new activation group. It does not create another Baxter.

`ResidentialRuntime.Install(Harmony, Assembly game, Assembly engine, Action<string>)`
is called only when the parent launcher selects the residential profile. The
normal save-suppression hooks must remain installed.

The native GameInfo.CurrentStageData setter accepts a StageData selected from
StageList.Items. The profile matches Episode 1 by its complete scene path and
substitutes the original Stage 12 StageData, preserving its associated metadata.
There is no verified startup argument for level selection. Native scene paths are
case-sensitive in GetStageDataByScenePath, so selection compares normalized paths
and retains the actual StageData object.

Scene2d.ForcedSpawnPos is honored both by player spawn and BeatEmUpCamera.Reset.
The latter snaps to the nearest waypoint and disables earlier camera blocks using
the game's own checkpoint behavior. The residential route starts at
`(4250,360,0)` and follows the existing camera path toward Baxter. Its Reset
postfix verifies the six exact intermediate camera blocks, then applies the
native checkpoint end callback, disabled flag, and camera-list removal to blocks
11 through 16. A matching TriggerBlock prefix also prevents the trigger-volume
entry path from starting those encounters. Boss and post-boss blocks remain.
The former `(5900,360,0)` short approach remains research evidence, not the
implemented start. The longer route still requires runtime collision and camera
verification. No game was launched during this implementation.

The boss camera block's native sequence is Hop, BossIntro, BossBanner, BossFight.
At its PostReset prefix, all original identities and memberships are preflighted.
Three existing FootShortMelee actors are transferred out of their former groups,
positioned within the boss neighborhood, and placed in a new wave after Hop.
Native PostReset deactivates them; native InternalStartWave resets and activates
them. Threshold zero and delay one require defeating these soldiers before the
boss introduction. All native boss waves remain in order.

Baxter.Reset discovers normal and frenzy lasers from the entire scene, including
objects outside the BossFight group. Keep those objects and their native positions.
Baxter also has capsule positions, mouser spawn positions, attack points, and
animation dependencies. Moving only the boss into an unrelated scene is therefore
not an equivalent substitute. IsBossDeathCompleted is available for verification;
retain the native death and level completion logic.

`ResidentialRuntimeTests.cs` is a separate synthetic runner, **not a launcher
source input**. Its native-shaped stand-ins verify actor transfer, wave insertion,
and rollback on insertion failure. It also verifies route identity preflight,
repeated reset behavior, and preservation of the boss camera. It does not
establish in-game compatibility.

The optional `tools/boss-probe/BossProbe.cs` extends the bounded base-record
inspection pattern to Baxter, BaxterLaser, FootShortMelee, and PlayerSpawnPoint.
Compile with Framework csc, run `--self-test`, then supply game root, scene .zpbn,
and the reader type. It executes trusted native readers without launching the
game. Redirect observations to ignored artifacts; no native report or game asset
is distributed in this repository.
