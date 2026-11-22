# Changelog

What changed, written for someone testing the game. Updated once per working
session. Newest first.

## Unreleased

### 2026-11-21

- Project started. Nothing to play yet.
- The plan was reviewed and changed: this is an original game about spin, not
  a rebuild of an existing one. The reasons are in the ROADMAP.md decision
  log.
- Milestone M0 (foundation) is built and signed off:
  - `run.bat` opens a window showing an empty wireframe tunnel. A "sim tick"
    count in the top-left corner rises by 60 a second.
  - `test.bat` checks the build, the rules tests and that the game boots,
    and ends with PASS or FAIL.
  - GitHub runs the same checks on every push and shows a green tick or red
    cross beside the commit.
- The spec for milestone M1a (the rally and the spin rules) was written and
  approved.
- Milestone M1a (the rally) is built and waiting for QA. There is now
  something to play:
  - `run.bat` opens a rally. Move the mouse to move your paddle and click to
    serve. The opponent at the far end is a placeholder that chases the ball.
    There is no score yet.
  - Swiping the paddle through the ball as you hit it puts spin on it. The
    ball goes out a little in the direction of the swipe and then bends back
    the other way. The two-tone pattern on the ball turns as fast as the
    spin.
  - A spinning ball kicks off the walls, faster or slower along the tunnel
    depending on which way it is spinning, and jumps sideways off a paddle
    that is not moving.
  - A marker on the floor sits under the ball to show how far away it is.
  - After a miss the screen says who missed, play stops for a second and
    the ball comes back to your paddle.
  - A line at the top left shows the ball's spin, its pace and the length of
    the rally. A line at the bottom left lists the keys.
  - F1 pauses and opens the tuning panel, where every number behind the
    feel can be changed. Changing one restarts the rally. Your values are
    remembered next time, and "CUSTOM TUNING" shows at the top right while
    any value differs from the defaults.
  - F9 saves a recording of the session so far into a `recordings` folder
    next to `run.bat`, and shows the file name.
  - R restarts the rally. Esc quits.
  - Three placeholder beeps: paddle hit, wall bounce and miss.
- After the first QA ("the ball feels floaty"), the tuning panel has a new
  value, *Gravity*. It starts at 0, so the game plays exactly as before
  until you raise it. With gravity the ball drops, arcs and bounces off the
  floor. A ball that has lost its bounce rolls along the floor until a
  paddle lifts it.
