# M1b: the game

Status: **draft, awaiting Joe's approval.** No code is written until approved.

## Goal

The rally becomes a game you can win and lose. At the end of M1b you start
on level 1, beat opponents that get quicker, lose lives when you miss, see a
score that pays more for spin, and reach either GAME OVER or YOU WIN.

Nothing about how the ball, the paddles or spin behave changes. M1b is built
on top of the rally you signed off.

## How a game goes

- **Levels.** A game is a ladder of 6 levels. Each level is one opponent.
- **Lives.** You start with 5 lives. Each opponent has 3. A miss costs
  whoever missed one life.
- **Clearing a level.** Take the opponent's last life and the level is
  cleared. You get one life back (never more than 5), a level bonus, a short
  pause, and the next level starts with you to serve.
- **Game over.** Lose your last life and the game ends. The screen shows
  GAME OVER, your score and the level you reached.
- **Winning.** Clear level 6 and the screen shows YOU WIN and your score.
- **Playing again.** After either ending, one click starts a new game at
  level 1. R does the same at any time.
- **You always serve**, as now.

## How levels get harder

The opponent is still the placeholder from M1a: it chases the ball. Each
level it moves a little quicker and the ball travels a little faster.

| Level | Opponent speed | Ball pace |
|---|---|---|
| 1 | 5 | 24 |
| 2 | 6 | 25 |
| 3 | 7 | 26 |
| 4 | 8 | 27 |
| 5 | 9 | 28 |
| 6 | 10 | 29 |

Level 1 is exactly the rally you signed off. The steps (1 and 1) are two
tuning values, so the whole ladder can be made steeper or gentler from the
panel.

Where these numbers come from: I played scripted games through the rules at
each speed. At speed 5 a rally lasts about 4 hits before the opponent
misses. At 10 it lasts about 26. At 14 it lasts about 47, which is too long
to be fun, so the ladder stops at 10. A quicker opponent is harder to get
the ball past, and it also puts more spin on its own shots, because spin
comes from paddle speed. Your own recording has you winning 3 points in 4
at level 1.

## Scoring

You only score by making the opponent miss. Spin is what makes a point worth
more. When the opponent misses, the winning shot is paid like this:

| For | Points |
|---|---|
| The winner itself | 100 |
| Each paddle hit in the rally | 10 |
| **Bend:** each unit the spin carried the winning shot off a straight line | 10 |
| **Kick:** each wall or floor bite that sped the winning shot up | 50 |

The total is then multiplied by the level number, so later levels are worth
more. Clearing a level pays a bonus of 500 times the level number. Missing
costs a life and no points.

The "winning shot" is the ball's flight from your last hit to the
opponent's miss. Bend counts only what spin did, not the drop from gravity.

**Nice touch.** Bend and kick points together are the shot's style. If the
style on a winner reaches 100, the game calls out NICE TOUCH and the style
points count double. A hundred style points is, for example, a shot that
bent 5 units and kicked off a wall once.

**You can see what you were paid for.** During the pause after a winner the
screen lists the lines above with their points and the total. That is
pillar 2 applied to the score.

Every number in this section is a tuning value.

## On screen

- Your score, the level, your lives and the opponent's lives, always.
- LEVEL 1 (and so on) when a level starts.
- The points breakdown after each winner, and NICE TOUCH when it is earned.
- LEVEL CLEAR with the bonus, GAME OVER and YOU WIN.
- The debug line, the keys line, CUSTOM TUNING and the tuning panel stay.
  The new values join the panel. Changing a value restarts the game.
- One more placeholder beep, for NICE TOUCH.

## Recordings

- F9 now records the whole game from its start. The file also holds the
  final score, lives and level, and a replay must match those as well as
  the ball.
- Your M1a recording stays in the tests. It goes on checking the rally
  rules, which do not change, so you do not need to save it again.

## Tests

Every rule above gets a rules test, written first. Among them:

- A miss costs the right side one life, and nothing else.
- Taking the opponent's last life clears the level, gives a life back up to
  the limit, and starts the next level quicker and faster.
- Losing your last life ends the game, and nothing more can be served until
  a new game starts.
- Clearing the last level wins the game.
- A plain winner scores the base points. Bend, kicks and rally length each
  add what the table says. The level multiplies it.
- Style at the mark calls NICE TOUCH and doubles the style points. Just
  under the mark does not.
- Gravity's drop is not counted as bend.
- The same inputs played twice give exactly the same game and score.
- A saved game recording replays to the same final score.
- The M1a recording still replays exactly.

## Exit test

1. `test.bat` ends with PASS and CI is green.
2. You save a recording of a whole game with F9, and I show it replaying in
   a test to the same final score.
3. You answer yes to every item on the checklist.

### Checklist

1. Do you always know your score, your lives, the opponent's lives and the
   level?
2. When you win a point, can you tell what you were paid for?
3. Does a bent or wall-kicked winner pay enough more than a plain one that
   you try for them?
4. Does NICE TOUCH come up on shots you are proud of, and stay quiet on
   ordinary ones?
5. Is each level a bit harder than the last, with no sudden wall?
6. Have you lost a game? (You have seen GAME OVER.)
7. Have you won a game? (You have cleared level 6 at least once.)
8. After a game ends, is starting again one click?
9. Do you want another go?

As in M1a, a "no" is what the tuning panel is for.

## Out of scope

- Opponents with character, and more than one kind of opponent (M3, M4)
- Upgrades and the run (M3)
- Saved high scores, menus, settings, a pause menu (M2)
- Hit pause, shake, trails, real art and real sound (M2). NICE TOUCH is text
  and a beep for now.
- Changes to how the ball, paddles, spin or gravity behave
- Watching a recording play back inside the game
- An exported `.exe`

## Three choices I made that are yours to overrule

1. **Points come only from winners, and spin is what raises them.** The
   alternative is points for every return, which rewards keeping the ball
   in play rather than a nice touch. The rally bonus keeps long rallies
   worth something without making them the goal.
2. **Five lives, one back for each level cleared.** With no lives back, a
   whole game allows 5 misses across about 18 winners, which looked too
   harsh against your recording. Refilling all lives every level looked too
   soft. This sits between, and all three numbers are tuning values.
3. **Six levels with an ending.** The alternative is levels that go on for
   ever as a score chase. An ending gives you a "can I win" answer for the
   checklist, and the run (M3) is where endless escalation belongs.

## Changes to other documents on approval

- ROADMAP.md gets the M1b task list, and these three choices in the
  decision log.
- DESIGN.md gains a short "Scoring" section.

## Known risks

- **A chase-only opponent may not make a good ladder.** It could be a
  pushover at one speed and a wall at the next. I will play scripted games
  at every level during the build and show you the numbers. If it cannot be
  tuned into a fair ladder, I will raise it with options. I will not invent
  new opponent behaviour inside M1b.
- **Serving aces may turn out to be the best way to score.** The rally
  bonus is the counterweight. If it is not enough, the simple fix is to pay
  less for a winner straight off the serve, and I will raise it.
- **The scoring numbers are guesses.** Expect to change them, most of all
  the NICE TOUCH mark.
- **Your M1a tuning carries over.** If your remembered values differ from
  the defaults when M1b arrives, the new values start at their defaults and
  yours are kept.
