# M0: foundation

Status: **approved by Joe, 2026-11-21.**

## Goal

An empty game that builds, tests and runs with one command each, so every
later change lands on rails. Nothing in M0 is fun. It proves the plumbing.

## What gets built

1. **Three projects that build together with one command.**
   - `src/NiceTouch.Sim`: the rules library. No Godot references.
   - `src/NiceTouch.Sim.Tests`: its xUnit tests.
   - `game/`: the Godot project, which uses the rules library.
2. **The smallest possible sim.** A ball with a position and a speed, and a
   step that moves it forward one tick. The tick rate (60 a second) is one
   named value in the sim.
3. **One real test.** After one tick, the ball has moved by its speed divided
   by 60.
4. **The wiring between the two halves.** Godot's fixed tick, set to the sim's
   rate, steps the sim. A small line of debug text in a corner of the window
   shows the sim's tick count going up. It exists to prove the game is
   really running the rules library, and it is the only thing on screen that
   moves.
5. **An empty tunnel.** A real 3D scene: a rectangular tunnel seen from the
   player's end through a perspective camera. Plain placeholder look. No
   ball, no paddles.
6. **`run.bat`.** Builds the game and opens it (the game, not the Godot
   editor). It finds Godot at the path in README.md, or at the `GODOT`
   environment variable if that is set, and says so clearly if it cannot.
7. **`test.bat`.** Runs the sim tests, then the boot smoke test. Prints one
   clear PASS or FAIL at the end.
8. **Boot smoke test.** Starts the game with no window, lets it run for a
   couple of seconds of ticks, and checks it exits cleanly with the tick
   count above zero.
9. **CI.** On every push to main, GitHub builds everything, runs the sim
   tests and runs the boot smoke test. A green tick or red cross shows next
   to each commit on GitHub.

## Rules

- The sim projects fail the build on any warning. The Godot project does not,
  because Godot generates code we do not control.
- **.NET version.** .NET 8 went out of support this month, so the projects
  target .NET 10, which is supported until late 2028. The first task checks
  that Godot 4.7.1 runs the game on .NET 10. If it does not, the projects
  target .NET 8 instead and the reason goes in the decision log.
- **Dependencies added, with their reasons:**
  - xUnit, to run the sim tests (named in the working agreement).
  - A GitHub action that installs Godot on the CI machine, so the boot smoke
    test can run there.
- The window opens at 1280 x 720 and can be resized. The tunnel stays
  centred and in proportion when it is.

## Exit test

On a fresh clone of the repo:

1. `test.bat` ends with PASS.
2. `run.bat` opens a window showing the tunnel, with the tick count rising by
   about 60 a second.
3. The latest commit on main has a green tick on GitHub.

Then Joe signs off.

## Out of scope

- Ball on screen, paddles, mouse input, spin, any game rule
- The seeded-match determinism test. There is no match to play yet, so it
  arrives with M1a.
- Sound, menus, settings
- Tuning panel and input recorder (M1a)
- An exported `.exe` and the release pipeline (M2). In M0 the game runs from
  source through `run.bat`.

## Known risks

- Running Godot without a window on GitHub's machines can be slow or flaky.
  If the boot smoke test proves unreliable in CI, I will raise it with
  options. I will not quietly remove it.
- If Godot 4.7.1 does not accept .NET 10, we fall back to .NET 8 as above.

## How Joe will test it

The QA handoff at the end of M0 will give the exact commit and these steps:

1. Double-click `test.bat`. Expect PASS on the last line.
2. Double-click `run.bat`. Expect a window with an empty tunnel and a tick
   count in the corner that keeps rising.
3. Resize the window. Expect the tunnel to stay centred and undistorted.
4. Close the window. Expect it to close at once with no error.
5. Open the repo page on GitHub. Expect a green tick beside the latest
   commit.
