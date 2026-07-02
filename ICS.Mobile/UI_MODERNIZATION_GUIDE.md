# ICS.Mobile Visual Modernization Guide

The binding style contract for the 2026 visual refresh. Every restyled screen
follows this. Tokens live in `_content/ICS.DesignSystem/css/tokens.css`.

## The look

Modern flat dark-glass on brand navy. Think current-gen field apps
(ServiceTitan, Dynamics FS mobile), not skeuomorphic 2009.

**Kill on sight (the "2009 tells"):**
- The wave PNG background (`background-gradient-wave.png`) → clean CSS gradient (already handled globally; remove any per-page references)
- Glossy gradient buttons (`linear-gradient(rgba(8,116,178...)`) → flat `var(--accent-strong)` fills
- 2px colored borders, dotted borders, beveled/inset looks → 1px hairline `var(--border-hairline)`
- Serif fonts, `font-weight: bolder`, `font-size: larger` → the type scale
- `box-shadow: 2px 2px ...` offset shadows → `var(--elevation-1)`
- Raw hex colors and `aliceblue`/`whitesmoke`/`yellow` keywords → tokens
- `!important` → fix specificity instead
- `<center>`, `align=`, `width=` presentational attributes where trivially replaceable by CSS

## Recipes

**Card (the universal container):**
```css
background: var(--surface-card);
border: var(--border-hairline);
border-radius: var(--radius-lg);
box-shadow: var(--elevation-1);
padding: var(--space-4);
```

**Section title:** `font-size: var(--text-sm); font-weight: var(--weight-semibold); letter-spacing: .06em; text-transform: uppercase; color: var(--text-muted); margin: var(--space-6) 0 var(--space-2);`

**List row:** card + `display:flex; gap: var(--space-3); align-items:center; min-height: var(--touch-target);` primary line `var(--text-lg)/var(--weight-semibold)/var(--text-primary)`, secondary line `var(--text-sm)/var(--text-muted)`. Whole row tappable.

**Status:** never restyle ad hoc — use `<StatusBadge Status="..." />` or `--status-*` tokens with icon + label.

**Contrast (measured, not vibes):** white text needs `var(--brand-700)` or darker
for fills at body sizes — `--accent-strong` only clears 3:1 (Large-text/badges
only), `--accent` fails. Danger-colored TEXT uses `--status-danger-text`
(`--status-danger` is for fills). On dark surfaces, Bootstrap's
`.text-secondary/.text-primary/.text-success/.text-danger/.text-dark` are all
remapped in calc-theme.css — never reintroduce raw Bootstrap contextual text
colors. Audit with the contrast walker (see session notes / preview eval).

**Buttons:** use `<AppButton>` where the element is a plain action button; otherwise style with: flat `var(--accent-strong)` (primary) / `var(--surface-raised-subtle)` + hairline (secondary), `border-radius: var(--radius-md)`, `min-height: var(--touch-target)`.

**Icon-only buttons:** 48x48 min, transparent bg, `color: var(--text-secondary)`, `border-radius: var(--radius-full)`, `:active { background: var(--surface-raised-subtle); }`, always `aria-label`.

**Inputs:** `background: var(--surface-input); border: var(--border-hairline); border-radius: var(--radius-md); min-height: var(--touch-target); color: var(--text-primary); font-size: var(--text-lg);` focus: `border-color: var(--accent);`.

## Anti-"AI-slop" addendum (from 2026 design research)

Documented tells of generic AI-generated UI, and our stance. (Sources: designer
critiques of code-gen output — vibecodekit.dev/ai-slop-design,
dev.to/alanwest "How to fix the AI-generated look", puckeditor.com
"AI slop vs constrained UI". Directionally consistent across sources.)

| Tell | Our rule |
|---|---|
| Purple→blue gradient washes, glossy hero gradients | Banned. Flat brand fills; the one background gradient is the app surface itself. |
| Indiscriminate glassmorphism / backdrop-blur everywhere | Blur allowed ONLY on the two fixed overlays (dialog scrim, workflow nav bar). Never on cards. |
| `rounded-2xl` on everything | One radius vocabulary: 6 / 10 / 16 / full — by component size, not fashion. |
| `shadow-lg` drop shadows on every card | Depth comes first from a 3–6% surface-lightness shift (`--surface-card`); `--elevation-*` reserved for true overlays. |
| Uniform Inter/Poppins/Geist sameness | System font stack (SF/Roboto) — native to the device, loads instantly, and isn't a codegen default. |
| Colored 3–4px left-border strip on generic cards | Only permitted as the classic alert pattern (CommonError); never as card decoration. |
| Neon-on-dark glowing borders | Banned. |
| Emoji-laden empty states / "Empower/Unlock/Transform" copy | Empty states use an icon + plain trade language ("No dispatches here"). |
| Inconsistent spacing | 4/8pt scale only; agents may not invent values. |

Numeric guardrails the research prescribes and we already meet: ≤3 hues in the
working palette (navy/blue + green + the status set), ~1.25 type-scale ratio,
16px body floor, 8px spacing multiples, high contrast (target 7:1 for critical
text — stricter than the research's floor, because sunlight).

## Icon canon

One glyph per meaning, Bootstrap Icons only (no Font Awesome, no inline SVG art):

- Back (within the app): `bi-arrow-left` · External link: `bi-box-arrow-up-right`
- Navigate deeper (row affordance): `bi-chevron-right`
- Update/refresh: `bi-arrow-repeat` · Download update: `bi-cloud-download`
- Logout: `bi-box-arrow-right` · Delete/destructive: `bi-trash3`
- Verified/secure: `bi-shield-check` · Person/profile: `bi-person-circle` (never as navigation)
- Status glyphs: only via `StatusBadge` / the `--status-*` pairings

Navigation rules: the bottom tab bar renders from the default layout on EVERY
page except Settings (which gets a `bi-arrow-left` back button instead), login,
and the workflow runner (immersive; has its own bottom step bar). The bar
auto-hides while scrolling and returns on settle (js/tabbar.js — toggleable in
Settings > Navigation, persisted in localStorage). Sub-pages may keep one
`bi-arrow-left` contextual back in the header. Rows/cards are tappable across
their whole surface — never an icon-only hotspot inside a larger container.

## Hard rules for restyling agents

1. Edit ONLY styling: the `<style>` blocks inside `CSSxxx.razor` style components, `.razor.css` files, and class attributes in markup. NEVER touch `@code` blocks, `@onclick`/`@bind` wiring, component parameters, or control flow.
2. Never delete a CSS class that markup references — restyle it.
3. Use tokens for every color, radius, shadow, spacing, and font size. No new hex values (exception: rgba(0,0,0,x)/rgba(255,255,255,x) overlays).
4. Both themes must work: check your values read correctly when `--surface-*`/`--text-*` flip to dark. Never hardcode navy/white pairs.
5. Touch targets ≥ 48px on anything tappable.
6. Keep diffs reviewable: no reformatting of untouched rules.
7. If a page uses Bootstrap components (calculators), keep Bootstrap classes; restyle via the token bridge idioms (`--bs-*` overrides scoped to the page container) rather than fighting it.
