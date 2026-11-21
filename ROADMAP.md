# Roadmap

A living document. It is updated in the same commit as the work it describes.

**How it works**

- **Now** is the one milestone in progress. Its tasks are specific and ticked
  off as they merge.
- **Next** is the milestone after that, described by its goal and exit test.
- **Later** is deliberately vague. It is not planned in detail until it
  becomes Next, because playtesting will change it.
- A milestone is done only when its exit test passes and Joe signs off in QA.
- Anything cut, deferred or reversed goes in the decision log with a reason.

Last updated: 2026-11-21

## Now: M0, foundation

Goal: an empty game that builds, tests and runs with one command each, so
every later change lands on rails.

Spec: specs/M0-foundation.md (approved 2026-11-21).

- [x] Confirm the Godot version and record it in README.md
- [x] Create the Godot project in `game/` and the solution with
      `NiceTouch.Sim` and `NiceTouch.Sim.Tests`
- [x] The sim ticks 60 times a second, driven by Godot's fixed tick
- [x] One real sim test passing (ball advances one tick)
- [ ] `run.bat` launches the game, `test.bat` runs the tests
- [ ] Boot smoke test: the game starts without a window, runs briefly and
      exits cleanly
- [ ] CI on GitHub Actions: build, test and boot smoke test on every push
- [x] A window opens showing the empty tunnel

Exit test: fresh clone, `test.bat` is green (sim tests and boot smoke test),
`run.bat` opens the tunnel, CI is green on main.

## Next: M1a, the rally

Goal: find out whether the swipe feels good, as early as possible.

- Ball, both paddles, spin, walls, serve and miss
- Spin that bends the flight, changes the bounce off a wall and can be seen
  on the ball
- A debug tuning panel so feel can be tuned in QA without code changes
- The input recorder (a key saves the last 30 seconds of play to a file)
- Placeholder visuals and sound are fine

Exit test: sim tests green, and Joe can rally, bend shots, and answer yes to
every item on the feel checklist written in the M1a spec.

## Later

In intended order. Details get written when each becomes Next.

- **M1b, the game.** Lives, levels, scoring, score and lives display, level
  clear, game over, restart. The "Nice touch" call-out for big plays.
- **M2, juice and shell.** Hit pause, shake, trails, real sound, menus,
  settings, local high scores. Windows export and a release pipeline. Ends
  with the first build shared with outside players (v0.1) as a quiet playtest
  on an unlisted page, not a launch.
- **M3, the run.** Opponent ladder, upgrade drafts, seeded runs, carry-over
  lives. The first run is capped at a small fixed set (about 8 upgrades and
  5 opponents). First balance pass from playtest feedback. Tuning moves to
  data files here.
- **M4, bosses.** Named opponents with their own rule and a tell. Start with
  three.
- **M5, ship.** Store page, trailer capture, final art and audio pass, v1.0.

## Decisions needed

- [ ] Final name. "Nice Touch" is still a working title, though it now has a
      reason behind it (see DESIGN.md, "The name").
- [ ] License for this repo (none chosen yet, so all rights reserved for now)
- [ ] Where v0.1 is distributed (an unlisted itch.io page is the default
      assumption)

## Decision log

| Date | Decision | Why |
|---|---|---|
| 2026-11-21 | Godot 4 with C# | Standalone exe, already installed, rules testable outside the engine |
| 2026-11-21 | Build order: core, then run, then bosses | Protect the swipe before adding anything around it |
| 2026-11-21 | No online multiplayer | Netcode cost and solo QA burden far outweigh the value |
| 2026-11-21 | Original SWF, art and sound never enter the repo | Not ours |
| 2026-11-21 | An original game, not a parity rebuild of an existing one. Reverses "faithful core", the parity tests and "Classic mode" | A public clone can be taken down at release, after the work is done. Swipe-to-curve is a genre mechanic; the numbers, levels, scoring and look are ours |
| 2026-11-21 | The notes on the original's mechanics stay on Joe's machine and out of the repo (`docs/reference/` is git-ignored). Reverses "rules and numbers are documented instead" | They are background, not the spec, and a public repo should not carry them |
| 2026-11-21 | Spin is the centre of the game: it bends flight, changes wall bounces and is visible. "Nice touch" is the call-out for big plays | Joe's direction. It is what makes the game its own |
| 2026-11-21 | M1 split into M1a (the rally) and M1b (the game) | Everything rests on the swipe, so Joe should have it in his hands first. Costs one extra sign-off |
| 2026-11-21 | M1 exit test is a written feel checklist, not "feels like the original" | A sign-off needs yes/no questions |
| 2026-11-21 | Sim runs 60 ticks a second on Godot's fixed tick. Decided now, not in M1 | M0 builds the loop. Using Godot's tick avoids writing our own |
| 2026-11-21 | Fading values are stored as half-lives | Per-second multipliers come out unreadable |
| 2026-11-21 | Determinism is promised on one machine only | Matching results across computers is expensive and nothing needs it |
| 2026-11-21 | Input recorder added to M1a | Determinism only reproduces a bug if the inputs were recorded |
| 2026-11-21 | Tuning lives in one C# file until M3. Defers "data over code" | Avoids a loader, a file format and an export packaging problem before there is enough data to need them. Costs a small refactor in M3 |
| 2026-11-21 | Boot smoke test added to M0's exit test and CI | It is the only automated check on the Godot half |
| 2026-11-21 | Windows download only, no browser build | C# Godot projects cannot export to the web in a stable release. Staying with C# keeps the tested rules library |
| 2026-11-21 | Real 3D scene, mouse mapped by window fraction, cursor confined during play | Same swipe gives the same spin at any resolution |
| 2026-11-21 | v0.1 is a quiet playtest on an unlisted page | The run is the hook, and it does not exist yet at M2 |
| 2026-11-21 | First run capped at about 8 upgrades and 5 opponents | One tester cannot balance an open-ended set |
| 2026-11-21 | Working agreement: changelog once per session, failing-test rule for sim bugs only, warnings fail the sim build, straight to main, QA handoff names the commit, bug report format, asset credits | See CLAUDE.md. Each removes noise or closes a gap found in the plan review |
