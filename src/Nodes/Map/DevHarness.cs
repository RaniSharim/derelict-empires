using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Stations;
using DerlictEmpires.Core.Tech;
using DerlictEmpires.Nodes.UI;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Self-contained debug surface — dev seeds, hostile-spawn shortcuts, screenshot/showcase/fullscreen.
/// Constructs entity data and hands it to MainScene.Register* for integration.
/// </summary>
public partial class DevHarness : Node
{
    private MainScene _main = null!;

    public void Configure(MainScene main) => _main = main;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

        bool handled = true;
        switch (key.Keycode)
        {
            case Key.B when key.ShiftPressed:
                if (key.CtrlPressed) SpawnHostileAndAttackAuto();
                else SpawnHostileAndAttack();
                break;
            case Key.F7:
                GrantShipSubsystems();
                break;
            case Key.F10:
                SaveScreenshot();
                break;
            case Key.F11:
                ToggleFontShowcase();
                break;
            case Key.F12:
                ToggleFullscreen();
                break;
            default:
                handled = false;
                break;
        }
        if (handled) GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// Marks Ship subsystems as available so the designer picker has content without waiting for real research.
    /// Red T1–T4 researched spans all 8 ship sub-types (ECM at T3, Support at T4); Blue T1 diplomacy-granted
    /// exercises the cross-source badge in the picker.
    /// </summary>
    public void GrantShipSubsystems()
    {
        var research = _main.PlayerResearchState;
        var registry = _main.TechRegistry;
        if (research == null || registry == null) return;

        int researched = 0, granted = 0;
        foreach (var sub in registry.Subsystems)
        {
            if (sub.Type != TechModuleType.Ship) continue;
            if (sub.Color == PrecursorColor.Red && sub.Tier <= 4)
            {
                research.ResearchedSubsystems.Add(sub.Id);
                researched++;
            }
            else if (sub.Color == PrecursorColor.Blue && sub.Tier == 1)
            {
                research.GrantFromDiplomacy(sub.Id);
                granted++;
            }
        }
        McpLog.Info($"[Dev] Ship grant — researched {researched}, diplomacy-granted {granted}");
    }

    /// <summary>Adds a starting colony at the player's home habitable POI plus a foreign station
    /// at the same POI so System View has a shared-POI scenario to render.</summary>
    public void SeedHomeColony()
    {
        var player = GameManager.Instance.Empires.FirstOrDefault(e => e.IsHuman);
        var galaxy = GameManager.Instance?.Galaxy;
        if (player == null || galaxy == null) return;
        var home = galaxy.GetSystem(player.HomeSystemId);
        if (home == null) return;
        var habitable = home.POIs.FirstOrDefault(p =>
            p.Type == POIType.HabitablePlanet || p.Type == POIType.BarrenPlanet);
        if (habitable == null) return;

        int nextColonyId = (GameManager.Instance.Colonies.Count > 0 ? GameManager.Instance.Colonies.Max(c => c.Id) : 0) + 1;
        var colony = new Colony
        {
            Id = nextColonyId,
            Name = $"{home.Name} Prime",
            OwnerEmpireId = player.Id,
            SystemId = home.Id,
            POIId = habitable.Id,
            PlanetSize = habitable.PlanetSize == PlanetSize.None ? PlanetSize.Medium : habitable.PlanetSize,
            Happiness = 70,
            Buildings = new List<string> { "food_farm", "mining_facility", "research_lab", "industrial_complex" },
        };
        // 2 Food + 1 Mining + 1 Production + 2 idle = 6 total, leaves 2 unassigned as reserve.
        colony.PopGroups.Add(new PopGroup { Count = 2, Allocation = WorkPool.Food });
        colony.PopGroups.Add(new PopGroup { Count = 1, Allocation = WorkPool.Mining });
        colony.PopGroups.Add(new PopGroup { Count = 1, Allocation = WorkPool.Production });
        colony.PopGroups.Add(new PopGroup { Count = 2, Allocation = WorkPool.Unassigned });

        var hab = BuildingData.FindById("hab_module");
        if (hab != null)
        {
            colony.Queue.Enqueue(new BuildingProducible(hab));
            colony.Queue.Entries[colony.Queue.Count - 1].Invested = hab.ProductionCost / 3;
        }

        if (!_main.RegisterColony(colony))
        {
            McpLog.Warn("[Dev] Colony seed skipped: settlement system not initialized.");
            return;
        }
        McpLog.Info($"[Dev] Seeded colony at home: {colony.Name} (system {home.Id}, poi {habitable.Id})");

        int foreignEmpireId = 999;
        var foreignStation = new Station
        {
            Id = 2000 + home.Id,
            Name = "Obsidian Watchpost",
            OwnerEmpireId = foreignEmpireId,
            SystemId = home.Id,
            POIId = habitable.Id,
            SizeTier = 2,
            BaseHp = 300,
            IsConstructed = true,
        };
        foreignStation.Modules.Add(new SensorModule());
        foreignStation.Modules.Add(new DefenseModule());

        var foreignMirror = new StationData
        {
            Id = foreignStation.Id,
            Name = foreignStation.Name,
            OwnerEmpireId = foreignStation.OwnerEmpireId,
            SystemId = foreignStation.SystemId,
            POIId = foreignStation.POIId,
            SizeTier = foreignStation.SizeTier,
            InstalledModules = new List<string> { "Sensors", "Defense" },
        };
        if (_main.RegisterStation(foreignStation, foreignMirror))
            McpLog.Info($"[Dev] Seeded foreign station at home POI: {foreignStation.Name}");
    }

    /// <summary>Spawns a hostile AI fleet at the player's home system. Bound to Shift+B.</summary>
    public void SpawnHostileAndAttack()
    {
        var player = _main.PlayerEmpire;
        if (player == null) return;

        var hostile = GameManager.Instance.Empires.FirstOrDefault(e => e.Id != player.Id && !e.IsHuman);
        if (hostile == null)
        {
            hostile = new EmpireData
            {
                Id = 999,
                Name = "Red Raiders",
                IsHuman = false,
                Affinity = PrecursorColor.Red,
                HomeSystemId = player.HomeSystemId,
            };
            GameManager.Instance.RegisterEmpire(hostile);
        }

        // 2 light hostile ships — weak enough that Scout Alpha gets a visible fight.
        int baseShipId = (GameManager.Instance.Ships.Count > 0 ? GameManager.Instance.Ships.Max(s => s.Id) : 0) + 1;
        int newFleetId = (_main.Fleets.Count > 0 ? _main.Fleets.Max(f => f.Id) : 0) + 1;
        var hostileFleet = new FleetData
        {
            Id = newFleetId,
            Name = "Raider Squadron",
            OwnerEmpireId = hostile.Id,
            CurrentSystemId = player.HomeSystemId,
            Speed = 10f,
        };
        var newShips = new List<ShipInstanceData>();
        for (int i = 0; i < 2; i++)
        {
            var ship = new ShipInstanceData
            {
                Id = baseShipId + i,
                Name = $"Raider {i + 1}",
                OwnerEmpireId = hostile.Id,
                SizeClass = ShipSizeClass.Fighter,
                Role = "Fighter",
                MaxHp = 40,
                CurrentHp = 40,
                FleetId = hostileFleet.Id,
            };
            newShips.Add(ship);
            hostileFleet.ShipIds.Add(ship.Id);
        }

        _main.RegisterFleet(hostileFleet, newShips, isPlayerFleet: false);

        var sysName = GameManager.Instance?.Galaxy?.GetSystem(hostileFleet.CurrentSystemId)?.Name;
        McpLog.Info($"[Combat-Dev] Spawned {hostileFleet.Name} at {sysName}");
    }

    /// <summary>Spawn hostile + immediately request combat. Bound to Ctrl+Shift+B.</summary>
    public void SpawnHostileAndAttackAuto()
    {
        SpawnHostileAndAttack();
        var player = _main.PlayerEmpire;
        if (player == null) return;
        var fleets = _main.Fleets;
        var hostile = fleets.LastOrDefault(f => f.OwnerEmpireId != player.Id);
        var friendly = fleets.FirstOrDefault(f =>
            f.OwnerEmpireId == player.Id && hostile != null && f.CurrentSystemId == hostile.CurrentSystemId);
        if (hostile != null && friendly != null)
            EventBus.Instance?.FireCombatStartRequested(friendly.Id, hostile.Id);
    }

    private void SaveScreenshot()
    {
        var img = GetViewport().GetTexture().GetImage();
        var stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var resPath = $"res://screenshots/shot_{stamp}.png";
        img.SavePng(resPath);
        McpLog.Info($"Saved screenshot: {ProjectSettings.GlobalizePath(resPath)}");
    }

    private void ToggleFontShowcase()
    {
        var existing = _main.GetNodeOrNull<CanvasLayer>("FontShowcaseLayer");
        if (existing != null)
        {
            existing.QueueFree();
        }
        else
        {
            var layer = new CanvasLayer { Name = "FontShowcaseLayer", Layer = 1000 };
            layer.AddChild(new FontShowcase { Name = "FontShowcase" });
            _main.AddChild(layer);
        }
    }

    private void ToggleFullscreen()
    {
        var current = DisplayServer.WindowGetMode();
        DisplayServer.WindowSetMode(
            current == DisplayServer.WindowMode.ExclusiveFullscreen
                ? DisplayServer.WindowMode.Maximized
                : DisplayServer.WindowMode.ExclusiveFullscreen);
    }
}
