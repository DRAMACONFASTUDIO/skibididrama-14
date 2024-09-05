using System.Linq;
using System.Threading;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.Spawners.Components;
using Content.Shared.Maps;
using Content.Shared.Random;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Spawners.EntitySystems;

public sealed class LootSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

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
        uid.SpawnRepeatingTimer(TimeSpan.FromSeconds(component.IntervalSeconds), () => OnTimerFired(uid, component), component.TokenSource.Token);
    }

    private void OnTimerFired(EntityUid uid, LootSpawnerComponent component)
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

            if (lootDict.Keys.Contains(entityPrototype.ID))
                return false;
        }

        return true;
    }
}
