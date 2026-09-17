# PAX-036 — Amendment 1 review

## A. D-030 verification

Command:

```text
grep -n "D-030" Docs/07_DECISIONS.md
170:### D-030 · 2026-09-17 · Accepted
```

The same line exists in `HEAD`; D-030 was already tracked, so no decision-record block was copied.

## B. Initial spawn / checkpoint 0

Option (i) is implemented. `CheckpointSetup.BuildMarker` creates id 0 for each reality at its
cat-start X and its `Geometry/Ground` top, with `Vector2.down`. `CheckpointProgress.Current`
starts at zero, so the normal table lookup selects id 0 before any checkpoint has been activated.
The added `SpawnTable_RespawnWithNoCheckpointReached_UsesCheckpointZero` test asserts that lookup
returns `(0.00, -3.50)` and down gravity at current id zero. No scene was saved.

## C. Exact `FallResetVolume.cs` diff

```diff
@@ -1,3 +1,5 @@
+using Parallax.Core;
+using Parallax.Gameplay.Observers;
 using Parallax.Gameplay.Player;
 using Parallax.Gameplay.Reality;
 using UnityEngine;
@@ -7,11 +9,13 @@ namespace Parallax.Gameplay.Checkpoints
     [RequireComponent(typeof(BoxCollider2D))]
     public sealed class FallResetVolume : MonoBehaviour
     {
-        [SerializeField] SpawnPoint spawn;
+        [SerializeField] CheckpointManager checkpoints;
+        [SerializeField] ObserverSet observers;
 
         BoxCollider2D box;
         ContactFilter2D filter;
         readonly Collider2D[] results = new Collider2D[8];
+        RealityRoot reality;
@@ -24,14 +28,14 @@ void Awake()
             box = GetComponent<BoxCollider2D>();
             box.isTrigger = true;
 
-            if (spawn == null)
+            if (checkpoints == null || observers == null)
             {
-                Debug.LogError($"FallResetVolume: '{gameObject.name}' has no SpawnPoint assigned. Disabling.", this);
+                Debug.LogError($"FallResetVolume: '{gameObject.name}' has no CheckpointManager or ObserverSet assigned. Disabling.", this);
                 enabled = false;
                 return;
             }
 
-            var reality = GetComponentInParent<RealityRoot>();
+            reality = GetComponentInParent<RealityRoot>();
@@ -44,6 +48,8 @@ void Awake()
 
         void FixedUpdate()
         {
+            if (reality == null) return;
+
             int count = box.Overlap(filter, results);
             for (int i = 0; i < count; i++)
             {
@@ -53,7 +59,10 @@ void FixedUpdate()
                 var respawn = body.GetComponent<CatRespawn>();
                 if (respawn == null) continue;
 
-                respawn.RespawnAt(spawn.Position, spawn.GravityDirection);
+                ObserverContext context = observers.Get(reality.Id);
+                if (context == null || context.Driver == null || !CheckpointPolicy.FallResets(context.Driver.Kind)) continue;
+
+                checkpoints.Respawn(context);
             }
         }
```

`SpawnPoint` remains referenced by `Editor/CatPlayerSetup.cs` and `Editor/Setup/RealitySetup.cs`;
it is not orphaned.

## D. Test inventory

Static NUnit inventory (a `[TestCase]` is one runnable case): before 121 `[Test]` + 4
`[TestCase]` = 125; after 126 `[Test]` + 12 `[TestCase]` = 138. The PAX-036 additions are:

- `Progress_AdvancesForwardOnly`
- `SpawnTable_ReturnsHighestIdLessOrEqualCurrent`
- `SpawnTable_EntriesArePerObserver`
- `SpawnTable_NoEntryForObserver_ReturnsFalse`
- `SpawnTable_RespawnWithNoCheckpointReached_UsesCheckpointZero`
- `Policy_ActivatesOnlyLocalHuman` (four kinds)
- `Policy_FallResetsEveryoneExceptEchoReplay` (four kinds)

`git diff --name-status HEAD -- Assets/_Game/Tests/EditMode` and the deleted-file check both
produced no output: no tracked test was deleted, renamed, or had assertions removed.

## E. Mechanical greps

Searched every changed/new C# source: Core/Checkpoints, Gameplay/Checkpoints, both new Editor
files, the new test, and modified DebugPanel. The `?.` / `??` command produced no output.

```text
rg -n '\?\.|\?\?' <all new/changed C# paths>
(no output)

rg --files-without-match 'using Parallax\.Core;' <new Gameplay and Editor C# paths>
(no output)
```

The previous ambiguous `Find` check is restated precisely: `rg -n '\bFind\b'
Assets/_Game/Core Assets/_Game/Gameplay -g '*.cs'` produced no output.

## F–G. Runtime-policy audit

`CheckpointManager.Respawn` has no `ResetSequences` reference. Its observed operations are:
resolve the table spawn; release only the fallen cat's `CatSeat` when seated; obtain that cat's
`CatRespawn`; call `RespawnAt`. It does not touch `EventSequencer`, Echo, anchors, or the other
cat.

`InactiveDriver.Kind` returns `InputSourceKind.Inactive` in solo. The policy test now checks
LocalHuman, Inactive, RemoteHuman, and EchoReplay for both predicates; only LocalHuman activates,
and only EchoReplay is excluded from fall reset.

## H. Editor verification status

Not passed. Unity batch mode was invoked for EditMode tests, but Unity immediately aborted because
this project is already open in another Unity instance. Therefore there is no clean compile result,
no green Test Runner count, and setup/validator were not run or saved. The exact blocker was:

```text
Aborting batchmode due to fatal error:
It looks like another Unity instance is running with this project open.
```

`git diff --check` passed.

## Amendment 2 addendum

This report's former checkpoint-0 literal is superseded. Setup now reads each reality's existing
`SpawnPoint.Position` and `SpawnPoint.GravityDirection` verbatim; the serialized SpawnPoint local
Y in `Sandbox_Realities.unity` is `-3.10` for A and B. The no-checkpoint test compares table output
against its `SpawnPoint`, not `(0.00, -3.50)`.

Setup enumerates every `FallResetVolume` beneath each `RealityRoot` and compare-before-assigns both
`checkpoints` and `observers`; it does not touch bounds or positions. The id-0 validator checks
both realities, and §11 reserves id 0 for level spawn.

D-032 is already present in `Docs/07_DECISIONS.md`. `GravityControlReceiver` subscribes to its
cat's respawn event and, if it has a held stream value, snaps gravity back to that local value.
No publish occurs. Three receiver tests cover no held value, held stream value, and an Echo-style
held stream value. The rerun of Unity Test Runner was blocked before compilation because the
project remains open in another Unity instance; no Runner count is reported here.
