# Forum — design handoff

Read in this order.

| File | What it is | For |
|---|---|---|
| `DESIGN-SYSTEM.md` | **Start here.** The written spec: requirements, token overrides, colour semantics, type, component boundaries, layout, states, a11y, do/don't. | Implementation source of truth |
| `Forum Design System.dc.html` | The visual sheet — every token and component with all its states, plus the `styles.css` override block and a `PostCard` template. Open in a browser. | Checking a component before building it |
| `Forum Screens.dc.html` | A wired 390px demo of every screen: login/register/guest, feed, post detail + thread, create, moderation queue, profile, and the loading/empty/error states. Open in a browser and click through. | Confirming behaviour and edge cases |

`support.js` must stay next to the two `.dc.html` files for them to open.

## Stack

- Angular **21** (earlier versions are not supported by the library)
- Tailwind CSS **v4**
- [`@ng-brutalism/ui`](https://ngbrutalism.khangtran.dev/) — `ng add @ng-brutalism/ui`
- Node 20.19+ or 22.12+

## Non-negotiables from the brief

These are behaviours, not styling. `Forum Screens.dc.html` demonstrates each one.

1. One like per user per post. Liking is idempotent from the client's view; a second attempt is a no-op.
2. A user cannot like their own post or their own comment. Ownership is derived from the signed-in identity, never a stored flag.
3. Anonymous visitors can browse and read everything. Any like, comment or post attempt prompts sign-in with a stated reason.
4. Only moderators can flag content as misleading. Flagged content is dimmed and banner-labelled, never hidden, and stays open for comment.
5. Every list has four designed states: loading, empty, error, populated. No spinners, no blank screens.
6. Filter, sort and paging are server concerns; the UI shows the active filter count and an accurate result count before applying.

## What the developer builds

Six components, each composed from library primitives — none of them restyle a primitive:

`AppShell`, `PostCard`, `LikeButton`, `CommentItem`, `FilterSheet`, `ModerationBar`.

Everything else in the screens is composition of those plus `nbSurface`, `nbStack`, `nbCluster`,
`nbSplit`, `nbButton`, `nbChip`, `nbCallout`, `nbInput`, `nbTextarea`, `nbSelect`.

## The one styling rule

Override semantic tokens at `:root`. Do not write component-level CSS to fight the library — set its
CSS variables instead. The full override block is section 2 of `DESIGN-SYSTEM.md`.
