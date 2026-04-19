# Derelict Empires — Ship Designer, Combat HUD, and Research Integration UI Spec

_Version 1.0 — April 2026_
_Supplement to `derelict_empires_ui_spec.md` and `derelict_empires_research_ui_spec.md`._

---

## 0. Scope and Relationship to Existing Specs

This document specifies three tightly-coupled surfaces that together form the core strategic-preparation loop of Derelict Empires:

- **Ship Designer** — a full-screen overlay for authoring ship designs and fleet templates. New.
- **Combat HUD** — the in-battle interface for real-time disposition control and post-battle debrief. New.
- **Research integration** — the additions to the existing research surfaces (topbar strip, RESEARCH tab, Tech Tree overlay) that make research legible from within the Designer and Combat HUD. Extends `derelict_empires_research_ui_spec.md`; does not supersede it.

All three surfaces **inherit the visual system** of `derelict_empires_ui_spec.md` without exception — glass material, faction color tokens, Exo 2 / Rajdhani / IBM Plex Mono typography, 40/36/44px click target minimums, no rounded corners, no drop shadows. No new colors, fonts, or materials are introduced here.

What is new here is:

1. The **interactions and information architecture** for the two new surfaces.
2. The **deep links** between all three surfaces so that a player never has to navigate by memory.
3. A pre-spec **commitment-shape characterization** of every key interaction, because the right UI technique depends on the shape of the decision (a stream needs rate/state legibility; a repeated-discrete interaction needs aggregation or delegation; applying the wrong technique produces wrong fixes).

---

## 1. Design Principles for the Trinity

### 1.1 Player-Experience Goal (PXG)

> The player should feel like a **resourceful scavenger-king** at every one of these three surfaces. Research is "which imperfect precursor tech did I adopt this run." Design is "how did I jury-rig it into a hull that might survive." Combat is "does the improvisation hold up, or do I learn what to cobble together next."

These three surfaces are the loop the GDD names as the core fantasy. A flow that is clean, fast, and spreadsheet-like at any of them scores well on standard usability but **fails the fantasy**. The right design makes space for improvisation — exposed trade-offs, visible efficiency penalties, obvious off-color costs — rather than hiding them behind a clean "optimal" answer.

### 1.2 Design Pillars Honored (by reference)

From the GDD and project context:

1. **Minimal micromanagement** — aggregation, templates, and fleet-role-level control, not per-ship control.
2. **The map is the game** — Combat HUD in particular overlays the galaxy, never replaces it. Designer is a modal overlay (like the tech tree), not a dedicated scene.
3. **Salvage everything** — locked module slots surface salvage hints inside the Designer. Combat wreckage links back to exploration.
4. **Nothing is locked** — cross-color use is always permitted with a visible efficiency multiplier, never a hard stop.
5. **Information asymmetry vs. UI clarity** — the fog of war is mechanic; UI opacity is a bug. Pre-combat reveal is gated by research (Blue Pre-Combat Scan), not by UI quirk.
6. **The 5th X — eXchange** — the Designer makes locked modules open a **Buy / Rent / Salvage** picker, not just a "research more" nag.

### 1.3 Shared Conventions

Across all three surfaces:

| Convention | Rule |
|---|---|
| Color language | Every subsystem, resource, or tech reference is **always** rendered in its faction's glow color on dark backgrounds. Never base color for text. |
| Expertise visibility | **Any time a specific module is referenced, its current expertise multiplier appears next to it in `IBM Plex Mono` 9px**: `1.4×` (on-color bonus), `0.7×` (cross-color penalty). This is the same number shown in the Tech Tree overlay's Expertise Bar (§5.4 of research spec) — never computed differently across surfaces. |
| Off-color cost | A multi-color design's supply cost is shown as a **per-color drain strip**, not a single number. This is the deep-trade pillar made visible. |
| Locked-slot affordance | A locked module or unavailable option **never** renders a flat "unavailable" state. It always offers one or more unlock paths: `[RESEARCH]`, `[BUY]`, `[RENT]`, `[SALVAGE HINT]`. Players should never hit a dead end. |
| Deep-link style | Cross-surface navigation uses a consistent **glass-chip** affordance: a 20px-tall pill with a left colored accent, an Exo 2 9px label, and a `→` glyph. Tapping it opens the target surface with state pre-selected. |

---

## 2. Commitment-Shape Characterization

**Why this section exists:** the skill's Mode B pre-pass-2 demands that before prescribing any UX technique, the mechanic's commitment shape is explicitly named. Applying a repeated-discrete technique (delegation, aggregation) to a stream, or a one-shot technique (one-time commitment, tiered info) to a repeated-discrete problem, produces wrong fixes. This table is the governing analysis for the rest of the spec.

| Interaction | Surface | Commitment shape | Why | Primary technique applied | Anti-pattern avoided |
|---|---|---|---|---|---|
| Selecting a chassis for a new design | Designer | **One-shot** | Pay once; design is saved; chassis rarely changes within a design | #6 tiered info (chassis stats on hover, comparison on click) | Black-box combat (chassis differences must be legible pre-commit) |
| Filling a slot | Designer | **Repeated-discrete within a one-shot** | Many slots per design; each is a small decision | #15 intent routing ("best available Red weapon big"), #4 templates for reinforcement | Optimization tax (each slot has an "obvious optimal") |
| Authoring a fleet template | Designer | **One-shot** (with low-frequency revision) | Set once, reused across builds and reinforces | #4 fleet templates with one-click reinforce, #12 aggregate (ship-as-unit, fleet-as-unit) | Stellaris pop-click (individual ships must never demand attention) |
| Refit existing fleet to new template | Designer → Fleet | **Conditional trigger** | Set up once; fires on next dock | H3 predictable-AI (refit rules are visible, editable, and predictable) | Auto-governor distrust |
| Engage / don't engage combat | Combat HUD | **Pre-commit with reveal** | Player commits (engage) before seeing outcome; reveal is partial without scan tech | #6 tiered info (pre-combat preview depth scales with research: Blue Pre-Combat Scan at Cat 5 T3 opens full reveal) | Black-box combat |
| Watching a battle progress | Combat HUD | **Stream** | Continuous simulation; player can exit (retreat) any time | Rate/state legibility on fleet HP bars; subsystem-event notifications; #10 auto-pause on meaningful events | "I have to babysit this activity" |
| Changing disposition mid-battle | Combat HUD | **Repeated-discrete within a stream** | Fleet-role-level, not ship-level; small finite set of choices | #8 batching (select role, apply disposition to all ships in role); aggressive aggregation | 1-UPT tactical gridlock |
| Changing target priority mid-battle | Combat HUD | **Repeated-discrete within a stream** | Per-role priority list; reordered occasionally | Role-scoped priority editor; drag-to-reorder | Mystery orb (verbs must be visible) |
| Retreating | Combat HUD | **One-shot (mid-stream exit)** | Decisive; emergent cost from simulation state | Retreat preview shows current range-to-safety and expected exit damage | Black-box combat |
| Post-battle salvage | Combat → Exploration | **One-shot** | Commits once post-battle | Overkill-vs-quality is shown during combat as a live projection, not only after | Black-box combat |
| Starting research from a locked slot | Designer → Research | **One-shot** (same as research start) | Opens tech tree; one selection | Deep-link consistency (same commit action as tech tree's primary button) | Terminology drift |
| Banked-memory update from combat | Combat → Research | **Conditional trigger** (only expertise, not module memory) | Combat drives expertise, not module memory | H3 predictable-AI (rules of what combat contributes are visible in a tooltip) | Auto-governor distrust |

**Key implication of this table:** Combat is primarily a **stream** interaction with a **pre-commit** at the front. The UI for a stream is not the UI for a repeated-discrete problem. The HUD must optimize for *mid-activity legibility* (is this battle going well? am I still in the prime zone? when should I leave?) rather than for minimizing clicks. Paradox's disposition-click-count is not the metric — decision legibility is.

---

## 3. Ship Designer — Full Spec

### 3.1 Purpose and Entry Points

The Ship Designer is a **full-screen overlay** (glass material, opacity transition 120ms, same treatment as the Tech Tree overlay) for authoring ship designs and fleet templates. It never interrupts simulation speed; the galaxy continues behind the glass.

**Entry points:**

- Left panel **BUILD** tab → `[+ NEW DESIGN]` button at top of designs list
- Left panel **BUILD** tab → click an existing design row
- Left panel **FLEETS** tab → fleet row → `[REFIT]` chip (opens designer scoped to that fleet's template)
- Combat Debrief → `[IMPROVE DESIGN]` chip (opens designer scoped to the design used in that battle)
- Hotkey `D`
- Tech Tree overlay → module focus panel → `[USE IN DESIGN]` chip (opens designer with that module pre-selected for a matching slot)

Every entry point **preserves the player's current galaxy camera and selection contexts**; closing the designer returns to exactly that state. The three orthogonal selection contexts from the main UI spec (Selected Fleet, Selected System, Selected POI) are never cleared by the designer.

### 3.2 Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│  SHIP DESIGNER · EDITING: Brawler Cruiser Mk II     [ SAVE ] [ ✕ ] │  ← 48px header
│  ─────────────────────────────────────────────────────────────────  │
│                                                                     │
│  ┌─────────────────────┐  ┌──────────────────────┐  ┌────────────┐│
│  │ CHASSIS             │  │ SLOT MATRIX          │  │ PROFILE    ││
│  │ ───────────         │  │                      │  │ ──────     ││
│  │ [thumbnail]         │  │  W1 ▮ Red Plasma T2  │  │ HP         ││
│  │                     │  │  W2 ▮ Red Plasma T2  │  │ Armor      ││
│  │ CRUISER — BRAWLER   │  │  W3 ○ [ EMPTY ]      │  │ Shields    ││
│  │ Variant: Aggressive │  │  D1 ▮ Red Armor T1   │  │ Speed      ││
│  │                     │  │  D2 ● [LOCKED T2 ]   │  │ Visibility ││
│  │ Slots:  3W 2D 1E 1S │  │  E1 ▮ Red Engine T1  │  │ Supply     ││
│  │ Base HP: 900        │  │  S1 ▮ Blue Scan T1   │  │ ──────     ││
│  │ Base speed: 85      │  │  U1 ○ [ EMPTY ]      │  │ ROLE       ││
│  │ Visibility: +15%    │  │                      │  │ Brawler ▾  ││
│  │ [ CHANGE CHASSIS ]  │  │  + 3 locked slots    │  │            ││
│  └─────────────────────┘  └──────────────────────┘  │ [COST]     ││
│                                                      │ Red  ▬▬▬▬ ││
│                                                      │ Blue ▬    ││
│                                                      │ Supply /s ││
│                                                      └────────────┘│
│  ─────────────────────────────────────────────────────────────────  │
│  FLEET TEMPLATES USING THIS DESIGN: 2 · [ SEE TEMPLATES ]          │  ← footer bar
└─────────────────────────────────────────────────────────────────────┘
```

**Dimensions:**

- Header: 48px, full width
- Main area fills remainder, 24px padding
- Left pane (Chassis): 260px fixed
- Centre pane (Slot Matrix): flex, min 420px
- Right pane (Profile): 280px fixed
- Footer bar: 40px, only visible when design is saved and referenced by one or more fleet templates

### 3.3 Chassis Pane (left)

Shows the currently selected chassis. Serves as a one-click path into the **Chassis Picker** sub-overlay.

**Content:**

- **Thumbnail**: 220×140px, schematic view of the chassis silhouette. No render fidelity — line-art only, in `TextFaint` with the chassis size-class keyed in `TextLabel`. The salvage-punk aesthetic is carried by the cracked-glass frame, not by busy illustration.
- **Class + Variant**: `Exo 2 13px` weight-600 ALL-CAPS. Class (`CRUISER`) in `TextBright`, variant (`Aggressive`) in `TextLabel`.
- **Slot summary**: `IBM Plex Mono 10px`. `3W 2D 1E 1S` means 3 weapon, 2 defense, 1 engine, 1 sensor. Universal slots shown as `U`.
- **Base stats**: 4 lines (HP, speed, visibility, supply base). Each: `Rajdhani 10px` label in `TextDim`, `IBM Plex Mono 10px` value in `TextLabel`.
- **`[ CHANGE CHASSIS ]`**: 36px button, full-width, opens the Chassis Picker.

#### 3.3.1 Chassis Picker (sub-overlay)

A sub-overlay centered over the designer. 680×460px. Shows the 14 chassis (7 size classes × 2 variants) as a 7-row grid. Each row is one size class; the two variant cells sit side by side.

```
FIGHTER     │  Interceptor ✓         │  Skirmisher ✓
CORVETTE    │  Gunship ✓             │  Scout-Corvette ✓
FRIGATE     │  Escort ✓              │  Raider ✓
DESTROYER   │  Line Destroyer ✓      │  Stealth Destroyer ⚠
CRUISER     │  Aggressive ✓          │  Utility ✓
BATTLESHIP  │  Dreadnought · SHIPYARD│  Carrier-BB · SHIPYARD
TITAN       │  Flagship · SHIPYARD   │  Worldbreaker · SHIPYARD
```

- `✓` (in `#66dd88`): can be built at at least one owned shipyard today.
- `⚠` (in `#ffcc44`): chassis class unlocked but no qualifying shipyard owned. Tooltip explains: "Needs a Tier 3 shipyard. Nearest capable: none."
- `SHIPYARD` tag (in `TextDim`): chassis class gated by shipyard size only. Hover: list of shipyard upgrades that would unlock it.
- Cell size: 310×54px. Each cell is clickable; hover reveals the chassis's full stat sheet in a side tooltip.
- Selecting a cell closes the picker and swaps the chassis in the main designer. **Existing slot fills migrate** where slot counts match; mismatched slots are orphaned into an "unassigned modules" tray shown at the bottom of the Slot Matrix with a `[REASSIGN OR DISCARD]` prompt. No silent loss of work.

**Anti-pattern avoided:** "Mystery orb" — the player always sees every chassis and the specific reason any is unavailable.

### 3.4 Slot Matrix (centre)

The primary editing surface. One row per slot.

**Row structure:**

| Zone | Width | Content |
|---|---|---|
| Slot ID | 36px | `W1`, `D2`, `E1`, etc. — `IBM Plex Mono 10px` letter-spacing 1px, color `TextDim`. Faint to keep focus on the module name. |
| Slot-type glyph | 20px | Icon indicating type (weapon, defense, engine, sensor, universal) and size (big/small) — small slots get a smaller glyph. Color `TextLabel`. |
| State dot | 12px | `▮` filled (glow color) if filled; `○` hollow if empty; `●` dimmed faction color if locked. |
| Module name | flex | Faction glow color if filled; `TextDim` if empty; dim faction base if locked. `Exo 2 11px` weight-500 ALL-CAPS. |
| Expertise multiplier | 44px | `IBM Plex Mono 9px`, faction glow. `1.4×`, `0.7×`, etc. Only shown when filled. |
| Hover affordance | — | Row expands vertically on hover (36→56px) to reveal stat line and `[CHANGE]` / `[CLEAR]` chips. |

**Empty slot behavior:** clicking an empty slot opens the **Slot Picker** dropdown in place (not a modal; collapses inline).

**Locked slot behavior:** clicking a locked slot opens the **Unlock Picker** (see §3.7) which offers 3–4 paths: Research, Buy, Rent, Salvage Hint.

**Bulk operations:** right-click on any row opens a contextual menu: `Clear`, `Copy to all compatible slots`, `Promote to template modifier` (the template modifier is an instruction like "always fill this slot with the best Red weapon big"; see §3.10).

#### 3.4.1 Slot Picker (inline dropdown)

When the player clicks an empty slot, a dropdown expands in place showing all modules compatible with the slot type and size.

**Structure of the dropdown (280px wide, up to 360px tall, scrollable):**

```
▮ RED          ← color subheader, Exo 2 10px, faction glow
  Red Plasma T1     12 dmg · 1.2s · 1.4×
  Red Plasma T2     24 dmg · 1.0s · 1.4×  ⧗ RESEARCH IN 3:12
  Red Rail T1       40 dmg · 2.0s · 1.4×
▮ BLUE
  Blue Pulse T1     8 dmg · 0.4s · 0.7×  ← cross-color penalty shown
  Blue Pulse T2     [ LOCKED — salvage, trade, or research ]
▮ GREEN
  (no researched modules)
...
─────────────────
[ SHOW LOCKED ▾ ]
[ BEST AVAILABLE ▸ ]   ← intent-routing shortcut
```

- **Grouped by faction**, each color as a subheader with the faction left-accent bar.
- **Rows sorted** within a color by tier, then name. Visible stats are the three most relevant for the slot type (weapons: dmg, rate of fire, expertise multiplier; defense: HP, regen, expertise; etc.).
- **Currently-researching modules** appear with a `⧗ RESEARCH IN 3:12` tag in `#ffcc44` (en-route color) — the player sees them *before* they're available so they can plan.
- **Locked modules** appear only if `[SHOW LOCKED]` is toggled on. Clicking a locked module opens the **Unlock Picker** (§3.7).
- **`[ BEST AVAILABLE ]`** intent-routing button: picks the highest expertise-weighted module for the slot from currently-researched options. One click. This is **Technique #15 (intent routing)** — the routing is busywork; the intent ("I want the best Red weapon available") is the meaningful decision. Players who want to fine-tune can ignore it.

The `BEST AVAILABLE` button has an optional **color filter** — Shift-click cycles through "Best Red / Best Blue / … / Best Any". This is the only chord in the designer.

### 3.5 Profile Pane (right)

Shows the projected stats and costs of the current design. Updates live as slots change.

**Sections (top to bottom):**

1. **Stats Block** — HP, Armor (flat reduction), Shield HP + regen, Speed, Visibility (lower is better), Supply consumption (base). Each is a row: `Rajdhani 10px` label / `IBM Plex Mono 11px` value / delta since last save (`+14`, `-22`) in `#66dd88` or `#ff6655` if applicable.
2. **Role Assignment** — a dropdown of the 6 fleet roles (Brawler, Guardian, Carrier, Bombard, Scout, Non-Combatant). Changing this sets the default role this design will take in fleet templates. Hover tooltip shows role behavior: "Brawler: charges forward, focuses closest threat."
3. **Supply Cost Strip** — **the deep-trade pillar made visible.** One horizontal strip per color that the design consumes:
   ```
   Red   ▬▬▬▬▬▬▬▬░░  +4.2/s
   Blue  ▬▬░░░░░░░░  +1.1/s
   Supply /s in combat
   ```
   - Strip height 3px, `IBM Plex Mono 8px` rate label.
   - Faction glow fill, faction base track.
   - The strip makes **multi-color ships visibly demanding** — a three-color ship shows three strips. This is the pillar working for the player, not against them.
4. **Build Requirements** — a requirements block shown only when building is possible:
   - Time to build at nearest shipyard
   - Component cost (itemized by color)
   - `[BUILD AT: Shipyard Ashur-3 ▾]` dropdown → updates time/cost based on shipyard and nearest supply.

If a design is **not buildable** (missing research, missing shipyard tier, color resource below threshold), the Build Requirements block becomes a **blocker list**:

```
CANNOT BUILD
  ⛔ Slot D2 is LOCKED — unlock to complete design
  ⚠ Red components: 340 / 500 needed (need +160)
  [ RESOLVE BLOCKERS ]
```

`[RESOLVE BLOCKERS]` expands the list with one-click deep links: `[RESEARCH]` for locked slots, `[BUY RED COMPONENTS]` for resource shortfalls (jumps to market). Each blocker is actionable — no dead-ends.

### 3.6 Header Controls

- **`EDITING:` label** — shows current design name. Click to rename inline (becomes a text field, Enter commits). Default name on new design: `[Chassis class] Draft` (e.g., `Cruiser Draft`).
- **`[ SAVE ]`** — primary action. Disabled until all required slots are filled. Tooltip on disabled: "Fill slots W1, W2, D1, E1 to save."
- **`[ ✕ ]`** — close. If there are unsaved changes, prompts with `Discard changes?` — three choices: `Discard`, `Save`, `Cancel`.

### 3.7 Unlock Picker (for Locked Slots)

When a player clicks a locked module, a centered glass dialog (520×320px) opens. This is **the primary place the 5th X (eXchange) pillar touches the designer.**

```
┌────────────────────────────────────────────────┐
│ UNLOCK: RED RAIL T2                     [ ✕ ]  │
│ ────────────────────────────────────────────── │
│ Kinetic, high-damage rail. Best flat DPS at    │
│ this tier.                                     │
│                                                 │
│ HOW TO UNLOCK:                                 │
│                                                 │
│ ▮ RESEARCH                                     │
│   Red Weapons T3 tier (62% banked)             │
│   Est. 14:22 from now at current rate          │
│   [ RESEARCH NOW ]                             │
│                                                 │
│ ▮ BUY MODULE DESIGN                            │
│   Available on market: 2 listings              │
│   Lowest price: 340 credits                    │
│   [ OPEN MARKET ]                              │
│                                                 │
│ ▮ RENT FROM EMPIRE                             │
│   Verdant Synthesis offers Red Rail T2         │
│   120 credits / cycle                          │
│   [ OPEN DIPLOMACY ]                           │
│                                                 │
│ ▮ SALVAGE HINT                                 │
│   3 known Red T2 derelicts in scan range.      │
│   Nearest: Tarsus-IV — 8 lanes away.           │
│   [ SHOW ON MAP ]                              │
└────────────────────────────────────────────────┘
```

Each unlock path has its own row with a colored accent:

- **RESEARCH**: green accent (`#22bb44`) if the tier is already being researched; `TextDim` if not. `[RESEARCH NOW]` deep-links to the Tech Tree overlay scoped to Red Weapons T3 with the tier pre-selected (same entry as §5 of the research spec).
- **BUY MODULE DESIGN**: gold accent if listings exist; `TextDim` "No listings" if none. `[OPEN MARKET]` jumps to the (future) market screen with a pre-filter.
- **RENT FROM EMPIRE**: gold accent if any empire has it and will rent; `TextDim` otherwise. `[OPEN DIPLOMACY]` jumps to diplomacy with the empire pre-selected.
- **SALVAGE HINT**: red accent if any known derelicts match; `TextDim` otherwise. `[SHOW ON MAP]` closes the designer, switches to galaxy map, pans camera to nearest candidate, and sets a waypoint ping.

**Rule:** if *no* unlock paths are available (truly dead-end), the picker still opens with a row labeled `NOTHING FOUND` and a suggestion — "Scout more systems to surface salvage options, or research the tier directly." The player never sees a disabled dialog with nothing to do.

**Anti-pattern avoided:** Mystery orb (all verbs visible), Black-box combat (every unlock source is labeled and costed), Auto-governor distrust (unlock rules are not automation, they are a menu — the player chooses).

### 3.8 Build Queue (BUILD Tab, Left Panel)

The BUILD tab in the left panel (the 4th tab in `FLEETS · COLONIES · RESEARCH · BUILD`) is the **per-colony build queue manager**. The Ship Designer *authors* designs; the BUILD tab *uses* them.

**Structure (within the 270px left panel):**

```
FLEETS · COLONIES · RESEARCH · BUILD
────────────────────────────────────
SHIPYARD SELECTOR: Ashur-3 ▾ (3 shipyards)
────────────────────────────────────

ACTIVE BUILD
  ▮ Brawler Cruiser Mk II
    ▬▬▬▬░░░░░ 48%  ~4:22
    [ CANCEL ]

QUEUE (3)
  1 ▮ Brawler Cruiser Mk II ×3  [✕]
  2 ▮ Scout Corvette ×2         [✕]
  3 ▮ Bombard Destroyer ×1      [✕]
  [ + ADD DESIGN ]

────────────────────────────────────
DESIGNS (14 saved)
  ▮ Brawler Cruiser Mk II   [BUILD] [EDIT]
  ▮ Scout Corvette          [BUILD] [EDIT]
  ▮ Bombard Destroyer       [BUILD] [EDIT]
  ...
  [ + NEW DESIGN ]
```

- **SHIPYARD SELECTOR**: if the empire has multiple shipyards, the dropdown picks which one this tab is showing. Default: most recently used.
- **ACTIVE BUILD**: the currently-building ship. Shows progress bar (same fill rules as research bars, 3px tall), ETA, and `[CANCEL]` (prompts confirmation, refunds ~60% of resources).
- **QUEUE**: up to 10 entries. Each entry is a design + quantity. `[✕]` removes; drag-handle reorders.
- **`[ + ADD DESIGN ]`**: opens a picker listing all saved designs buildable at the current shipyard. Designs that can't be built here (too-large chassis, locked slots) are shown dimmed with the blocker tooltip from §3.5.
- **DESIGNS list**: all saved designs. `[BUILD]` adds to queue; `[EDIT]` opens the Ship Designer scoped to that design.

**Micromanagement technique applied: #4 fleet templates with one-click reinforce, #8 batching.** A design + quantity is a batched order. Reinforce-fleet calls use this queue implicitly.

### 3.9 Fleet Templates — The Aggregation Layer

A **fleet template** is a named collection of (ship design, count, role) tuples. It is the unit of strategic thinking — the player designs at the *fleet* level, not the *ship* level.

**Where fleet templates live:** Fleet templates appear as a sub-section inside the Ship Designer itself when the designer is opened in **template mode** (entry point: FLEETS tab → `[+ NEW TEMPLATE]`).

**Template editor layout:**

```
FLEET TEMPLATE: Frontier Striker
────────────────────────────────
COMPOSITION
  3 × Brawler Cruiser Mk II     [EDIT DESIGN]
  2 × Scout Corvette            [EDIT DESIGN]
  1 × Guardian Frigate Mk I     [EDIT DESIGN]
  [ + ADD SHIP TYPE ]

FLEET ROLE DEFAULTS
  Brawler:    Charge forward ▾
  Scout:      Stand back ▾
  Guardian:   Hold mid ▾

SUPPLY PROFILE (combined)
  Red   ▬▬▬▬▬▬▬▬▬▬  +18/s
  Blue  ▬▬▬▬         +6/s
  Total credit cost (build): 4,820

APPLIES TO: 2 fleets
  · Strike Fleet Aleph (reinforce needed: 1 ship)
  · Strike Fleet Beth (at template)
  [ REINFORCE ALL ]
```

**Key interactions:**

- **`[REINFORCE ALL]`** — one-click reinforcement. Technique #4 exemplar. Sends build orders to the nearest shipyards of each fleet to refill missing ships. Progress is tracked in the BUILD tab.
- **`[EDIT DESIGN]`** — opens the Designer scoped to that design. Changes propagate to the template automatically. Fleets currently *at template* show `REFIT PENDING` and, at next dock, offer `[APPLY NEW DESIGN]`.
- **Supply Profile** is the sum of member-ship supply strips. Multi-color templates show multi-color strips — the logistics burden of a diverse fleet is visible *before* it's built.

**Anti-pattern avoided:** Stellaris pop-click (individual ships never addressable). The fleet template is the aggregate entity. Technique #12.

### 3.10 Template Modifiers (optional, advanced)

An advanced player can promote a slot fill to a **template modifier** — an instruction that applies when the fleet is reinforced, not the literal module choice.

Examples:
- "Slot W1: Best available Red weapon big"
- "Slot S1: Any researched sensor"

When the fleet is reinforced, the modifier is resolved at build time using the current research state. This lets a template **evolve with research** without manual edits. Technique #15 (intent-level routing) applied to a conditional trigger.

**UI:** in the Slot Matrix, right-click → `Promote to template modifier` → slot display changes from the module name to the rule in `TextDim` italic. Template modifiers are visually distinct from literal fills.

**Anti-pattern avoided:** Auto-governor distrust (H3) — the modifier is *stated*, not inferred, and the player sees exactly which modules each rule would pick today via a hover preview.

### 3.11 Expertise Visibility

Every module reference in the designer shows its current **empire-wide expertise multiplier**. This number is:

- **1.0×** — baseline
- **1.4×** or higher — on-color expertise bonus (e.g., Red-affinity empire using Red modules with maxed General Color Expertise)
- **0.7×** or lower — cross-color penalty (e.g., Red-affinity empire using Purple modules)

Hovering the multiplier reveals the breakdown:

```
EXPERTISE: 1.4×
  General Red:    +0.3×  (from 12,400 Red XP)
  Subsystem:      +0.1×  (from 220 Red Plasma T2 builds)
  Faction affinity: +0.0× (Red-origin empire, applied base)
```

This satisfies pillar #4 (nothing locked, but efficiency obvious) and reinforces pillar #7 (color is a language). The same breakdown appears in the Tech Tree overlay's Expertise Bar (research spec §5.4) — computed identically across surfaces.

---

## 4. Combat HUD — Full Spec

### 4.1 Entry Points and Pre-Commit

Combat begins when two or more hostile fleets enter the same star system (or meet mid-lane; lane combat is rare but uses identical UI). The player's response has two phases:

**Phase 0: Pre-Combat Decision.**

When the simulation detects imminent combat, **the game auto-pauses** (Technique #10, table stakes) and a Pre-Combat dialog appears:

```
┌──────────────────────────────────────────────┐
│ ENGAGEMENT IMMINENT · Tarsus-IV              │
│ ──────────────────────────────────────────── │
│                                              │
│ YOUR FORCES                  HOSTILE FORCES  │
│ Strike Fleet Aleph           Unknown Fleet   │
│                                              │
│ ▮ 3× Brawler Cruiser         ● 2× Unknown    │
│ ▮ 2× Scout Corvette          ● 4× Unknown    │
│ ▮ 1× Guardian Frigate        ● 1× Unknown    │
│                                              │
│ COMPOSITION ESTIMATE                         │
│ Based on coarse scan:                        │
│   Est. 7 hostiles · size class est. Corvette │
│   through Cruiser                            │
│                                              │
│ ODDS (estimate): Even to favorable           │
│                                              │
│ [ ENGAGE ]  [ RETREAT ]  [ OPEN HUD ]        │
└──────────────────────────────────────────────┘
```

**What is shown depends on research and scan state.** This is the pillar-6 tension (information asymmetry vs. UI clarity) resolved explicitly:

| Scan state | What the hostile side shows |
|---|---|
| No scan, first sight | `● Unknown Fleet` only — count and size are both hidden |
| Coarse scan active | Count + size-class estimate (`Unknown — Corvette to Cruiser range`) |
| Full scan completed | Ship classes visible but *designs* hidden (`2× Corvette`, `1× Cruiser`) |
| **Blue Pre-Combat Scan researched** (Cat 5 T3) | **Full composition revealed — specific subsystems and expertise estimates shown** |

**This gate is the single most important research↔combat integration.** The Pre-Combat Scan tech (Blue, Cat 5 Tier 3) literally *upgrades this dialog from a coarse estimate to a full composition sheet*. Players who invest in Blue scanning get to make engage/retreat decisions with real information. Players who don't get the coarse version. The research investment has direct in-combat UX consequences, not just stat consequences.

**Anti-pattern avoided:** Black-box combat. The dialog always shows *something* and the tier of information maps to the player's research state legibly. Commitment shape: **pre-commit with reveal** — the reveal depth is the technique.

**Phase 0 buttons:**

- `[ ENGAGE ]` — commits. Opens the Combat HUD (phase 1) and resumes simulation at 1×.
- `[ RETREAT ]` — attempts to disengage. For a fleet not yet in weapon range, this is clean (no damage). If the fleet is already at range (ambush situation), a preview shows expected retreat damage before commit.
- `[ OPEN HUD ]` — opens the HUD without committing; the game stays paused. Lets the player inspect the situation in detail. A `[ENGAGE]` button remains available in the HUD header.

### 4.2 Combat HUD Layout

The Combat HUD is **an overlay on the galaxy map**, not a separate scene. The map stays visible. Combat happens at the site of the fleets involved, and the camera pans and zooms to frame that location.

```
┌─────────────────────────────────────────────────────────────────┐
│  TOPBAR (unchanged — research, resources, time controls)        │
├──────────┬──────────────────────────────────────┬───────────────┤
│          │                                      │               │
│ OUR      │                                      │ THEIR         │
│ FLEET    │          GALAXY MAP (zoomed)         │ FLEET         │
│ PANEL    │          Combat visualization         │ PANEL         │
│          │          fleet icons, tracers         │               │
│ 270px    │                                      │ 275px         │
│          │                                      │               │
├──────────┴──────────────────────────────────────┴───────────────┤
│  BATTLE BAR (64px) · dispositions, priorities, retreat, speed   │
└─────────────────────────────────────────────────────────────────┘
```

**Layout changes from galaxy-map default:**

- **Left panel** becomes OUR FLEET PANEL (replaces Fleet tab content).
- **Right panel** becomes THEIR FLEET PANEL (replaces System Inspector content).
- **Battle Bar** is a new 64px strip at the bottom of the viewport, above the speed/time widget area. It becomes the primary input surface during combat.
- Topbar is unchanged — research continues, resources tick, time controls work.
- Minimap shows the entire galaxy with the combat system flashing for situational awareness if other fleets are nearby.

### 4.3 OUR FLEET Panel (Left, 270px)

Groups friendly ships by **fleet role**, never by individual ship. The aggregation is enforced — there is no per-ship row.

```
┌────────────────────────────────┐
│ STRIKE FLEET ALEPH             │
│ 6 ships · 92% effective        │
│ ──────────────────────────     │
│                                │
│ BRAWLER · 3 ships              │
│  HP       ▬▬▬▬▬▬▬░░░  72%     │
│  Morale   ▬▬▬▬▬▬▬▬▬░  92%     │
│  [CHARGE ▾] [FOCUS: Brawler ▾]│
│                                │
│ SCOUT · 2 ships                │
│  HP       ▬▬▬▬▬▬▬▬▬▬  98%     │
│  Morale   ▬▬▬▬▬▬▬▬▬▬ 100%     │
│  [STAND BACK ▾] [FOCUS: any ▾]│
│                                │
│ GUARDIAN · 1 ship              │
│  HP       ▬▬▬▬▬▬░░░░  55%     │
│  Morale   ▬▬▬▬▬▬▬▬░░  78%     │
│  [HOLD ▾] [PROTECT: Scouts ▾] │
│                                │
│ ──────────────────────────     │
│ SUPPLY DRAIN (this battle)     │
│  Red   -28/s                   │
│  Blue  -6/s                    │
│ ──────────────────────────     │
│ [ RECALL FIGHTERS ]            │
│ [ RETREAT FLEET ]              │
└────────────────────────────────┘
```

**Per-role row contents:**

| Element | Detail |
|---|---|
| Role name + count | `Exo 2 11px` weight-600 ALL-CAPS, `TextBright` |
| HP bar | 4px tall, averaged across ships in role; segments visible for each ship (so the player sees "one brawler is down to 20%") |
| Morale bar | 4px tall, also averaged. Color shifts to `#ff6655` when below 30% (rout threshold) |
| Disposition dropdown | `[CHARGE / HOLD / STAND BACK / RETREAT]` — 4 options per role. Default from fleet template. |
| Target priority dropdown | `[FOCUS: X ▾]` where X is a ship class, a role, or "any" |

**HP bar segments:** the bar is visually split into N pips where N = ship count. Each pip fills individually. A role of 3 brawlers at (100%, 80%, 0%) shows three pips: green, yellow, and a dark pip with a ✕. **This lets aggregation serve the player without hiding critical detail.**

**Supply Drain strip** — same visual language as the Ship Designer's Profile pane. Shows per-color supply burn rate for this battle. If a color's empire-wide stockpile is running out, its strip pulses and a warning appears: `Blue components: 2:10 remaining at this rate`.

### 4.4 THEIR FLEET Panel (Right, 275px)

Mirrors OUR FLEET structure but with faction coloration for the hostile empire (or neutral-red tint if unknown). Information density depends on the scan state (see §4.1 table).

**With Blue Pre-Combat Scan researched**, per-role rows additionally show:

- Primary weapon types mounted (`Red Plasma, Red Rail`)
- Primary defenses (`Red Armor T2`)
- Estimated expertise ("Red veteran — ~1.3× expertise")

**Without Pre-Combat Scan**, the rows show only what is directly observable mid-battle: HP (inferred from visible hit rate), rough count, class estimate.

**Anti-pattern avoided:** Black-box combat (information scales predictably with research); Mystery orb (unknown information is *labeled* unknown, not hidden).

### 4.5 Battle Bar (Bottom, 64px)

The most action-dense strip during combat. One persistent bar with four segments:

```
┌─ TIME & BATTLE STATE ─┬─ GLOBAL COMMANDS ─┬─ TARGET PRIORITY ─┬─ EMERGENCY ──┐
│ ⏸ 1× 2× 4× · T+01:24  │ [ALL CHARGE]       │ [Biggest threat ▾]│ [RETREAT ALL]│
│ Battle progress: 38%  │ [ALL HOLD]         │ override: 0 rules │ [SURRENDER]  │
└───────────────────────┴────────────────────┴───────────────────┴──────────────┘
```

- **Time & Battle State**: speed controls (only 1×/2× during combat — no 4×/8× while engaged, to prevent out-of-pace loss-of-control); battle timer (T+MM:SS); progress indicator (roughly, % of simulated time elapsed vs. expected duration for battle size).
- **Global Commands**: `[ALL CHARGE]` / `[ALL HOLD]` / `[ALL STAND BACK]` — one-click disposition for all roles. Technique #8 (batching).
- **Target Priority override**: a fleet-wide priority rule that overrides per-role priorities. E.g., `[Biggest threat]`, `[Enemy carriers]`, `[Nearest to our carrier]`. Dropdown; rules persist until changed.
- **Emergency**: `[RETREAT ALL]` triggers retreat for the whole fleet with a preview of expected retreat damage; `[SURRENDER]` (if diplomacy allows) offers surrender terms.

The Battle Bar is **always visible during combat** and disappears when combat ends. It does not stack with the normal galaxy-map bottom widgets — the speed/time widget and events widget are hidden while the Battle Bar is up, because their functions are subsumed.

### 4.6 Live Event Notifications

During combat, events emit small toasts in the **upper-right corner of the viewport** (below the topbar). Examples:

- `⚠ Brawler Cruiser lost — Red Armor T2 destroyed` — red dot
- `▼ Morale break: 2 Scouts fleeing` — yellow dot
- `▲ Subsystem: Red Plasma T2 critical hit (×2.4 damage)` — green dot
- `⏸ Auto-paused: Fleet HP < 50%` — blue dot (if auto-pause on threshold is enabled in settings)

**Technique #10 — auto-pause** fires on configurable thresholds (set in the settings screen, not here):

- First contact with unknown enemy: **always on** (non-configurable).
- Fleet HP drops below 50%: default on.
- Morale break: default on.
- Subsystem destroyed: default off.

**Technique #7 — smart categorized silenceable notifications.** Each category has a toggle. A log of all combat events is stored and accessible post-battle in the debrief.

### 4.7 Overkill / Salvage Projection

A subtle but important piece of feedback: the **Salvage Projection Indicator** sits as a small chip in the upper-right of THEIR FLEET PANEL when hostile ships are nearing destruction.

```
┌──────────────────────────┐
│ ⊕ SALVAGE PROJECTION     │
│ Current: 2 wrecks, 1 debris│
│ +340 components projected │
└──────────────────────────┘
```

**Why this matters:** the combat systems design (§5.8) states that overkill degrades wreck quality — massive overkill turns salvageable wrecks into raw-resource-only debris. A player who wants to capture or salvage should prefer sustained weapons and avoid overkill. **The projection makes this mechanic visible in real time**, not just after the fact.

Hovering the chip shows the rule: `Sustained damage on ships below 10% HP converts potential wrecks to debris. Disable focus-fire to preserve wrecks.` This is **Technique #6 (tiered info)** — basic glance state on the chip, rule text on hover, with a `[How wrecks work]` link to docs on Shift-hover.

**Anti-pattern avoided:** Black-box combat. The overkill mechanic is legible in-battle, not only after.

### 4.8 Post-Combat Debrief

When combat ends (one side routed, retreated, or destroyed), the Combat HUD transitions to the **Debrief**. The galaxy map stays paused.

```
┌─────────────────────────────────────────────────────────────┐
│ BATTLE DEBRIEF · Tarsus-IV                          [ ✕ ]  │
│ Result: VICTORY · T+03:22                                   │
│ ─────────────────────────────────────────────────────────── │
│                                                             │
│  LOSSES                          SALVAGE                    │
│  Our:   1 Brawler Cruiser        ▮ Red components  +140     │
│  Enemy: 4 Corvettes, 1 Cruiser   ▮ Red advanced    +22      │
│                                  ▮ Blue components +8       │
│                                  Expected from debris: ~60% │
│                                                             │
│ ─────────────────────────────────────────────────────────── │
│ PER-DESIGN PERFORMANCE                                      │
│                                                             │
│  Brawler Cruiser Mk II  (3 engaged, 2 survived)             │
│   Damage dealt:     4,820 (42% of fleet)                    │
│   Damage taken:     2,150                                   │
│   Top contributor:  Red Plasma T2 (1,840 dmg)               │
│   Underperformer:   Red Armor T1 (prevented only 280 hp)    │
│   [ IMPROVE DESIGN ]                                        │
│                                                             │
│  Scout Corvette  (2 engaged, 2 survived)                    │
│   Damage dealt:     380                                     │
│   Key role:         Scouting saved 1 Brawler from ambush    │
│   [ IMPROVE DESIGN ]                                        │
│                                                             │
│  Guardian Frigate  (1 engaged, 1 survived at 55%)           │
│   Damage prevented: 1,420 (protected Scouts)                │
│   Underperformer:   Blue PD T1 (cross-color, 0.7×)          │
│   [ IMPROVE DESIGN ]  [ RESEARCH ALTERNATIVE ]              │
│                                                             │
│ ─────────────────────────────────────────────────────────── │
│ RESEARCH GAINED                                             │
│  Red expertise:  +420 XP                                    │
│  Blue expertise: +80 XP                                     │
│  Red Plasma T2 subsystem: +180 XP (1 threshold crossed)     │
│                                                             │
│ [ FULL EVENT LOG ]   [ SALVAGE DETAILS ]   [ CONTINUE ]     │
└─────────────────────────────────────────────────────────────┘
```

**Key properties:**

- **Per-design breakdown is the anchor.** The player sees which of their designs did well, which didn't, and which specific modules were the top contributors and underperformers.
- **`[IMPROVE DESIGN]`** — deep-link to the Ship Designer scoped to that design. Closes the debrief, opens the designer.
- **`[RESEARCH ALTERNATIVE]`** (shown when an underperformer was cross-color or low-tier) — deep-link to the Tech Tree overlay scoped to the module's color and category. Helps the player close the learning loop: "My Blue PD was underperforming because my empire is Red-primary. Let me see what Red defensive options exist."
- **Research Gained** shows expertise awarded. This is the **combat → research integration** made explicit. The numbers are the same ones that update the research spec's Expertise Bar.
- **Full Event Log** opens a scrollable log of every combat notification, shot fired, and state change — for players who want to analyze in depth.

**Anti-pattern avoided:** Black-box combat. The debrief is the complete explanation of what happened and why, with actionable deep links to the designs and research that would change the outcome next time.

### 4.9 Multi-Battle Outliner

When more than one battle is running simultaneously (late-game situation), a **Battle Outliner** chip appears in the top-left of the viewport:

```
⚔ 3 BATTLES ACTIVE
  ⚫ Tarsus-IV — losing (HP 34%)
  ⚫ Orin Cluster — winning
  ⚫ Gate-17 — even
```

Clicking a row jumps the camera to that battle and sets that battle's HUD as the active one. The current battle's fleet panels replace the galaxy panels; switching battles swaps panels.

**Priority ordering**: battles are sorted by urgency — losing battles first, then even, then winning. **Technique #9 (saved views/filters) + #7 (smart notifications).**

The outliner is dismissable per battle (e.g. the player has accepted a losing battle and no longer wants it in the list). Dismissing doesn't end the battle; it just stops surfacing it.

### 4.10 What the Combat HUD Does NOT Support

Explicitly omitted, to preserve pillars:

- **Per-ship orders.** There is no click-a-ship interaction in combat. Roles are the unit of control.
- **Weapon-by-weapon target assignment.** Target priority is role-scoped only.
- **Formation editing.** Formation is derived from disposition; no sub-formation controls.
- **Special abilities.** The GDD explicitly states no abilities — nothing to add here.
- **Pause-and-queue commands.** Dispositions are applied immediately. There is no "command queue" like a pause-based RTS; this is real-time combat where the player tunes the sim, not issues micro.

These are deliberate non-features. Anyone requesting them is pulling toward 1-UPT tactical gridlock or StarCraft-style micro, both of which violate minimal-micromanagement.

---

## 5. Research Integration Addendum

The existing `derelict_empires_research_ui_spec.md` specifies the three research surfaces in full: topbar strip, RESEARCH tab, Tech Tree overlay. This section adds **integration surfaces** that connect research to the Designer and Combat HUD.

### 5.1 New Entry Points to the Tech Tree Overlay

In addition to the entry points documented in research spec §5, the Tech Tree overlay now opens from:

| New entry | Source | Scope on open |
|---|---|---|
| `[RESEARCH]` chip | Ship Designer → Locked slot → Unlock Picker | Overlay opens on the module's color; tier row containing the module is pre-selected; focus panel shows the tier; `[START TIER RESEARCH]` is the primary action |
| `[RESEARCH ALTERNATIVE]` chip | Combat Debrief → Underperforming module row | Overlay opens on the *empire's primary* color (not the underperformer's); focus is on the category matching the module type. The implicit suggestion: "You were using cross-color, consider on-color." |
| `[USE IN DESIGN]` chip | Tech Tree → Module focus panel → when a researched module is selected | Inverse direction: closes the Tech Tree overlay, opens Ship Designer with a new draft that has the module pre-placed in its first compatible slot |
| Combat HUD → `[IMPROVE DESIGN]` → Designer → Unlock Picker | Indirect path through Designer | Standard path; the overlay opens as above |

All new entry points honor the overlay's queue-mode behavior (research spec §5.8). If the player opens the overlay from a locked slot while their active research is full, the overlay opens in **queueing mode** automatically with a banner: `QUEUEING: TIER TRACK — Red Weapons T3 will start when current research completes.`

### 5.2 Tech Tree Module Focus Panel — New Elements

The module focus panel (research spec §5.5) gains two new blocks when a **researched** module is selected:

**Block: USED IN DESIGNS.**

```
USED IN 3 OF YOUR DESIGNS
  · Brawler Cruiser Mk II   (slot W1, W2)
  · Bombard Destroyer       (slot W1)
  · Line Destroyer          (slot W1)
  [ USE IN NEW DESIGN ]
```

- Each design is a deep-link chip → opens the Ship Designer scoped to that design.
- `[USE IN NEW DESIGN]` opens the Designer with a new draft.

**Block: COMBAT PERFORMANCE (last 5 battles).**

```
COMBAT PERFORMANCE
  Engagements:   5
  Avg damage/use: 1,680
  Kill contribution: 14 ships
  Expertise this subsystem: 780 XP (2,300 to next threshold)
```

This block exists when the player has used the module in at least one battle. It **surfaces combat-driven empirical performance inside the research surface**, so the player sees the full trinity loop in one view: researched ⟶ designed into ⟶ fought with ⟶ learned from.

### 5.3 Topbar Research Strip — New Hover Affordance

The topbar TIER and MOD rows already show a tooltip with rate/sources/ETA (research spec §3.6). Add one new row to the tooltip **only when the currently-researching module is used by at least one design**:

```
RED PLASMA T3 (active)
Rate: +1.2/s  ETA: 1:22
Sources: active research (100%)
─────────────────────
USED IN 2 PENDING BUILDS
  Bombard Destroyer Mk II (queued ×2)
  This tech unlocks on completion.
```

This lets the player see that their current research is *already ordered to be used* — the link between research and their build queue. If no pending builds use the module, this row is omitted.

### 5.4 Banked Progress List — No Change Required

The Banked Progress list (research spec §4.4) continues to work as specified. Combat **does not** deposit to module memory — combat drives expertise, a separate system. Keeping these clean preserves the mechanic's clarity.

The one place combat does touch research is the **Expertise Bar** on the Tech Tree overlay's matrix (research spec §5.4). Combat XP flows into general color expertise, which updates that bar. The Debrief's "Research Gained" section (§4.8 of this spec) shows the same numbers — computed identically across surfaces, as required by §1.3's expertise-visibility rule.

### 5.5 Queue-Driven Research Suggestions (optional polish)

When the player saves a design that has locked slots, a non-modal toast appears in the Recent Events widget:

```
⚫ New design saved: Brawler Cruiser Mk II
   3 locked slots — [ RESEARCH PLAN ]
```

`[RESEARCH PLAN]` opens the Tech Tree overlay in a special mode: the tiers required by locked slots in the most-recently-saved design are highlighted with a faction-glow border. Clicking one queues it directly; the overlay stays open so the player can queue multiple. A small counter in the overlay header shows `3 LOCKED · 0 QUEUED · [ QUEUE ALL ]`. One click queues all three in order of gate-dependency.

This is **Technique #15 (intent-level routing) applied to research planning** — the intent is "unlock this design"; the routing (which tiers and in what order) is handled by the UI.

**Design-level concern (Pass 3 design lens):** This is a clean-flow affordance and risks pushing the game toward "spreadsheet optimizer" feel. Mitigate by making `[QUEUE ALL]` opt-in, never automatic, and by keeping the suggestion subtle (a toast, not a modal). The scavenger-king sometimes *wants* the locked slot unresolved — it's a hook for exploration and trade.

---

## 6. Cross-Surface Navigation Patterns

### 6.1 Deep-Link Chip Standard

Every cross-surface link uses a consistent visual treatment so players learn the affordance once.

**Chip anatomy:**

- 20px tall, variable width
- 3px left accent bar in the target surface's color (blue for research, gold for designer, red for combat — though the right accent always relates to the *content's* faction color where applicable)
- Icon + label + `→` glyph
- `Rajdhani 10px` weight-500 ALL-CAPS letter-spacing 1px
- Hover: border `BorderBright`, color `TextBright`, no size change
- Click: 100ms fade to the target surface, target state pre-selected

The `→` glyph is the visual indicator of a cross-surface link. Inline buttons that stay within the current surface never use the `→` glyph.

### 6.2 Return Paths

Cross-surface navigation is **modal-stack based** — closing the target returns to the previous surface with state intact.

| From | To | Close returns to |
|---|---|---|
| Ship Designer → Tech Tree (via `[RESEARCH]`) | Tech Tree | Ship Designer (same draft, same slot focus) |
| Combat Debrief → Ship Designer (via `[IMPROVE DESIGN]`) | Ship Designer | Combat Debrief (same scroll position) |
| Tech Tree → Ship Designer (via `[USE IN DESIGN]`) | Ship Designer | Tech Tree (same focus) |
| Ship Designer → Market (via `[BUY]`) | Market screen | Ship Designer |
| Ship Designer → Diplomacy (via `[RENT]`) | Diplomacy screen | Ship Designer |
| Ship Designer → Galaxy Map (via `[SHOW ON MAP]`) | Galaxy map with waypoint ping | Ship Designer (via Esc or explicit return button) |

The modal stack is visible as a breadcrumb in the current overlay's header. Example (three overlays deep):

```
TECH TREE ← SHIP DESIGNER ← FLEETS                 [ ✕ ]
```

Clicking any segment of the breadcrumb jumps directly to that level, closing intermediate overlays. Esc closes just the top overlay.

### 6.3 Hotkeys

All three surfaces honor these hotkeys globally:

| Key | Effect |
|---|---|
| `T` | Open Tech Tree overlay |
| `D` | Open Ship Designer (new draft) |
| `B` | Focus BUILD tab in left panel |
| `F` | Focus FLEETS tab in left panel |
| `R` | Focus RESEARCH tab in left panel |
| `Esc` | Close topmost overlay; if none open, deselects current selection (fleet/system/POI); if none, does nothing (no full-screen menu open) |
| `Space` | Pause / resume |
| `1` / `2` / `4` / `8` | Set game speed (disabled during combat above 2×) |

No hotkey requires a modifier for common actions. Modifiers are reserved for power-user features (Shift-click for "Best Any" color filter, Alt-hover for tiered tooltip depth per Rise-of-Legends pattern).

---

## 7. Godot Implementation Notes

### 7.1 Scene Structure

The additions to the existing scene graph (research spec §8.1):

```
GalaxyMapScreen
├── Topbar (existing, no change)
├── LeftPanel (existing, BUILD tab content added)
│   └── TabContent
│       ├── FleetsTab   (existing)
│       ├── ColoniesTab (existing)
│       ├── ResearchTab (existing)
│       └── BuildTab    ← new, per §3.8
├── RightPanel (existing, Combat mode content added)
├── ShipDesignerOverlay    ← new, full-screen glass
│   ├── Header
│   ├── ChassisPane
│   ├── SlotMatrix
│   ├── ProfilePane
│   ├── FooterBar
│   ├── ChassisPickerSubOverlay  (hidden by default)
│   ├── SlotPickerDropdown       (inline, hidden by default)
│   └── UnlockPickerDialog       (hidden by default)
├── TechTreeOverlay (existing; new entry-point handling per §5.1)
└── CombatHUDOverlay  ← new
    ├── PreCombatDialog   (hidden by default)
    ├── OurFleetPanel     (replaces left panel content during combat)
    ├── TheirFleetPanel   (replaces right panel content during combat)
    ├── BattleBar         (bottom strip, new z-index 61)
    ├── LiveEventToasts   (upper right, z-index 200)
    ├── SalvageProjectionChip
    ├── BattleOutliner    (upper left, shown only when >1 battle)
    └── CombatDebrief     (post-battle, replaces HUD)
```

### 7.2 State Management

Three new autoloads (or services):

- **`ShipDesignState`** — authoritative state for the currently-edited design. Holds the draft and original. Emits `ShipDesignState.Changed` on any slot fill, chassis swap, or stat recompute.
- **`FleetTemplateState`** — authoritative state for all saved fleet templates. Emits `FleetTemplateState.Changed` on template edits.
- **`CombatHUDState`** — transient; exists only while a battle is active. Wraps a reference to the underlying `Battle` simulation object from `CombatManager` (implementation plan §5) and publishes UI-friendly aggregates (role HP averages, supply drain, salvage projection) at 4Hz. Emits `CombatHUDState.Changed` for panel updates.

Following the project's event-bus architecture (implementation plan §2), **no direct cross-service calls**. Cross-surface deep links go through the bus:

- `DesignerRequest.OpenForSlot(design, slotId)` — from Unlock Picker to Tech Tree overlay
- `TechTreeRequest.OpenForModule(moduleId, intent = Research | QueueTier | UseInDesign)` — from Designer to Tech Tree
- `DesignerRequest.OpenForDesign(designId)` — from Combat Debrief
- `CombatHUDRequest.FocusBattle(battleId)` — from Battle Outliner

### 7.3 Performance Budget

- **Ship Designer stat recompute**: must complete in <16ms on slot change. Stats are pure functions of (chassis, slot fills, expertise state). Memoize by (designId, slotFills-hash).
- **Combat HUD aggregate updates**: 4Hz. Averages over all ships in a role are cheap (N<30 typically). Never recompute on every frame.
- **Tech Tree matrix cells**: already documented as lazy (research spec §8.5). The new deep-link entry points invalidate only the single target cell, not the full matrix.

### 7.4 Animation Budget

The research spec (§8.4) sets a strict animation budget: 3 animations, all low-frequency state cues. This spec adds:

1. **Battle HP pip drain** — when a ship's HP changes, its pip drains with a 200ms ease-out. At most N pips (total ships in combat) animate simultaneously.
2. **Supply strip pulse** — when a color's empire-wide stockpile is running out (§4.3), the strip pulses on a 0.5s sine cycle. Same curve as the near-completion research pulse; reuse the shader.
3. **Salvage projection tick** — the projection chip ticks the `+340 components projected` number upward on salvage events. 150ms ease-out per tick. No decorative loop.
4. **Deep-link chip fade-through** — 100ms opacity fade when navigating between surfaces. One continuous fade, not an overlap.

No decorative motion. No idle animations. The battlefield visualizer on the galaxy map (fleet icons, weapon tracers) is not covered by this spec — that's a visualization concern handled by the map shader.

### 7.5 Input Handling

- **Designer overlay** captures all input while open. Esc closes. Click outside the glass closes *only if* no draft is dirty; otherwise prompts the unsaved-changes dialog.
- **Combat HUD** captures input for its panels and Battle Bar. Speed hotkeys are intercepted so that speeds above 2× are ignored. Mouse input on the galaxy map itself still works (camera pan, zoom) but clicks on fleets behave differently: friendly fleets in the active battle highlight in OUR FLEET PANEL; enemy fleets highlight in THEIR FLEET PANEL. No selection-based orders.
- **Simultaneous overlays** are supported within the stack: Designer > Tech Tree > Chassis Picker is a valid three-deep stack. Esc unwinds one level. Each overlay's glass backdrop deepens the dim underneath, so depth is visually readable.

---

## 8. Pillar Check

*Mandatory end-of-mode verification per the strategy-game-ux skill. Pillars from `/references/derelict-empires-context.md`.*

| Pillar | Status | Rationale |
|---|---|---|
| 1. Minimal micromanagement | ✅ | Designer aggregates at the design-and-template level, not the ship. Combat aggregates at the role level with no per-ship interaction. Templates + `[REINFORCE ALL]` (Technique #4) keep fleet-recovery one-click. Template modifiers (Technique #15) let designs evolve with research without manual edits. |
| 2. The map is the game | ✅ | Designer is a modal overlay, not a scene. Combat HUD is an overlay on the galaxy map with panels replacing the fleet/system inspectors, not a separate scene. The map remains visible throughout combat. |
| 3. Salvage everything | ✅ | Unlock Picker (§3.7) surfaces `[SALVAGE HINT]` as a first-class unlock path, with camera-ping deep-link to nearest known derelicts. Overkill/salvage projection (§4.7) makes salvage a visible in-combat concern. Combat Debrief's salvage block connects post-battle debris to the resource system. |
| 4. Nothing is locked | ✅ | Every cross-color subsystem usage shows its expertise multiplier (`0.7×`, `1.4×`) rather than being blocked. Chassis picker always shows every chassis with labeled unlock reasons. Locked slots are never dead-ends — every locked slot has 2–4 unlock paths in the Unlock Picker. |
| 5. Cooperation and competition | ⚪ | Neutral — this trinity doesn't directly touch diplomacy. Unlock Picker's `[RENT]` path routes to diplomacy; `[BUY]` routes to market. Handoffs are respectful but not the focus here. |
| 6. Information asymmetry | ✅ with deliberate tension | **Critical integration point**: Pre-Combat Dialog (§4.1) reveals enemy composition in tiers, with Blue Pre-Combat Scan (Cat 5 T3) being the research that upgrades a coarse estimate to a full composition sheet. This is the pillar working exactly as intended — the fog is a mechanic, but the UI is unambiguous about what the player knows and doesn't know. |
| 7. The 5th X — eXchange | ✅ | Unlock Picker (§3.7) surfaces market `[BUY]` and diplomacy `[RENT]` as peer unlock paths alongside research and salvage. Multi-color supply strips in both Designer and Combat HUD make the logistics burden of off-color tech visible — creating real incentive to trade for the color you specialize in rather than refine off-color in-house. |

### Conflict: None to resolve.

### Scavenger-King Test

> Does this recommendation make the game feel more like a *resourceful scavenger-king*, or more like a *spreadsheet optimizer*?

✅ **More like a scavenger-king** on balance, with one managed risk.

**Supporting elements:**

- Locked slots always offer 4 unlock paths (research/buy/rent/salvage), pushing the player toward opportunistic improvisation rather than a single "correct" research order.
- Combat Debrief names underperforming modules and offers to research *alternatives*, not just the same module at a higher tier — inviting the player to change loadout based on what they learn in combat.
- Expertise multipliers are visible everywhere, making cross-color experimentation readable as a trade-off rather than a mistake.
- The deep-link chip pattern makes the three surfaces feel like three facets of the same salvage-punk project rather than three disconnected screens.

**Managed risk:** the Queue-Driven Research Suggestion (§5.5) — `[RESEARCH PLAN]` with `[QUEUE ALL]` — can nudge the player toward clean, optimized research pipelines that undercut the improvisational feel. Mitigations applied: the suggestion is a passive toast, never a modal; `[QUEUE ALL]` is opt-in; the suggestion has no persistent badge. The scavenger-king sometimes *wants* to leave the locked slot unresolved as a hook for exploration or trade, and the UI doesn't penalize that choice.

### Sign-off

- [x] No conflicts. Recommendation is safe to commit.

---

## 9. What This Spec Does NOT Cover (Deferred)

Consistent with the project's existing scoping discipline, the following are explicitly out of scope here and belong to later design passes:

- **Market / Trade screen** — the `[BUY]` and `[RENT]` chips deep-link into it, but its internal UI is not specified here.
- **Diplomacy screen** — `[RENT]` and `[OPEN DIPLOMACY]` deep-link into it; internals not specified here.
- **Ground combat HUD** — the systems doc (§5.10) notes the same structure as space combat with different roles. A direct port of this spec should work; full spec is deferred.
- **Ship visualization in combat** — the battlefield visualizer (tracers, impact effects, damage states on hull icons) is a shader/art concern, not a UX concern. Covered elsewhere.
- **Admiral / leader progression surfaces** — the GDD defers admiral progression. When designed, admirals will attach to fleet templates with a new row in the template editor.
- **Multi-empire combat UI** — the systems doc (§5.6) establishes multi-party mechanics but the UI for three-way+ battles is deferred. The Combat HUD as specified works for 1v1; extension to 1v2+ needs additional design.
- **Accessibility layer** — colorblind modes, motion-reduction settings, screen-reader hooks. Requires dedicated pass.
- **Tutorial overlays for the Designer and Combat HUD** — deferred to onboarding design.
- **Mobile / touch adaptation** — desktop-pointer only per the existing spec's convention.