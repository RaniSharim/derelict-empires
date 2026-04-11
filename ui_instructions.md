# Derelict Empires — Galaxy Map Screen: Claude Code Implementation Instructions
_Based on approved mockup v1.2 — April 2026_

These instructions describe **exactly** what to build. Reference the attached screenshot at every step. Do not improvise layout or styling — follow the spec values precisely.

---

## OVERVIEW

The galaxy map screen is a **full-viewport canvas** with glass UI panels overlaid. There is no traditional window chrome. The map renders behind everything.

**Tech stack:** Godot 4.3, C#. The galaxy map is currently **3D (Node3D)** — the UI layer is a 2D CanvasLayer overlay. Do not convert to 2D; the map stays Node3D.

**Target resolution:** 1920×1080. Use `Display > Window > Stretch Mode = canvas_items`, `Stretch Aspect = expand`.

**Scene tree root** (matches existing `MainScene : Node3D`):
```
MainScene (Node3D — already exists)
├── GalaxyMap (Node3D — star/lane rendering, already exists)
├── StrategyCameraRig (Camera3D rig, already exists)
├── FleetContainer (Node3D — fleet icons, already exists)
├── UILayer (CanvasLayer, layer=1 — already exists, rebuild contents)
│   ├── Topbar (PanelContainer)
│   ├── LeftPanel (PanelContainer)
│   ├── RightPanel (PanelContainer)
│   ├── MinimapWidget (PanelContainer)
│   ├── SpeedTimeWidget (PanelContainer)
│   └── RecentEventsWidget (PanelContainer)
└── ScanlineOverlay (CanvasLayer, layer=99)
    └── ColorRect (full-rect, MOUSE_FILTER_IGNORE)
```

---

## FONTS

Download static TTF files from Google Fonts. Place in `res://assets/fonts/`.

| File | Usage |
|---|---|
| `Exo2-SemiBold.ttf` | Titles, fleet names, system names, POI names |
| `Exo2-Medium.ttf` | Secondary Exo 2 usage |
| `BarlowCondensed-SemiBold.ttf` | UI labels, tab text, button text, section headers |
| `BarlowCondensed-Medium.ttf` | Body descriptions, metadata lines |
| `BarlowCondensed-Regular.ttf` | Secondary body text |
| `ShareTechMono-Regular.ttf` | ALL numbers, ALL status tags, ALL coordinates |

**Godot Theme setup** — create `res://assets/theme/main_theme.tres`:
- `Label` default font → `BarlowCondensed-Regular`, size 11, color `#88aabb`
- `Button` default font → `BarlowCondensed-SemiBold`, size 9
- `TabBar` font → `BarlowCondensed-SemiBold`, size 9
- Exo 2 and Share Tech Mono are **per-node overrides only**, not theme defaults

**Critical rendering settings:**
- Under `Rendering > 2D > Snap`: enable **Snap 2D Transforms to Pixel**
- On Labels using Share Tech Mono at sizes ≤ 10px: set `Label > Extra Caret Spacing = 0`, antialiasing off

---

## COLORS

Define these as a static C# class `UIColors`:

```csharp
// Base palette
public static readonly Color BgDeep       = new("#040810");
public static readonly Color GlassDark    = new Color(4/255f, 8/255f, 16/255f, 0.88f);
public static readonly Color BorderDim    = new Color(60/255f, 110/255f, 160/255f, 0.30f);
public static readonly Color BorderBright = new Color(90/255f, 160/255f, 230/255f, 0.50f);
public static readonly Color TextFaint    = new("#4a6880");
public static readonly Color TextDim      = new("#7b9eb5");
public static readonly Color TextBody     = new("#88aabb");
public static readonly Color TextLabel    = new("#b8d2de");
public static readonly Color TextBright   = new("#e0eef6");
public static readonly Color Accent       = new("#2288ee");

// Faction glow colors (for text, active icons, owned nodes)
public static readonly Color RedGlow    = new("#f04030");
public static readonly Color BlueGlow   = new("#2288ee");
public static readonly Color GreenGlow  = new("#22bb44");
public static readonly Color GoldGlow   = new("#ddaa22");
public static readonly Color PurpleGlow = new("#9944dd");

// Faction background tints (for panel fills)
public static readonly Color RedBg    = new Color(140/255f, 28/255f, 16/255f, 0.28f);
public static readonly Color BlueBg   = new Color(16/255f, 58/255f, 128/255f, 0.28f);
public static readonly Color GreenBg  = new Color(16/255f, 88/255f, 28/255f, 0.28f);
public static readonly Color GoldBg   = new Color(118/255f, 88/255f, 8/255f, 0.28f);
public static readonly Color PurpleBg = new Color(68/255f, 18/255f, 108/255f, 0.28f);

// Status
public static readonly Color Alert    = new("#ff5540");
public static readonly Color Moving   = new("#ffcc44");
public static readonly Color DeltaPos = new("#66dd88");
public static readonly Color DeltaNeg = new("#ff6655");
```

---

## GLASS PANEL SYSTEM (TARNISHED)

All UI panels use a consistent **tarnished glass** material. Panels never have rounded corners (`corner_radius_* = 0`) and use no drop shadows.

**Material properties:**
- **Fill:** `rgba(4, 8, 16, 0.88)` (`UIColors.GlassDark`)
- **Border:** `1px solid rgba(90, 160, 230, 0.50)` (`UIColors.BorderBright`)
- **Backdrop blur:** 14–20px — apply via `BackBufferCopy` node behind each panel + a Gaussian blur shader on a `ColorRect` child. The blur samples the galaxy map behind the panel.
- **Tarnish overlay:** A high-contrast scratch/grime alpha mask applied to panel borders and corners at **15% opacity** to simulate physical wear. Use a tileable texture (`res://assets/textures/tarnish_mask.png`) on a `TextureRect` child with `MOUSE_FILTER_IGNORE`, blend mode `MIX`, self-modulate alpha `0.15`.
- **Grain overlay:** `fractalNoise`-style texture at **3% opacity**. Use a small tileable noise texture (`res://assets/textures/grain_noise.png`) on a `TextureRect` child, `MOUSE_FILTER_IGNORE`, self-modulate alpha `0.03`. Tile via `TextureRect.StretchMode = TILE`.

**Standard glass panel (code):**
```csharp
var panelStyle = new StyleBoxFlat();
panelStyle.BgColor = UIColors.GlassDark;
panelStyle.SetBorderWidthAll(1);
panelStyle.BorderColor = UIColors.BorderBright;
panelStyle.SetCornerRadiusAll(0);
```
The `StyleBoxFlat` handles fill + border. Backdrop blur, tarnish, and grain are separate child nodes layered inside each panel (see material properties above).

**Inner divider (for use inside panels):**
```
1px solid rgba(60, 110, 160, 0.30)
```

**Left faction accent bar (3px):**
```csharp
var accentStyle = new StyleBoxFlat();
accentStyle.BgColor = factionGlowColor;
accentStyle.SetBorderWidthAll(0);
accentStyle.SetCornerRadiusAll(0);
// Apply as a 3px-wide ColorRect child, not as StyleBox border
```

**Button states:**
- Normal: fill `rgba(16,28,48,0.70)`, border `BorderDim`
- Hover: fill `rgba(34,136,238,0.15)`, border `BorderBright`  
- Pressed / Active: fill `rgba(34,136,238,0.22)`, border `Accent` `#2288ee`
- Primary action button (e.g. SEND FLEET): fill `rgba(34,136,238,0.16)`, border `rgba(34,136,238,0.45)`, text `#55bbff`

---

## TOPBAR

**Node:** `Topbar` (HBoxContainer inside a PanelContainer)  
**Position:** `top=0, left=0, right=0`, height **68px**, z-index 100  
**Background:** `GlassDark` fill + `BorderBright` bottom border  
**Bottom accent line:** 1px ColorRect at bottom of topbar, color gradient:
```
linear: transparent → rgba(34,136,238,0.70) at 20% → rgba(34,136,238,0.70) at 80% → transparent
```
Use a `TextureRect` with a generated gradient texture, or a shader on a ColorRect.

### Section 1 — Logo (~160px, fixed width)
```
DERELICT          ← Exo 2 SemiBold, 14px, #b8d2de, letter-spacing 4px
EMPIRES           ← same, second line or same line depending on space
```
Right border: 1px `BorderBright`. Padding: `0 20px`.

### Section 2 — Credits (~140px, fixed width)
Background tint: `rgba(90,110,60,0.08)`. Right border: 1px `BorderBright`.

Layout (VBoxContainer centered):
```
[coin icon 20px]  1,228,090     ← Share Tech Mono 14px bold, #ccd898
                  +29,590       ← Share Tech Mono 10px, #66dd88
CREDITS           ← Barlow Condensed 7px ALL-CAPS letter-spacing 2px, #7b9eb5
```

### Section 3 — Five Faction Resource Boxes (HBoxContainer, flex fills remaining width)

Each box is equal width (flex: 1). See dedicated section below.

---

## FACTION RESOURCE BOX — DETAILED SPEC

This is the component Gemini is getting wrong. Follow this exactly.

### Structure

Each faction has **4 resources** (2 common + 2 rare), from `ResourceDefinition.All`.

Each box is a `VBoxContainer` inside a `PanelContainer` with:
1. A **3px-wide ColorRect** on the left edge (the accent bar), faction glow color
2. A **background tint** fill matching the faction bg color
3. A **header row** at top
4. A **1px divider** `rgba(60,110,160,0.30)`
5. A **common resources row** (Row A) — 2 cells (SimpleEnergy + SimpleParts)
6. A **1px divider** `rgba(255,255,255,0.06)`
7. A **rare resources row** (Row B) — 2 cells, visibly dimmer (AdvancedEnergy + AdvancedParts)

### Header Row

```
[FACTION NAME]                    ← left-aligned
```
- Faction name: Barlow Condensed SemiBold, 8px, ALL-CAPS, letter-spacing 1.5px, color = faction glow color
- Height: ~16px, padding `2px 6px 2px 10px`
- No "DELTA" label in the header — deltas live inside each cell

### Resource Rows (Row A = Common, Row B = Rare)

**⚠️ THIS IS THE CORRECT LAYOUT:**

Each row is an HBoxContainer containing exactly **2 resource cells**, each with equal width (`size_flags_horizontal = EXPAND_FILL`):

```
[Cell 1]   [Cell 2]
```

#### Resource Cell layout — each cell shows ONE resource:

```
[icon 12px]  [stock number]
             [delta]
```

As a VBoxContainer inside an HBoxContainer:
- `icon`: TextureRect 12×12px, SVG icon for that specific resource, left side
- `stock`: Share Tech Mono, **11px bold** (common) / **10px** (rare), color = faction glow color
- `delta`: Share Tech Mono, **8px** (common) / **7px** (rare), positive = `#66dd88`, negative = `#ff6655`
  - Format: `+14` or `-2` — **no parentheses**, just the sign and number

The icon and numbers stack like this within the cell:
```
[icon]  412       ← stock: Share Tech Mono 11px bold, glow color
        +14       ← delta: Share Tech Mono 8px, green/red
```

Padding per cell: `2px 5px`. Background: `rgba(0,0,0,0.18)`. Border: `1px solid rgba(255,255,255,0.05)`.  
Gap between cells: `2px`.

#### Row A vs Row B visual difference:

| Property | Row A (Common) | Row B (Rare) |
|---|---|---|
| Cell background | `rgba(0,0,0,0.18)` | `rgba(0,0,0,0.28)` |
| Stock font size | 11px, bold | 10px, opacity 0.80 |
| Stock color | faction glow color | faction glow color, 80% opacity |
| Delta font size | 8px | 7px |
| Overall row opacity | 100% | 85% |

### Resources per faction (2 per row × 2 rows = 4 per faction)

Names sourced from `ResourceDefinition.All` in `src/Core/Models/ResourceDefinition.cs`:

| Faction | Row A — Common (SimpleEnergy · SimpleParts) | Row B — Rare (AdvancedEnergy · AdvancedParts) |
|---|---|---|
| 🔴 Crimson Forge | Plasma Embers · Scrap Iron | Fusion Cores · Forge Matrices |
| 🔵 Azure Lattice | Signal Residue · Data Chips | Quantum Resonance · Lattice Crystals |
| 🟢 Verdant Synthesis | Bio-Luminance · Organic Polymers | Genesis Catalysts · Genetic Templates |
| 🟡 Golden Ascendancy | Solar Dust · Navigation Fragments | Hyperlane Essence · Transit Matrices |
| 🟣 Obsidian Covenant | Void Whispers · Exotic Fragments | Dark Matter Cores · Consciousness Shards |

### Hover tooltip

On hover over any cell, show a `PopupPanel` anchored above that cell:
- Background: `GlassDark`, top border: 1px `Accent` color
- Content: resource name, Barlow Condensed 9px, color `TextLabel`

### Complete visual example (Crimson Forge box):

```
┌─ 3px red accent bar
│ ┌────────────────────────────────┐
│ │ CRIMSON FORGE                  │  ← header, 16px
│ ├────────────────────────────────┤
│ │ [🔥] 412   │ [⚙] 287         │  ← Row A: Plasma Embers, Scrap Iron
│ │     +14    │    +22           │
│ ├╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┤
│ │ [⚛]   7   │ [◆]  3          │  ← Row B: Fusion Cores, Forge Matrices
│ │      +1   │    −0            │
│ └────────────────────────────────┘
```

Each number sits directly under its icon, delta directly under the stock number. No combined delta column.

---

## LEFT PANEL

**Position:** `top=68px, left=0, bottom=0`, width **270px**  
**Background:** `GlassDark` + `BorderBright` right edge

### Tab Bar (FLEETS / COLONIES / RESEARCH / BUILD)

- 4 tabs in HBoxContainer
- Height: **44px** minimum (padding: 12px top/bottom)
- Font: Barlow Condensed SemiBold, 9px, ALL-CAPS, letter-spacing 1.5px
- Inactive: color `TextDim` (`#7b9eb5`), no background fill
- Hover: color `TextBright`, background `rgba(34,136,238,0.06)`
- Active: color `#55bbff`, background `rgba(34,136,238,0.08)`, bottom border 2px `Accent`
- Implement as `Button` nodes (not labels) for proper click targets

### Sub-header row (below tabs)

```
FLEETS        ALL-CAPS Share Tech Mono
```
- Barlow Condensed Regular, 8px, ALL-CAPS, letter-spacing 2px, color `TextFaint`
- Padding: `4px 14px`
- Bottom border: 1px `BorderDim`

### Fleet List Items

Each item is a `Button` node (for 40px minimum click target), custom styled:

```
[Fleet Name]                [STATUS]     ← row 1
[Location · N ships]                     ← row 2  
[▲ pip][▲ pip][● pip][■ pip]            ← row 3: ship pips
```

**Row 1:**
- Fleet name: Exo 2 SemiBold, 12px, ALL-CAPS, letter-spacing 0.5px, color `TextBright`
- Status tag: Share Tech Mono, 8px, letter-spacing 1px
  - Default (PATROL): color `TextDim`
  - Alert (UNDER FIRE): color `#ff5540`, text-shadow glow `rgba(255,64,40,0.5)`
  - Moving (EN ROUTE / EXPLORING): color `#ffcc44`
  - Status has a colored badge background: `rgba(color, 0.15)`, padding `1px 4px`

**Row 2:**
- Barlow Condensed Regular, 10px, color `TextDim`

**Row 3 — Ship pips** (5×5px shapes, 3px gap):
- Fighter/Corvette: triangle `clip-path: polygon(50% 0, 100% 100%, 0 100%)`, color `rgba(80,120,160,0.5)`
- Capital (Destroyer+): rectangle 8×5px, color `rgba(120,170,210,0.75)`
- Salvager: circle, color `#22bb44`
- Builder: rectangle 8×5px, color `#ddaa22`
- Damaged: any shape, color `#f04030`, opacity 0.85

**Item states:**
- Default: left border 2px transparent
- Hover: background `rgba(34,136,238,0.07)`
- Selected: background `rgba(34,136,238,0.10)`, left border 2px `Accent`

**Dividers between items:** 1px `rgba(30,50,72,0.5)`, inset 14px each side

**Minimum item height:** 56px (to fit 3 rows comfortably + pips)

---

## RIGHT PANEL

**Position:** `top=68px, right=0, bottom=0`, width **275px**  
**Background:** `GlassDark` + `BorderBright` left edge

### System Header

Padding: `12px 16px`. Bottom border: 1px `BorderDim`.

```
Exo 2                    ← Barlow Condensed 8px, TextFaint, top label showing current font
CYGNUS PRIME             ← Exo 2 SemiBold, 18px, TextBright, ALL-CAPS, letter-spacing 3px
                         ← text-shadow: 0 0 18px rgba(34,136,238,0.3)
```

Below name: `Share Tech Mono 9px, TextFaint, letter-spacing 2px` — shows arm name + POI count.  
POI count: color `#55aaff`.

### POI Section Header

```
POIs          ← Barlow Condensed SemiBold, 9px, ALL-CAPS, letter-spacing 2px, TextFaint
```
Padding: `8px 16px 4px`.

### POI List Items

Left border 2px by POIType (from `src/Core/Enums/POIType.cs`):
- HabitablePlanet → `#22bb44` (green — colonizable)
- BarrenPlanet → `#7b9eb5` (dim — limited use)
- AsteroidField → `#ddaa22` (gold — mining)
- DebrisField → `#9944dd` (purple — salvage)
- AbandonedStation → `#9944dd` (purple — salvage)
- ShipGraveyard → `#f04030` (red — danger/salvage)
- Megastructure → `#2288ee` (blue — special)

Per item (padding `8px 16px`):

**Line 1:**
- Bullet dot (4px circle, faction color with glow) + POI name label: Barlow Condensed SemiBold, 11px, TextLabel, ALL-CAPS
- Type tag (Share Tech Mono 8px, TextDim, border 1px BorderDim, padding `1px 4px`)

**Line 2–3:** Share Tech Mono 9px, TextDim. Keys in TextFaint.
Display actual data based on POIType:
- HabitablePlanet (colonized): `POP [n]/[max]  HAP [n]%`
- HabitablePlanet (uncolonized): `SIZE [Small/Medium/Large/Prime]  TERRAIN [modifier]`
- AsteroidField: `YIELD [resource] ×[n]/cycle`
- DebrisField / AbandonedStation / ShipGraveyard: `COLOR [faction]  STATUS [exploration state]`
- Megastructure: `COLOR [faction]  STATUS [exploration state]`
- BarrenPlanet: `SIZE [size]  DEPOSITS [n]`

### Action Buttons

4 full-width `Button` nodes, stacked vertically. Padding: `12px 16px`. Min height: 40px.

- Font: Barlow Condensed SemiBold, 10px, ALL-CAPS, letter-spacing 2px
- Primary (SEND FLEET): fill `rgba(34,136,238,0.16)`, border `rgba(34,136,238,0.45)`, text `#55bbff`
- Secondary (BUILD MINING STATION, EXPLORE, SCAN): fill `rgba(16,28,48,0.70)`, border `BorderDim`, text `TextBody`
- Hover state: fill `rgba(34,136,238,0.12)`, border `BorderBright`

### Recent Events (inside right panel, bottom section)

Header: Barlow Condensed SemiBold, 8px, ALL-CAPS, letter-spacing 2px, TextFaint  
Border top: 1px `BorderDim`

Each event row:
- 4px colored circle (faction color, glow shadow)
- Event name: Exo 2 Medium, 10px, TextBody — **this is the fleet/entity name**
- Event detail: Barlow Condensed Regular, 9px, TextDim, on second line
- Timestamp: Share Tech Mono, 8px, TextFaint, right-aligned

---

## GALAXY MAP

**Node:** GalaxyMap (Node3D), rendered behind the CanvasLayer UI. Already exists at `src/Nodes/Map/GalaxyMap.cs`.

### Background
Radial gradient (use a shader or pre-baked texture):
```
center (52%, 44%): #071525 → #030810 at 60% → #010408
```

### Stars
~300 points: radius 0.15–0.45px at low opacity. ~5% "bright" stars up to 1.5px. Color: `#90b0c8`.

### Nebula arm glows
5 large `PointLight2D` or blurred `Sprite2D` ellipses, one per faction color, 7% opacity, stdDeviation ~42px equivalent, positioned toward each arm.

### Starlanes

**Normal lane** — draw as two Line2D nodes stacked:
1. Glow underlayer: width 3px, color `#3366aa`, alpha 0.15
2. Main line: width 1.2px, color `#3a6090`, alpha 0.75

**Hidden lane** (only visible with Hauler origin or tech):
- Dashed Line2D: width 1.2px, color `#8833cc`, alpha 0.45, dash pattern `[5, 7]`

### System Node Types — distinct shapes per type

System nodes are rendered via `StarRenderer` and `StarSystemNode` (Node3D). Visual differentiation based on POI contents:

| System contains | Shape | Notes |
|---|---|---|
| Normal (no notable POI) | Circle | Faction color dot |
| HabitablePlanet (colonized) | Circle + inner ring | Inner ring at 60% radius, white stroke |
| AbandonedStation / ShipGraveyard | Diamond (rotated square) | Draw as 4-point polygon, X lines inside |
| DebrisField | 8-point star | Alternating radius points, danger indicator |
| AsteroidField | Irregular 7-point polygon | Random-jitter the vertices at spawn |
| Megastructure | Hexagon | Larger than normal nodes |

**Unowned:** faction base color, 65% opacity, no glow, halo 6% opacity  
**Owned:** faction glow color, 92% opacity, `PointLight2D` for glow effect, halo 18% opacity, bright center dot at 30% radius

**Selected system reticle** (tight, per spec):
- Outer dashed ring: radius+6px, `#2288ee`, 40% alpha, dash `[3,4]`
- Inner solid ring: radius+4px, `#44aaff`, 80% alpha, 1.2px stroke
- 4 cardinal tick marks: from radius+3 to radius+7, `#55ccff`, 1.2px

**Danger pip:** 2.8px red circle `#f04030` with glow, offset top-right of node.

**Node label** (below node):
- Exo 2, 8px (9px if selected), weight 400 (600 if owned)
- Owned: `#c8dde8` | Selected: `#55bbff` | Unowned: `#4a6880`
- Letter-spacing: 1.5px, ALL-CAPS

### Minimap

Position: `bottom=16px, left=284px`. Size: 100×100px. `GlassDark` fill, `BorderBright` border.

- Label "SECTOR MAP": Share Tech Mono 6px, TextFaint, top-left
- Each system: 3px dot (unowned) or 5px dot (owned), faction colors
- Derelict nodes: rotated square instead of circle on minimap
- Viewport rectangle: 1px `rgba(34,136,238,0.45)` border, `rgba(34,136,238,0.05)` fill

---

## FLOATING WIDGETS

### Speed + Time Widget

Position: `bottom=16px`, right of the minimap (~`left=392px`).  
`GlassDark` fill, `BorderBright` border. Padding: `8px 14px`.

Layout (HBoxContainer):
```
[T-142]  [CYCLE label]  [divider]  [SPD label]  [⏸] [×1] [×2] [×4] [×8]
```

- Turn number: Exo 2 SemiBold, 16px, TextBright, letter-spacing 3px
- "CYCLE": Share Tech Mono, 7px, TextFaint, letter-spacing 2px, below the number
- "SPD": Share Tech Mono, 8px, TextFaint, letter-spacing 2px
- Speed buttons: min-width 36px, padding `5px 10px`
  - Inactive: fill `rgba(16,24,40,0.60)`, border `BorderDim`, text `TextDim`
  - Active: fill `rgba(34,136,238,0.22)`, border `Accent`, text `#55bbff`, glow
  - Hover: border `BorderBright`, text `TextLabel`

---

## SCANLINE OVERLAY

Full-screen `ColorRect` at z=9999, `MOUSE_FILTER_IGNORE`. Apply this shader:

```glsl
shader_type canvas_item;

void fragment() {
    float line = mod(FRAGCOORD.y, 4.0);
    COLOR = vec4(0.0, 0.0, 0.0, line < 1.0 ? 0.05 : 0.0);
}
```

---

## Z-INDEX LAYER MAP

```
0    Galaxy map background (stars, nebulae)
10   Galaxy overlay (lane lines, system nodes, fleet icons)
50   Side panels (left, right)
60   Floating widgets (minimap, speed+time, map overlay buttons)
100  Topbar
200  Tooltips (PopupPanel)
999  Modal dialogs
9999 Scanline overlay
```

---

## WHAT NOT TO BUILD (on this screen)

- No color expertise bars (Empire Overview screen only)
- No turn-end button (real-time game)
- No resource names visible in topbar (tooltip only)
- No fleet detail panel (opens as separate overlay on click)
- No rounded corners anywhere
- No drop shadows anywhere
