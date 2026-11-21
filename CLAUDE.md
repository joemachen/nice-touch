# Working agreement

Read this, DESIGN.md and ROADMAP.md before any work.

## Roles

- **Joe** is product owner and QA lead. He decides what the game is, sets
  priorities and signs off milestones. He does not write code, so explain
  trade-offs in plain language and never hand him a task that needs coding.
- **Claude Code** is the engineering team: design, code, tests, build, docs.

## How work flows

1. Work only on the **Now** milestone in ROADMAP.md. If something outside it
   seems necessary, stop and raise it.
2. Before a milestone starts, write a short spec in `specs/` (goal, rules,
   exit test, what is out of scope). Joe approves it before code is written.
3. Small commits, one concern each, with messages that say why.
4. Commits go straight to main. Nobody reviews code, so there are no pull
   requests. CI must stay green: a red main is fixed before anything else.
5. Update ROADMAP.md in the same commit as the change. Update CHANGELOG.md
   once per session with what a tester would notice.
6. End every working session with a QA handoff: what changed, the exact
   commit to test, the one command that runs it, what to look for, and
   anything known to be rough.

## Definition of done

- Tests pass locally and in CI
- New sim behaviour has tests written for it
- The build has no warnings. The sim projects fail the build on any warning.
- Roadmap and changelog updated
- QA handoff written

## Lean code

- Build what the current milestone needs. No speculative options, hooks,
  settings or abstractions for later milestones.
- An abstraction is earned by the second real use, not the first.
- Every dependency needs a one-line justification in the commit that adds it.
  Prefer what .NET and Godot already provide.
- Delete dead code. Do not comment it out. Git remembers.
- No duplicate sources of truth: a tuning value lives in one place.
- If a change makes the code smaller and the tests still pass, that is a win.

## Testing

- All rules live in `NiceTouch.Sim` and are tested with xUnit, test first
  where the rule is known in advance.
- The sim is deterministic. Keep a test that plays the same seeded match
  twice and compares the results.
- Do not unit test Godot scene glue. Cover it with a smoke test that the game
  boots, and with Joe's QA.
- A fix for a sim bug starts with a failing test that reproduces the bug. A
  fix for a Godot-side bug starts with written steps that reproduce it, and
  those steps go in the QA handoff.

## Bug reports

When Joe finds a bug he reports:

- what he did
- what happened
- what he expected
- the recorder file, once the input recorder exists (M1a)

## Hard rules

- No game rules in Godot scene scripts (see DESIGN.md, "Architecture").
- This is an original game. Do not copy another game's level tables, scoring
  tables, art, sound or look.
- Never commit the original SWF or anything extracted from it. That includes
  the notes in `docs/reference/`, which git ignores.
- Every sound, font and image added to the repo gets its source and licence
  recorded in CREDITS.md in the same commit.
- Never commit secrets, keys or tokens.
- Do not skip, delete or weaken a failing test to get to green. Fix the cause
  or raise it.
