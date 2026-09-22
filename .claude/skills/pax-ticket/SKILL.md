---
name: pax-ticket
description: Run one approved PARALLAX ticket end to end — pre-flight, as-built trace, tests first, implementation, verification, an internal review loop with the pax-reviewer agent, and a clear handover for the developer. Use when the developer says "run PAX-0xx" or invokes /pax-ticket with a ticket path.
disable-model-invocation: true
argument-hint: <ticket path, e.g. Docs/0_TASKS/PAX-050.md>
---

# Run a PARALLAX ticket end to end

Ticket: $ARGUMENTS

You are the implementer. The goal is to hand the developer a finished, reviewed ticket in one
run, stopping early only when a real question needs them. You never commit, never write `.git`,
never run setup menus, never enter Play mode, never touch scenes, prefabs, `.asset`, `.meta`
or build settings. Those are the developer's steps.

## 0. Read

1. `Docs/07_DECISIONS.md` (newest entry wins), `CLAUDE.md`, the `Docs/02_ARCHITECTURE.md`
   sections the ticket touches, then the ticket.
2. If the ticket's status is not approved, or it cites a decision that isn't in
   `07_DECISIONS.md`, stop and say so.

## 1. Pre-flight — stop if any fails

- `git log --oneline -5`: the ticket's "Depends on" is committed.
- `git status`: no unrelated modified files you would have to stage around; in particular
  `Sandbox_Realities.unity` is not modified. Untracked files from other tickets (e.g. art
  tooling) are fine; list them so they are kept out of this commit.
- `run_tests` (EditMode): record the baseline total. It must match the ticket's baseline.

## 2. As-built trace

Answer every Phase 1 question the ticket asks, briefly, with file:line references. Also trace
every existing path the change touches (callers, events, per-tick order, other kill or reset
paths). Past tickets found hidden paths this way, e.g. a `FixedUpdate` kill path nobody knew about.

- If the ticket says to wait after Phase 1, post the trace and stop.
- If an answer contradicts the ticket, needs a file outside the allowed list, or needs a new
  decision: post the trace, list the options with a recommendation, and stop.
- Otherwise post the trace and continue.

## 3. Tests first

Write every test the ticket requires (and any you need for a trace finding) before the
implementation. Run them. Each new test must fail, and for the reason the ticket names, not
because a type is missing. Record the failing output. A test that can't fail for the right
reason gets rewritten, not kept.

## 4. Implement

- Only the ticket's allowed files. Anything else: stop and ask.
- Follow `CLAUDE.md` and the rules in the ticket. Pure logic goes in `Parallax.Core`.
- Repetitive scene wiring goes in an idempotent `PARALLAX/Setup/...` menu with a scene guard
  that refuses to run outside its target scene.

## 5. Verify

`refresh_unity` (with compilation) → `validate_script` on changed files → clear the console →
`read_console` → `run_tests`. Total must be baseline + new tests, all green. Quote relevant
console lines; known noise is listed in `CLAUDE.md`.

## 6. Review loop

Invoke the `pax-reviewer` agent with the ticket path. Then:
- Fix every Blocker and Should-fix that is inside the ticket's scope, each with a failing-first
  test, and re-verify (step 5).
- A finding that needs out-of-scope work: don't fix it; carry it to the handover.
- At most two review rounds. If Blockers remain after two, stop and report them.

## 7. Handover

1. The review file, exactly as the ticket specifies (usually
   `~/Desktop/pax0xx_review.txt`).
2. The `CLAUDE.md` required output.
3. The final `pax-reviewer` verdict, and what you fixed from each round.
4. **Developer steps**, written for someone doing them in the Unity Editor for the first time:
   - numbered steps, each with numbered sub-steps;
   - exact window and menu paths (e.g. "In the top menu bar, click **PARALLAX → Setup → …**");
   - before any setup menu: "Check the top line of the Hierarchy says `<scene>`";
   - after each action, what the developer should see ("the Console shows …", "the Hierarchy
     has …");
   - a concrete play plan with known inputs and expected results (e.g. "die 2 times in room 1;
     the screen shows `Room 1 — 2 deaths`");
   - the `git status` check: which files should and must not appear;
   - the exact `git add` paths and commit message for the code commit, with docs in a separate
     commit;
   - finally, what to send back (numbers, log lines, `git status`).
5. Doc updates the change needs (`02_ARCHITECTURE.md`, `CLAUDE.md`), as exact text for the
   developer's separate docs commit. Don't edit docs unless the ticket allows it.

Then stop.

## Rules that always hold

- Never claim something was tested in Play mode or on a device. EditMode and MCP checks are not
  device validation.
- Say "not verified" when you couldn't verify something.
- If you deviate from these steps, say where and why in the handover.
