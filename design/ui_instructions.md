# Derelict Empires — UI Art Direction Spec

**Version:** 1.0  
**Author:** Art Direction  
**Target:** 1920×1080 native, responsive to 1366×768 – 3840×2160  
**Engine/Stack agnostic** — all values in CSS-equivalent notation for portability.

---

## 0. Design Intent

The UI evokes a **worn military command bridge** — not sleek sci-fi, not retro pixel art. Displays feel physically present: translucent instrument panels bolted over a viewport into the galaxy. Everything is slightly aged, slightly scratched, faintly humming with projected light. The word we keep coming back to is **tarnished**.

Visual pillars:

1. **Materiality over flatness.** Panels have depth, grain, edge-lighting. They aren't colored rectangles — they're objects.
2. **Information density without clutter.** Small type, monospaced data readouts, condensed fonts. Every pixel earns its place.
3. **Color as meaning.** Accent colors are never decorative. Green = yours/positive. Red = threat/loss. Gold = movement/warning. Cyan = UI focus/selection. Purple = strange/rare.
4. **Restraint in motion.** Animations are slow ambient pulses, not flashy transitions. The bridge hums; it doesn't dance.

---

## 1. The "Tarnished Glass" Effect

This is the single most important visual element. Every panel, card, modal, tooltip, and overlay uses this treatment. Get it right and the whole UI coheres. Get it wrong and it looks like every other glassmorphism tutorial.

### Layer Stack (bottom to top)

```
┌─────────────────────────────────────────┐
│  6. Content (text, icons, data)         │
├─────────────────────────────────────────┤
│  5. Edge lighting (border + glow)       │
├─────────────────────────────────────────┤
│  4. Inner vignette (inset shadow)       │
├─────────────────────────────────────────┤
│  3. Noise texture (::before overlay)    │
├─────────────────────────────────────────┤
│  2. Base fill (gradient)                │
├─────────────────────────────────────────┤
│  1. Backdrop blur (filters behind)      │
└─────────────────────────────────────────┘
```

**Layer 1 — Backdrop blur:**
```css
backdrop-filter: blur(12px) saturate(0.7);
```
Blurs the galaxy/map behind, but *desaturates* it — cold, faded, like looking through thick dirty glass. Not vibrant.

**Layer 2 — Base fill:**
```css
background: linear-gradient(
  135deg,
  rgba(8, 12, 28, 0.82),
  rgba(14, 18, 36, 0.88)
);
```
Not flat. The diagonal gradient gives the glass a sense of thickness — darker in the bottom-right, as if light is catching the top-left edge. Opacity 82-88% lets the blurred background breathe through, but only barely.

**Layer 3 — Noise texture:**
A tiled 64×64px grain PNG applied via `::before` pseudo-element:
```css
.panel::before {
  content: '';
  position: absolute;
  inset: 0;
  background: url('noise-warm-64.png') repeat;
  opacity: 0.04;           /* 4-6%, barely visible */
  mix-blend-mode: overlay;
  pointer-events: none;
  border-radius: inherit;
}
```
The noise should be **warm-neutral** — not pure white static. Think: fine scratches on a tinted visor. This is what makes it "tarnished" rather than "frosted."

**Layer 4 — Inner vignette:**
```css
box-shadow: inset 0 0 30px rgba(0, 0, 0, 0.4);
```
Darkens the edges of every panel, creating depth — like the glass is thicker at the edges, or the backlight doesn't reach the corners evenly. Scale the blur radius with panel size: 15px for small cards, 30px for main panels, 50px for full-screen overlays.

**Layer 5 — Edge lighting:**
```css
border: 1px solid rgba(80, 120, 180, 0.25);
box-shadow:
  0 0 1px rgba(100, 140, 200, 0.3),
  inset 0 1px 0 rgba(255, 255, 255, 0.04);
```
The border is almost invisible — a faint blue-gray line. The outer glow simulates edge-lit glass. The inner top highlight is a 4% white line simulating light catching the bevel. Together they make the panel feel like a physical object, not a CSS `div`.

**Layer 6 — Corner radius:**
```css
border-radius: 4px;
```
Sharp enough to feel military/technical. Soft enough to avoid looking brutalist. **Exception:** the speed/time widget uses `border-radius: 22px` (full pill) to differentiate it as a floating widget rather than a docked panel.

### Tarnished Glass Intensity Variants

| Context | Blur | Base Opacity | Noise | Vignette | Notes |
|---------|------|-------------|-------|----------|-------|
| **Main panels** (left/right/top) | 12px | 82-88% | 4-6% | 30px | Full treatment |
| **Cards** (fleet/location cards inside panels) | inherit from parent | 90% (slightly brighter base) | 3% | 15px | Elevated from panel — creates depth hierarchy |
| **Tooltips** | 8px | 92% | 2% | 10px | Needs to be readable, less atmosphere |
| **Minimap** | 6px | 85% | 2% | 8px | Small; too much blur = mud |
| **Modals/Overlays** | 16px | 78% | 6% | 50px | More translucent, more dramatic |
| **Speed widget** | 10px | 85% | 3% | 12px | Pill shape, stands apart |

### What This Is NOT

- Not clean Apple-style glassmorphism (too pristine).
- Not cyberpunk neon glow (too flashy, too saturated).
- Not flat/Material Design (no materiality).
- Not skeumorphic metal texture (we're not painting rivets — we're using light and grain).

---

## 2. Color System

All colors serve as **information**. If a color doesn't encode meaning, it shouldn't be there.

### Tokens

| Token | Value | Meaning |
|---|---|---|
| `--bg-void` | `#060a14` | The void behind everything |
| `--panel-base` | `rgba(8, 12, 28, 0.85)` | Tarnished glass fill (see §1) |
| `--panel-border` | `rgba(80, 120, 180, 0.25)` | Tarnished glass edge (see §1) |
| `--text-primary` | `#d8dce6` | Main text — NOT pure white. Slightly blue-gray, like projected light |
| `--text-secondary` | `#667a8c` | De-emphasized labels, inactive controls |
| `--text-dim` | `#3a4a5c` | Disabled, placeholder, decorative |
| `--accent-cyan` | `#44aaff` | **UI focus/selection.** Active tabs, selected nodes, title accents, interactive highlights |
| `--accent-green` | `#4caf50` | **Positive/owned.** Income, friendly territory, colonized, health, ON states |
| `--accent-red` | `#e85545` | **Negative/threat.** Losses, combat, damage, alerts, critical warnings |
| `--accent-gold` | `#ffcc44` | **Movement/caution.** Fleet transit, active speed, contested, moderate warnings |
| `--accent-purple` | `#b366e8` | **Strange/rare.** Derelicts, tech discoveries, anomalies, research |
| `--accent-orange` | `#e8883a` | **Military/resource.** Military stats, contested routes, aggressive actions |
| `--accent-olive` | `#8a8a3c` | **Environmental.** Asteroid fields, nebulae, natural hazards |

### Color Application Rules

1. **Accent colors appear on:** text, icon strokes, left-border accents on cards, status dots, route lines, node fills/strokes, delta values.
2. **Accent colors NEVER appear on:** panel backgrounds (use the tarnished glass system instead), large filled areas (exception: active speed button uses `--accent-gold` fill, but it's tiny).
3. **Colored background tints** on cards: When a card represents a typed entity (colony, derelict, asteroid), its tarnished glass fill gets a barely-perceptible tint of the type color — 3-6% opacity additive. Colony cards: `rgba(76, 175, 80, 0.04)`. Derelict cards: `rgba(179, 102, 232, 0.04)`. This is felt more than seen.
4. **Delta coloring:** Always parenthesized. `(+N)` in `--accent-green`. `(-N)` in `--accent-red`. Zero or neutral: `--text-secondary`.

### Text Glow

All text gets a faint self-illumination effect:
```css
text-shadow: 0 0 8px rgba(currentColor, 0.3);
```
At small sizes this is nearly invisible. At header sizes with accent colors (cyan system name, gold status badge) it produces a subtle holographic quality. **Do not increase this.** It should never look like neon signage.

---

## 3. Typography

**Two-font system. Three sizes. No exceptions.** Implemented in [`UIFonts.cs`](../src/Nodes/UI/UIFonts.cs).

### The Two Fonts

**Exo 2 SemiBold** (variable font, weight 600) — for **titles**. Only used where something has a name that the player identifies with: fleet names, POI names, system names, colony names, the game title. One weight, one size (16px).

**B612 Mono Bold** (static TTF) — for **everything else**. Body text, descriptions, numbers, deltas, status badges, tab labels, buttons, event log entries, tooltips. Originally designed by Airbus for cockpit readouts — readable at small sizes, fixed-width for columnar data, heavy strokes that survive hinting. Two sizes (14 normal / 12 small).

### Sizes

```csharp
UIFonts.TitleSize   = 16  // Exo 2 SemiBold — titles only
UIFonts.NormalSize  = 14  // B612 Mono Bold — default body/data size
UIFonts.SmallSize   = 12  // B612 Mono Bold — labels, deltas, status badges, captions
```

**12px is the hard floor.** Never render text smaller.

### Role Mapping

| Role | Font | Size | Used For |
|---|---|---|---|
| Title | Exo 2 SemiBold | 16px | Fleet/POI/system/colony names, "DERELICT EMPIRES", section headers |
| Normal | B612 Mono Bold | 14px | Resource values, location strings, body descriptions, event log entries, main data readouts |
| Small | B612 Mono Bold | 12px | Status badges (IDLE, MOVING), deltas (+14), fleet IDs (#fcc00), tabs, tab labels, subtitles, rate indicators, secondary metadata |

### Why This System Works

Two fonts are enough — every additional font choice is another visual axis to manage. Exo 2 draws the eye to names ("*what is this thing?*"); B612 Mono Bold handles everything the game computes, renders, or logs. The monospaced grid aligns resource columns and data readouts without manual spacing. Bold weight carries contrast down to 12px without needing a separate weight hierarchy.

### Font File Settings (applied programmatically)

Fonts are loaded via `FileAccess` in [`UIFonts.cs`](../src/Nodes/UI/UIFonts.cs) — not through Godot's import pipeline — so import-panel settings are irrelevant. Every `FontFile` gets:

- `Hinting = Normal` (Full hinting, snaps strokes to pixel grid)
- `ForceAutohinter = false` (B612 Mono's built-in hints are better than the autohinter)
- `Antialiasing = Gray`
- `SubpixelPositioning = Disabled` (snap glyphs to whole pixels — critical at 12-14px)
- `GenerateMipmaps = false`

### Example

A fleet card:
- `"SCOUT ALPHA"` → Exo 2 SemiBold 16 (name)
- `"IDLE"` → B612 Mono Bold 12 (status badge)
- `"#fcc00"` → B612 Mono Bold 12 (ID)
- `"Location: Theta Persei · 1 SHIPS"` → B612 Mono Bold 14 (body)

Three visible elements, one font + two sizes. The Exo 2 name is the only visual anchor that stands apart.

---

## 4. Icons

**Style:** Line-art / stroke-only. 1.5px stroke weight. No fills, no complex illustrations.  
**Reference sets:** Lucide or Phosphor at "light" weight.  
**Color:** Always meaningful — use `--text-primary` for neutral, or the relevant accent color when the icon represents a typed concept (green for colony icon, purple for derelict icon, etc.).

| Context | Size | Notes |
|---|---|---|
| Inline with text (resource stats) | 14px | Vertically centered with adjacent value |
| Card category indicator | 20px | Left side of location/fleet cards |
| Action buttons | 24px | Centered in 56×56px button |
| Ship silhouettes (fleet composition) | 16×10px | Filled (not stroke), `--text-secondary` |
| Status dots | 6px diameter | Filled circles, accent-colored, with `box-shadow: 0 0 4px` glow |

### Ship Silhouette Vocabulary

Fleet cards show a strip of tiny ship shapes for at-a-glance composition:

| Class | Shape | Relative Width |
|---|---|---|
| Scout / Fighter | Narrow pointed triangle | 1× |
| Frigate | Wider triangle, stubby wings | 1.2× |
| Cruiser | Elongated diamond, mid-body bulge | 1.3× |
| Capital | Large angular wedge | 1.5× |
| Support / Salvage | Boxy rectangle, arm protrusions | 1.4× |

Rendered as simple filled silhouettes in `--text-secondary`, 2px gap between each, left-aligned.

---

## 5. Component Library

### 5.1 Panels

Panels are the large docked UI regions (left fleet list, right system detail, top bar, event log). They use the full tarnished glass treatment and are fixed to viewport edges.

```
┌── 1px border (--panel-border) ─────────────────────┐
│ ┌── noise + vignette ───────────────────────────┐  │
│ │                                               │  │
│ │   [content area with 12-16px padding]         │  │
│ │                                               │  │
│ └───────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────┘
  └── edge glow (box-shadow outer) 
```

**Width:** `clamp(260px, 16vw, 320px)` for side panels. Full-width for top bar.  
**Corner radius:** 4px.  
**Padding:** 12px on small panels, 16px on main panels.

### 5.2 Cards

Cards live inside panels. They represent individual entities: a fleet, a colony, a location, an event. They are slightly elevated from the panel surface.

**Elevation hierarchy:**
```
Galaxy map (background, z-0)
  └── Panel (tarnished glass, z-10)
       └── Card (brighter base, z-11, thinner noise/vignette)
            └── Sub-element (inline, no additional elevation)
```

**Card specs:**
- Background: `rgba(16, 22, 44, 0.9)` — brighter than panel base to create depth.
- Border: 1px `rgba(80, 120, 180, 0.2)`.
- Border-radius: 4px.
- Padding: 10px 12px.
- **Left accent border:** 3-4px wide, colored by entity type/faction. This is the primary visual grouping mechanism.
- Noise: inherited or at 3% (thinner than parent panel).
- Vignette: `inset 0 0 15px rgba(0, 0, 0, 0.3)` (lighter than parent panel).

**Card type tinting:**

| Entity Type | Left Border Color | Background Tint |
|---|---|---|
| Colony / Friendly | `--accent-green` | `rgba(76, 175, 80, 0.04)` |
| Derelict / Anomaly | `--accent-purple` | `rgba(179, 102, 232, 0.04)` |
| Asteroid / Hazard | `--accent-olive` | `rgba(138, 138, 60, 0.06)` |
| Station / Infrastructure | `--accent-cyan` | `rgba(68, 170, 255, 0.03)` |
| Hostile / Contested | `--accent-red` | `rgba(232, 85, 69, 0.04)` |
| Fleet (by faction color) | varies | none — uses faction accent |

### 5.3 Buttons

Two button types exist in the UI.

**Action Buttons (square, icon + label):**
- Size: 56×56px.
- Tarnished glass variant: `rgba(20, 28, 50, 0.8)` base.
- Border: 1px `--panel-border`, radius 4px.
- Icon: 24px, centered vertically in upper area, `--text-primary`.
- Label: below icon, Small (B612 Mono Bold 12px), `--text-secondary`.
- **Selected state:** Left border 3px `--accent-cyan`, icon color → `--accent-cyan`.
- **Hover:** Background brightens to `rgba(30, 40, 65, 0.9)`, icon gains `drop-shadow(0 0 4px var(--accent-cyan))`.
- **Press:** `transform: scale(0.97)` for 100ms.
- **Disabled:** Entire button at `opacity: 0.35`, no hover.

**Pill Buttons (speed controls):**
- Size: 36×26px, radius 4px.
- **Inactive:** transparent bg, `--text-secondary` text, `1px solid rgba(80, 120, 180, 0.15)`.
- **Active:** `--accent-gold` fill, `#0a0e14` text, `box-shadow: 0 0 8px rgba(255, 204, 68, 0.3)`.

### 5.4 Toggle Pills

Used for map layer visibility (FLEETS ON/OFF, etc).

- Size: 36×18px, border-radius 9px (full pill).
- **ON:** `--accent-green` background, dark text "ON", Small (B612 Mono Bold 12px).
- **OFF:** `rgba(60, 70, 80, 0.6)` background, `--text-secondary` text "OFF".
- Adjacent label: Small (B612 Mono Bold 12px), right-aligned, `--text-primary`.

### 5.5 Tabs

Horizontal text-only tabs for panel mode switching.

- Font: `UIFonts.Main` at `SmallSize` (B612 Mono Bold 12px, uppercase).
- Gap: 20px between tabs, or separated by `·` glyphs in `--text-dim`.
- **Active:** `--accent-cyan` text + 2px bottom border in `--accent-cyan`.
- **Inactive:** `--text-secondary`, no border.
- **Hover:** Text brightens to `--text-primary`.
- Underline separator: 1px `rgba(80, 120, 180, 0.15)` spanning full panel width below tab row.

### 5.6 Status Badges

Inline text labels showing entity state (MOVING, IDLE, COMBAT, STATIONED).

- Font: `UIFonts.Main` at `SmallSize` (B612 Mono Bold 12px, uppercase).
- Color encodes status:
  - MOVING → `--accent-gold`
  - IDLE → `--text-secondary`
  - COMBAT → `--accent-red`
  - STATIONED → `--accent-green`
- No background, no border. Just colored text with the standard text-glow. Positioned right-aligned in card header rows.

### 5.7 Stat Readouts

Used in resource cards, location detail cards, anywhere numeric data is displayed.

- **Label:** `UIFonts.Main` at `SmallSize` (B612 Mono Bold 12px, uppercase), `TextDim`. E.g., "POP:", "DEFENSE:", "INCOME:".
- **Value:** `UIFonts.Main` at `NormalSize` (B612 Mono Bold 14px), `TextBright`. E.g., "2.1B", "1500", "4.5K/M".
- **Delta:** `UIFonts.Main` at `SmallSize` (B612 Mono Bold 12px), parenthesized, green/red. E.g., "(+4)", "(-3)".
- Layout: label and value on the same line, or label above value in tight spaces. Deltas always immediately follow their value.

### 5.8 Event Log Entries

Scrollable list of game events.

- **Indicator dot:** 8px circle, accent-colored by event category, with `box-shadow: 0 0 4px rgba(color, 0.5)`.
- **Text:** Normal (B612 Mono Bold 14px), `--text-primary`. Entity names within text can use inline accent coloring.
- **Timestamp:** Right-aligned, Small (B612 Mono Bold 12px), `--text-secondary`. 24-hour format "HH:MM".
- **Detail line (optional):** Indented 16px below main text, Small (B612 Mono Bold 12px), `--text-secondary`.
- Gap between entries: 6px.

---

## 6. Galaxy Map Rendering

The map fills the viewport behind all panels. It's the visual foundation — every panel floats over it.

### 6.1 Background Layers

1. **Solid void:** `--bg-void` (`#060a14`), fills everything.
2. **Galaxy image:** Pre-rendered or procedural spiral galaxy, centered. Features:
   - Bright white-blue core (~80px diameter brightest area, radial gradient).
   - 2-3 spiral arms in muted blue, purple, warm orange.
   - These are purely decorative — they don't align to game topology.
3. **Star field:** Hundreds of 1-2px white dots, opacity 0.2–0.8, some slightly warm-yellow or blue-white.
4. **Nebula patches:** Irregular translucent clouds (teal, purple, warm orange), 10-20% opacity.
5. **Radial vignette:** Viewport edges darken, focusing attention on center.

### 6.2 System Nodes

| Node Shape | Meaning | Visual |
|---|---|---|
| Circle (8-12px) | Star system | Fill color by ownership (see below) |
| Diamond / Rhombus (10px, rotated 45°) | Special site (derelict, anomaly, ruin) | Outline only, `--accent-purple` or `--accent-gold`, 1.5px stroke |
| Pentagon / Irregular | Asteroid field, resource deposit | `--accent-olive` fill, slightly larger |

**Ownership coloring (circles):**

| State | Fill | Stroke |
|---|---|---|
| Player-owned / Colonized | `--accent-green` | Darker green |
| Neutral / Uncharted | None (transparent) | `--accent-cyan` |
| Hostile / Contested | `--accent-orange` or `--accent-red` | Darker variant |
| Derelict / Abandoned | None | `--accent-purple`, dashed stroke |

**Node labels:** System name, Small (B612 Mono Bold 12px), `--text-primary`, positioned 6px below/right of node. Heavy text-shadow for readability:
```css
text-shadow:
  0 0 4px rgba(0, 0, 0, 0.8),
  0 1px 2px rgba(0, 0, 0, 0.9);
```

**Selected node:** Pulsing concentric ring animation. Two thin circles (1px `--accent-cyan` stroke) expand outward from node center, 0→30px radius, opacity 0.6→0, over 2.5s, staggered by 1.25s. Infinite loop.

### 6.3 Route Lines

Connections between systems. Always **quadratic bezier curves**, not straight lines — following the organic shape of galactic arms.

| Route Type | Color | Width | Style | Extra |
|---|---|---|---|---|
| Owned / Friendly | `--accent-green` | 2px | Solid, opacity 0.6 | `drop-shadow(0 0 3px rgba(76,175,80,0.4))` |
| Fleet in transit | `--accent-gold` | 2px | Dashed (`8, 4`) | Animated dash-offset ("marching ants") |
| Contested / Combat | `--accent-orange` | 2px | Solid | — |
| Unexplored / Potential | `--text-dim` | 1px | Dotted (`2, 4`), very low opacity | — |

### 6.4 Fleet Markers

Small arrow-shaped icons (12-16px) on route lines showing fleets in transit. Point toward destination. Color `--accent-gold` with a faint motion glow or trail.

### 6.5 Map Interactions

- **Hover system:** Node brightens, tooltip appears (tarnished glass tooltip variant).
- **Click system:** Selects it, updates right panel, starts pulsing ring animation.
- **Scroll:** Zoom (smooth 300ms ease-out on scale transform).
- **Drag:** Pan (no transition, direct input tracking, inertia deceleration on release).

---

## 7. Screen Layout (1920×1080)

```
┌──────────────────────────────────────────────────────────────┐
│  TOP BAR  [Title] [Currency] [5× Resource Cards] [Toggles]  │  80px
├───────────┬──────────────────────────────────�┬───────────────┤
│           │                                  │               │
│   LEFT    │                                  │    RIGHT      │
│   PANEL   │        GALAXY MAP                │    PANEL      │
│   310px   │        (fills remaining)         │    306px      │
│           │                                  │               │
│  Tabs:    │                                  │  [System Name]│
│  FLEETS   │      Star systems, routes,       │  [Location    │
│  COLONIES │      fleets rendered here        │   Cards ×3]   │
│  RESEARCH │                                  │  [Action      │
│  BUILD    │                                  │   Buttons ×4] │
│           │                                  ├───────────────┤
│  [Fleet   │                                  │  EVENT LOG    │
│   Cards   │                                  │  [Entry rows  │
│   ×6]     │                                  │   scrollable] │
│           │                                  │  160px        │
├───────────┤      ┌──────────────┐            │               │
│  MINIMAP  │      │ ⏸ x1 x2 x4 x8 CYCLE 150│               │
│  100×100  │      └──────────────┘            │               │
└───────────┴──────────────────────────────────┴───────────────┘
```

**CSS Grid:**
```css
.game-hud {
  display: grid;
  grid-template-columns: clamp(260px, 16vw, 320px) 1fr clamp(260px, 16vw, 320px);
  grid-template-rows: 80px 1fr auto;
  height: 100vh;
  width: 100vw;
}
```

Map always gets priority — panels shrink or collapse before map area is reduced.

---

## 8. Panel Breakdown

### 8.1 Top Bar

Full-width, 80px tall, tarnished glass. Flex row, `align-items: center`, padding `0 20px`, gap 16px.

**Contents left → right:**

| Element | Width | Key Details |
|---|---|---|
| Game title | ~240px | Title (Exo 2 SemiBold 16px), cyan underline with glow (`box-shadow: 0 0 8px rgba(68,170,255,0.5)`), 3px thick, subtitle below in Small (B612 Mono Bold 12px) `--text-dim` |
| Currency | ~150px | 28px green circle icon + value in Normal (B612 Mono Bold 14px) + income delta in Small (B612 Mono Bold 12px) `--accent-green` |
| Resource cards ×5 | flex center | 130×58px each, tarnished glass, 4px colored left border per colony, 2×2 stat grid inside (icon + value + delta per cell), Normal (B612 Mono Bold 14px) for values, Small (B612 Mono Bold 12px) for deltas |
| Layer toggles ×3 | ~130px | Vertical stack, Small (B612 Mono Bold 12px) labels + toggle pills (§5.4) |

### 8.2 Left Panel — Fleet / Command List

310px wide, tarnished glass, left-anchored, spans from below top bar to 120px from bottom (leaving room for minimap).

- **Tab bar** (40px): 4 tabs (§5.5). Active = FLEETS by default.
- **Fleet card list** (scrollable, 6px gap): Fleet cards (§5.2) at ~290×90px each. Each shows:
  - Header: fleet name (Title (Exo 2 SemiBold 16px)) + status badge (§5.6)
  - Detail: fleet class (Normal (B612 Mono Bold 14px), `--text-secondary`)
  - Location: origin/destination string (Normal (B612 Mono Bold 14px), `--text-secondary`)
  - Bottom: ship silhouette strip (§4) + status dots (§4)
- **Scrollbar:** 4px wide, `rgba(80, 120, 180, 0.3)` thumb, transparent track, rounded.

### 8.3 Right Panel — System Detail

306px wide, tarnished glass, right-anchored, spans ~55% of available height.

- **System header** (48px): Selected system name in Title (Exo 2 SemiBold 16px). Separator line below.
- **Location cards** (stacked, 8px gap, scrollable): Cards (§5.2) for each orbital body / POI. Each shows:
  - Type icon (20px) in accent color
  - Name (Title (Exo 2 SemiBold 16px), accent-colored) + type label (Normal (B612 Mono Bold 14px))
  - Stat readouts (§5.7): POP, INCOME, DEFENSE — right-aligned
- **Action buttons** (80px area): Section label in Small (B612 Mono Bold 12px) + 4 square buttons (§5.3) — SEND FLEET, BUILD STATION, EXPLORE, SCAN.

### 8.4 Event Log

306px × ~160px, tarnished glass, bottom-right, 12px below right panel.

- **Header:** "RECENT EVENTS" in Small (B612 Mono Bold 12px), optional 1px cyan accent line below (40px wide).
- **Event entries** (scrollable): As defined in §5.8.

### 8.5 Minimap

100×100px, tarnished glass (light variant), absolute bottom-left, 12px from edges.

- Simplified galaxy render: 2px colored dots for systems, 1px route lines.
- White/cyan viewport rectangle showing current map view area. Semi-transparent fill `rgba(255, 255, 255, 0.05)`.
- Clickable/draggable to pan main map.

### 8.6 Speed & Time Widget

~300×44px, centered bottom, 12px from edge. Tarnished glass, **pill-shaped** (`border-radius: 22px`).

- Pause button: 32px circle, `--accent-cyan` border + icon. Toggles play/pause.
- Speed buttons ×4: pills (§5.3), labels "x1"–"x8", active one in gold.
- Cycle counter: "CYCLE [N]" in Small (B612 Mono Bold 12px) + speed label in Small (B612 Mono Bold 12px) `--accent-gold`.
- Dot separators (4px, `--text-dim`) between elements.

---

## 9. Interaction & Motion

### State Table

| Element | Hover | Active/Selected | Press | Disabled |
|---|---|---|---|---|
| Panel | — | — | — | — |
| Card | Border brightens to `rgba(80,120,180,0.5)`, vignette lightens, 150ms ease | Left border 3px accent, background +5% lighter | — | opacity 0.4 |
| Action button | Bg → `rgba(30,40,65,0.9)`, icon gains accent glow | Left border 3px `--accent-cyan`, icon → `--accent-cyan` | `scale(0.97)` 100ms | opacity 0.35 |
| Tab | Text → `--text-primary` | Text → `--accent-cyan` + 2px bottom border | — | — |
| Pill button | — | Gold fill, dark text, glow | — | — |
| Toggle | — | Green/gray pill swap | — | — |
| System node | Brightens, tooltip | Pulsing ring animation | — | — |

### Ambient Animations

These run continuously. They should be subtle enough that you only notice them if you stop and look.

| Animation | Target | Behavior | Duration |
|---|---|---|---|
| Node pulse | Colonized system nodes | Opacity 0.7 → 1.0 | 3s ease-in-out, infinite |
| Selection rings | Selected system node | Two expanding/fading circles | 2.5s per ring, staggered 1.25s |
| Route energy flow | Owned route lines | Animated `stroke-dashoffset` | Continuous, slow |
| Star twinkle | Background star field | Random opacity 0.3 → 0.8 | 2-5s per star, staggered |
| Noise crawl (optional) | Panel noise texture | `background-position` drift | 60s loop |

### Transition Timing

| Transition | Duration | Easing |
|---|---|---|
| Panel slide in/out | 250ms | ease-out |
| Tab content swap | 150ms | opacity crossfade |
| Card hover | 150ms | ease |
| Button press | 100ms | ease |
| Map zoom | 300ms | ease-out |
| Map pan | 0ms (direct tracking) | Inertia on release |

### Audio Hooks (Implementation Reference)

| Event | Sound Character |
|---|---|
| Button hover | Faint high-pitched tick |
| Button press | Muted mechanical click |
| Tab switch | Soft slide/shuffle |
| New event (log) | Subtle chime, pitch varies by severity |
| Speed change | Engine-hum pitch shift |

---

## 10. Responsive Behavior

| Viewport | Side Panels | Top Bar Cards | Minimap | Speed Widget | Type Scaling |
|---|---|---|---|---|---|
| ≥2560×1440 | 320px fixed | 140px ea, all 5 | 120×120 | 320px | Upper bounds |
| **1920×1080** | 310 / 306px | 130px ea, all 5 | 100×100 | 300px | **Baseline** |
| 1600×900 | 280px each | 110px ea, show 4 + "+1" | 90×90 | 280px | Mid-range |
| 1366×768 | **Collapsible** (36px grip → 260px slide) | 3 cards + "+2" overflow | 80×80 | 260px, icons-only | Lower bounds |
| 2560×1080 UW | 310 / 306px | All 5, generous gap | 100×100 | 300px | Baseline |

**Collapsed panel grip:** 36px vertical strip with grip texture (three 2px horizontal lines in `--text-dim`) and panel icon. Clicking slides the panel out to full width. Auto-hides after 5s of no interaction or when map is clicked.

**Principle:** The galaxy map always gets maximum space. Panels compress and collapse. Typography scales via `clamp()`. Cards reduce count and show overflow indicators. The map never shrinks below 60% of viewport width.

---

## 11. Checklist for Implementation

Before shipping any panel or component, verify:

- [ ] Tarnished glass stack is complete (blur + gradient + noise + vignette + edge lighting)
- [ ] Noise texture is warm-neutral, not pure white, at correct opacity for component size
- [ ] Text uses correct token (`--type-*`) — not ad-hoc font/size combinations
- [ ] Accent colors encode meaning (not decoration)
- [ ] Cards have left accent border colored by entity type
- [ ] Status badges use Small (B612 Mono Bold 12px) in correct status color
- [ ] Names use Exo 2 SemiBold at TitleSize (16); everything else uses B612 Mono Bold at NormalSize (14) or SmallSize (12)
- [ ] Text glow (`text-shadow`) is present but subtle
- [ ] Hover/active/disabled states are implemented per §9
- [ ] Component works at 1366×768 (collapsed/compact mode)
- [ ] No pure white (`#ffffff`) text anywhere — use `--text-primary` (`#d8dce6`)
- [ ] No large filled accent-color areas (exception: active speed button)