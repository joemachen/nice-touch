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
- After the first QA ("the ball feels floaty"), the ball has gravity: it
  drops, arcs and bounces off the floor. A ball that has lost its bounce
  rolls along the floor until a paddle lifts it. The strength is a new value
  in the tuning panel, *Gravity*, which starts at 7. Setting it to 0 gives
  the old weightless flight.
- Joe's first saved recording (55 seconds, four serves) replays exactly in
  the rules tests, and is now replayed on every run of `test.bat`.
- The same swipe now puts twice as much spin on the ball (*Swipe spin* is 2
  by default, from Joe's tuning), so the cap is reached with a gentler
  swipe.
- Milestone M1a is signed off: every item on the feel checklist is a yes.
  Next is M1b, which turns the rally into a game with lives, levels and a
  score. Its spec is written and waiting for approval. Nothing new to play
  from it yet.
- Milestone M1b (the game) is built and waiting for QA. The rally is now a
  game you can win and lose:
  - A line near the top always shows your score, the level, your lives and
    the opponent's lives.
  - You start on level 1 with 5 lives. Each opponent has 3. A miss costs
    whoever missed one life.
  - Take the opponent's last life and the screen says LEVEL 1 CLEAR. You get
    one life back (never more than 5) and a bonus, and after a second
    LEVEL 2 starts with you to serve. Each level the opponent moves a
    little quicker and the ball travels a little faster. There are 6.
  - Lose your last life and it is GAME OVER, with your score and the level
    you reached. Clear level 6 and it is YOU WIN. After either, wait a
    second and one click starts a new game.
  - You only score by making the opponent miss. After every winner the
    screen shows what it paid as a sum: the winner itself, the length of
    the rally, *bend* (how far spin carried the shot off a straight line),
    *kick* (each wall or floor bounce that sped it up), the level, and the
    total. Later levels multiply it.
  - When bend and kick together reach a mark, the screen calls NICE TOUCH
    in gold, there is a new high beep, and those style points count
    double.
  - R now starts a new game. F9 now saves the game in hand, from its first
    serve, as `game-<date>-<time>.json`, with the score, lives and level it
    had reached.
  - Twelve new values at the bottom of the tuning panel (scroll down):
    lives for each side, lives back, the number of levels, how much quicker
    each level gets, every points value, the NICE TOUCH mark and the level
    bonus. Changing one restarts the game. Your remembered values from M1a
    are kept and the new ones start at their defaults.
  - Messages on screen now have a dark outline so they read over the
    tunnel's lines.
- Known before QA: with the starting numbers NICE TOUCH comes up on nearly
  every swiped winner. *Nice touch mark* is 100, and about 250 looks nearer
  the intent. It has been left at the approved number for Joe to judge.
