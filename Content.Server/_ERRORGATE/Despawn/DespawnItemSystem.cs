using System.Linq;
using System.Threading;
using Content.Server._ERRORGATE.LootManager;
using Content.Server.Body.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization.Manager.Exceptions;

namespace Content.Server._ERRORGATE.Despawn;

public sealed class DespawnItemSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly LootManagerSystem _lootManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DespawnItemComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DespawnItemComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<DespawnItemComponent, EntParentChangedMessage>(OnParentChanged);
    }

    private void OnShutdown(EntityUid uid, DespawnItemComponent component, ComponentShutdown args)
    {
        StopDespawnTimer(uid, component);
        DecreaseLootManagerCount(uid);
    }

    public void OnMapInit(EntityUid uid, DespawnItemComponent component, MapInitEvent args)
    {
        UpdateDespawnTimer(uid, component);
    }

    public void OnParentChanged(EntityUid uid, DespawnItemComponent component, EntParentChangedMessage args)
    {
        if (args.OldParent == args.Transform.ParentUid)
            return;

        if (args.Transform.ParentUid == EntityUid.Invalid) // If the entity was despawned
            return;

        UpdateDespawnTimer(uid, component);
    }

    private void UpdateDespawnTimer(EntityUid uid, DespawnItemComponent component)
    {
        if (!component.Despawn)
        {
            StopDespawnTimer(uid, component);
            return;
        }

        var transform = Transform(uid);

        if (HasComp<MapGridComponent>(transform.ParentUid)) // item is attached to a grid
        {
            StartDespawnTimer(uid, component);
            return;
        }

        StopDespawnTimer(uid, component);
    }

    private void StartDespawnTimer(EntityUid uid, DespawnItemComponent component)
    {
        component.TokenSource?.Cancel();
        component.TokenSource = new CancellationTokenSource();

        uid.SpawnTimer(TimeSpan.FromSeconds(component.DespawnAfterSeconds), () => Despawn(uid), component.TokenSource.Token);
    }

    private void StopDespawnTimer(EntityUid uid, DespawnItemComponent component)
    {
        component.TokenSource?.Cancel();
    }

    private void Despawn(EntityUid uid)
    {
        _entityManager.DeleteEntity(uid);
    }

    private void DecreaseLootManagerCount(EntityUid uid)
    {
        if (!TryComp<MetaDataComponent>(uid, out var metadata) || metadata.EntityPrototype == null)
            return;

        if (!_lootManager.LootManager.Keys.Contains(metadata.EntityPrototype.ID))
            return;

        if (_lootManager.LootManager[metadata.EntityPrototype.ID].Count <= 0)
        {
            _lootManager.LootManager[metadata.EntityPrototype.ID].Count = 0;
            return;
        }

        _lootManager.LootManager[metadata.EntityPrototype.ID].Count -= 1;
    }
}
