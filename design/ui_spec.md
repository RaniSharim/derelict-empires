# Derelict Empires — UI Design Specification
## Galaxy Map Screen & General Guidelines
_Version 1.1 — April 2026_

---

## 1. Design Philosophy

**Aesthetic:** Burned-steel salvagepunk. Every panel looks like it was salvaged from a wrecked warship, cleaned up just enough to function. The galaxy is always visible — the UI floats over it as tinted, cracked glass, never obscuring the map entirely.

**Core principles:**
- The map is the game. UI overlays it; it never replaces it.
- Information density is high but calm. No blinking, no noise. Alerts use color and glow, not animation spam.
- Faction colors are a language. Red/Blue/Green/Gold/Purple appear everywhere resources, tech, and ownership are shown. Players learn to read color before they read text.
- Contrast is non-negotiable. Every text element must be readable against the dark background. When in doubt, go brighter.
- Click targets must be generous. Minimum 40px height for any tappable row or button. Tabs never less than 36px tall.

---

## 2. Color System

### 2.1 Base Palette

| Token | Hex | Usage |
|---|---|---|
| `BgDeep` | `#040810` | Page/viewport background |
| `GlassDark` | `rgba(4, 8, 16, 0.88)` | Panel fill (backdrop-blur behind) |
| `GlassMid` | `rgba(6, 12, 22, 0.76)` | Secondary panel fill |
| `BorderDim` | `rgba(60, 110, 160, 0.30)` | Quiet borders, dividers |
| `BorderBright` | `rgba(90, 160, 230, 0.50)` | Panel edges, active borders |
| `TextFaint` | `#354e62` | Disabled, placeholder, watermark |
| `TextDim` | `#5a7a8e` | Secondary labels, metadata |
| `TextBody` | `#88aabb` | Default body text |
| `TextLabel` | `#b8d2de` | Panel labels, secondary headers |
| `TextBright` | `#e0eef6` | Primary names, selected items |
| `Accent` | `#2288ee` | Selection rings, active tabs, primary buttons |
| `AccentDim` | `rgba(34, 136, 238, 0.18)` | Hover backgrounds |

### 2.2 Faction Colors

Every precursor faction has a **base** color (used for unowned/dim contexts) and a **glow** color (used for owned, active, or highlighted contexts). Both must be used consistently everywhere that faction's resources, systems, or technology appear.

| Faction | Base | Glow | Background tint |
|---|---|---|---|
| Crimson Forge (Red) | `#c43020` | `#f04030` | `rgba(140, 28, 16, 0.28)` |
| Azure Lattice (Blue) | `#1a5599` | `#2288ee` | `rgba(16, 58, 128, 0.28)` |
| Verdant Synthesis (Green) | `#1a7730` | `#22bb44` | `rgba(16, 88, 28, 0.28)` |
| Golden Ascendancy (Gold) | `#997711` | `#ddaa22` | `rgba(118, 88, 8, 0.28)` |
| Obsidian Covenant (Purple) | `#552288` | `#9944dd` | `rgba(68, 18, 108, 0.28)` |

**Rule:** Never use a faction's base color for text on a dark background — always use the glow color. Base colors are for fills, borders, and backgrounds only.

### 2.3 Status Colors

| State | Color | Usage |
|---|---|---|
| Alert / Under Fire | `#ff5540` with `rgba(255,64,40,0.5)` glow | Combat, critical warnings |
| En Route / Moving | `#ffcc44` | Fleet movement, pending actions |
| Positive delta | `#66dd88` | Resource income, progress |
| Negative delta | `#ff6655` | Resource loss, damage |
| Neutral / Disabled | `#354e62` | Locked, unavailable |

---

## 3. Typography

Three fonts are used. Each has a strict role — do not mix them outside their role.

| Font | Import name | Role |
|---|---|---|
| **Exo 2** | `Exo 2` (400, 500, 600) | Titles, fleet names, POI names, colony names, screen headers — the display face |
| **Barlow Condensed** | `Barlow Condensed` (300, 400, 500, 600) | All readable body text: metadata lines, event log, POI details, tab labels, descriptions — the legibility workhorse |
| **Share Tech Mono** | `Share Tech Mono` (400) | All numerical data, resource values, status codes, coordinates, timestamps, tags — anything that is a value not a word |

```
Import URL:
https://fonts.googleapis.com/css2?family=Exo+2:wght@400;500;600
  &family=Barlow+Condensed:wght@300;400;500;600
  &family=Share+Tech+Mono
```

**Rationale for this stack:**
- Exo 2 gives titles and names a futuristic geometric character without feeling gimmicky.
- Barlow Condensed is highly legible at 9–12px in narrow panels, space-efficient, and neutral enough not to compete with Exo 2 — it reads cleanly where Share Tech Mono would feel choppy.
- Share Tech Mono anchors all data/numbers with a teletype quality, creating clear visual separation between "information" (Barlow Condensed) and "values" (Share Tech Mono).

### Font Size Scale

| Token | Size | Font | Usage |
|---|---|---|---|
| `TitleLarge` | 15–18px | Exo 2 600 | System name in right panel header, screen titles |
| `TitleMedium` | 12–13px | Exo 2 600 | Fleet names, POI names, colony names — ALL-CAPS |
| `BodyPrimary` | 11–12px | Barlow Condensed 500 | Metadata lines, location strings, descriptions |
| `BodySecondary` | 10–11px | Barlow Condensed 400 | Event log entries, secondary details |
| `LabelUI` | 9–10px | Barlow Condensed 500 | Tab text, section headers, button labels — ALL-CAPS, letter-spacing 1.5px |
| `LabelMono` | 8–9px | Share Tech Mono | Status tags (PATROL, EN ROUTE), resource labels — ALL-CAPS |
| `DataLarge` | 11–13px | Share Tech Mono | Resource stock numbers, primary values |
| `DataSmall` | 7–9px | Share Tech Mono | Deltas (+14), sub-labels, tooltips, timestamps |
| `Micro` | 6–8px | Share Tech Mono | Watermarks, minimap labels |

### Typography Rules

- **ALL-CAPS + letter-spacing:** Use for anything functioning as a UI label — tab text (`LabelUI`), status tags (`LabelMono`), section headers, button text. Letter-spacing: 1.5px for Barlow Condensed labels, 1px for Share Tech Mono tags.
- **Sentence case:** Use for fleet names, system names, event log descriptions, and all human-readable proper nouns. These use Exo 2 (titles) or Barlow Condensed (body).
- **Numbers are always Share Tech Mono.** Resource counts, deltas, coordinates, turn numbers, percentages, timer values — no exceptions. This creates instant visual separation between data and language.
- **Never use Barlow Condensed for numbers** — mixing proportional and mono in a data context creates misalignment.
- **Minimum text contrast:** Body text (`#88aabb`) on `GlassDark` (`rgba(4,8,16,0.88)`) passes. Never go darker than `TextDim` (`#5a7a8e`) for anything interactive. `TextFaint` (`#354e62`) is for decorative/watermark use only.

---

## 4. Glass Panel System

All UI panels use a consistent glass material. In Godot, this is achieved via `StyleBoxFlat` with low-alpha fills and careful border colors, with a `BackBufferCopy` or shader for the blur effect if performance allows. On simpler hardware, use the flat fill without blur and increase the alpha slightly.

### Glass Material Properties

```
Fill:            rgba(4, 8, 16, 0.88)
Border:          1px solid rgba(90, 160, 230, 0.50)   ← panel edges
Inner border:    1px solid rgba(60, 110, 160, 0.30)   ← internal dividers
Backdrop blur:   14–20px (where supported)
Grain overlay:   fractalNoise texture at 3% opacity (see shader section)
Crack overlay:   subtle SVG path lines at 3–4% opacity (decorative only)
```

### Accent Line

The bottom edge of the topbar and the top edge of tooltips get a 1px glow line:
```
linear-gradient(90deg, transparent, rgba(34,136,238,0.70) 20%, rgba(34,136,238,0.70) 80%, transparent)
```
This is the primary visual "brand" line of the UI — it reinforces the blue accent everywhere.

### Panel Rules

- Panels never have rounded corners. All edges are hard (corner-radius: 0).
- No drop shadows. Depth is communicated through fill opacity and border brightness, not shadow.
- Colored **left accent bar** (3px wide, faction glow color, 80% opacity) is used to identify faction ownership on any panel or list item that is faction-specific (resource boxes, POI items, fleet items when fleet is damaged or in a faction's territory).

---

## 5. Galaxy Map Screen — Layout

The galaxy map is the primary and persistent view. It fills the entire viewport. All other elements are overlaid on top of it.

```
┌─────────────────────────────────────────────────────────────────┐
│  TOPBAR (68px, full width, glass overlay)                       │
├──────────┬──────────────────────────────────────┬───────────────┤
│          │                                      │               │
│  LEFT    │         GALAXY MAP (SVG)             │  RIGHT        │
│  PANEL   │         fills entire screen          │  PANEL        │
│  270px   │         behind all panels            │  275px        │
│          │                                      │               │
│          │   [MAP OVERLAY BUTTONS]  top-right   │               │
│          │                                      │               │
│          │   [MINIMAP]  bottom, after left edge │               │
│          │                                      │               │
│          │   [SPEED+TIME]    floating bottom    │               │
│          │   [RECENT EVENTS] floating bottom-R  │               │
└──────────┴──────────────────────────────────────┴───────────────┘
```

### Positioning Summary

| Element | Position | Size |
|---|---|---|
| Topbar | `top: 0, left: 0, right: 0` | `height: 68px` |
| Left Panel | `top: 68px, left: 0, bottom: 0` | `width: 270px` |
| Right Panel | `top: 68px, right: 0, bottom: 0` | `width: 275px` |
| Galaxy SVG | `inset: 0` (absolute, z=0) | Full viewport |
| Map Overlay Buttons | `top: 80px, right: 290px` | Auto |
| Minimap | `bottom: 16px, left: 284px` | `100×100px` |
| Speed+Time | `bottom: 16px, right: ~338px` | Auto |
| Recent Events | `bottom: 16px, right: 16px` | `width: 306px` |

---

## 6. Topbar

Height: **68px**. Divided into fixed-width sections separated by `1px solid rgba(90,160,230,0.5)` vertical dividers. All sections are `full-height` flex children.

### Section Order (left → right)

**1. Logo block** (~160px)
- Font: Syncopate 11px, letter-spacing 5px, color `#b8d2de`
- The underscore between words: color `#33aaff` (accent)
- Text-shadow: `0 0 20px rgba(34,136,238,0.4)`

**2. Credits block** (~120px, faint olive-green tint background)
- Icon: circular coin SVG, 24px
- Value: Share Tech Mono 14px bold, color `#ccd898`
- Delta: Share Tech Mono 9px, color `rgba(160,190,80,0.55)`
- Label: Share Tech Mono 7px ALL-CAPS letter-spacing 2px, color `rgba(140,165,75,0.5)`

**3. Five faction resource boxes** (flex: 1 each, equal width, fill remaining space)

Each box contains:
- **3px left accent bar** in faction glow color
- **Background tint** in faction background color (see §2.2)
- **Top row (Common resources):** 3 resource cells side by side
- **1px divider** `rgba(255,255,255,0.06)`
- **Bottom row (Rare resources):** 3 resource cells side by side, slightly dimmer

**Resource cell layout:**
```
[12px SVG icon] [stock number]    ← Share Tech Mono 11px bold, faction glow color
                [±delta]          ← Share Tech Mono 8px, green (#66dd88) or red (#ff6655)
```
- Cell padding: `2px 5px`
- Cell background: `rgba(0,0,0,0.18)`, border `1px solid rgba(255,255,255,0.05)`
- Rare row cells: background `rgba(0,0,0,0.25)`, stock at 10px, delta at 7px
- Hover shows tooltip: resource name, panel `GlassDark` fill, top border in `Accent` color

### Resources per faction box

| Faction | Common (top row) | Rare (bottom row) |
|---|---|---|
| 🔴 Red | Plasma Embers · Scrap Iron · Power Cores | Fusion Cores · Forge Matrices · Weapon Components |
| 🔵 Blue | Signal Residue · Data Chips · Sensor Components | Quantum Resonance · Lattice Crystals · Encryption Modules |
| 🟢 Green | Bio-Luminance · Organic Polymers · Biological Samples | Genesis Catalysts · Genetic Templates · Organic Processors |
| 🟡 Gold | Solar Dust · Navigation Fragments · Trade Goods | Hyperlane Essence · Transit Matrices · Hyperdrive Components |
| 🟣 Purple | Void Whispers · Exotic Fragments · Exotic Materials | Dark Matter Cores · Consciousness Shards · Consciousness Cores |

---

## 7. Left Panel

Width: **270px**. Extends from below the topbar to the bottom of the screen. Glass material. Right edge: `1px solid rgba(90,160,230,0.5)`.

### Tab Bar

4 tabs: **FLEETS · COLONIES · RESEARCH · BUILD**

- Height: minimum **44px** (11px padding top and bottom)
- Font: Share Tech Mono, 8px, letter-spacing 1.5px, ALL-CAPS
- Inactive: color `#5a7a8e`, no background
- Hover: color `#b8d2de`, background `rgba(34,136,238,0.06)`
- Active: color `#55bbff`, background `rgba(34,136,238,0.08)`, bottom border `2px solid #2288ee`
- Bottom border of tab bar: `1px solid rgba(60,110,160,0.30)`

### FLEETS Tab — List Items

Each fleet row:
- Padding: `10px 14px`
- Left border: 2px, transparent by default
- Selected state: background `rgba(34,136,238,0.10)`, left border `#2288ee`
- Hover: background `rgba(34,136,238,0.07)`

Content per row:
```
[Fleet Name]          [STATUS TAG]
[Location · N ships]
[ship pip] [ship pip] [ship pip] ...
```

- **Fleet Name:** Exo 2 12px weight-600, color `#e0eef6`, ALL-CAPS, letter-spacing 0.5px
- **Status tag:** Share Tech Mono 8px, letter-spacing 1px
  - Default: `#5a7a8e`
  - Alert (UNDER FIRE): `#ff5540` + glow `rgba(255,64,40,0.5)`
  - Moving (EN ROUTE, EXPLORING): `#ffcc44`
- **Location line:** Share Tech Mono 9px, color `#5a7a8e`
- **Ship pips:** 5×5px shapes, arranged in a row with 3px gap
  - Fighter/Corvette: triangle clip-path, `rgba(80,120,160,0.5)`
  - Capital/Destroyer+: rectangle 8×5px, `rgba(120,170,210,0.75)`
  - Salvager: circle, `#22bb44`
  - Builder: rectangle, `#ddaa22`
  - Damaged: triangle, `#f04030` at 85% opacity

Row dividers: `1px` line `rgba(30,50,72,0.5)`, inset `14px` each side.

---

## 8. Right Panel

Width: **275px**. Extends from below the topbar to the bottom of the screen. Glass material. Left edge: `1px solid rgba(90,160,230,0.5)`.

### System Header

Padding: `12px 16px`, bottom border `1px solid rgba(60,110,160,0.30)`.

- **System name:** Syncopate 15px, color `#e0eef6`, ALL-CAPS, letter-spacing 3px, glow `rgba(34,136,238,0.3)`
- **Subtitle:** Share Tech Mono 9px, color `#354e62`, letter-spacing 2px — arm name and POI count. POI count in `#55aaff`.

### POI List Items

Left border 2px colored by type:
- Colony: `#22bb44`
- Derelict: `#9944dd`
- Asteroid: `#ddaa22`

Per item:
- **POI name:** Exo 2 12px weight-600, color `#b8d2de`, ALL-CAPS
- **Type tag:** Share Tech Mono 8px, color `#5a7a8e`, border `1px solid rgba(60,110,160,0.30)`
- **Detail lines:** Share Tech Mono 9px, color `#88aabb`, keys in `#354e62`
- **Progress bars:** 2px height, factional color fill on `rgba(20,30,48,0.9)` track

### Action Buttons

Row of 4 equal-width buttons. Padding: `10px 3px` (generous height). Font: Share Tech Mono 8px ALL-CAPS.

- Default: fill `rgba(16,28,48,0.70)`, border `BorderDim`, color `TextBody`
- Hover: fill `rgba(34,136,238,0.15)`, border `BorderBright`, color `TextBright`
- Primary (SEND FLEET): fill `rgba(34,136,238,0.16)`, border `rgba(34,136,238,0.45)`, color `#55bbff`
- Primary hover: fill `rgba(34,136,238,0.25)`, border `Accent`

---

## 9. Galaxy Map

The SVG/canvas layer sits at `z-index: 0`, filling the entire viewport. All panels and floaters sit above it.

### Background

Radial gradient centered slightly right of center:
```
center at (52%, 44%):
  #071525 → #030810 at 60% → #010408 at 100%
```

Stars: ~300 points, mostly `0.12–0.45px` radius at low opacity, ~5% larger bright stars up to `1.5px`. Color: `#90b0c8`.

Arm glow nebulae: 5 large blurred ellipses (blur stdDeviation ~42), one per faction color, at 7% opacity, positioned toward the galaxy rim in each arm direction.

### Starlanes

**Normal lanes:**
- Glow underlayer: 3px stroke, `#3366aa` at 15% opacity
- Main line: 1.2px stroke, `#3a6090` at 75% opacity

**Hidden lanes** (require Hauler origin or tech to see):
- 1.2px dashed stroke, dash `5 7`, color `#8833cc` at 45% opacity

Never render lanes thinner or dimmer than these values. Chokepoint readability depends on clear lane visibility.

### System Node Iconography

Each system type uses a distinct shape to communicate content at a glance, independent of color:

| Type | Shape | Notes |
|---|---|---|
| Normal star | Circle | Simple dot, faction color |
| Colony | Circle + inner ring | Inhabited worlds; owned colonies also get a bright center point |
| Derelict | Diamond (rotated square) + X cross | Ancient ruins/wrecks |
| Wreck/Danger | 8-point jagged star | Battle sites, contested zones |
| Nebula | Soft blurred cloud (multiple overlapping blurred circles) | No hard edge |
| Asteroid field | Irregular 7-point polygon | Rough, rocky shape |

**Owned system** treatment (any type):
- Use faction **glow** color (not base)
- Opacity: 92%
- Apply `glow-node` filter (feGaussianBlur stdDeviation 5, merged with source)
- Halo: circle radius+8, faction glow, 18% opacity

**Unowned system** treatment:
- Use faction **base** color
- Opacity: 65%
- No glow filter
- Halo: 6% opacity

**Selected system:**
- Outer dashed ring: radius+13, `#2288ee` at 40%, dash `3 4`
- Inner solid ring: radius+8, `#44aaff` at 80%, 1.2px stroke
- 4 cardinal tick marks extending from radius+6 to radius+12, `#55ccff`, 1.8px

**Danger pip:** 2.8px red circle (`#f04030`) with `glow-soft` filter, offset top-right of the node.

### Node Labels

- Font: Exo 2, 8px (9px if selected), weight 300 (500 if owned)
- Color: owned `#c8dde8`, selected `#55bbff`, unowned `#4a6880`
- Letter-spacing: 1.5px, ALL-CAPS
- Position: below node, centered

### Minimap

100×100px glass panel, `bottom: 16px, left: 284px`.
- Node dots: 3px (unowned) or 5px (owned), faction colors at respective opacities
- Derelict nodes: rotated square instead of circle
- Viewport indicator: `1px solid rgba(34,136,238,0.45)` rectangle, `rgba(34,136,238,0.05)` fill

---

## 10. Floating Elements

These sit at `z-index: 60`, glass material, no fixed panel attachment.

### Speed + Time Widget

Position: `bottom: 16px, right: ~338px` (leaves room for events panel).

Contents (left → right):
- Turn counter: Syncopate 14px, color `#e0eef6`, letter-spacing 3px
- Label "CYCLE": Share Tech Mono 7px, color `#354e62`, letter-spacing 2px
- 1px vertical divider
- Label "SPD": Share Tech Mono 8px, color `#354e62`, letter-spacing 2px
- Speed buttons: ⏸ · ×1 · ×2 · ×4 · ×8

Speed buttons:
- Size: minimum `36px` wide, `5px 11px` padding
- Default: fill `rgba(16,24,40,0.60)`, border `BorderDim`, color `TextDim`
- Active: fill `rgba(34,136,238,0.22)`, border `Accent`, color `#55bbff`, glow `rgba(34,136,238,0.30)`
- Hover: border `BorderBright`, color `TextLabel`

### Recent Events Widget

Position: `bottom: 16px, right: 16px`. Width: `306px`. Glass material.

- Header: Share Tech Mono 8px ALL-CAPS letter-spacing 2px, color `TextFaint`
- Each event row:
  - 4px colored dot (faction or status color, with box-shadow glow matching color), `margin-top: 4px`
  - Event text: Exo 2 10px weight-400, color `TextBody`, line-height 1.5
  - Timestamp: Share Tech Mono 8px, color `TextFaint`, right-aligned
  - Bottom margin: `7px` per row

### Map Overlay Toggle Buttons

Position: `top: 80px, right: 290px`. Stacked column, `3px gap`.

- Min-width: 110px. Padding: `8px 12px`. Text-align: right.
- Font: Share Tech Mono 8px ALL-CAPS letter-spacing 1px
- Default: fill `rgba(4,8,18,0.75)`, border `BorderDim`, color `TextDim`
- Active: color `#55bbff`, border `rgba(34,136,238,0.45)`, fill `rgba(34,136,238,0.12)`
- Hover: color `TextLabel`, border `BorderBright`

---

## 11. Godot Implementation Notes

### Fonts

Download all three from Google Fonts and place in `res://assets/fonts/`:
- `Exo2-Regular.ttf`, `Exo2-Medium.ttf`, `Exo2-SemiBold.ttf`
- `BarlowCondensed-Light.ttf`, `BarlowCondensed-Regular.ttf`, `BarlowCondensed-Medium.ttf`, `BarlowCondensed-SemiBold.ttf`
- `ShareTechMono-Regular.ttf`

Download static (non-variable) TTF exports from Google Fonts — Godot 4 supports variable fonts but static files are more reliable across platforms.

### Theme Architecture

Use a single `Theme` resource applied at the root `Control` node. Override per-node only when a specific component genuinely diverges. Key theme entries:

```
Label         → font: BarlowCondensed-Regular, size: 11, color: #88aabb
Button        → font: BarlowCondensed-SemiBold, size: 9 (ALL-CAPS via code)
               normal StyleBox: GlassDark fill + BorderDim border
PanelContainer → StyleBox: GlassDark fill + BorderBright border
TabBar        → font: BarlowCondensed-Medium, size: 9
               selected color: #55bbff, unselected: #5a7a8e
ScrollBar     → width: 3px, grabber: BorderMid color
```

Exo 2 and Share Tech Mono are applied as per-node font overrides since they appear less frequently and in specific contexts (titles and data values respectively). Do not set them as theme defaults.

### StyleBox Pattern

All panels and buttons use `StyleBoxFlat` with:
- `corner_radius_*`: 0 (all zero — no rounded corners anywhere)
- `border_width_*`: 1 (uniform, except the left accent bar which is border_width_left: 3)
- `draw_center`: true
- No `shadow_size` — shadows are never used

### Glass Effect

Godot doesn't have CSS `backdrop-filter`. Approximate it with:
1. A `BackBufferCopy` node behind the panel, set to `Rect` mode
2. A `ColorRect` or `Panel` over it with a shader that samples the back buffer and applies blur + tint
3. Alternatively (simpler, lower perf cost): use a `StyleBoxFlat` with fill `rgba(4,8,16,0.92)` and skip the blur — the higher alpha compensates

Grain texture: generate a 512×512 noise texture at startup or import a pre-baked PNG. Apply as a `TextureRect` over the panel at `3% alpha`, `MOUSE_FILTER_IGNORE`.

### Z-Index Layers

```
0  — Galaxy map (SVG viewport / SubViewport)
10 — Galaxy overlay elements (fleet icons, selection rings)
50 — Side panels (left, right)
60 — Floating widgets (minimap, speed, events, map buttons)
100 — Topbar
200 — Tooltips
999 — Modal dialogs, confirmation popups
```

### Scanline Overlay

A full-screen `ColorRect` at z=9999 with a shader that draws repeating horizontal lines:
```glsl
// Every 4px, darken by 5%
float line = mod(UV.y * screen_height, 4.0);
COLOR = vec4(0.0, 0.0, 0.0, line < 1.0 ? 0.05 : 0.0);
```
Set `mouse_filter = MOUSE_FILTER_IGNORE`.

### Input / Click Target Rules

- **Minimum interactive height:** 40px for list rows, 36px for buttons, 44px for tabs
- **List rows** should use `Button` with custom StyleBox (not `Label` + `InputEventMouseButton`)
- **All tabs** must be implemented as `Button` nodes in an `HBoxContainer`, not custom drawn
- **Resource cells** in the topbar are display-only; no click needed on the bar itself. A future market screen handles trading.

---

## 12. Screen-Specific Notes: What This Screen Does NOT Show

The galaxy map screen deliberately omits:

- **Color expertise bars** — shown in the Empire Overview screen, not here
- **Turn-end button** — real-time game, speed is controlled via the speed widget
- **Individual resource names in topbar** — names appear on hover tooltip only
- **Fleet detail** — clicking a fleet in the list opens a detail overlay; the list shows only name, status, location, and ship pips
- **System detail beyond the right panel** — clicking a system fills the right panel; a separate System View (zoom-in) is a different scene

---

## 13. Future Screens (design TBD)

These screens will follow the same glass panel system, faction color language, and typographic rules defined here:

- **System View** (zoom into a single system — primary management screen)
- **Tech Tree** (per-color, per-category, 6-tier tree with salvage unlock slots)
- **Ship Designer** (slot-based, no drag-drop)
- **Diplomacy** (agreement negotiation, reputation display)
- **Empire Overview** (color expertise, statistics, colony comparison)
- **Market / Trade** (component listings, auctions, tech rental)
- **Combat HUD** (fleet roles, disposition controls, morale bars)