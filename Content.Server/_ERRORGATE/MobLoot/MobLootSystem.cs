using System.Linq;
using Content.Server.Body.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Random;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._ERRORGATE.MobLoot;

public sealed class MobLootSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobLootComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public void OnMobStateChanged(EntityUid uid, MobLootComponent lootComponent, MobStateChangedEvent args)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobstate))
            return;

        if (mobstate.CurrentState == MobState.Dead && !lootComponent.HasDropped)
        {
            if (TryPickLoot(lootComponent.LootTablePrototype, out var lootprototype))
            {
                var lootdrop = SpawnNextToOrDrop(lootprototype, uid);

                var landEvent = new LandEvent(lootdrop, true); // Play dropping sound
                RaiseLocalEvent(lootdrop, ref landEvent);
            }

            lootComponent.HasDropped = true; // So it only attempts to drop once

            if (lootComponent.GibOnDrop)
            {
                _bodySystem.GibBody(uid);
            }
        }
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
}
