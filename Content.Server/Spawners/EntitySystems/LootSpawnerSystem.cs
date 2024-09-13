using System.Linq;
using System.Threading;
using Content.Server.Construction.Completions;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.Spawners.Components;
using Content.Shared.Maps;
using Content.Shared.Random;
using Content.Shared.SimpleStation14.Clothing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Server.Spawners.EntitySystems;

public sealed class LootSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LootSpawnerComponent, ComponentInit>(OnSpawnerInit);
        SubscribeLocalEvent<LootSpawnerComponent, ComponentShutdown>(OnSpawnerShutdown);
        SubscribeLocalEvent<LootSpawnerComponent, MapInitEvent>(OnMapInit);

    }

    private void OnMapInit(EntityUid uid, LootSpawnerComponent component, MapInitEvent args)
    {
        if (component.SpawnOnInit)
        {
            TrySpawnLoot(uid, component);
        }
    }

    private void OnSpawnerInit(EntityUid uid, LootSpawnerComponent component, ComponentInit args)
    {
        component.TokenSource?.Cancel();
        component.TokenSource = new CancellationTokenSource();

        component.CollisionBlackList = new List<string>();

        if (!AssembleBlacklist(component, component.CollisionBlackList)) //if false it means we entered an endless loop
        {
            _entityManager.QueueDeleteEntity(uid);
            return;
        }

        uid.SpawnRepeatingTimer(TimeSpan.FromSeconds(component.IntervalSeconds), () => TrySpawnLoot(uid, component), component.TokenSource.Token);
    }

    private void TrySpawnLoot(EntityUid uid, LootSpawnerComponent component)
    {
        // Calculate spawn chance based on interval and spawn rate per hour (3600 seconds)
        var chance = Math.Min(1, component.SpawnRate * component.IntervalSeconds / 3600f);

        if (!_random.Prob(chance) && !component.Guaranteed)
            return;

        var loottable = component.LootTablePrototype;

        var number = _random.Next(component.MinimumEntitiesSpawned, component.MaximumEntitiesSpawned);
        var coordinates = Transform(uid).Coordinates;

        for (var i = 0; i < number; i++)
        {
            if (!TryPickLoot(loottable, out var entityProto))
            {
                Log.Error($"Null prototype in a loottable: {loottable}");
                continue;
            }

            if (!CheckSpawnPointCondition(coordinates, component))
                continue;

            SpawnAtPosition(entityProto, coordinates);
            Spawn("PuddleSparkle", coordinates); // Cool effect
        }

        if (component.TrySpawnOnceAndDelete)
        {
            _entityManager.QueueDeleteEntity(uid);
        }
    }

    private void OnSpawnerShutdown(EntityUid uid, LootSpawnerComponent component, ComponentShutdown args)
    {
        if (!Deleted(uid))
            component.TokenSource?.Cancel();
    }

    private bool TryPickLoot(ProtoId<WeightedRandomPrototype> loottable, out string? proto)
    {
        var options = _prototypeManager.Index(loottable).Weights.ShallowClone();

        EntityPrototype? selectedPrototype = null;
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

                if (!_prototypeManager.TryIndex(key, out selectedPrototype))
                    Log.Error($"Invalid prototype {selectedPrototype} in a loottable: {loottable}");

                options.Remove(key);
                sum -= weight;
                break;
            }

            if (selectedPrototype is not null)
            {
                proto = selectedPrototype.ID;
                return true;
            }
        }

        proto = null;
        return false;
    }

    private bool CheckSpawnPointCondition(EntityCoordinates coordinates, LootSpawnerComponent component)
    {
        if (component.StackMultiple)
            return true;

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

            if (component.CollisionBlackList.Contains(entityPrototype.ID))
                return false;
        }

        return true;
    }

    //Recursive function
    //Cycles through every entity in the loot table, and that entity's loot table, if its a spawner
    //Adds every entity prototype to the blacklist for later use in checking spawn conditions
    private bool AssembleBlacklist(LootSpawnerComponent component, List<string> blacklist)
    {
        var loottable = (ProtoId<WeightedRandomPrototype>) component.LootTablePrototype;

        var lootDict = _prototypeManager.Index(loottable).Weights.ShallowClone();

        foreach (var potentialspawner in lootDict)
        {
            if (!_prototypeManager.TryIndex(potentialspawner.Key, out var proto))
                continue;

            var loot = _serializationManager.CreateCopy(proto, notNullableOverride: true);

            // If its not a spawner, add its prototype and skip
            if (!loot.TryGetComponent<LootSpawnerComponent>(out var lootSpawnerComponent))
            {
                blacklist.Add(potentialspawner.Key);
                continue;
            }

            if (component.LootTablePrototype == lootSpawnerComponent.LootTablePrototype)
            {
                Log.Error($"{component.Owner} AssembleBlacklist tried to enter an endless loop");
                return false;
            }

            if (!AssembleBlacklist(lootSpawnerComponent, blacklist))
                return false;
        }

        return true;
    }
}
