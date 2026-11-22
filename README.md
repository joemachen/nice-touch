# Nice Touch

Working title. An original 3D tunnel pong game about spin: swipe the paddle
through the ball to bend the shot. It will grow into a roguelike run with boss
opponents.

**Status:** the rally (milestone M1a) is built and signed off. You can serve,
rally against a placeholder opponent and put spin on the ball. There is no
score, no lives and no levels yet: those are the next milestone, M1b. See
[ROADMAP.md](ROADMAP.md) for what is being built now.

## Run it

- **`run.bat`** builds the game and opens it. Move the mouse to move the
  paddle and click to serve. F1 opens the tuning panel, F9 saves a recording
  to `recordings/`, R restarts the rally and Esc quits.
- **`test.bat`** builds, runs the rules tests, then boots the game with no
  window to check it starts. The last line says PASS or FAIL.

## Documents

- [DESIGN.md](DESIGN.md): what the game is and how the code is shaped
- [ROADMAP.md](ROADMAP.md): what gets built when, and the decision log
- [CHANGELOG.md](CHANGELOG.md): what changed, in tester's terms
- [CLAUDE.md](CLAUDE.md): the working agreement
- [specs/](specs/): one short spec per milestone

## What you need to build it

| Tool | Version | Notes |
|---|---|---|
| Godot | 4.7.1 stable, .NET build | Expected at `D:\Projects\Godot\Godot_v4.7.1-stable_mono_win64\`. Set the `GODOT` environment variable to the editor executable if it lives elsewhere. |
| .NET SDK | 10 | |

## Licence

No licence has been chosen yet, so all rights are reserved for now.
