using System.Linq;
using Content.Server._ERRORGATE.LootManager;
using Content.Server.Body.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Random;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Utility;

namespace Content.Server._ERRORGATE.MobLoot;

public sealed class MobLootSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly LootManagerSystem _lootManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobLootComponent, ComponentInit>(OnMobInit);
        SubscribeLocalEvent<MobLootComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobInit(EntityUid uid, MobLootComponent component, ComponentInit args)
    {
        component.LootTable = new();
    }

    public void OnMobStateChanged(EntityUid uid, MobLootComponent lootComponent, MobStateChangedEvent args)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobstate))
            return;

        if (lootComponent.HasDropped)
            return;

        if (mobstate.CurrentState != MobState.Dead)
            return;

        TrySpawnLoot(uid, lootComponent);

        lootComponent.HasDropped = true; // So it only attempts to drop once

        if (lootComponent.GibOnDrop)
            _bodySystem.GibBody(uid);
    }

    private void TrySpawnLoot(EntityUid uid, MobLootComponent component)
    {
        if (!_random.Prob(component.DropChance))
            return;

        UpdateLootTable(component);

        if (component.LootTable.Values.Sum() <= 0)
        {
            Log.Debug($"Loot table for {uid} is empty");
            return;
        }

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

        var lootdrop = SpawnNextToOrDrop(entityProto, uid);

        var landEvent = new LandEvent(lootdrop, true); // Make it fall
        RaiseLocalEvent(lootdrop, ref landEvent);

    }

    private bool TryPickLoot(MobLootComponent component, out string? proto)
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

    private void UpdateLootTable(MobLootComponent component)
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
