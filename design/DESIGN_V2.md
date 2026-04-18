All projects
Derlict empires
Designing and building a space 4x game where exploration stays relevant even in late game and exploitation is not straight forward


How can I help you today?

    Improving galaxy star visuals in Godot
    Last message 7 minutes ago
    Game UI image description for developer
    Last message 1 hour ago
    Color personality in tech tree design
    Last message 1 hour ago
    Green tech tree redesign for regeneration
    Last message 1 day ago
    Galaxy spiral arm generation algorithm
    Last message 1 day ago
    System implementation planning
    Last message 1 day ago
    Clarifying system design through questions
    Last message 1 day ago
    Building a Godot game with Claude
    Last message 1 day ago
    Improving game UI with Claude and Godot
    Last message 3 days ago
    Game design document for Claude code
    Last message 6 days ago
    Haulers and Galactic Connectivity
    Last message 9 months ago
    Galactic Precursor Civilization Game Design
    Last message 9 months ago

Memory
Only you

Purpose & context Rani is designing a 4X/5X real-time strategy game called Derelict Empires — a salvage-punk space strategy game set in a post-apocalyptic galaxy where five precursor civilizations have mysteriously vanished. The core design philosophy is resourcefulness: players scavenge and repurpose imperfect technology rather than building from scratch. The "used future" aesthetic should permeate all visual design. The fifth "X" — eXchange (trade) — is treated as a core design pillar alongside the traditional four, not an afterthought. A key design value throughout is minimal micromanagement: automation, fleet presets, and simple colony allocation keep player focus on strategic decisions. --- Current state A comprehensive Game Design Document (GDD) has been produced as a formatted .docx file covering 20 sections. Major systems designed and documented include: Galaxy structure: ~100 star systems in spiral arms with visible and hidden lanes; no automatic territory — players must physically guard or diplomatically negotiate resource access; core vs. rim tradeoffs Five precursor civilizations, each tied to a color: Red/Crimson, Blue/Azure, Green/Verdant, Gold/Golden, Purple/Obsidian Four origin templates: Warriors, Servitors, Haulers, Chroniclers, plus Free Race Resource system: 4 raw resources per color (simple energy, advanced energy, simple parts, advanced parts) = 20 raw resources total, plus 10 manufactured components (5 colors × 2 tiers) Six-tier tech tree with five categories per color Ship design: slot-based, non-visual interface Fleet roles: Brawler, Guardian, Carrier, Bombard, Scout, Non-Combatant Combat: Real-time with disposition commands (not individual ship control); weapons triangle (lasers vs. shields, railguns vs. armor, missiles vs. point defense); logistics costs tied to weapon/defense type Logistics & supply chain, colony/outpost management, diplomacy with reputation consequences, abstracted espionage, and a deep trade system including tech renting The GDD was updated to reflect the revised 4-resource-per-color system (down from an earlier iteration). --- On the horizon Several systems were explicitly deferred for future design sessions: Detailed currency and inflation system Victory conditions and the precursor mystery narrative Narrative/story events Leader progression system Economic warfare mechanics --- Approach & patterns Rani's preferred design workflow is exhaustive iterative Q&A to surface and resolve design gaps, followed by structured document generation. Claude conducted multiple questioning rounds before producing the GDD. When inconsistencies arose (e.g., galaxy size, resource count), decisions were made deliberately and the documentation updated to stay consistent. Rani engages at a detailed systems-design level and has clear opinions on scope and player experience. --- Tools & resources Existing design documentation brought into the conversation as a starting point GDD produced as a formatted .docx file — the canonical reference artifact for the project

Last updated 1 day ago
Instructions

Add instructions to tailor Claude’s responses
Files
5% of project capacity used
Indexing

Derelict_Empires_Systems_Design_v2.md

44.74 KB •772 lines•Formatting may be inconsistent from source
# Derelict Empires — Systems Design Document v2

**Supplement to GDD v1 — April 2026**

This document captures all system-level design decisions made during iterative Q&A sessions. It refines, replaces, or extends the corresponding sections in the GDD v1. Where this document contradicts the GDD, this document takes precedence.

---

# 1. Resource System

## 1.1 Overview

The resource system has been restructured from the GDD v1 description. The GDD described overlapping "raw resources" and "components" without clear relationships. The revised model has three distinct layers per precursor color, with clear conversion pipelines between them.

## 1.2 Resource Layers (Per Color × 5 Colors)

| Layer | Types | Source | Role |
|---|---|---|---|
| Raw Ore | Simple ore, Advanced ore | Mined from planetary deposits, asteroid fields | Universal input material |
| Energy | Simple energy, Advanced energy | Refined from ore + generated passively by buildings/reactors | Powers shields, weapons, sensors, movement |
| Components | Basic, Advanced | Refined from ore + salvaged directly from derelicts | Builds and repairs ships, buildings, armor, ammo |

**Totals:** 10 raw ores + 10 energy types + 10 component types = 30 resource types across all five colors.

## 1.3 Refinement Pipeline

- Refineries passively convert raw ore into energy or components. The default split is automatic.
- Refinery efficiency is improvable through technology, expertise, and pop allocation.
- With advanced technology, cross-refining is possible (e.g., ore normally destined for components can be refined into energy, or vice versa) at higher cost.
- Some late-game technology allows converting basic components/energy into advanced variants. This is powerful and gated behind significant research.
- Manufacturing from raw materials is always more expensive than salvaging — finding precursor sites remains valuable throughout the game.

## 1.4 Color-Matching Rule

Building anything with a specific color's technology requires that color's components and energy. A Red-affinity empire building Blue scanner probes must spend Blue components. This is the core driver of the trade economy — empires must trade, raid, or venture into off-color territory to access off-color resources.

## 1.5 Fleet Supply Consumption

Ships and buildings consume both energy and components from the same empire-wide stockpile used for construction. Supply is color-matched: a Red laser weapon burns Red energy components in the field, while Red armor repair costs Red components. This creates strategic tension between feeding frontline fleets and building new ships at shipyards.

The energy-vs-components split preserves the logistics identity of different weapon/defense types:

- Shields, energy weapons, active sensors = energy-heavy
- Armor repair, railgun ammo, missile production = component-heavy

## 1.6 Food

Food is a universal resource not tied to any precursor color. Any empire can produce food through colony agriculture. Green (Verdant Synthesis) technology significantly improves food production efficiency, growth rates, and related capabilities, making Green tech the food-adjacent specialization without monopolizing it.

## 1.7 Topbar Display

The player HUD shows 6 values per faction color box, arranged in two rows:

| Top Row (Common) | Bottom Row (Rare) |
|---|---|
| Simple ore | Advanced ore |
| Simple energy | Advanced energy |
| Basic components | Advanced components |

Same icon ordering across all five colors for quick visual scanning. 30 values total displayed, plus credits.

## 1.8 Interconnections

- **Trade system:** Color-matching forces inter-empire trade for off-color resources (see Section 3).
- **Expertise system:** Refinery efficiency improves with expertise in the relevant color (see Section 2).
- **Logistics:** Fleet loadout determines which color supply chains are stressed (see Section 6 Combat, Section 9 Ship Design).
- **Exploration:** Salvage bypasses the refinement step, yielding components directly (see Section 7).

---

# 2. Expertise System

## 2.1 Overview

The expertise system is a dual-layer mastery mechanic that rewards consistent use of technology and creates meaningful dilemmas about when to upgrade versus maintaining proficiency with proven equipment.

## 2.2 Two Parallel Tracks

**Subsystem Expertise** — Tracked per specific subsystem (e.g., "Red T2 Plasma Cannons"). Provides direct bonuses to that subsystem's performance and cost.

**General Color Expertise** — Tracked per color (e.g., "Red overall"). Accumulates from all activity with that color's technology. Provides different bonus types than subsystem expertise.

## 2.3 Earning Expertise

Expertise is earned through multiple activities, weighted differently:

- Building units (production-focused)
- Operating units over time (fleet-hours in service)
- Using units in combat

All three contribute, but at different rates. This ensures both peaceful builder empires and warmonger empires gain expertise, but through different paths.

**Using rented technology** (see Section 4) builds expertise at full rate — the renter is building and operating the systems themselves. Using rented ships builds expertise at a tiny rate.

## 2.4 Subsystem Expertise Thresholds

Subsystem expertise uses 12+ interleaved thresholds with rotating bonus types. Rather than a smooth curve, expertise hits milestones that unlock specific benefits:

**Bonus categories that rotate across thresholds:**
- Build time reduction
- Build cost reduction
- Weight/slot efficiency
- Supply consumption reduction
- Combat effectiveness increase

Example progression (illustrative, not final numbers):

| Level | Bonus Type | Cumulative |
|---|---|---|
| 1 | Build time -3% | |
| 2 | Build cost -2% | |
| 3 | Supply consumption -2% | |
| 4 | Weight -2% | |
| 5 | Build time -3% | -6% build time |
| 6 | Build cost -3% | -5% build cost |
| 7 | Combat effectiveness +2% | |
| 8 | Supply consumption -3% | -5% supply |
| 9 | Weight -2% | -4% weight |
| 10 | Build time -2% | -8% build time |
| 11 | Build cost -3% | -8% build cost |
| 12 | Combat effectiveness +3% | +5% combat |

At full mastery, stacked bonuses reach approximately 25-30% total advantage across multiple dimensions. This is "medium" impact — meaningful and strategic but not dominant.

## 2.5 General Color Expertise

General color expertise uses a separate set of bonus types from subsystem expertise:

- Research speed in that color
- Salvage yield from that color's sites
- Affinity-related perks

General color expertise persists across tier upgrades — it represents the long-term investment in "being a Red empire" even as individual subsystems are upgraded.

## 2.6 XP Curve

Logarithmic progression — fast early gains, increasingly slow as you approach mastery. Reaching maximum expertise on a subsystem takes months of game time with active use. This means adopting new tech gives noticeable benefits quickly, but mastery is a long-term investment.

## 2.7 No Tier Carryover

When upgrading from T2 to T3 in a subsystem, T3 starts at zero expertise. There is no carryover from the previous tier. A mastered T2 subsystem genuinely competes with a fresh T3 subsystem, making the upgrade decision strategic rather than automatic.

General color expertise does carry over, since it is not tier-specific.

## 2.8 Decay

Both subsystem and general color expertise decay slowly without active use. An empire's expertise has a "shape" — strongest in what has been used recently, with a long tail of diminishing older expertise. This prevents empires from stockpiling mastery in everything simultaneously.

## 2.9 Visibility

Expertise is empire-wide, not per-ship. Individual ships do not carry expertise — the empire's expertise with a given subsystem applies to all ships using it.

Enemy empires can discover your expertise profile through espionage and intelligence gathering (see Section 11). This is strategic intel — knowing an opponent is a Red Weapons master informs fleet composition decisions.

## 2.10 UI

- **Ship Designer:** Hovering over a component shows the empire's current expertise level and unlocked bonuses for that subsystem.
- **Dedicated Expertise Screen:** Full list of all subsystem expertise with search functionality. Empire-wide overview of color expertise.

## 2.11 Interconnections

- **Ship Design:** Expertise bonuses affect weight, cost, and performance of installed subsystems (see Section 9).
- **Tech Renting:** Using rented tech builds expertise at full rate, making rentals a path toward self-sufficiency (see Section 4).
- **Ship Sales:** Purchased ships carry the seller's approximately half expertise baked into performance (see Section 4).
- **Trade:** Expertise creates comparative advantage — empires with high expertise manufacture more cheaply, driving export potential (see Section 3).

---

# 3. Economy & Trade

## 3.1 Currency

Universal credits. Single currency for all empires. Per-empire minting, inflation, and exchange rates are deferred to a future expansion.

## 3.2 Credit Income Sources

- Trade goods (passive, abstract — see 3.3)
- Colony taxation
- Market sales (components, energy, ore, ships)
- Chokepoint tolls
- Salvage sales
- Tech rental fees
- Contract manufacturing fees
- Espionage contracts
- Lending expertise through services (manufacturing, mining with superior tech/efficiency)

## 3.3 Trade Goods

Trade goods (Quantum Flux, Bio-Samples, Energy Signatures, Void Crystals, Navigation Data) are abstract passive income generators. The player does not manage trade goods inventory. They are auto-generated based on technology level and trade infrastructure, auto-traded across connected empires, and produce credit income. Trade goods become more valuable as related technology advances, creating escalating strategic importance for tech-scaled resources.

## 3.4 Credit Sinks

- Market purchases (components, energy, ore, ships from other empires)
- Leader salaries (admirals, governors)
- Espionage funding
- Rush orders
- Fleet upkeep (credits on top of component/energy consumption)
- Station maintenance
- Diplomatic gifts and bribes

## 3.5 Market Structure

**Two market layers:**

**Global Commodity Market:** For components, energy, and ore. Player-set prices (pure player economy, no AI price floors). Browse listings at first contact with an empire. Reduced fees and better access with a formal trade agreement. AI empires participate as market actors.

**Direct Negotiation:** For ships, tech rentals, contract manufacturing, and other large deals. Back-and-forth proposals between two empires.

## 3.6 Trade Logistics

**Physical delivery through lanes.** Purchased goods do not teleport — they must travel through the lane network via automated trade convoys. This means:

- Blockading lanes intercepts trade (major strategic tool)
- Controlling chokepoints has direct economic consequences
- Players can assign escort fleets to protect trade routes
- Gold (Golden Ascendancy) tech improves convoy efficiency, supply waste, and hub capacity

**Toll collection:**
- Trade convoys operating under trade agreements pay tolls automatically when passing through claimed chokepoints.
- Everything else is detection-based — the station or patrol fleet must detect the convoy to collect.
- Empires can refuse toll and attempt to run the blockade, triggering a chase/combat situation.

## 3.7 Contract Manufacturing

A player with superior expertise or technology in a color can offer manufacturing services to other empires. This is established through a diplomatic agreement and creates a physical trade route (blockade-able). The manufacturing takes capacity at the provider's colony. The client does not need to micromanage beyond allocating production — goods flow automatically once the contract is set.

## 3.8 Ship Sales

Complete ships can be bought and sold on the market. Purchased ships carry approximately half of the seller's expertise baked into their performance — buying a master-built warship is a genuine shortcut. The buyer gains full expertise from operating the purchased ship normally. Dismantling a purchased ship yields significant research progress (but destroys the ship).

## 3.9 Interconnections

- **Resource System:** Color-matching forces trade for off-color resources (see Section 1).
- **Expertise:** Superior expertise = lower manufacturing costs = competitive advantage on the market (see Section 2).
- **Territory:** Chokepoint control gates trade flow (see Section 8).
- **Diplomacy:** Trade agreements improve market access; blockades and embargoes are economic warfare (see Section 10).
- **Tech Renting:** A distinct trade mechanic with its own rules (see Section 4).

---

# 4. Tech Renting & Ship Deals

## 4.1 Overview

Technology and ship transactions form a spectrum of knowledge transfer, from zero-learning mercenary deals to full-access manufacturing partnerships.

## 4.2 Deal Types Comparison

| Deal Type | Capability | Research Gain | Expertise Gain | Notes |
|---|---|---|---|---|
| Ship Rental | Full (seller's crew) | Tiny | Tiny | Ships return when deal ends. No disassembly option. |
| Ship Purchase | Seller's ~half expertise | Moderate (much more if dismantled) | Full from operating | Buyer has the product but not the blueprints |
| Tech Rental | Build at seller's efficiency | Good — diminishing, gap-dependent | Full (buyer builds and operates) | Buyer sees internals, builds it themselves |

## 4.3 Tech Rental Mechanics

**What is rented:** Fully researched subsystems only. The seller can choose to rent individual subsystems or package deals (e.g., "all Red T3 Weapons"). Pricing and scope are set by the seller. No subletting — you can only rent out tech you have fully researched yourself.

**Efficiency:** The renter builds at the seller's efficiency level, including the seller's expertise bonuses. This is the premium being paid for.

**Research leakage:** The renter gains research progress toward the rented technology. The rate depends on two factors:
- **Tech gap:** Renting 1 tier ahead = fast learning. Renting 3+ tiers ahead = barely any gain (mirrors the salvage research rules).
- **Diminishing returns over time:** The first month teaches the most. Research gain slows the longer you rent. Eventually, the renter must finish unlocking through free research or salvage.

**Expertise gain:** Full rate. The renter is building and operating the systems, so they gain expertise normally.

**Payment:** Fully negotiable between players — recurring credits, lump sum, hybrid. Whatever terms the two parties agree on.

**Termination:** Either party can cancel at any time. Early termination incurs a reputation penalty for the canceling party.

**No cap** on active rental agreements.

**Strategic implication:** Tech rental is the most dangerous thing a seller can offer — they are essentially training a future competitor. Pricing should reflect the risk of the renter eventually achieving independence.

## 4.4 Ship Rental

Seller's crew, seller's tech, full performance. Ships return when the deal ends. Tiny research and expertise gain from operating alongside rented ships. No disassembly option — rented ships cannot be taken apart for study. Pure mercenary arrangement.

## 4.5 Ship Purchase

Buyer receives a ship performing at approximately half the seller's expertise level. Full expertise gain from operating the purchased ship. Moderate research gain from studying it in use. Dismantling the ship destroys it but yields a significant research windfall — a strategic choice between keeping a powerful ship and accelerating your own tech development. The seller can detect dismantling via espionage.

## 4.6 Interconnections

- **Expertise:** Tech rental builds buyer expertise at full rate; ship purchase at full operating rate; ship rental at tiny rate (see Section 2).
- **Espionage:** Sellers can detect dismantled purchased ships. Expertise profiles reveal rental dependencies (see Section 11).
- **Diplomacy:** Rental agreements are subject to the reputation system. Cancellation incurs penalties (see Section 10).
- **Economy:** All deals flow through the credit system. Physical delivery of purchased ships means they can be intercepted (see Section 3).

---

# 5. Combat

## 5.1 Overview

Combat is a continuous real-time simulation. No formal phases — range naturally closes over time as fleets converge. Player input is limited to adjusting dispositions and target priorities per fleet role. There is no individual ship control and no special abilities.

## 5.2 Pacing

| Game Stage | Battle Duration | Context |
|---|---|---|
| Early game | ~30 seconds | Small skirmishes, few ships |
| Mid game | ~1-2 minutes | Fleet engagements with defined roles |
| Late game | ~5 minutes | Major battles with carriers, multiple roles, morale shifts |

## 5.3 Player Input During Combat

**Continuous adjustment.** The player can modify dispositions and target priorities at any time during the battle. Longer late-game battles reward active management — reading the battle state and adjusting priorities as conditions change.

**Available commands:**
- Set positioning per role (charge forward, hold mid-range, stand back)
- Set attack priority per role (focus carriers, ignore scouts, concentrate on brawlers, etc.)
- Assign guardian protection targets
- Launch/recall fighters and drones
- Order partial or full retreat

**The skill is in preparation** (fleet composition, ship design, supply status) and **reading the battle** (when to retarget, when to reposition, when to retreat).

## 5.4 Fleet Size

Fleet size scales with command technology — no hard cap. Larger fleets are soft-capped by:
- **Logistics burden:** More ships = more supply consumption, longer supply chains
- **Detection visibility:** Larger fleets are easier to detect from further away
- **Strategic telegraphing:** A 100-ship deathball announces your intentions

Multiple smaller coordinated fleets may be strategically superior to one mega-fleet.

## 5.5 Reinforcements

A friendly fleet arriving mid-battle joins with a short coordination delay. Ships arrive but take a moment to integrate into fleet roles before being fully effective.

## 5.6 Multi-Party Battles

When three or more empires meet in combat, targeting is automatic based on diplomatic presets (enemies attacked, neutrals ignored, allies supported). The player can override targeting mid-combat to focus on specific empires or shift priorities.

## 5.7 Retreat

Retreat cost is emergent from the simulation state, not a flat penalty:

- A fleet at maximum range with fast engines barely takes damage while disengaging.
- A brawler fleet locked in close quarters trying to disengage gets shredded.
- The simulation continues until all combatants are separated by approximately 2x maximum weapon range.

**Implications:**
- "Stand Back" positioning makes retreat cheap. "Charge Forward" is a commitment.
- Engine/speed technology directly affects retreat viability.
- Rearguard tactics are real — leave brawlers to cover while carriers and supply ships escape.

## 5.8 Ship Destruction and Overkill

When a ship reaches 0 structure HP, incoming shots do not stop. The degree of overkill determines wreck quality:

| Overkill Severity | Wreck State | Recovery |
|---|---|---|
| Massive (HP far negative) | Debris field | Raw resources only |
| Moderate | Salvageable wreck | Components + some subsystems |
| Minimal (barely destroyed) | Disabled hull | Potentially repairable or capturable |

**Implications:** Weapon choice affects salvage. Railguns that one-shot a frigate leave less to recover than beams that slowly drain shields. A player who wants to capture enemy ships should use sustained weapons and avoid overkill.

## 5.9 Combat Notifications

No auto-resolve. Every engagement gets player attention:
- **Single-player:** Configurable popup with option to pause and navigate to the battle.
- **Multiplayer:** Popup with "take me there" option. May lower the player's speed vote automatically.

## 5.10 Ground Combat

Same depth as space combat but with different roles: infantry, vehicles, and air support instead of brawler/guardian/carrier. Same disposition and target priority system. Orbital bombardment can soften defenses before invasion — bombardment intensity is player-chosen and directly affects damage to buildings and population (see Section 8 Colonization).

## 5.11 Interconnections

- **Ship Design:** Fleet composition and subsystem choices determine combat performance (see Section 9).
- **Logistics:** Supply status directly affects morale, accuracy, and willingness to hold formation (see Section 1).
- **Exploration:** Post-battle salvage depends on overkill mechanics; salvage ships in the fleet can recover components from debris (see Section 7).
- **Territory:** Chokepoint battles have outsized importance due to lane-based movement (see Section 8).

---

# 6. Exploration & Salvage

## 6.1 Discovery

When a scout enters a new system:
- **Large/obvious POI** (megastructures, ship graveyards, major installations) are visible immediately to any empire.
- **Small POI** require scanning technology to detect.
- **Deliberately hidden POI** (secret bases, high-reward caches) require advanced scanning technology.
- **Color affinity** improves discovery speed and quality for that color's sites.
- **Revisiting old systems** with better technology reveals previously undetectable POI. The galaxy keeps giving.

## 6.2 Survey System (Three Tiers)

| Tier | Requirement | Reveals |
|---|---|---|
| Basic | Starting tech | General site type, rough contents, obvious hazards |
| Detailed | Mid-tier scanning tech | Specific subsystems, quantities, hazard details, off-color caches |
| Deep | Advanced scanning tech | Full contents, hidden layers, precise hazard mapping |

**Survey tiers do not gate access.** A player can attempt extraction from any site at any survey level. However, lower survey quality means worse extraction efficiency and higher hazard risk. Surveying is information, not a key.

**Color affinity** improves survey speed and depth for that color's sites.

**Existing surveyed sites reveal new layers** when revisited with better technology. A site surveyed at basic level years ago yields new information with a detailed scan.

## 6.3 Extraction

**Two modes:**

**Salvage Ships (mobile, fast):** Park salvage ships at a site. They extract while stationed. Good for grabbing high-value resources quickly before a rival arrives, or for working sites without committing infrastructure.

**Outposts (sustained, efficient):** Build an outpost at the POI. Better yields, can assign pops, benefits from buildings. Takes time to establish and announces your presence.

**Derelict salvage bypasses refinement** — salvaged components go directly into the component stockpile without needing to refine from raw ore. This is why salvage remains valuable throughout the game even when manufacturing is available.

## 6.4 Site Depletion

Sites have diminishing returns but never fully deplete. There are always residual low-level resources, but the high-value contents go first. This means even "picked over" systems retain some value for desperate or late-arriving empires.

## 6.5 Shared Exploitation

Multiple empires can exploit the same site simultaneously. Different players may extract different subsystems based on their tech level and specialization. Cooperation is possible and sometimes beneficial. The shift from cooperation to competition is entirely player-driven — only becomes competitive when someone decides to block access militarily.

## 6.6 Hazards

All derelict sites carry risk. Hazard severity scales with site value:

- **Minor sites:** Equipment damage, sensor disruption, minor repair costs
- **Major sites:** Guardian fleets (automated precursor defenses requiring combat to defeat), environmental hazards (radiation, contamination, structural collapse), or both
- **Risk reduction:** Higher color affinity, smaller tech gap, and better survey quality all reduce hazard probability and severity

## 6.7 Salvage Yields

Yields are mostly color-matched — Red sites produce Red resources. Cross-color surprises occur at precursor intersection sites (where multiple civilizations collaborated) and battlefield debris (where mixed-color equipment was destroyed).

## 6.8 Scan Data as Commodity

Survey data is tradeable. An empire can sell its scan results to other empires for credits. This makes information brokering a valid playstyle — a Blue-focused empire with superior scanning might profit as much from selling scan data as from direct extraction.

## 6.9 Information Asymmetry

- Low tech: "Salvage detected" — no details.
- High tech: Specific contents, quantities, hazard types, subsystem values.
- Enables bluffing ("this site is worthless"), data trading, and espionage on rival extraction activities.

## 6.10 Interconnections

- **Resource System:** Salvage bypasses refinement, yielding components directly (see Section 1).
- **Expertise:** Salvaging and using precursor tech builds expertise (see Section 2).
- **Economy:** Scan data and salvaged goods are tradeable commodities (see Section 3).
- **Combat:** Post-battle debris fields are salvage sites; overkill determines wreck quality (see Section 5).
- **Territory:** No automatic ownership of sites; military presence or diplomatic agreement required (see Section 8).

---

# 7. Territory & Claims

## 7.1 Core Rule: No Automatic Territory

There is no border system. Having a colony or outpost in a system does not prevent other empires from operating there. If you want exclusive access to a resource, you must guard it militarily or negotiate agreements. This is a fundamental design commitment.

## 7.2 Early Game Implications

Nothing mechanically protects a player's starting area. This is the starting advantage of the Warrior origin — they begin with the fleet strength to defend their home. Other origins must explore fast, make friends, or accept risk. This must be clearly communicated in the tutorial.

## 7.3 Influence Map

The galaxy map shows empire influence as color-coded regions, but these are visual feedback only — not a mechanical system that grants bonuses.

**What projects influence:** Any military asset contributes — stations, fleets, outposts, patrol routes. The more assets in a region, the stronger the influence coloring.

**Contested systems** show both empire colors, split or striped based on relative military strength.

**Influence fades** at the edges of an empire's military reach rather than having hard borders.

## 7.4 Claims

Players can place diplomatic claims on individual POI or entire systems (player chooses scope). Claims are:

- **Visible** to all empires who have contact with you.
- **Configurable** with auto-responses: warn, demand toll, or attack on violation.
- **Detection-based:** The "attack on violation" response requires an actual fleet or station present to enforce. Claims do not shoot on their own. If a stealth ship enters a claimed area undetected, the auto-response does not trigger.
- **Not mechanically enforced:** Claims rely on reputation and the threat of force. Another empire can ignore your claim — the consequence is diplomatic, not automatic.

## 7.5 Toll Collection

- **Automatic** for trade convoys operating under trade agreements — tolls are deducted when convoys pass through claimed chokepoints.
- **Detection-based** for everything else — the station or patrol fleet must detect the passing ship/fleet to demand or collect a toll.
- **Refusal is possible:** An empire can refuse to pay and attempt to run past, triggering a chase or combat situation.

## 7.6 Chokepoint Control

Both patrol fleets and stations can enforce lane control:

- **Patrol fleets** operate on standing orders based on diplomatic status (auto-intercept hostiles, challenge unknowns, pass allies).
- **Stations** provide permanent detection and can be configured with the same diplomatic rules.
- **Combined** coverage is strongest — a station detects, a patrol fleet intercepts.

## 7.7 Interconnections

- **Economy:** Chokepoint control gates trade flow. Blockading trade convoys is economic warfare (see Section 3).
- **Diplomacy:** Claims are a diplomatic signal. Violating claims has reputation consequences (see Section 10).
- **Combat:** Chokepoint battles have outsized importance (see Section 5).
- **Exploration:** Shared exploitation of sites is the default; military blockade is the exception (see Section 6).
- **Visibility:** Detection mechanics determine whether stealth ships can bypass claims (see GDD Section 8).

---

# 8. Colonization

## 8.1 Founding Colonies

**Requirements:** A builder ship + food + credits. No resource (ore/component) cost. The builder ship is occupied for a period while establishing the colony but is not consumed — it becomes available again after setup completes.

**Immediate functionality:** Colonies are functional at baseline immediately. Buildings produce output without pops; population acts as a soft multiplier that boosts production. A fresh colony is not useless — it improves as population grows.

## 8.2 Colony Scale

Building slots are fixed per planet, expandable through technology and terraforming:

| Planet Size | Starting Slots | Expandable To |
|---|---|---|
| Small | 2-3 | +2-4 with tech |
| Medium | 3-4 | 6-8 with tech |
| Large | 5-6 | 8-10 with tech |
| Prime | 8-10 | 12+ with tech |

Every building slot matters. With tight limits, each building decision is meaningful.

## 8.3 Buildings

- **Basic buildings** are universal (farms, mines, basic refineries) — any empire can build them at full efficiency.
- **Advanced buildings** are color-coded (Red forge, Blue data center, Green biolab). Off-color advanced buildings operate at reduced efficiency, matching the general tech efficiency tiers (specialized > adjacent > distant).
- Single production queue per colony.
- Buildings provide: passive production, resource extraction bonuses, expert slots, population capacity, defensive emplacements, logistics hub capacity.

## 8.4 Population

- **Soft multiplier:** Buildings produce baseline output. Pops boost that output. More pops = more production, but a colony with zero extra pops still functions.
- **Multiple species** can coexist on one colony. Species have distinct mechanical traits (research aptitude, mining efficiency, combat readiness, growth rate). Assigning the right species to the right work pool matters.
- **Growth** is slow by default. Accelerated by food surplus, credits investment, and Green technology.
- **Movement** between colonies requires transport ships and takes travel time.
- **New species** enter your empire through conquest (capture enemy colonies), immigration agreements (diplomacy), and natural migration along trade routes.

## 8.5 Pop Allocation

Pops are allocated to broad work pools (unchanged from GDD): Production, Research, Food, Mining, and Expert Slots (building-granted specialist positions). Colony priority can be set (research focus, production focus, growth focus) to adjust automatic allocation.

## 8.6 Terraforming

A mid-game option. Gradually increases planet habitability and size, unlocking additional building slots and potentially upgrading outposts to full colonies. Green technology significantly accelerates terraforming. This provides a path to creating habitable worlds in systems that lack them naturally.

## 8.7 Conquest

When conquering an enemy colony through ground combat, damage is proportional to bombardment intensity — the player chooses how hard to shell before invasion:

- **Light bombardment:** Colony infrastructure mostly intact, population reduced slightly, but integration is slower (unhappy population).
- **Heavy bombardment:** Significant building and population damage, but remaining population offers less resistance.
- Surviving population is unhappy and integrates over time.

## 8.8 Colony Death

A colony that reaches 0 population is abandoned. Buildings remain but become inactive. The colony decays over time and can be reclaimed by any empire. Special automation technology can keep colonies and outposts running without population — a late-game capability.

## 8.9 Interconnections

- **Resource System:** Colonies host refineries that convert ore to energy/components. Mining requires local deposits (see Section 1).
- **Expertise:** Colony pop allocation to manufacturing affects refinery efficiency (see Section 2).
- **Economy:** Colonies generate trade goods, tax revenue, and can host contract manufacturing (see Section 3).
- **Territory:** Colonies do not create borders. Military protection is required (see Section 7).
- **Combat:** Ground combat determines conquest outcomes; bombardment intensity is a strategic choice (see Section 5).

---

# 9. Ship Design

## 9.1 Design Model

Ships use a pure slot-based system. Each chassis has a fixed set of slots. Each slot has two properties:

- **Size:** Big or small
- **Type:** Specific (weapon, defense, engine, sensor, etc.) or universal (anything goes)

The player fills each slot from a dropdown of researched subsystems that match the slot's size and type. This is the entire system — no separate "free capacity" or "extras" layer. Customization depth comes from which subsystems are placed in which slots.

## 9.2 Chassis

Seven size classes (Fighter through Titan), each with two chassis variants. The two variants differ across multiple axes:

- Slot layout (one may be combat-focused, the other utility-focused)
- Visibility profile
- Speed
- Durability

This provides meaningful choice at the chassis level before slot-filling even begins.

## 9.3 Color Mixing

A single ship can mount subsystems from multiple precursor colors. A cruiser might carry Red plasma cannons, Blue sensor arrays, and Green regenerative armor. Each color's subsystems consume that color's energy and components for supply. Multi-color ships are powerful but logistically demanding — they stress multiple color supply chains simultaneously.

## 9.4 Design Constraints

- Can only design with currently researched subsystems — no aspirational blueprints.
- Ship designs are saved per empire and buildable at any qualifying shipyard.
- Larger ship classes are gated by both technology research AND shipyard size. Starting shipyards can only build smaller classes; shipyard upgrades are required for capital ships.

## 9.5 Refit

Existing ships can be fully redesigned at any shipyard — any slot can be swapped. Refitting takes time and resources. There are no expertise implications for the ship itself since expertise is empire-wide per subsystem, not per hull.

## 9.6 Interconnections

- **Expertise:** Subsystem expertise bonuses (build time, cost, weight, supply consumption, combat effectiveness) apply to every ship using that subsystem (see Section 2).
- **Resource System:** Multi-color ships create multi-color supply chain demands (see Section 1).
- **Combat:** Ship design determines fleet role effectiveness, weapon/defense matchups, and logistics profile (see Section 5).
- **Economy:** Ships can be bought and sold; seller's expertise bakes into performance (see Section 3, Section 4).
- **Logistics:** Gold technology becomes critical for empires running diverse multi-color fleets.

---

# 10. Diplomacy

## 10.1 Negotiation

Back-and-forth proposal system: offer, counter-offer, accept, or reject. Single-term deals — complex relationships are built by stacking multiple separate agreements, not by bundling everything into one mega-deal. Everything maps to credits for clear value comparison.

## 10.2 Deal Duration

Player chooses per deal: fixed duration or ongoing. Fixed-duration deals expire naturally with no reputation cost. Ongoing deals continue until canceled.

## 10.3 Agreement Types

- Non-aggression pacts
- Alliances (military cooperation, shared vision, mutual defense)
- Trade agreements (improved market access, reduced fees)
- Mining/salvage/passage rights (with optional toll percentage)
- Tech rental (see Section 4)
- Ship rental and purchase
- Contract manufacturing
- Tributary agreements (income cut to the dominant power, but no diplomatic control)
- Peace treaties with negotiated terms

## 10.4 Reputation

**Per-empire.** Each empire forms its own opinion of you based on direct interactions. Reputation spreads slowly to other empires through contact and trade networks. An empire you have no contact with has no opinion of you.

**Recovery:** Slow passive recovery over time with good behavior. Extreme betrayals (breaking non-aggression pacts, surprise attacks) may leave some permanent damage.

**Espionage interaction:** Espionage can accelerate the spread of negative reputation but cannot fabricate events (see Section 11).

## 10.5 War

**Formal declaration** is possible with a small reputation cost — it is expected and understood. **Surprise attack** (attacking without declaration) incurs massive reputation damage. The player always has the option to declare formally first.

**Peace** is negotiated through the same back-and-forth proposal system as any other deal. The winner can demand terms (credits, rights, territory concessions). Both sides must agree.

## 10.6 Defensive Coalitions

When an empire behaves aggressively, AI empires may propose defensive pacts to threatened neighbors. These are semi-automatic — the AI proposes, the player must accept. Players can also call allies into wars through alliance agreements.

## 10.7 Market Access

First contact with an empire allows browsing their market listings. A formal trade agreement provides reduced fees and better access.

## 10.8 Information in Negotiations

Public information is revealed through scouting and trade — things like colony count, general military strength, and known fleet positions. This information may be slightly outdated. Espionage reveals hidden details: secret fleets, build queues, new technology research, resource stockpile levels. Players negotiate with incomplete information unless they have invested in intelligence.

## 10.9 AI Diplomacy

Aspirationally advanced. AI empires have personalities driven by their precursor color and origin template. A Red Warrior AI is aggressive and territorial. A Gold Hauler AI seeks trade partnerships. A Blue Chronicler AI hoards information and plays empires against each other. AI difficulty levels adjust strategic sophistication. AI empires propose deals, form coalitions, evaluate threats, and participate in the market economy.

## 10.10 Interconnections

- **Economy:** Trade agreements gate market access. Blockades and embargoes are economic tools (see Section 3).
- **Territory:** Claims are diplomatic signals enforced by reputation and military threat (see Section 7).
- **Espionage:** Intelligence quality affects negotiation leverage (see Section 11).
- **Tech Renting:** Rental agreements are subject to reputation consequences (see Section 4).
- **Reputation:** A long-term strategic resource — empires known for honoring deals form partnerships more easily.

---

# 11. Espionage

## 11.1 Overview

Espionage is abstracted (not agent-based). Two layers of intelligence gathering with limited active operations available at high cost and risk.

## 11.2 Passive Intelligence

Trade routes and diplomatic contact generate low-level intelligence for free. This is random and general — basic awareness of an empire's activity, not targeted information. The more contact (trade, proximity, shared sites), the more passive intel flows.

## 11.3 Active Operations — Intelligence Gathering

Targeted operations: the player picks an empire, an intelligence category, and invests credits over time. Longer investment = higher success chance. The player chooses when to launch — risk/reward tradeoff between investing longer for better odds versus acting quickly.

**Intelligence categories:** Map intelligence, activity intelligence, technology intelligence, resource intelligence, construction intelligence, salvage intelligence (unchanged from GDD).

## 11.4 Active Operations — Offensive

Limited, expensive, and risky. Three available offensive operations:

- **Sabotage:** Damage buildings or shipyards
- **Steal Research:** Gain research progress in a targeted subsystem
- **Spread Negative Reputation:** Accelerate the spread of real negative information about a target empire to other empires (cannot fabricate events)

**Framing:** On exceptional operation success (regardless of empire color), the player gets the option to frame another empire for the operation. Blue empires achieve exceptional success more often due to their espionage advantage, but framing is not Blue-exclusive.

## 11.5 Counter-Intelligence

Two layers of defense:

- **Passive defense floor:** Credit investment raises baseline resistance to all espionage. Always active.
- **Active detection chance:** On top of the passive floor. Chance to detect operations in progress or after completion.

**Detection spectrum:**
- **Partial detection:** Target knows an operation occurred but not who did it. Creates suspicion and diplomatic tension without proof.
- **Full detection:** Target identifies the perpetrator. Reputation damage, diplomatic incident, potential war justification.

## 11.6 Physical Espionage

Scout ships can be upgraded with espionage modules (Blue tech specialization) to gather intelligence while performing normal scouting duties. This creates overlap between exploration and espionage roles — a Blue empire's scout fleet doubles as an intelligence network.

## 11.7 Blue Advantage

Azure Lattice technology provides a 30-50% effectiveness advantage in espionage operations — best but not exclusive. Blue tech also improves counter-intelligence quality and espionage module effectiveness. Other empires are competent at espionage; Blue empires are superior.

## 11.8 Interconnections

- **Diplomacy:** Espionage reveals information that affects negotiation leverage. Detection of operations causes reputation damage (see Section 10).
- **Expertise:** Enemy expertise profiles are discoverable through espionage, revealing strategic capabilities (see Section 2).
- **Economy:** Espionage is funded by credits. Intelligence about resource stockpiles and production informs trade strategy (see Section 3).
- **Tech Renting:** Sellers can detect dismantled purchased ships via espionage (see Section 4).
- **Exploration:** Scout ships with espionage modules serve dual roles (see Section 6).
- **Territory:** Intelligence about hidden fleets and build queues informs territorial defense decisions (see Section 7).

---

# Appendix A: Deferred Systems

The following systems were identified during design but explicitly deferred for future iteration:

- **Detailed currency and inflation mechanics** — per-empire minting, exchange rates, inflation. Current design uses universal credits.
- **Victory conditions** — precursor mystery victory (requiring all five colors), domination, economic victory, and other paths.
- **Narrative events and quest chains** — story-driven discovery sequences, the precursor disappearance mystery.
- **Leader progression** — admiral and governor skill trees, experience gain, leveling.
- **Economic warfare** — market manipulation, dumping, resource cornering. Currently limited to blockades and embargoes.
- **Active precursor crisis events** — Stellaris-inspired endgame threats where precursor remnants become active.
- **Black market and smuggling** — mechanics for bypassing blockades. Referenced in GDD but not designed.
- **AI replacement for disconnected multiplayer players.**

---

# Appendix B: Key Design Changes from GDD v1

| Area | GDD v1 | This Document |
|---|---|---|
| Resource layers | Ambiguous overlap between "raw resources" and "components" | Three clear layers: ore, energy, components with defined conversion pipelines |
| Resource count per color | Unclear (4 raw + 2 components, or 4 raw becoming 4 components) | 2 ore + 2 energy + 2 components = 6 tracked values per color |
| Fleet supply | Consumed "energy and parts" without clear source | Consumes color-matched energy and components from the same pool as construction |
| Expertise thresholds | Vague "dual-layer mastery" | 12+ interleaved thresholds with rotating bonus types, logarithmic XP curve |
| Expertise tier carryover | Not specified | No carryover — mastered T2 competes with fresh T3 |
| Currency | Per-empire with inflation (ambitious) | Universal credits; inflation deferred to expansion |
| Trade delivery | Not specified | Physical delivery through lanes, blockade-able |
| Tech rental research gain | "Renter gains research progress" (vague) | Good gain, gap-dependent, diminishing over time. Full expertise gain. |
| Ship purchase expertise | Not specified | ~Half seller's expertise baked in. Full operating expertise gain. |
| Ship extras/free capacity | Separate "extras" system with "free capacity" | Eliminated — all customization through sized and typed slots |
| Territory | "No automatic territory" (stated but not detailed) | Full influence map, claim system with auto-responses, detection-based enforcement |
| Exploration survey | "Multiple turns" (vague) | Three explicit tiers (basic/detailed/deep), each gating efficiency not access |
| Combat retreat | "Takes time, eating fire" | Emergent from simulation — cost depends on range, speed, engagement depth |
| Ship destruction | Not specified | Overkill spectrum — massive damage = debris, minimal = capturable hull |
| Espionage framing | Blue-exclusive | Available to any empire on exceptional success |
