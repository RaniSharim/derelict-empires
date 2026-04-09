

**DERELICT EMPIRES**

Comprehensive Game Design Document

*A Post-Apocalyptic Salvage-Punk 4X/5X Strategy Game*

Version 1.0  •  April 2026

**CONFIDENTIAL**

# **Table of Contents**

# **1\. Game Overview**

## **1.1 High Concept**

Derelict Empires is a real-time 4X/5X strategy game set in a post-apocalyptic galaxy where five great precursor civilizations have mysteriously vanished. Players control young civilizations that must salvage, reverse-engineer, and cobble together technology from the ruins to rebuild. The fifth X — eXchange — elevates trade, tech renting, and diplomatic negotiation to a core pillar of gameplay alongside eXplore, eXpand, eXploit, and eXterminate.

## **1.2 Core Fantasy**

The player is a resourceful scavenger-king, making the most of imperfect, jury-rigged technology. Every ship, every station, every weapon tells a story of adaptation. The galaxy is a graveyard of wonders, and the question is never whether you have enough — it’s whether you can make what you have work better than your rivals can.

## **1.3 Aesthetic Direction**

**Used Future.** Everything looks functional, repaired, improvised. Hulls are patched with mismatched plating. Stations have visible welds. Off-color technology appears visibly jury-rigged and inefficient. The galaxy itself is scarred — depleted, war-torn towards the core, more pristine at the rim.

## **1.4 Key Design Principles**

* **Salvage Everything:** Technology comes from finding and adapting precursor remnants. Manufacturing from scratch is late-game and expensive.

* **Nothing Is Locked:** Any empire can use any technology, just with varying efficiency. Specialization is emergent, not enforced.

* **Cooperation and Competition:** Sites can be shared until resources become scarce. No automatic territory — if you don’t guard it, anyone can take it.

* **Information Asymmetry:** Knowledge is power. What you can see, scan, and deduce determines your strategic options.

* **Minimal Micromanagement:** Automation, fleet presets, and simple colony management keep focus on strategic decisions.

* **The 5th X — eXchange:** Deep trade mechanics including tech renting, component markets, mining/salvage rights negotiation, and passive trade goods flow.

## **1.5 Target Specifications**

| Parameter | Specification |
| :---- | :---- |
| Genre | Real-Time 4X/5X Strategy |
| Players | 1–8 human players \+ up to 30 AI empires |
| Game Length | \~10 hours single-player, shorter multiplayer |
| Galaxy Size | \~100 star systems (configurable) |
| Game Speed | Real-time with x0 (pause), x1, x2, x4, x8 speed controls |
| Multiplayer Speed | Averaged vote among connected players |
| Mod Support | Data-driven design for community modding |

# **2\. Galaxy Structure & Map**

## **2.1 Galaxy Topology**

The galaxy is a node-based map where star systems are connected by navigable lanes. The galaxy has a spiral arm structure with a dense central core.

### **2.1.1 Spiral Arms**

* Configurable 2–8 arms (default 4).

* Each arm is aligned primarily to one precursor color, with mixing increasing toward the core.

* Arms start wide at the core connection and narrow toward the rim.

* Each arm contains approximately 20 systems (at default 100-system galaxy).

* The core blob contains approximately 20 interconnected systems.

* 1–5 parallel paths through each arm with some cross-connections.

* Each arm has a decent chance of 1–3 natural chokepoints.

### **2.1.2 Core vs. Rim Tradeoff**

| Factor | Core | Rim |
| :---- | :---- | :---- |
| Habitable Planets | Scarce, depleted, war-torn | More pristine, more habitable |
| Precursor Sites | Dense ship graveyards, bunkers, stations | Fewer but less dangerous |
| Resources | More color resources, higher concentration | Less concentrated, more asteroid mining |
| Contact with Others | Early, frequent, contested | Delayed, more time to develop |
| Danger Level | Higher — guardians, traps, competition | Lower — safer expansion |

### **2.1.3 Lane Types**

* **Visible Lanes:** Standard navigable connections between systems. Available to all empires from game start.

* **Hidden Lanes:** Invisible connections that require specific racial traits (Hauler origin) or technology to detect and use. Scattered randomly throughout the galaxy. Many inter-arm connections are hidden lanes.

* **Free Navigation (Late-Game):** Advanced technology allows off-lane travel between stars. Significantly slower than lane travel. Opens strategic flanking options.

## **2.2 Star Systems**

Each star system contains 3–5 points of interest (POI). Intra-system travel time is very short. Ships must be stationed at a specific POI or set to patrol the entire system.

### **2.2.1 Point of Interest Types**

* **Habitable Planets:** Rare. Can support colonies with population growth. Quality varies by size category (see Colonies section).

* **Barren/Hostile Planets:** Common. Can host outposts for mining, salvage, or logistics. May become colonizable with terraforming tech.

* **Asteroid Fields:** Especially common in the rim. Suitable for mining operations.

* **Debris Fields:** Remnants of precursor battles. Rich salvage sites.

* **Abandoned Stations:** Precursor-built stations orbiting planets or other POI. Can be claimed, repaired, or scavenged.

* **Ship Graveyards:** Clusters of derelict vessels. Major salvage sites, may contain recoverable ships.

* **Megastructures:** Massive precursor constructions. Extremely valuable, extremely dangerous.

### **2.2.2 System Terrain Effects**

Points of interest within a system can have terrain modifiers that affect combat and development. These include nebula pockets (sensor disruption), radiation zones (shield interference), gravity anomalies (movement penalties), and dense asteroid clusters (cover for small ships). These apply as flat modifiers rather than tactical positioning.

# **3\. The Five Precursor Civilizations**

Five great civilizations once dominated the galaxy. Their ruins, technology, and mysteries form the backbone of all gameplay systems. Each is identified by a color that permeates their technology, architecture, and salvageable components.

## **3.1 Precursor Overview**

### **3.1.1 The Crimson Forge (Red)**

* **Specialization:** Weapons, energy production, combat vessels, industrial manufacturing

* **Artifacts:** Plasma weapons, fusion reactors, automated factories, forge-worlds

* **Salvage Yields:** Power cores, weapon components, industrial matrices

* **Manufacturing Focus:** Combat vessels, weapons, defensive structures

### **3.1.2 The Azure Lattice (Blue)**

* **Specialization:** Information warfare, espionage, communication, scanning technology

* **Artifacts:** Quantum computers, spy satellites, data archives, sensor arrays

* **Salvage Yields:** Encryption modules, sensor components, data cores

* **Manufacturing Focus:** Spy satellites, communication arrays, scanner probes

### **3.1.3 The Verdant Synthesis (Green)**

* **Specialization:** Biological technology, terraforming, adaptation, medical tech

* **Artifacts:** Seed vaults, bio-computers, living ships, genetic labs

* **Salvage Yields:** Biological samples, genetic templates, organic processors

* **Manufacturing Focus:** Colony ships, terraforming equipment, medical facilities

### **3.1.4 The Golden Ascendancy (Gold)**

* **Specialization:** Transportation, logistics, trade, exploration

* **Artifacts:** Jump gates, trade stations, navigation systems, cargo vessels

* **Salvage Yields:** Navigation data, hyperdrive components, trade goods

* **Manufacturing Focus:** Trade ships, exploration vessels, logistics infrastructure

### **3.1.5 The Obsidian Covenant (Purple/Black)**

* **Specialization:** Exotic technologies, consciousness manipulation, unique components

* **Artifacts:** Psi-amplifiers, consciousness matrices, void extractors

* **Salvage Yields:** Exotic materials, consciousness cores, dark matter

* **Manufacturing Focus:** Unique components, experimental modules, specialty upgrades

## **3.2 Color Resources**

Each precursor color has four associated raw resources: simple and advanced variants for both energy and parts. Simple resources are common and fuel early-game technology. Advanced resources are rare, found primarily at major precursor sites and deep excavations, and gate late-game capabilities. All resources can be mined from deposits or salvaged from derelicts. They are more concentrated in their aligned galaxy arm and in the core, but can appear anywhere — cross-color battlefield sites may spawn on any arm.

### **3.2.1 Red (Crimson Forge) Resources**

| Resource | Type | Rarity | Description |
| :---- | :---- | :---- | :---- |
| Plasma Embers | Simple Energy | Common | Residual thermal energy harvested from forge remnants |
| Fusion Cores | Advanced Energy | Rare | Intact precursor reactor fuel cells |
| Scrap Iron | Simple Parts | Common | Salvageable metals and structural alloys |
| Forge Matrices | Advanced Parts | Rare | Precision-machined industrial templates |

### **3.2.2 Blue (Azure Lattice) Resources**

| Resource | Type | Rarity | Description |
| :---- | :---- | :---- | :---- |
| Signal Residue | Simple Energy | Common | Ambient electromagnetic traces from data networks |
| Quantum Resonance | Advanced Energy | Rare | Stabilized quantum field generators |
| Data Chips | Simple Parts | Common | Fragmentary storage media and circuit boards |
| Lattice Crystals | Advanced Parts | Rare | Perfectly structured computing substrates |

### **3.2.3 Green (Verdant Synthesis) Resources**

| Resource | Type | Rarity | Description |
| :---- | :---- | :---- | :---- |
| Bio-Luminance | Simple Energy | Common | Bioluminescent organisms that convert light to power |
| Genesis Catalysts | Advanced Energy | Rare | Concentrated life-force accelerants |
| Organic Polymers | Simple Parts | Common | Biological structural compounds and fibres |
| Genetic Templates | Advanced Parts | Rare | Complete precursor genome sequences and growth patterns |

### **3.2.4 Gold (Golden Ascendancy) Resources**

| Resource | Type | Rarity | Description |
| :---- | :---- | :---- | :---- |
| Solar Dust | Simple Energy | Common | Stellar particle collections from trade route beacons |
| Hyperlane Essence | Advanced Energy | Rare | Concentrated spatial-fold energy from jump gate remnants |
| Navigation Fragments | Simple Parts | Common | Partial star charts and basic guidance hardware |
| Transit Matrices | Advanced Parts | Rare | Complete hyperdrive calibration assemblies |

### **3.2.5 Purple (Obsidian Covenant) Resources**

| Resource | Type | Rarity | Description |
| :---- | :---- | :---- | :---- |
| Void Whispers | Simple Energy | Common | Faint exotic energy traces from covenant sites |
| Dark Matter Cores | Advanced Energy | Rare | Stabilized dark matter power sources |
| Exotic Fragments | Simple Parts | Common | Unusual material samples with anomalous properties |
| Consciousness Shards | Advanced Parts | Rare | Psionic crystalline structures with encoded awareness |

## **3.3 Component System**

All technology runs on a dual-tier component system derived from the five precursor colors.

* **Basic Components** (Basic Red, Basic Blue, etc.): Common salvageable materials from abandoned outposts, damaged infrastructure, and surface-level excavations. Power cells, data chips, organic samples, navigation crystals, exotic particles. Foundation for early technologies.

* **Advanced Components** (Advanced Red, Advanced Blue, etc.): Rare, high-quality materials from major precursor sites, deep excavations, and significant derelict discoveries. Fusion cores, quantum processors, genetic templates, hyperdrive matrices, consciousness cores. Required for late-game capabilities.

This creates 10 total component types (5 colors × 2 tiers) plus 20 raw resources (5 colors × 4 resource types: simple energy, advanced energy, simple parts, advanced parts). Advanced resources and components ensure major precursor sites remain contested throughout the game, while simple resources provide the foundation for early expansion.

## **3.4 Tech-Scaled Trade Goods**

Special resources that become more valuable as related technology advances, creating escalating strategic importance:

* **Quantum Flux (Blue):** Worthless → Computing substrate → Essential processing medium

* **Bio-Samples (Green):** Organic matter → Genetic data → Evolution templates

* **Energy Signatures (Red):** Background radiation → Power source → Weapons fuel

* **Void Crystals (Purple):** Decorative → Consciousness storage → Reality manipulation

* **Navigation Data (Gold):** Confusing charts → Trade routes → Hyperlane prediction

# **4\. Origins & Starting Conditions**

## **4.1 Precursor Affinity**

Each player chooses which precursor civilization uplifted or created their species. This determines:

* Starting technology understanding in that color.

* Increased nearby derelict spawns of that color.

* Small efficiency bonus with patron’s equipment.

* Starting position biased toward that color’s galaxy arm.

## **4.2 Origin Templates**

The player’s origin determines their starting role in precursor society and provides distinct gameplay advantages:

| Origin | Focus | Key Advantages | Bonus Starting Ship |
| :---- | :---- | :---- | :---- |
| Warriors | Military / Enforcement | Combat bonuses, military efficiency, stronger starting fleet | Extra Fighter |
| Servitors | Research / Maintenance | Research speed bonus, maintenance cost reduction, better repair | Extra Salvager |
| Haulers | Logistics / Trade | Can see hidden lanes, extended starting visibility, trade bonuses | Extra Scout |
| Chroniclers | Research / Color Affinity | Deeper color research bonus, better salvage identification, faster expertise gain | Extra Scout |
| Free Race | Independent Development | No precursor affinity, balanced start, unique flexibility | Extra Builder |

## **4.3 Starting Assets**

All players begin with:

* One home colony on a habitable planet.

* One station with a small shipyard.

* One Scout ship.

* One Fighter ship.

* One Salvage ship.

* One Builder ship.

* One bonus ship determined by origin template.

* Basic research progress based on affinity and origin.

* Visibility of systems connected to the home system (Haulers see further).

## **4.4 Starting Location**

Players choose only their arm preference (which galaxy arm to start near). Starting position is then placed based on this choice. Players starting near the core encounter other empires sooner but access richer precursor sites. Players near the rim have more time to develop but fewer high-value targets.

## **4.5 Empire Traits**

* **Creative (inspired by Master of Orion 2):** Empire-wide trait. When unlocking a research tier in a category, Creative empires unlock all 3 subsystems instead of the normal 2 random \+ 1 from salvage/trade. Powerful but may come at a cost in other areas.

Additional empire traits to be designed. These should provide meaningful strategic differentiation without hard-locking any gameplay path.

# **5\. Technology & Research**

## **5.1 Tech Tree Structure**

The technology system is organized by color, category, and tier. Research is empire-wide (single research queue) to minimize micromanagement.

### **5.1.1 Categories (same across all 5 colors)**

* **Weapons / Propulsion / Energy:** Ship weapons, engine tech, power generation, energy efficiency.

* **Computing / Sensors / Systems / Automation:** Detection, electronic warfare, fleet command, automated processes.

* **Industry / Mining / Terraforming:** Resource extraction, manufacturing, planet modification, construction.

* **Administrative / Social / Logistics:** Supply chains, governance, morale, trade efficiency, fleet provisioning.

* **Special:** Exotic technologies, synergy unlocks, unique precursor-specific capabilities.

### **5.1.2 Tier Structure**

* 6 tiers per category per color (totaling 150 base research nodes across the full tree).

* Unlocking a tier in a category reveals 3 subsystems. 2 are randomly available for immediate research; 1 must be obtained through salvage, trade, or the Creative trait.

* Each subsystem is a shorter research project that unlocks specific equipment, buildings, or capabilities.

* Some subsystems are standard, others are “high-tier” with greater cost but proportionally greater power.

### **5.1.3 Efficiency Tiers**

Every civilization can eventually access all technologies, but with varying efficiency based on color affinity:

| Affinity Level | Efficiency | Research Speed | Manufacturing Cost | Expertise Gain |
| :---- | :---- | :---- | :---- | :---- |
| Specialized (your color) | 100% | Fastest | Lowest | Fastest |
| Adjacent (synergy colors) | 70% | Moderate | Higher | Moderate |
| Distant (opposite colors) | 40% | Slowest | Highest | Slowest |

## **5.2 Research Methods**

### **5.2.1 Free Research**

Standard research progress through the tech tree. Slower but guaranteed. Players allocate empire-wide research output to a chosen project.

### **5.2.2 Salvage-Driven Research**

Salvaging precursor artifacts provides research bonuses based on the tech gap:

* **Same tier salvage:** Research points toward specific subsystem if not yet unlocked.

* **1 tier ahead:** Research points toward both the tier unlock and the specific subsystem.

* **2 tiers ahead:** Research points toward the tier unlock only.

* **3+ tiers ahead:** Yields only raw components for parts. Too advanced to learn from.

## **5.3 Expertise System**

A dual-layer mastery system that rewards consistent use of technology:

* **Specific Equipment Expertise:** Building and using specific units/subsystems reduces their cost and increases their efficiency over time.

* **General Color Expertise:** Broader understanding of a color’s technology provides small bonuses to all related tech. Tracked empire-wide.

This creates meaningful dilemmas about when to upgrade to newer technology versus maintaining expertise with proven equipment.

## **5.4 Synergy Technologies**

When a player reaches sufficient tier levels in two different colors, synergy research options unlock. These hybrid technologies provide unique capabilities unavailable from any single color:

| Combination | Synergy Name | Capability |
| :---- | :---- | :---- |
| Red \+ Blue | Precision Targeting | Enhanced weapon accuracy through sensor integration |
| Red \+ Green | Regenerative Armor | Self-repairing hull plating using biological processes |
| Green \+ Gold | Self-Replicating Logistics | Supply chains that grow organically |
| Blue \+ Gold | Perfect Efficiency | Optimized manufacturing and trade routing |
| Gold \+ Blue | Deep Scanning | Extended detection and survey range |
| Gold \+ Purple | Dimensional Surveying | Detection of hidden lanes and anomalies |
| Green \+ Red | Extreme Adaptation | Colonization of hostile environments |
| Green \+ Purple | Conscious Worlds | Terraforming with sentient biospheres |
| Blue \+ Purple | Psychic Intelligence | Espionage through consciousness manipulation |
| Blue \+ Green | Biological Infiltration | Living spy systems and counter-intelligence |

## **5.5 Manufacturing**

Manufacturing components from raw resources (rather than salvaging) is an advanced capability:

* Basic component manufacturing unlocks at mid-tier research in the relevant color.

* Advanced component manufacturing requires late-tier research.

* Buildings provide both passive manufacturing output and efficiency bonuses to active production.

* Manufacturing is expensive compared to salvage — finding precursor sites remains valuable throughout the game.

* Higher-tier tech can sometimes achieve the same benefit as resource-intensive lower-tier approaches, creating upgrade incentive.

# **6\. Ships & Fleet Management**

## **6.1 Ship Size Classes**

Seven size classes from smallest to largest. Each size class has 2 chassis variants that trade off between visibility and available slots.

| Size Class | Role Tendency | Base Slots | Visibility | Notes |
| :---- | :---- | :---- | :---- | :---- |
| Fighter | Swarm / Intercept | \~1 | Very Low | Carrier-launched only. Cannot operate independently. |
| Corvette | Fast Attack / Scout | \~2–3 | Low | 2 chassis: fast/stealthy vs. armed/visible |
| Frigate | Escort / Patrol | \~3 | Low-Medium | 2 chassis: e.g. fast/few slots vs. slow/more slots |
| Destroyer | Multi-Role | \~3 | Medium | 2 chassis: balanced between offense and utility |
| Cruiser | Line Combat | \~3 | Medium-High | 2 chassis: heavy weapons vs. heavy defense |
| Battleship | Heavy Assault | \~3 | High | 2 chassis: broadside vs. carrier-capable |
| Titan | Flagship / Siege | \~3 | Very High | 2 chassis: supreme firepower vs. fleet command |

## **6.2 Ship Design**

Ship design uses a slot-based system without visual drag-and-drop:

* **Big System Slots:** Each chassis has a fixed number of major slots for primary systems (weapons, shields, engines, sensors, etc.). Filled from a list/dropdown interface.

* **Free Capacity:** Remaining weight/space capacity for extras, upgrades, and modifications to installed systems.

* **Extras:** Adding modifications to a big system costs weight and energy but not additional slots. This allows fine-tuning without slot competition.

* **Refit:** Existing ships can be refitted with new technology at a shipyard. Takes time and resources but preserves the hull.

## **6.3 Fleet Roles**

Each fleet has slots for defined roles. Each ship is assigned to exactly one role. Role slot limits are determined by command technology and admiral traits.

| Role | Function | Typical Ships |
| :---- | :---- | :---- |
| Brawler | Front-line combat, direct engagement, high damage output | Destroyers, Cruisers, Battleships |
| Guardian | Point defense, electronic warfare, fleet protection. Set to protect specific fleet elements. | Frigates, Destroyers, Cruisers |
| Carrier | Launch and recover fighters and drones. Operates from range. | Battleships (carrier variant), dedicated carriers |
| Bombard | Orbital bombardment, siege, structure damage | Cruisers, Battleships, Titans |
| Scout | Detection, screening, reconnaissance. Configurable screen radius. | Corvettes, Frigates |
| Non-Combatant | Supply ships, salvagers, builders. Separate slots, not limited by combat command tech. | Any civilian-fitted chassis |

### **6.3.1 Fleet Role Limits**

* Each role has a maximum ship count determined by command technology level.

* Admiral traits can modify role limits (e.g., a carrier-focused admiral gets an extra carrier slot, or can exchange a brawler slot for a bombard slot).

* Non-combatant slots are separate from combat role limits.

* Multiple fleets can operate independently in the same system.

## **6.4 Fleet Commands & Automation**

* **Patrol Lanes:** Station a fleet and assign lane patrol. The fleet automatically covers a few hops in each direction.

* **Exploration:** Automated exploration command sends scouts to discover new systems.

* **Fleet Templates:** Save a fleet composition template. Auto-reinforce pulls replacement ships from shipyards to maintain the template.

* **Dispositions:** Pre-set combat behavior (see Combat section) that persists until changed.

## **6.5 Fleet Management UI**

* Global fleet list accessible from anywhere, showing fleet name, location, composition summary, and current orders.

* “Take me to fleet” button for instant camera navigation.

* Fleets can be renamed for organization.

* Splitting fleets incurs a reorganization delay proportional to the number of ships involved.

* Merging fleets is faster but still requires brief coordination time.

# **7\. Combat**

## **7.1 Combat Overview**

Combat is simulated in 3D space but abstracted to role-based commands rather than individual ship control. Engagements take real time (target \~30 seconds for an early-game skirmish) and players can adjust fleet disposition during the battle. Players do not control individual ships — they assign ships to fleet roles and set dispositions for each role.

## **7.2 Weapons Triangle**

Three weapon types interact with three defense types in an asymmetric triangle:

### **7.2.1 Weapons**

| Weapon | Tracking | Range | Speed | Logistics Cost | Best Against |
| :---- | :---- | :---- | :---- | :---- | :---- |
| Lasers / Beams | Best | Medium | Instant | High Energy, Low Parts | Shields (sustained drain), small ships |
| Railguns | Poor (vs small) | Longest | Slow projectile | Low Energy, High Parts | Armor (raw penetration), large ships |
| Missiles / Drones | Good | Long | Fast | Medium Energy, Medium Parts | Overwhelm PD, flexible targeting |

### **7.2.2 Defenses**

| Defense | Activation | Effect | Logistics Cost | Notes |
| :---- | :---- | :---- | :---- | :---- |
| Point Defense (PD) | First (intercept phase) | Attempts to destroy incoming missiles, drones, fighters, and railgun rounds. % hit based on tracking vs. evasion/ECM. Can be AoE or pinpoint. | Moderate | Also effective against fighters and drones |
| Shields | Second (absorption phase) | Absorbs a percentage of incoming damage. Loses points per hit. Regenerates during combat. | High Energy, Low Parts | Best against sustained laser fire |
| Armor | Last (damage reduction) | Flat damage reduction per hit. Reduces directly from armor health pool. | Low Energy, High Parts | Best against many small hits |

Structure health sits behind all defenses. Once armor is breached, hits damage structure directly.

## **7.3 Combat Dispositions**

Players set behavior preferences for each fleet role before and during combat:

### **7.3.1 Positioning**

* **Charge Forward:** Close range aggressively. Best for brawlers with short-range weapons.

* **Hold Position (Mid-Range):** Maintain medium engagement range. Balanced option.

* **Stand Back (Max Range):** Stay at maximum weapon range. Best for bombardment and carriers.

### **7.3.2 Attack Priorities**

Attack roles (Brawlers, Bombard) can be set to prioritize targeting enemy fleet roles: focus carriers first, ignore scouts, concentrate on brawlers, etc.

### **7.3.3 Guardian Assignment**

Guardians are assigned to protect specific elements of your own fleet: screen the carriers, protect the flagship, guard supply ships.

## **7.4 Carrier Operations**

* **Fighters:** Crewed small craft launched from carriers. Harder to jam, better armed than drones. Destroyed fighters are lost permanently.

* **Drones:** Unmanned craft. Lighter, harder to hit, but easier to jam or destroy. Recoverable after combat if not destroyed. Consume parts/energy as expendables.

## **7.5 Morale in Combat**

* Fleet supply status directly affects morale (well-stocked \= high morale).

* Morale affects accuracy, willingness to hold formation, and retreat threshold.

* Ships with broken morale may flee independently, ignoring player orders.

* Ships may also charge forward recklessly if morale breaks in an aggressive direction.

## **7.6 Retreat**

* Retreat is possible but takes time — retreating ships must eat enemy fire without shooting back.

* Partial retreat is supported: leave brawlers or guardians as a rearguard while other roles disengage.

* Retreating ships move toward the nearest lane exit or, with free navigation tech, any escape vector (slower).

## **7.7 Combat Salvage**

Destroyed ships leave salvageable debris. If you have salvage ships present (or nearby), you can recover components and potentially partially rebuildable hulls from the battlefield. This applies to both your own losses and enemy wrecks, making post-battle salvage a meaningful consideration in fleet composition.

## **7.8 Station Combat**

* Stations have their own weapon slots, defenses, and garrison.

* A well-defended station should be able to engage approximately 3 equivalent-tier ships with moderate damage, and potentially hold against 4 with heavy losses.

* Stations are typically one size class larger than the top-tier player ships of the same era.

* Stations can be boarded and captured, raided to destroy specific components, or destroyed outright.

* Stations provide free visibility (they cannot hide) and can host garrison fleets for lane patrol.

## **7.9 Ground Combat**

Ground combat uses a similar system to space combat — armies with unit roles and dispositions rather than individual unit control. Orbital bombardment can soften planetary defenses before invasion but damages colony infrastructure. The player chooses bombardment intensity and duration. Conquered colonies can be kept and integrated into the empire.

# **8\. Visibility & Detection**

## **8.1 Core Mechanics**

Every ship, station, and fleet has two key detection attributes:

* **Visibility:** How detectable the object is. Larger ships, high-powered engines, and active scanners increase visibility.

* **Detection Range:** How far the object can detect others. Better sensors and active scanning increase range but also increase visibility.

## **8.2 Active vs. Passive Detection**

* **Passive Sensors:** Low range, but the scanning ship generates minimal visibility. Good for stealthy observation.

* **Active Sensors:** Much greater range, but the scanning ship becomes highly visible. Good for area denial and intimidation.

## **8.3 Scout Screen**

Fleets can assign scout-role ships to form a detection screen:

* The player sets the screen radius — how far from the fleet the scouts operate.

* Larger screen radius means better early warning but higher chance of scouts being detected.

* Scout quality (sensor power vs. own visibility) determines whether you detect the enemy before they detect your scouts.

* If scouts don’t care about being detected, they can use high-powered active scanners for maximum range.

## **8.4 Information Revealed by Detection**

Detection quality is a calculation of visibility vs. scan range vs. observation time:

* **Minimal Detection:** “Something is there.” Unidentified contact.

* **Basic Detection:** Approximate fleet size, general heading.

* **Detailed Detection:** Ship count by size class, fleet role composition.

* **Full Detection:** Exact ship designs, loadouts, supply status.

## **8.5 Silent Running**

Ships and fleets can enter silent running mode: engines at minimum, active sensors off, emissions suppressed. This dramatically reduces visibility but also eliminates detection range. Useful for ambushes, bypassing patrols, and covert operations.

## **8.6 Stealth Ships**

Small, purpose-built vessels with minimal visibility profiles. Used for special operations, deep reconnaissance, and bypassing enemy detection networks. Blue tech specializes in stealth capabilities.

## **8.7 Station Detection**

Stations provide permanent, free visibility in their system and nearby lanes. They cannot hide. Stations with upgraded sensors can detect activity in adjacent systems depending on range and technology.

# **9\. Logistics & Supply**

## **9.1 Supply Philosophy**

Fleets require continuous provisioning to restock ammunition, repair damage, and maintain operational readiness. The logistics system is the strategic backbone of military operations — a powerful fleet without supply is a floating wreck.

## **9.2 Supply Requirements**

Fleet supply consumption is driven by three categories:

### **9.2.1 Energy**

* Consumed by shields, energy weapons (lasers/beams), active sensors, and movement.

* Drawn from color-specific energy resources (simple energy for routine operations, advanced energy for high-tier systems).

* Longer combat \= more energy consumed.

* More efficient (but slower) engines are available as a tech tradeoff.

### **9.2.2 Parts**

* Consumed by armor repair, railgun ammunition, missile/drone production, and general maintenance.

* Drawn from color-specific parts resources (simple parts for basic maintenance, advanced parts for high-tier equipment repair).

* Green tech advantage: biological systems regenerate some parts naturally.

### **9.2.3 Food**

* Required for crew sustenance.

* Transported from colonies through the logistics network.

## **9.3 Weapon/Defense Logistics Profile**

| System | Energy Cost | Parts Cost | Notes |
| :---- | :---- | :---- | :---- |
| Shields | High | Low | Sustained energy drain, minimal physical wear |
| Energy Weapons | High | Low | Power-intensive, few moving parts |
| Armor | Very Low | High | Passive but needs physical repair materials |
| Railguns | Very Low | High | Kinetic projectiles need manufacturing |
| Missiles | Medium | Medium | Balanced consumption, expendable |
| Drones | Medium | Medium | Recoverable if not destroyed |
| Movement | Low–Medium | Very Low | Efficient engines reduce this further |
| Repair | Low | Medium–High | Green tech reduces parts cost |

## **9.4 Logistics Network**

### **9.4.1 Logistics Hubs**

Supply flows through a network of logistics hubs — colonies, stations, or outposts with logistics modules. Each hub can funnel a set amount of supply “points.” Supply is lost over distance: the further apart hubs are, the more waste occurs in transit.

### **9.4.2 Supply Ships**

Dedicated supply vessels can stock with a fleet, providing mobile resupply. This enables deep strikes into territory without established logistics infrastructure.

### **9.4.3 Scavenger Ships**

Scavenger ships attached to a fleet can recover supply from derelicts and battle debris. A resourceful commander can partially sustain operations through salvage alone.

### **9.4.4 Supply Line Warfare**

Cutting enemy supply lines is a valid and powerful strategy. Starving forward fleets of energy, parts, and food degrades their combat effectiveness, morale, and ability to repair. Controlling chokepoints has direct logistic consequences.

## **9.5 Morale & Supply**

* Well-stocked fleets have higher morale and better combat performance.

* Under-supplied fleets suffer morale penalties, increasing the risk of ships breaking formation or fleeing.

* Prolonged supply deprivation can render a fleet combat-ineffective without a shot being fired.

## **9.6 Color Tech & Logistics**

* **Gold (Golden Ascendancy):** More supply points per hub, less waste over distance, better supply ship efficiency.

* **Green (Verdant Synthesis):** Biological regeneration reduces parts consumption. Some systems self-repair.

* **Red (Crimson Forge):** Favors energy-heavy loadouts. Industrial efficiency reduces manufacturing costs.

* **Blue (Azure Lattice):** Optimization algorithms improve logistics routing. Automation reduces supply overhead.

* **Purple (Obsidian Covenant):** Exotic energy sources and unconventional supply methods. High ceiling, high cost.

## **9.7 Station Supply Benefits**

Fleets stationed at friendly stations receive reduced maintenance costs. Stations act as major logistics hubs and can stockpile resources for nearby operations.

# **10\. Colonies & Outposts**

## **10.1 Settlement Types**

### **10.1.1 Outposts**

Small work camps on most planetary bodies and POI. Limited but essential for resource extraction and forward positioning.

* Population cap: 3–5 pops.

* Cannot have most buildings — limited to mining, salvage operations, small defenses.

* No population growth or trade generation.

* Easy prey without external defenses (station or fleet garrison).

* Can upgrade to colony through terraforming technology (gradual process, increasing planet size each time).

* Choose what to exploit at each outpost — limited by manpower.

### **10.1.2 Colonies**

Full settlements on habitable planets. Rare and valuable. The backbone of an empire’s economy and population.

* Population cap varies by planet hospitability:

  * Small planet: 5–7 pops

  * Medium planet: 7–10 pops

  * Large planet: 12–15 pops

  * Prime planet: 15–25 pops

  * Exceptional (rare): up to 30 pops

* Support full building construction, population growth, production queues, and trade generation.

* Habitable planets are scarce overall, worse toward the core (depleted/war-torn), more pristine at the rim.

* A target of approximately 15 well-developed colonies mid-game.

### **10.1.3 Space Habitats**

Late-game technology or repaired precursor habitats. Allow colony-level settlement in systems without habitable planets.

## **10.2 Colony Management**

Colony management is designed for minimal micromanagement. Decisions are meaningful but infrequent.

### **10.2.1 Population**

* Population is tracked as a number per species type per colony.

* Multiple species can coexist in one colony.

* Population grows naturally (slowly unless resources are invested).

* Population can die from starvation, combat, plague. Depopulation is a real threat.

* Pops can be manually moved between colonies.

### **10.2.2 Pop Allocation**

Pops are allocated to broad work pools:

* **Production:** Manufacturing, construction, shipyard support.

* **Research:** Contributes to empire-wide research output.

* **Food:** Agriculture to feed the colony and generate surplus for logistics.

* **Mining:** Resource extraction from local deposits.

* **Expert Slots:** Special building-granted slots that produce 2x output or unique products. Buildings or tech unlock expert slots; the player assigns a pop to fill them.

### **10.2.3 Colony Priority**

Each colony can be set to a priority (e.g., research focus, production focus, growth focus) without a governor. This adjusts automatic allocation weighting.

### **10.2.4 Happiness**

A happiness/stability meter per colony. Affected by supply status, recent events, species relations, and enemy proximity. Low happiness reduces productivity and may trigger unrest.

## **10.3 Buildings**

* Single production queue per colony.

* Buildings require production points and components (basic components early, advanced components for high-tier buildings).

* Buildings provide: passive manufacturing, resource extraction bonuses, expert slots, population capacity, defensive emplacements, logistics hub capacity.

* Mines require a “free” (player-worked) resource deposit at the colony location.

* Not many buildings per colony — each decision matters.

## **10.4 Territory & Claims**

**There is no automatic territory system.** Having a colony or outpost in a system does not prevent other empires from operating there. If you want exclusive access to a resource, you must guard it militarily or negotiate agreements. Diplomatic claims can be placed on POI as a signal of intent, but these are not mechanically enforced — they rely on reputation and the threat of force. This is a core gameplay mechanic and must be clearly communicated in the tutorial.

## **10.5 Asteroid Mining**

Asteroid fields, especially common in the rim, support mining operations via outpost or dedicated mining ships. A viable alternative to planetary resource extraction.

# **11\. Stations**

## **11.1 Station Construction**

* Built by Builder ships at planets or points of interest. Cannot move once placed.

* Constructed at a starting size, with modules added incrementally (similar to Stellaris).

* Size upgrades require technology. Larger stations support more modules and stronger defenses.

## **11.2 Station Modules**

* **Shipyard:** Required for ship construction. Shipyard size limits the maximum ship class that can be built. Production assisted by nearby colony or outpost.

* **Defense:** Weapons, shields, armor, point defense. Stations fight in combat.

* **Logistics:** Supply hub capacity. Extends supply network.

* **Trade:** Enables and boosts passive trade goods flow.

* **Garrison:** Hosts patrol fleets that cover nearby lanes. Reduces maintenance for stationed fleets.

* **Sensors:** Provides free, always-on visibility. Upgradeable for extended detection range into adjacent systems.

## **11.3 Station Placement**

* Built around planets or at specific points of interest within a system.

* Can be built in unclaimed systems as forward operating bases.

* Strategic station placement at chokepoints, contested sites, and logistics corridors is a major gameplay element.

## **11.4 Precursor Stations**

Abandoned precursor stations can be found throughout the galaxy. They offer unique opportunities:

* **Claim & Repair:** Restore a precursor station to operational status. Expensive but grants access to systems far beyond current tech level.

* **Scavenge:** Strip the station for components and research data.

* **Dangers:** Precursor stations may be booby-trapped, infested, or defended by automated systems. Higher color affinity and closer tech level reduce trigger probability.

* **Tech Unlocks:** Repairing a precursor station can unlock technologies otherwise inaccessible at current research levels.

# **12\. Exploration & Salvage**

## **12.1 Exploration Flow**

* **Discovery:** Scouts enter a system and reveal POI. Basic scanning shows general site type.

* **Survey:** Dedicated survey reveals deeper information. Higher tech reveals specific systems and components. Low tech sees only “salvage detected.”

* **Exploitation:** Deploy salvage ships or establish outpost. Extract resources, components, research data.

## **12.2 Salvage Site Types**

| Site Type | Description | Typical Rewards |
| :---- | :---- | :---- |
| Minor Derelict | Small abandoned outpost or damaged ship | Basic components, minor research |
| Major Precursor Site | Large installation requiring extended excavation | Advanced components, significant research, unique tech |
| Precursor Intersection | Where multiple civilizations collaborated | Multi-color components, synergy tech research |
| Ship Graveyard | Cluster of derelict vessels | Parts, recoverable ships, combat salvage |
| Failed Salvager Wreck | Previous failed player/NPC attempt at rebuilding | Hybrid tech, cautionary data (procedurally generated from actual failed attempts) |
| Desperation Project | Late-stage precursor attempt to prevent extinction | Exotic tech, mystery clues, high danger |
| Debris Field | General battlefield remnants | Basic components, chance of special derelict discovery |

## **12.3 Excavation Mechanics**

* Excavation speed depends on: tech level gap to the site’s color, survey capacity deployed, and site size.

* Small, low-tech-gap sites can be processed in weeks of game time.

* Major sites with large tech gaps can take extended periods.

* Specialized salvage ships are required. Outposts can contribute but are limited by manpower.

* Players must choose which POI to prioritize — limited salvage capacity means tradeoffs.

## **12.4 Shared Exploitation**

Multiple empires can exploit the same site simultaneously. This is a core diplomatic mechanic:

* Different players can extract from different subsystems based on their tech level and specialization.

* Cooperative excavation is possible and sometimes beneficial.

* Sneaky exploitation — quietly extracting from a site another empire considers “theirs” — is a valid strategy.

* As resources deplete, cooperation gives way to competition.

## **12.5 Derelict Ships**

Derelict precursor vessels are found at specific POI, in ship graveyards, or occasionally at mid-points between systems along lanes.

* **Scan:** Determine ship type, condition, tech level, and color.

* **Salvage for Parts:** Break down for components. Safe, guaranteed return.

* **Use As-Is:** Operate with reduced efficiency. Devastating in early game, equivalent to high-tier tech in late game.

* **Jury-Rig:** Repurpose for a different role (mining ship as warship). Functional but suboptimal.

* **Repair:** Full restoration. Extremely expensive and time-consuming.

* **Replicate:** Theoretically possible but requires massive research investment across all installed subsystems. A long-term project.

* **Maintenance:** Precursor ships may require advanced components that are scarce in your supply chain, creating ongoing logistics challenges.

## **12.6 Derelict Hazards**

All derelict sites carry risk of triggering automated defenses, traps, or contamination. Risk factors:

* Higher color affinity reduces risk.

* Smaller tech level gap reduces risk.

* Survey quality provides warning of specific hazards.

* Some sites are more dangerous than others regardless of preparation.

## **12.7 Information Asymmetry in Exploration**

Low tech levels see only basic “salvage detected” at a site. Higher tech reveals specific systems, components, and values. This creates opportunities for bluffing (“this site is worthless, I’m just here for basic components”), information trading (selling scan data), and espionage (spying on what rivals are extracting).

## **12.8 Persistent Discovery**

Exploration remains relevant throughout the game. Early-game scans miss sites that require higher tech to detect. Hidden lanes reveal new systems. Technology unlocks access to deeper excavation layers. Guardians protecting high-value sites can only be defeated with advanced fleets. The galaxy should never feel “solved.”

# **13\. Economy & Trade**

## **13.1 Trade Goods & Passive Income**

Empires generate trade goods passively (increased by technology) and actively (converting production to trade goods). These goods flow automatically to all known systems — both yours and other empires’. Goods fetch higher prices where the tech gap between producer and consumer is larger. This passive flow generates currency through taxation.

## **13.2 Active Market**

Players can actively sell on the market:

* **Components:** Sell surplus basic or advanced components.

* **Resources:** Sell raw color resources.

* **Ships:** Sell complete vessels.

* **Pricing:** Set a fixed price, or start a timed auction with a minimum bid.

* In multiplayer, players can see and bid on listings from empires they have contact with.

## **13.3 Tech Renting**

A unique trade mechanic where technology access can be temporarily shared:

* **Rent Technology:** Grant another empire temporary ability to build ships using your subsystem at your efficiency level. The renter gains research progress toward the tech the more they build with it.

* **Sell Ships:** Sell completed vessels. Buyer gets no research progress unless they dismantle the ship (which the seller can detect via espionage).

* **Rent Ships:** Lend crewed vessels. No research transfer at all — your people, your tech. Ships return when the agreement ends.

## **13.4 Negotiable Rights**

Diplomatic agreements can include:

* Mining rights to specific systems or POI.

* Salvage rights to specific derelict sites.

* Passage rights through controlled chokepoints (including % toll for passage).

* Exclusive or shared exploitation agreements.

## **13.5 Trade Routes & Blockades**

* Trade flows automatically through lane connections based on tech infrastructure.

* Controlling a chokepoint allows you to blockade or tax trade passing through.

* Embargoes can cut specific empires off from your market.

* A black market exists for smuggling past blockades, giving trade-focused players additional options.

## **13.6 Currency System**

Each empire generates currency through trade taxation. The detailed currency/inflation system (including per-empire minting, exchange rates, and inflation mechanics) is reserved for future design iteration.

# **14\. Diplomacy**

## **14.1 First Contact**

Diplomatic communication requires physical proximity. You must reach another empire’s outpost, colony, or station, or encounter them at a shared POI (orbiting the same planet or point of interest), before diplomatic channels open.

## **14.2 Agreement Types**

* **Non-Aggression Pact:** Mutual agreement not to attack. Breaking incurs severe reputation penalty.

* **Alliance:** Military cooperation, shared vision (optional), mutual defense.

* **Trade Agreement:** Opens or enhances trade goods flow between empires.

* **Rights Agreements:** Mining, salvage, passage, and exploitation rights for specific locations.

* **Tech Rental:** Temporary technology access (see Economy section).

* **Team Alliances:** Formal team structures for multiplayer (2v2v2v2, cooperative vs. AI, etc.).

## **14.3 Reputation System**

Breaking agreements triggers a warning before confirmation. Broken agreements cause lasting reputation damage that:

* Makes future diplomacy harder with all empires (word spreads).

* May trigger defensive alliances against the oath-breaker.

* Takes significant time and good behavior to recover.

Reputation is a long-term strategic resource. Empires known for honoring deals have an easier time forming profitable agreements.

## **14.4 Information Trading**

Scan data, system maps, fleet positions, and derelict site information can all be traded as commodities. A Hauler or Blue-focused empire might profit as much from selling information as from direct resource extraction.

## **14.5 AI Diplomacy**

AI empires have preferred colors, origins, and personalities that drive their diplomatic behavior. A Red Warrior AI will be aggressive and territorial. A Gold Hauler AI will seek trade partnerships. A Blue Chronicler AI will hoard information and play empires against each other. AI difficulty levels adjust their strategic sophistication.

# **15\. Espionage**

## **15.1 Espionage Model**

Espionage is abstracted rather than agent-based. Players invest resources to gain intelligence or can acquire it passively through trade and cultural contact.

## **15.2 Intelligence Categories**

* **Map Intelligence:** Reveal explored systems, fleet positions, station locations.

* **Activity Intelligence:** What an empire is building, researching, and salvaging.

* **Technology Intelligence:** Steal research progress or identify tech levels.

* **Resource Intelligence:** Stockpile levels, production capacity, supply status.

* **Construction Intelligence:** What is being built where — ships, stations, buildings.

* **Salvage Intelligence:** What rivals are extracting from derelict sites.

## **15.3 Intelligence Methods**

* **Active Investment:** Spend currency to fund espionage operations targeting specific intelligence categories.

* **Passive Acquisition:** Trading relationships and cultural contact provide low-level intelligence naturally.

* **Scout Networks:** Physical observation through stealth ships and sensor stations.

## **15.4 Blue Tech Advantages**

Azure Lattice technology provides significant espionage advantages:

* Better spy effectiveness and counter-intelligence.

* Ability to frame other empires for espionage actions.

* Ability to plant false information in enemy intelligence.

* Superior electronic warfare and sensor capabilities.

# **16\. Admirals & Governors**

## **16.1 Acquisition**

Admirals and governors are hired. The available pool and maximum count are limited by empire population size, relative wealth, and technology level.

## **16.2 Admirals**

Assigned to fleets. Each admiral has fixed traits that affect:

* Fleet combat performance (accuracy, damage, survivability).

* Logistics efficiency (supply consumption, range).

* Scout range and detection effectiveness.

* Morale resilience and recovery.

* Fleet role slot modifications (e.g., extra carrier slot, swap brawler for bombard).

## **16.3 Governors**

Assigned to colonies. Each governor has fixed traits that provide passive bonuses:

* Production output, research output, mining efficiency.

* Population growth rate, happiness.

* Specialization bonuses (mining governor, research governor, etc.).

Colonies can set priority focus without a governor. Governors provide additional bonus on top.

# **17\. User Interface**

## **17.1 Primary Views**

### **17.1.1 Galaxy Map (Primary)**

The galaxy map is the primary view. It shows:

* Star systems as nodes connected by lane lines.

* Spiral arm structure with color tinting.

* Fleet icons at their current positions.

* System production/status summary icons.

* POI indicators within systems.

* Toggleable overlays for: resource flows, logistics networks, trade routes, territory claims.

### **17.1.2 System View (Secondary, Most Time Spent)**

Zooming into a system shows:

* All POI with type icons and status indicators.

* What is being extracted/built at each location.

* Which empires are present.

* Fleet positions within the system.

* Ideally, colony and POI management is handled via overlay panels within the system view, minimizing full-screen transitions.

### **17.1.3 Colony/Planet View**

Accessible from system view. Shows population allocation, building queue, resource output, happiness, and governor assignment. Designed to require infrequent visits.

## **17.2 Key Management Screens**

* **Fleet List:** Global list of all fleets with name, location, composition, orders, and “Take me to fleet” button.

* **Colony Overview:** All colonies compared side-by-side: population, output, priority, governor.

* **Research Tree:** Top-down view showing current tier, available subsystems. Click left/right to switch categories, dropdown to switch colors. Shows current tier prominently with next tier below.

* **Expertise & Statistics:** Empire-wide tracking of color expertise, resource extraction rates, fleet sizes and locations.

* **Diplomacy:** Active agreements, reputation status, market access.

* **Ship Designer:** Select chassis, fill big system slots from lists, allocate free capacity for extras.

## **17.3 Notifications & Automation**

* Configurable notification system with priority levels.

* Events, contacts, attacks, and completed constructions generate alerts.

* Auto-pause options for critical events (configurable).

* Fleet automation (patrol, explore) reduces manual commands.

* Fleet templates with auto-reinforce reduce replacement micro.

## **17.4 Speed Controls**

Real-time with pause (x0), normal (x1), fast (x2), faster (x4), and fastest (x8). In multiplayer, speed is determined by averaged vote among connected players.

# **18\. Multiplayer**

## **18.1 Player Configuration**

* Up to 8 human players.

* Up to 30 AI empires.

* Team modes supported: free-for-all, 2v2v2v2, cooperative vs. AI, custom teams.

## **18.2 Speed Control**

Players vote on desired game speed. The game runs at the average of all votes. This prevents one player from forcing uncomfortable pacing on others.

## **18.3 Shared Market**

Players can see and bid on market listings from empires they have established contact with. Forming trade cartels or exclusive agreements is allowed through diplomatic systems.

## **18.4 Disconnection**

Disconnected players are not replaced by AI. Their empire pauses autonomous actions until they reconnect. (Future consideration: AI takeover for extended disconnections.)

# **19\. Early Game Flow**

The following describes the intended player experience during the first 30 minutes of a standard game (at x1 speed):

## **19.1 Minutes 0–5: Setup & First Orders**

* Review starting colony, station, and ships.

* Set initial research priority.

* Send scout to explore connected systems.

* Begin initial production/building queue.

* Send salvager to nearest detected POI.

## **19.2 Minutes 5–15: Discovery Phase**

* Scout reveals 2–4 connected systems with various POI.

* First derelict sites and resource deposits identified.

* Player decides: which POI to prioritize, where to send the builder for outposts.

* First technology decisions based on what’s been found.

* 10–15 minute mark: first contact with another empire’s scouts, usually at a contested site.

## **19.3 Minutes 15–30: Expansion & Tension**

* First outposts established at priority POI.

* 10–15 subsystems and buildings become available, requiring prioritization.

* Resource extraction and early salvage begin generating components.

* First diplomatic decisions: cooperate at shared sites or posture defensively.

* 20–30 minute mark: need to invest in military to protect assets.

* First ship designs beyond starting templates.

## **19.4 Pacing Philosophy**

The early game should feel like a rush of discovery and difficult prioritization. There’s always more to explore than you have ships to explore with, more sites to exploit than salvagers to work them, and more tech to research than time allows. The arrival of other empires transforms the experience from pure exploration into a negotiation about who gets what — backed by the growing reality that military force may be needed.

# **20\. Events & Future Considerations**

## **20.1 Random Events**

The game will include random events beyond derelict discoveries: space storms, rogue AI activations, pirate factions, and precursor defense systems coming online. Events can create new POI mid-game, keeping the galaxy dynamic. Detailed event design is reserved for future iteration.

## **20.2 Victory Conditions**

The precursor mystery victory condition (requiring artifacts from all five colors) and other victory paths (domination, economic, etc.) are reserved for detailed design after core gameplay systems are finalized.

## **20.3 Tutorial**

A separate tutorial scenario will teach core mechanics. Critical tutorial elements include: the no-automatic-territory system (claims are not enforced), salvage workflow, fleet roles and dispositions, logistics basics, and the tech renting system.

## **20.4 Future Features**

* **Active Precursors:** Stellaris-inspired crisis events where precursor remnants become active threats.

* **Detailed Currency System:** Per-empire minting, inflation, exchange rates.

* **Economic Warfare:** Market manipulation, dumping, resource cornering.

* **Narrative Events & Quest Chains:** Story-driven discovery sequences.

* **Leader Progression:** Admiral and governor skill trees and experience.

* **AI Replacement:** AI takeover for disconnected multiplayer players.

## **20.5 Modding Support**

The game is designed to be data-driven for modding support. Community modders should be able to add new precursor colors, ship types, technologies, events, and galaxy configurations. Galaxy generation settings (arm count, density, resource richness, precursor site frequency) are configurable per game.