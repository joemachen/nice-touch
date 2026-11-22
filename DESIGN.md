# Nice Touch: design

Working title. An original 3D tunnel pong game about spin, grown into a
roguelike run with boss opponents.

It takes its starting point from the tunnel-pong Flash games of the mid-2000s.
It is not a remake of any of them: the rules, numbers, levels, scoring and
look are our own.

This file says what the game is and how the code is shaped. ROADMAP.md says
what gets built when. If the two disagree, fix one of them in the same change.

## The name

"Nice touch" is what the game calls out when the player makes a high-value or
outrageous play. It comes from basketball: a shooter with a nice touch puts
the right spin on the ball, so it kisses off the backboard or rolls around the
rim and still drops, and you can see the rotation doing the work.

That is the game's promise. Spin is real, it is visible, and good players
have a nice touch.

## Pillars

1. **The swipe is the game, and spin is what it makes.** One verb: move the
   paddle through the ball at contact. That puts spin on the ball. Spin bends
   the flight and changes how the ball comes off a wall. Every feature must
   make spin more interesting. A feature that distracts from it is cut.
2. **Readable at speed.** The player can always tell where the ball is in
   depth, where it will be, how it is spinning, and why they lost the point.
3. **Earned, then broken.** Start as an honest skill game. Let a run's
   upgrades escalate until the player is throwing shots that should not be
   legal.
4. **Opponents are characters.** Bosses have personalities and tells that can
   be learned, in the spirit of Punch-Out. Losing to one teaches something.

## Spin

The full rules are in specs/M1a-the-rally.md. In short:

- The ball carries two kinds of spin: side spin, and top spin (back spin is
  top spin the other way round). Spin is a real quantity the ball has, and
  the ball on screen rotates exactly that fast.
- **The swipe makes it.** A paddle's sideways speed at contact rubs spin onto
  the ball, up to a cap, and also pushes the ball a little the same way. So
  a swipe to the right sends the ball right and the spin bends it back left.
- **It bends the flight**, all the way down the tunnel. A faster ball follows
  the same shaped curve and just gets there sooner.
- **It fades**, by a half-life.
- **It bites the walls.** A ball with no spin bounces cleanly. A ball that
  curved into the wall comes off faster down the tunnel. One spinning the
  other way checks up and comes off slower. The bite uses up some spin, and
  the speed change lasts until the next paddle hit, inside set limits.
- **It bites the paddles.** A spinning ball jumps sideways off a still paddle
  and keeps part of its spin, so returning heavy spin cleanly means swiping
  against it. The rule is the same for the player and the opponent.

Every one of these effects has a strength in the tuning values that can be
turned down to zero.

## Gravity

The ball has a little weight. It is pulled gently towards the floor, so it
arcs and bounces off the floor on its way down the tunnel, where top and
back spin bite. A ball with no bounce left rolls. This makes up and down
different from left and right: side spin plays off the side walls now and
then, top and back spin play off the floor on most shots.

## Scoring

The full rules are in specs/M1b-the-game.md. In short:

- **Only a winner scores.** Points are paid when the opponent misses, never
  for a return. Missing costs a life and no points.
- **Spin is what raises the price.** A winner pays a flat amount, a little
  for each paddle hit in the rally, and then for style: *bend*, how far spin
  carried the winning shot off a straight line, and *kick*, each wall or
  floor bite that sped it up. Gravity's drop is not bend.
- **Nice touch.** When the style on a winner reaches a mark, the game calls
  out NICE TOUCH and the style points count double.
- **Later levels pay more.** The total is multiplied by the level number,
  and clearing a level pays a bonus.
- **The player sees the sum.** After every winner the screen lists what was
  paid for, line by line. That is pillar 2 applied to the score.

Every amount is a tuning value.

## The three layers

Built in this order. Each layer ships playable before the next starts.

1. **The core game.** A tunnel, a ball, two paddles and spin, with lives,
   levels and scoring, at modern frame rates with modern feedback (sound, hit
   pause, shake, trails). Tuned by feel in QA, not matched to another game.
2. **The run.** A run is a ladder of opponents. After each win the player
   drafts one of three upgrades (more spin, slower spin decay, wider paddle,
   multiball, and so on). Lives carry across the run. Lose them all and the
   run ends.
3. **Bosses.** Every few rungs the opponent is a named character with a rule
   of its own and a tell that reveals how to beat it.

Arcade mode (layer 1 alone) stays in the game for good.

## Out of scope

- Online multiplayer.
- Mobile and touch.
- A level editor.
- A browser build. C# Godot projects cannot export to the web in a stable
  release, so the game is a Windows download.

Reopening any of these is a roadmap decision, logged there.

## Architecture

Two parts with a hard wall between them.

```
src/NiceTouch.Sim/         Pure C# class library. No Godot references.
                           All rules: ball, spin, paddles, opponent AI,
                           scoring, run and upgrade logic. Tuning values too.
src/NiceTouch.Sim.Tests/   xUnit tests for the sim.
game/                      Godot 4 (.NET) project. Rendering, input, audio,
                           UI, menus. Feeds input to the sim, draws its state.
```

Rules:

- **The sim never knows it is being drawn.** It takes inputs and a tick, and
  returns state. That is what makes it testable without launching the game.
- **Deterministic on one machine.** Same build, same seed and same inputs give
  the same match, every time, on the same computer. Randomness comes from one
  seeded generator owned by the sim. Identical results across different
  computers are not promised, because nothing planned needs them.
- **Recorded inputs.** The game records every input since the game in hand
  began and saves it to a file on a key press, with the tuning values in use
  and the score, lives and level reached. A recording (plus the seed, once
  there is one) replays the game exactly, which turns a QA bug report into a
  failing test.
- **Time.** The sim advances 60 ticks a second, driven by Godot's built-in
  fixed tick. Drawing smooths between ticks, so it looks right on any display.
  Speeds are stored per second, never per tick. Values that fade (spin decay,
  paddle smoothing) are stored as half-lives: "loses half its strength every
  X seconds".
- **One place for tuning.** Tuning values and level tables live in one C# file
  in the sim until the run arrives (M3). When opponents and upgrades multiply
  they move to data files, so adding one does not need a new class unless it
  needs a new rule.
- **Godot side stays thin.** No game rules in scene scripts. If a scene script
  contains an `if` about scoring or physics, it is in the wrong place.
- **A real 3D scene.** The tunnel is drawn with a normal perspective camera.
  Depth comes from the camera, not from a scaling trick.
- **Mouse by window fraction.** The cursor's position as a fraction of the
  window maps onto the tunnel face, so the same swipe gives the same spin at
  any resolution. The cursor is confined to the window during play.

## Open questions

Tracked in ROADMAP.md under "Decisions needed", not here.
