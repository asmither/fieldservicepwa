# ICS.Mobile Design System

UI/UX refactor foundation for the technician PWA. This document is the reference
for the token system, the new component primitives, the research behind the
decisions, and the roadmap for finishing the rollout (including sharing the
system with ICS.Portal.Web).

> ICS.Mobile.Functions is a backend-only Azure Functions project — it has no UI
> and is intentionally untouched by this work.

---

## 1. What changed (Phase 1 — foundation)

| Area | Change |
|---|---|
| **Design tokens** | New `wwwroot/css/tokens.css`: semantic color/surface/text/status tokens, type scale, 4pt spacing scale, radii, elevation, touch-target sizes. Loaded before all other CSS. |
| **Legacy bridge** | Every old variable (`--ThemeColor`, `--PendingColor`, …) is aliased to a semantic token, so the existing 12k+ lines of CSS and per-page `<CSSxxx>` styles theme automatically. `root.css` no longer defines raw colors. |
| **Dark mode** | Real runtime theming: `data-theme` attribute driven by `window.icsTheme` (bootstrapped pre-paint in `index.html`), persisted in localStorage, `ThemeService` (DI) + Light/Dark/Auto control in Settings. Replaces the abandoned `Theme/ThemeVariables.cs` approach (left in place, still unused). |
| **First-load splash** | Branded splash (app icon + determinate progress bar) driven by Blazor's real `--blazor-load-percentage`, replacing the default purple ring. |
| **Bottom tab bar** | New `Layout/AppTabBar.razor` — persistent 4-tab navigation (Home / Dispatches / Time / Settings) on the hub pages. Router-driven active state via `NavLink`, 48px+ targets, safe-area aware. The old `MainFooter`/`PageFooter` were dead code (linked to a nonexistent `/checklist` route) and remain unused. |
| **Primitives** | `AppButton`, `StatusBadge`, `EmptyState`, `Skeleton` — all with CSS isolation (`.razor.css`) and ARIA support. |
| **Accessibility** | `ICSDialogBox`: `role="dialog"`, `aria-modal`, labelled header, Escape-to-cancel, focus moves into dialog on open. `SyncIndicator`: `role="status"` + descriptive labels. Global `:focus-visible` ring, `touch-action: manipulation`, `prefers-reduced-motion` support, `.visually-hidden` utility. |
| **Error handling** | `EmptyLayout` (default layout) wraps `@Body` in an `ErrorBoundary` with a friendly retry/go-home screen; auto-recovers on navigation. |
| **Service worker** | Registered with `{ updateViaCache: 'none' }` (Microsoft's recommendation for all Blazor PWAs) so new versions aren't blocked by the HTTP cache. |
| **Page fixes** | TimePage approval controls: 48px approve/decline buttons, status marks now color+icon+label via tokens (was inline `color: red` spans). DispatchList: empty state when no dispatches. |

---

## 2. Token reference (use these in all new work)

Defined in the shared **ICS.DesignSystem** RCL
(`../ICS.DesignSystem/wwwroot/css/tokens.css`, served as
`_content/ICS.DesignSystem/css/tokens.css`). Light (default navy) and
`[data-theme="dark"]` (night work) themes redefine the same names.
The primitives (`AppButton`, `StatusBadge`, `EmptyState`, `Skeleton`) also live
in the RCL (`ICS.DesignSystem.Components`, imported app-wide via `_Imports.razor`).

**Surfaces & text** — `--surface-app`, `--surface-raised`, `--surface-raised-subtle`,
`--surface-overlay`, `--surface-inverse`, `--text-primary`, `--text-secondary`,
`--text-muted`, `--text-inverse`, `--text-on-accent`.

**Accent** — `--accent`, `--accent-strong`, `--accent-soft`, `--accent-gradient`.

**Status (canonical — never invent new status colors)** —
`--status-pending` (purple), `--status-traveling` (magenta), `--status-working`
(orange), `--status-complete` (gray), `--status-success` (green: approved/synced),
`--status-danger` (red: declined/failed/urgent), `--status-info` (blue: review),
`--status-warning` (gold). Statuses must always render as **color + icon + label**
(use `StatusBadge`) — ~8% of male users have red-green color vision deficiency.

**Type scale** — `--text-xs` 12 · `--text-sm` 14 · `--text-base` 16 (body floor) ·
`--text-lg` 20 (row titles) · `--text-xl` 24 · `--text-2xl` 28 (page titles) ·
`--text-3xl` 36 · `--text-display` 60 (glanceable numerals).

**Spacing (4pt grid)** — `--space-1` 4 · `--space-2` 8 · `--space-3` 12 ·
`--space-4` 16 · `--space-6` 24 · `--space-8` 32 · `--space-12` 48.

**Touch** — `--touch-target` 48px minimum for anything tappable;
`--touch-target-primary` 56px for primary lifecycle actions; `--touch-gap` 8px
minimum between adjacent targets. Sized for gloved hands.

**Other** — `--radius-sm/md/lg/full`, `--elevation-1/2`, `--focus-ring`,
`--motion-fast/base`, `--tabbar-height`.

Rules:
1. New markup uses semantic tokens, never raw hex and never the legacy aliases.
2. New components get a `.razor.css` file (CSS isolation). No new `<style>` blocks.
3. Touch targets ≥ 48px; primary actions ≥ 56px, bottom-anchored (thumb zone).
4. Every async action gives feedback (`AppButton Busy`, `Skeleton`, `role="status"`).
5. Every list handles its empty state (`EmptyState`).

## 3. Component primitives

```razor
<AppButton Variant="AppButton.ButtonVariant.Primary" Size="AppButton.ButtonSize.Large"
           FullWidth="true" Icon="bi-play-fill" Busy="@saving" OnClick="BeginJob">
    Begin Job
</AppButton>

<StatusBadge Status="@dispatch.Status" />          @* Pending/Traveling/Working/Complete/… *@

<EmptyState Icon="bi-truck" Title="No dispatches here"
            Message="Dispatches assigned to you will show up in this list." />

<Skeleton Shape="Skeleton.SkeletonShape.Rect" Height="72px" Count="5" />
```

## 4. Research base (full findings in session reports)

- **Touch**: 44pt (Apple HIG) / 48dp (Material 3) minimums; gloves push practical
  minimums to 48–64px. WCAG 2.2 SC 2.5.8.
- **Navigation**: visible bottom tabs beat hidden nav ~48% vs ~21% discoverability
  (NN/g); 3–5 destinations max; primary actions in the bottom third (thumb zone).
- **Outdoor readability**: 16px body floor, 7:1 contrast target for critical text
  in sunlight, regular-width faces, weight 500–600 for key data.
- **Offline-first UX**: every screen renders from local data; sync state honest and
  visible (pending/synced/failed); optimistic UI except signatures/payments.
- **Loading**: skeletons over spinners for full-screen loads (perceived-speed
  research, NN/g); determinate progress for first WASM download.
- **Blazor**: CSS isolation as the styling unit with tokens piercing scopes;
  `InputBase<T>` for form inputs; `<Virtualize>` for long lists; global
  `ErrorBoundary` + `Recover()`; no heavyweight component library (MudBlazor adds
  multi-MB to WASM payloads — wrong tradeoff for a mobile PWA).

## 5. Roadmap (Phase 2+)

1. ~~**Workflow footers**~~ — DONE: the four dead footer variants are deleted; the
   live step navigation (`Pages/Workflow/Components/Navigation.razor`) is rebuilt on
   `AppButton` with a 56px primary action and an optional `NextLabel` lifecycle verb.
2. **Forms pass** — one field-wrapper component (label + input + validation +
   `aria-describedby`); correct `inputmode` per field; migrate `ICSTextBox` & friends.
3. **Status sweep** — replace remaining hardcoded status colors in page markup
   (`DispatchDetail`, `Item.razor`, sync page) with `StatusBadge`/status tokens.
4. **List virtualization** — `<Virtualize>` on dispatch list & directory.
5. **PWA update banner** — surface `registration.waiting` as an in-app "Update now"
   prompt instead of silent updates on relaunch.
6. **`!important` diet** — style.css (7.4k lines, mostly unused outside
   `BootstrapEmptyLayout`) needs an audit; likely large dead-code deletion.

## 6. Sharing with ICS.Portal.Web (and Next-Everound)

DONE: the `ICS.DesignSystem` RCL now holds `tokens.css` and the four primitives,
referenced by both ICS.Mobile and ICS.Portal.Web (and added to
`ics-solution-portal.sln`). The Portal links the shared tokens in
`Pages/_Layout.cshtml` and maps Bootstrap 5.3's CSS variables onto them via
`wwwroot/css/design-tokens-bridge.css` (palette, status colors, links, radii,
`.btn-primary`/`.btn-outline-primary`/pills/pagination) — so both apps draw from
one palette while the Portal stays light-mode. App-specific styling stays in each
app: ICS.Mobile overrides `--surface-app` with its brand wave image in `root.css`.

Next: adopt the primitives in actively-maintained Portal pages, and seed
Next-Everound's design system from the same tokens so none of this work is
throwaway. Deeper Portal UX investment should still flow into Next-Everound.
