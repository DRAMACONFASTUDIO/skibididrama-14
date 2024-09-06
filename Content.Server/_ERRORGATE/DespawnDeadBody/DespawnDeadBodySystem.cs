using System.Threading;
using Content.Server.Body.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._ERRORGATE.DespawnDeadBody;

public sealed class DespawnDeadBodySystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DespawnDeadBodyComponent, ComponentShutdown>(OnSpawnerShutdown);
        SubscribeLocalEvent<DespawnDeadBodyComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnSpawnerShutdown(EntityUid uid, DespawnDeadBodyComponent component, ComponentShutdown args)
    {
        component.TokenSource?.Cancel();
    }

    public void OnMobStateChanged(EntityUid uid, DespawnDeadBodyComponent component, MobStateChangedEvent args)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobstate))
            return;

        if (mobstate.CurrentState != MobState.Dead)
        {
            component.TokenSource?.Cancel(); // Stop despawn timer if we are not dead
        }

        if (mobstate.CurrentState == MobState.Dead)
        {
            StartDespawnTimer(uid, component);
        }
    }

    private void StartDespawnTimer(EntityUid uid, DespawnDeadBodyComponent component)
    {
        component.TokenSource?.Cancel();
        component.TokenSource = new CancellationTokenSource();

        Log.Debug($"Started a {component.DespawnAfterSeconds} seconds respawn timer for entity {uid}");
        uid.SpawnTimer(TimeSpan.FromSeconds(component.DespawnAfterSeconds), () => OnTimerFired(uid, component), component.TokenSource.Token);
    }

    private void OnTimerFired(EntityUid uid, DespawnDeadBodyComponent component)
    {
        if (component.GibInsteadOfDeleting)
        {
            _bodySystem.GibBody(uid);
            Log.Debug($"Despawned dead body {uid} by gibbing");
            return;
        }

        _entityManager.DeleteEntity(uid);
        Log.Debug($"Despawned dead body {uid} by deleting");
    }
}
