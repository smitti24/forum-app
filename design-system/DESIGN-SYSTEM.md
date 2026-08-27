# Forum design system

A neo-brutalist design system for the iiDENTIFii forum, built on
[ng-brutalism](https://ngbrutalism.khangtran.dev/) (`@ng-brutalism/ui`) for Angular + Tailwind CSS v4.

- **Live sheet:** `Forum Design System.dc.html`
- **Screens:** `Forum Screens.dc.html`
- **Superseded:** `Forum Design System (Nocturne v1).dc.html` (dark variant, kept for reference)

ng-brutalism ships the primitives. This document decides what each token *means* in a forum.

---

## 1. Requirements

| | |
|---|---|
| Angular | **21** (earlier versions are not supported) |
| Tailwind | **v4** |
| Node | 20.19+ or 22.12+ |
| Install | `ng add @ng-brutalism/ui` |

The schematic installs the package, configures Tailwind v4 and PostCSS, and adds the global
stylesheet import. For manual setup, import both sheets once at the app entry.

---

## 2. Tokens

Only the semantic colours are overridden. Everything else is the library default.

```css
/* src/styles.css */
@import "tailwindcss";
@import "@ng-brutalism/ui/styles.css";

:root {
  --nb-primary:   #ff90e8;  /* post, submit, liked      */
  --nb-secondary: #c8a2ff;  /* moderator, author        */
  --nb-accent:    #8ae9ff;  /* active filter, active tab*/
  --nb-warning:   #ff9c42;  /* misleading-info flag     */
  --nb-success:   #63e6be;  /* answered, saved          */
  --nb-danger:    #ff4f8a;  /* delete, field error      */
  --nb-radius:      0rem;
  --nb-border-width: 2px;
}
```

### Colour semantics

| Token | Value | Means, in this app |
|---|---|---|
| `--nb-primary` | `#ff90e8` | The one action that creates something: post, submit, send. Also the *liked* heart. |
| `--nb-secondary` | `#c8a2ff` | Authority and authorship: moderator badge, "author" chip, unflag action. |
| `--nb-accent` | `#8ae9ff` | Current selection: active filter chip, active sort, active bottom tab, category tag. |
| `--nb-warning` | `#ff9c42` | The misleading-information flag, and only that. |
| `--nb-success` | `#63e6be` | Answered threads, saved confirmations. |
| `--nb-danger` | `#ff4f8a` | Destructive actions and field-level validation errors. |
| `--nb-main` | `oklch(90% 0.15 95)` | App bar and page headers. Structural, not interactive. |
| `--nb-field-bg` | `#faf3d6` | Every input and textarea. A field is always this colour. |
| `--nb-surface` | `#ffffff` | Cards, sheets, list rows. |
| `--nb-secondary-background` | `oklch(96% 0 0)` | Page ground and nested/indented content. |

**Rule:** one accent per surface. A card may carry a flag banner *or* a liked heart in colour, not
both competing for the same glance.

### Structure

| Token | Value | Notes |
|---|---|---|
| `--nb-border` / `--nb-shadow` | `#000000` | Never a grey border. Black or nothing. |
| `--nb-border-width` | `2px` | 3px only for error fields and the page header. |
| `--nb-radius` | `0` | No exceptions, including avatars. |
| `--nb-shadow-offset-x/y` | `4px` | 8px for the page header only. Reverse (`-4px`) for pressed-in surfaces. |
| Press behaviour | `translate(4px, 4px)` + shadow removed | Every button. 80ms, no easing curve worth naming. |
| Focus | `3px solid #000`, offset `2px` | Never removed, never restyled per component. |

### Type

`system-ui`, weight **500** body / **700** everything structural. Monospace
(`--nb-font-mono`) for metadata, labels, counts and timestamps — it is the system's voice for
"machine-generated fact".

| Role | Size | Weight | Case |
|---|---|---|---|
| Screen title | 30px | 700 | UPPER, `-0.03em` |
| Post title | 18px | 700 | Sentence |
| Body, comments | 15px | 500 | Sentence |
| Meta, labels, counts | 11–12px | 700 | UPPER mono, `+0.06em` |

Minimum body size is 15px. Minimum tap target is 44px.

---

## 3. Components

Six app components carry the product. Each composes library primitives — none of them restyle a
primitive.

| Component | Composed from | Responsibility |
|---|---|---|
| `AppShell` | `nbSurface`, `nbSection` | App bar, bottom nav (mobile) / left rail (≥768px), route outlet |
| `PostCard` | `nbSurface`, `nbCluster`, `nbStack`, `nbSplit`, `nbChip`, `nbCallout` | Feed row and detail header. One component, two densities |
| `LikeButton` | `nbButton` | Owns the one-like-per-user rule and its four disabled reasons |
| `CommentItem` | `nbCluster`, `nbStack`, `nbButton` | One comment, one indent level, reply affordance |
| `FilterSheet` | `nbSurface`, `nbChipGroup`, `nbInput`, `nbButton` | Keyword, category, sort, hide-flagged |
| `ModerationBar` | `nbCallout`, `nbButton` | Flag banner + moderator-only unflag |

Library primitives in use: `nbSurface`, `nbSection`, `nbStack`, `nbCluster`, `nbSplit`, `nbButton`,
`nbIconButton`, `nbChip`, `nbChipGroup`, `nbCallout`, `nbBadge`, `nbInput`, `nbTextarea`,
`nbSelect`, `nbAvatarGroup`, `nbStatusDot`, `nbSeparator`.

Icons: [Phosphor](https://phosphoricons.com) — **bold** weight for controls, **fill** for active
and for warnings.

### The post card

Author header → content → bordered action row. Sections are separated by a 2px black rule that
runs the full card width; there is no internal padding gap between them.

```html
<article nbSurface shadow="default">
  <div nbCluster align="center" gap="sm">…author…</div>
  <nb-callout *ngIf="post().isFlagged" tone="warning">…</nb-callout>
  <div nbStack gap="sm">…title, body, chips…</div>
  <div nbSplit>…like, comment, share…</div>
</article>
```

### The like button

Five states, all designed:

| State | Treatment |
|---|---|
| Rest | White, bold outline heart |
| Hover | Ground fill + 3px shadow |
| Liked | `--nb-primary` fill, filled heart, count +1 |
| Own post | 45% opacity, `cursor: not-allowed` — the API rejects self-likes |
| Guest | Dashed border, label "Sign in" |

Optimistic update on click; roll back and show the callout on a 409.

### Flagged content

Flagged posts are **dimmed in tone but never hidden**. The banner states who flagged it. Readers
still judge the content; the flag says the moderators disagree. Only moderators see *Unflag*.

---

## 4. Layout

One breakpoint: **768px**.

- **Below** — single column, 390px design width, bottom tab bar, full-bleed cards, 44px targets.
- **At and above** — bottom tabs become a left rail, the feed gains a filter sidebar. The post card
  itself is byte-identical at both widths.

Page ground is `--nb-secondary-background`; cards are white and float on a 4px black shadow.
Content max-width 1180px.

---

## 5. Feedback states

Every list has four designed states: **loading**, **empty**, **error**, **populated**.

- **Loading** — bordered skeleton blocks, one per expected row, opacity pulse. Never a spinner.
- **Empty** — icon tile, uppercase headline, one sentence, one action that resolves it.
- **Error** — same shape as empty, `--nb-danger` icon tile, retry action, and a note that the
  user's draft is preserved.
- **Validation** — 3px `--nb-danger` field border, message below the field, never only a toast.

---

## 6. Accessibility

- Focus is `3px solid #000` at `2px` offset on every interactive element. Never removed.
- Colour never carries meaning alone: the flag has an icon and a text label, the liked state has a
  filled glyph and an incremented count.
- All icon-only buttons carry `aria-label`.
- Tap targets 44px minimum; comment-level controls 34px are paired with a 44px row hit area.
- Black on every accent colour in the palette clears 4.5:1.

---

## 7. Do / don't

**Do**

- Take colour, spacing, border and shadow from `--nb-*`. Override at `:root`, a wrapper, or the
  element — the most local value wins.
- Compose new UI from primitives; put art direction in CSS variables, not in overriding classes.
- Keep the mono voice for anything machine-generated.

**Don't**

- Don't add a radius. Don't add a grey border. Don't add a second shadow layer.
- Don't restyle a library primitive's internals; set its tokens instead.
- Don't use `--nb-warning` for anything but the misleading-information flag.
- Don't hide flagged content, and don't let a flag remove the ability to comment.
