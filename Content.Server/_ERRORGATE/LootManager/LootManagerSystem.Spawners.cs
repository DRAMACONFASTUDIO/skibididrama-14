using System.Linq;
using System.Threading;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._ERRORGATE.LootManager;

public sealed class LootManagerSystemSpawners: EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly LootManagerSystem _lootManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LootSpawnerComponent, ComponentInit>(OnSpawnerInit);
        SubscribeLocalEvent<LootSpawnerComponent, ComponentShutdown>(OnSpawnerShutdown);
        SubscribeLocalEvent<LootSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, LootSpawnerComponent component, MapInitEvent args)
    {
        TrySpawnLoot(uid, component);
    }

    private void OnSpawnerInit(EntityUid uid, LootSpawnerComponent component, ComponentInit args)
    {
        component.TokenSource?.Cancel();
        component.TokenSource = new CancellationTokenSource();

        component.LootTable = new();

        // Randomize the interval a bit
        var duration = _random.NextFloat(component.IntervalSeconds * 0.9f, component.IntervalSeconds * 1.1f);

        uid.SpawnRepeatingTimer(TimeSpan.FromSeconds(duration), () => TrySpawnLoot(uid, component), component.TokenSource.Token);
    }

    private void TrySpawnLoot(EntityUid uid, LootSpawnerComponent component)
    {
        // Calculate spawn chance based on interval and spawn rate per hour (3600 seconds)
        var chance = Math.Min(1, component.SpawnRate * component.IntervalSeconds / 3600f);

        if (!_random.Prob(chance))
            return;

        UpdateLootTable(component);

        if (component.LootTable.Values.Sum() <= 0)
        {
            Log.Debug($"Loot table for {uid} is empty");
            return;
        }

        var coordinates = Transform(uid).Coordinates;

        if (!CheckSpawnPointCondition(coordinates, component))
            return;

        if (!TryPickLoot(component, out var entityProto) || entityProto == null)
        {
            Log.Error($"Null prototype in a loottable");
            return;
        }

        _lootManager.LootManager[entityProto].Count += 1;

        var childEntries = _lootManager.LootManager[entityProto].ChildEntries;

        if (childEntries.Count > 0)
        {
            foreach (var child in childEntries)
            {
                if (_lootManager.LootManager.TryGetValue(child, out var parameters))
                    parameters.Count += 1;
            }
        }

        SpawnAtPosition(entityProto, coordinates);
        Spawn("PuddleSparkle", coordinates); // Cool effect

    }

    private void OnSpawnerShutdown(EntityUid uid, LootSpawnerComponent component, ComponentShutdown args)
    {
        if (!Deleted(uid))
            component.TokenSource?.Cancel();
    }

    private bool TryPickLoot(LootSpawnerComponent component, out string? proto)
    {
        var options = component.LootTable;

        string? selectedPrototype = null;
        var sum = options.Values.Sum();

        while (options.Count > 0)
        {
            var accumulated = 0f;
            var rand = _random.NextFloat(sum);
            foreach (var (key, weight) in options)
            {
                accumulated += weight;
                if (accumulated < rand)
                    continue;

                selectedPrototype = key;
                break;
            }

            if (selectedPrototype is not null)
            {
                proto = selectedPrototype;
                return true;
            }
        }

        proto = null;
        return false;
    }

    private bool CheckSpawnPointCondition(EntityCoordinates coordinates, LootSpawnerComponent component)
    {
        var tileRef = coordinates.GetTileRef();

        if (tileRef == null)
        {
            return false;
        }

        var xform = Transform(component.Owner);

        if (xform.GridUid is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        var spawnPos = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        var coords = _map.ToCenterCoordinates(grid, spawnPos);

        var intersecting = _entityLookup.GetEntitiesIntersecting(coords, LookupFlags.All);

        foreach (var intersect in intersecting)
        {

            var entityPrototype = MetaData(intersect).EntityPrototype;
            if (entityPrototype == null)
                continue;

            if (component.LootTable.Keys.Contains(entityPrototype.ID))
                return false;
        }

        return true;
    }

    private void UpdateLootTable(LootSpawnerComponent component)
    {
        foreach (var entry in _lootManager.LootManager)
        {
            if (entry.Value.RarityTiers.Contains(component.Rarity) &&
                entry.Value.Locations.Contains(component.Location))
            {
                component.LootTable[entry.Key] = Math.Max(0, entry.Value.MaxCount - entry.Value.Count);
            }
        }
    }
}
