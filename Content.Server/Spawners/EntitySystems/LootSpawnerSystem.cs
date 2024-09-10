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
    }

    private void OnSpawnerInit(EntityUid uid, LootSpawnerComponent component, ComponentInit args)
    {
        component.TokenSource?.Cancel();
        component.TokenSource = new CancellationTokenSource();

        if (component.SpawnOnInit)
        {
            TrySpawnLoot(uid, component);

            if (component.TrySpawnOnceAndDelete)
            {
                _entityManager.QueueDeleteEntity(uid);
                return;
            }
        }

        uid.SpawnRepeatingTimer(TimeSpan.FromSeconds(component.IntervalSeconds), () => TrySpawnLoot(uid, component), component.TokenSource.Token);
    }

    private void TrySpawnLoot(EntityUid uid, LootSpawnerComponent component)
    {
        if (!_random.Prob(component.Chance))
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

            if (!CheckSpawnPointCondition(coordinates, component, loottable))
                continue;

            SpawnAtPosition(entityProto, coordinates);
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

    private bool CheckSpawnPointCondition(EntityCoordinates coordinates, LootSpawnerComponent lootspawner, ProtoId<WeightedRandomPrototype> loottable)
    {
        if (lootspawner.StackMultiple)
            return true;

        var tileRef = coordinates.GetTileRef();

        if (tileRef == null)
        {
            return false;
        }

        var lootDict = _prototypeManager.Index(loottable).Weights.ShallowClone();
        var collisionblacklist = new List<string>();
        var debugstring = string.Empty;

        //Cycle through every entity in the loot table, and if it is a spawner,
        //Add entities from its loottable to the blacklist to check for collision
        foreach (var potentialspawner in lootDict)
        {
            if (!_prototypeManager.TryIndex(potentialspawner.Key, out var proto))
                continue;

            var loot = _serializationManager.CreateCopy(proto, notNullableOverride: true);

            // If its not a spawner, add its prototype and skip
            if (!loot.TryGetComponent<LootSpawnerComponent>(out var lootSpawnerComponent))
            {
                collisionblacklist.Add(potentialspawner.Key);
                continue;
            }

            var lootTable = (ProtoId<WeightedRandomPrototype>) lootSpawnerComponent.LootTablePrototype;

            var options = _prototypeManager.Index(lootTable).Weights.ShallowClone();

            foreach (var option in options.Keys)
            {
                collisionblacklist.Add(option);
                debugstring += option + " ";
            }
        }

        Log.Debug($"{lootspawner.Owner} assembled blacklist: {debugstring}");

        var xform = Transform(lootspawner.Owner);

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

            if (collisionblacklist.Contains(entityPrototype.ID))
                return false;
        }

        return true;
    }
}
