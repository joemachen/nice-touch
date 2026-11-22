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

## Now: M1b, the game

Goal: the rally becomes a game you can win and lose.

Spec: specs/M1b-the-game.md, approved by Joe on 2026-11-21.

- [x] The rally reports two facts about the shot in flight: how far spin has
      bent it and how many wall or floor bites have sped it up
- [ ] Tuning values for lives, levels, the two level steps, every points
      value and the NICE TOUCH mark
- [ ] Scoring: what a winner pays, NICE TOUCH, the level multiplier
- [ ] The game: lives, six levels that get quicker, level clear with a life
      back and a bonus, GAME OVER, YOU WIN, and one click to play again
- [ ] A recording holds a whole game with its final score, lives and level,
      and replays to them
- [ ] On screen: score, level and both sides' lives, LEVEL n, the points
      breakdown, NICE TOUCH with its beep, LEVEL CLEAR, GAME OVER, YOU WIN
- [ ] The new values join the tuning panel
- [ ] Scripted games played at every level, with the numbers reported to Joe
- [ ] QA handoff to Joe

Exit test: `test.bat` ends with PASS and CI is green, Joe saves a recording
of a whole game that replays in a test to the same final score, and he
answers yes to every item on the checklist in the spec.

## Next: M2, juice and shell

Goal: the game feels and sounds finished enough to hand to someone else.

- Hit pause, shake, trails, real sound, menus, settings, local high scores
- Windows export and a release pipeline
- Ends with the first build shared with outside players (v0.1) as a quiet
  playtest on an unlisted page, not a launch

Exit test: written in the M2 spec.

## Later

In intended order. Details get written when each becomes Next.

- **M3, the run.** Opponent ladder, upgrade drafts, seeded runs, carry-over
  lives. The first run is capped at a small fixed set (about 8 upgrades and
  5 opponents). First balance pass from playtest feedback. Tuning moves to
  data files here.
- **M4, bosses.** Named opponents with their own rule and a tell. Start with
  three.
- **M5, ship.** Store page, trailer capture, final art and audio pass, v1.0.

## Done

- **M0, foundation.** Signed off by Joe on 2026-11-21. The game builds, tests
  and runs with one command each, and CI runs the same checks on every push.
  Spec: specs/M0-foundation.md.
- **M1a, the rally.** Signed off by Joe on 2026-11-21. You can serve, rally
  against a placeholder opponent and put spin on the ball, with a tuning
  panel and an input recorder. Joe answered yes to all nine items on the
  feel checklist, and his saved recording replays exactly in a test. Gravity
  was added during QA and his tuning became the defaults (gravity 7, swipe
  spin 2). Spec: specs/M1a-the-rally.md.

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
| 2026-11-21 | The projects target .NET 10 | .NET 8 went out of support this month, and Godot 4.7.1 builds and runs the game on .NET 10 |
| 2026-11-21 | CI runs on Windows and calls `test.bat` | One definition of green, shared by Joe's machine and GitHub |
| 2026-11-21 | M1a spec approved. Spin is a physical model: besides bending the flight it changes how the ball comes off a wall and makes it jump off a still paddle | It gives a player with a nice touch more to do than bend shots. Every effect can be turned down to zero in the tuning panel, so the simple version is one setting away |
| 2026-11-21 | The serve is from the paddle: the ball rides on it until the click | The player can serve from anywhere in the tunnel, with a swipe |
| 2026-11-21 | Tuning values are remembered between runs in a local file that is not committed. The screen shows "CUSTOM TUNING" while any value differs from the defaults | Tuning takes several sittings. The notice stops stale values passing unnoticed |
| 2026-11-21 | The recorder keeps every input since the rally session began. Replaces "the last 30 seconds" | Simpler and exact: a replay has to start from a known state |
| 2026-11-21 | DESIGN.md gains a "Spin" section summarising the M1a rules | The centre of the game should be described in the design, not only in a milestone spec |
| 2026-11-21 | Gravity added to M1a as a tuning value that starts at zero. Changes the approved spec | In first QA Joe found the ball floaty, "like it's missing some gravity". At zero nothing changes, so he can decide by feel whether it stays. If it does, up and down stop behaving like left and right, and DESIGN.md will say so |
| 2026-11-21 | Gravity stays, with a starting value of 7. DESIGN.md gains a "Gravity" section | Joe tried it in QA and 7 "feels like good starting gravity". In scripted rallies at 7 the ball almost never lost its bounce, so no extra rule for lift is needed yet |
| 2026-11-21 | M1b spec approved as written | Joe read it and agreed to the three choices below |
| 2026-11-21 | Points come only from winners, and spin is what raises them: bend, wall and floor kicks, and NICE TOUCH. A small bonus for each hit in the rally | Points for every return would reward keeping the ball in play, not a nice touch |
| 2026-11-21 | Five lives, with one back for each level cleared | No lives back allowed 5 misses across about 18 winners, which looked too harsh against Joe's recording. Refilling every level looked too soft. All three numbers are tuning values |
| 2026-11-21 | Six levels with an ending (YOU WIN), not an endless score chase | An ending answers "can I win". Endless escalation belongs to the run (M3) |
| 2026-11-21 | DESIGN.md gains a "Scoring" section | The spec promised it, and scoring is where the game says what it values |
