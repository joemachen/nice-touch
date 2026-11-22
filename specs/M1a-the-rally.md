# M1a: the rally

Status: **approved by Joe, 2026-11-21**, including the three flagged choices.

## Goal

Find out whether the swipe feels good, as early as possible. At the end of
M1a you can serve, rally against an opponent, put spin on the ball and watch
it work. There is no score, no lives and no levels yet.

## The spin rules

These are the heart of the game, so they are written out in full. Every
number mentioned is a tuning value you can change in the tuning panel.

**The ball carries two kinds of spin.** Side spin and top spin. Top spin the
other way round is back spin. Spin is a real thing the ball has, not a label:
it is what you see rotating on screen.

**1. The swipe makes spin.** When a paddle hits the ball, the paddle's
sideways speed at that moment rubs spin onto the ball. A still paddle adds
none. A fast swipe adds a lot, up to a cap. Swiping left or right makes side
spin. Swiping up or down makes top or back spin. A diagonal swipe makes both.

**2. The swipe also pushes.** The ball leaves a little in the direction of
the swipe. So a swipe to the right sends the ball out to the right, and the
spin then bends it back to the left. That is the banana shot.

**3. Spin bends the flight.** A spinning ball curves, all the way down the
tunnel. More spin, more curve. A faster ball curves along the same shaped
path, it just gets there sooner.

**4. Spin fades.** It loses half its strength every few seconds.

**5. Spin bites the walls.** A ball with no spin bounces off a wall cleanly.
A spinning ball kicks:

- If it curved into the wall, it shoots off faster down the tunnel.
- If it was spinning the other way, it checks up and comes off slower.

Side spin bites the side walls. Top and back spin bite the floor and ceiling.
The bite uses up some of the spin. The speed change lasts until the next
paddle hit and stays inside a set range, so the ball can never stall or go
wild.

**6. Spin bites the paddle too.** If a spinning ball hits a paddle that is
not moving, it jumps off sideways and keeps part of its spin. To return a
heavy spin shot cleanly, you have to swipe against it. This is the same rule
for you and for the opponent, so heavy spin is how you force a miss.

**7. You can see it.** The ball has a two-tone pattern, so its rotation is
visible, and it rotates exactly as fast as the spin it carries.

## The other rules

- **Tunnel.** The ball travels between your end and the opponent's end and
  bounces off the four walls.
- **Pace.** The ball travels down the tunnel at one set speed. Only a wall
  bite changes it, and the next paddle hit puts it back.
- **Gravity.** Added on 2026-11-21 after the first QA, because the ball
  felt floaty. A tuning value pulls the ball towards the floor. It was
  built switched off so Joe could try it, and he set its starting value to
  7. Zero is no gravity. The ball bounces off the floor without losing
  height, and top or
  back spin bites the floor as it does any wall. A ball with too little
  bounce left to stay in the air for a quarter of a second rolls along the
  floor instead, silently and without a bite, until a paddle lifts it.
  Gravity does not touch a ball waiting on the paddle.
- **Your paddle.** It follows the mouse almost instantly, with a touch of
  smoothing, and stays inside the tunnel. The mouse cursor is hidden and kept
  inside the window during play. The paddle's position follows the cursor's
  position as a fraction of the window.
- **Hit or miss.** When the ball reaches a paddle's end of the tunnel, it is
  a hit if any part of the ball overlaps the paddle at that moment. The sim
  works out exactly where the ball was when it crossed, so a fast ball cannot
  slip through.
- **Miss.** Play stops for a moment, the screen says who missed, and the
  ball comes back to you to serve.
- **Serve.** The ball sits on your paddle and moves with it. Click to
  serve. Your swipe at the moment of the click puts spin on it, like any
  other hit.
- **Opponent.** A placeholder. It chases the ball while the ball is coming
  towards it, and drifts back to the centre otherwise. One tuning value sets
  how quickly it reacts. It makes spin by the same rules you do.

## What else gets built

- **Tuning panel.** F1 pauses play, shows the cursor and opens a list of
  every tuning value with a box to change it. Changing a value restarts the
  rally. The panel has three buttons:
  - *Reset to defaults*
  - *Copy values*, which puts them on the clipboard so you can paste them to
    me to become the new defaults
  - *Close*

  Your values are remembered between runs in a file on your machine that is
  not committed. While any value differs from the defaults, the screen shows
  "CUSTOM TUNING" so stale values can never pass unnoticed.
- **Input recorder.** The game records every input from the start of the
  current rally session. F9 saves the recording to a `recordings` folder
  next to `run.bat` and shows the file name on screen. The file holds the
  tuning values in use, every input, and where the ball ended up. I can
  replay it exactly in a test.
- **Debug line.** Shows the ball's spin, its pace and the length of the
  current rally, so you can see numbers when something feels off.
- **Depth cue.** A marker on the tunnel floor directly under the ball, so
  you can read how far away it is.
- **Smooth drawing.** The picture is smoothed between sim ticks, so it looks
  right on a high refresh rate screen.
- **Placeholder sound.** Three plain beeps made by the code, with no sound
  files: paddle hit, wall bounce, miss.
- **Keys.** F1 tuning panel, F9 save recording, R restart the rally, Esc
  quit.

## Tests

Every rule above gets a sim test, written first. Among them:

- A ball with no spin flies straight and bounces cleanly.
- Spin curves the ball the right way, on the way out and on the way back.
- Spin halves in the stated time.
- Gravity pulls the ball down, a dropped ball bounces back to the height it
  fell from, a ball too slow to bounce rolls, and with gravity at zero the
  ball does not drop.
- A wall bite speeds the ball up or slows it down, and never past the
  limits.
- A faster swipe makes more spin, up to the cap.
- A spinning ball jumps sideways off a still paddle.
- A fast ball cannot pass through a paddle.
- The same inputs played twice give exactly the same rally.
- A saved recording replays to exactly the same final ball position.

## Exit test

1. `test.bat` ends with PASS and CI is green.
2. You save a recording with F9, send it to me, and I show it replaying
   exactly in a test.
3. You answer yes to every item on the feel checklist.

### Feel checklist

1. Does the paddle feel attached to your hand, with no lag and no float?
2. When you swipe through the ball, does it bend the way you expected, time
   after time?
3. Can you choose between a little spin and a lot?
4. Can you see the spin on the ball?
5. Can you tell how far away the ball is, and where it will arrive?
6. When a spinning ball hits a wall, can you see it kick, and can you make
   it happen on purpose?
7. When a heavy spin shot comes back at you, can you learn to return it?
8. When you miss, do you know why?
9. Is a rally fun for five minutes with nothing else in the game?

A "no" is not a failure of the milestone. It is what the tuning panel is for.
We tune until it is a yes, or change the rule and log why.

## Out of scope

- Lives, levels, score, and the "Nice touch" call-out (M1b)
- More than one opponent, or opponents with character
- Hit pause, shake, trails, real art and real sound (M2)
- Menus and settings
- Watching a recording play back inside the game. Recordings replay in tests
  only for now.
- An exported `.exe`. The game still runs from source through `run.bat`.

## Three choices I made that are yours to overrule

1. **A physical spin model, not a simple one.** The simple version is "spin
   bends the flight" and nothing more. I have specified more: spin also
   changes how fast the ball comes off a wall, and makes it jump off a still
   paddle. That is what gives a player with a nice touch more to do than
   bend shots, and it is the part that makes the game its own. It costs more
   tuning time. Every one of these effects has a strength you can turn down
   to zero in the panel, so the simple version is always one setting away.
2. **The serve is from the paddle.** The ball rides on your paddle until you
   click, so you can serve from anywhere in the tunnel.
3. **Tuning values are remembered between runs.** The alternative is that
   they reset every time you close the game, which is safer but makes tuning
   across several sittings tedious.

## Changes to other documents on approval

- DESIGN.md says the recorder keeps "the last 30 seconds". It will say
  "every input since the rally session began", which is simpler and exact.
- DESIGN.md gains a short "Spin" section summarising the rules above.
- Both go in the decision log.

## Known risks

- **Too many spin effects at once could be hard to read.** If so, we turn
  effects down in the panel one at a time until it reads clearly. Pillar 2
  (readable at speed) wins over realism.
- **Mouse feel differs between mice.** Mapping by window fraction is the
  logged decision. If item 1 on the checklist stays a "no", the alternative
  is raw mouse movement with a sensitivity setting, and I will raise it.
