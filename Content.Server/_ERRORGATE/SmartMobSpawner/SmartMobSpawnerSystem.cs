using System.Threading;
using Robust.Shared.Random;

namespace Content.Server._ERRORGATE.SmartMobSpawner;

public sealed class SmartMobSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SmartMobSpawnerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SmartMobSpawnerComponent, ComponentShutdown>(OnSpawnerShutdown);
        SubscribeLocalEvent<SmartMobSpawnerSpawnedComponent, ComponentShutdown>(OnMobCompShutdown);
    }

    private void OnMapInit(EntityUid uid, SmartMobSpawnerComponent component, MapInitEvent args)
    {
        if (component.SpawnMobOnInit)
            SpawnMob(uid, component);
        else
            StartRespawnTimer(uid);
    }

    private void OnSpawnerShutdown(EntityUid uid, SmartMobSpawnerComponent component, ComponentShutdown args)
    {
        component.TokenSource?.Cancel();
    }

    private void OnMobCompShutdown(EntityUid uid, SmartMobSpawnerSpawnedComponent component, ComponentShutdown args)
    {
        var spawner = component.SpawnedBy;

        if (Deleted(spawner))
        {
            Log.Error($"Spawned mob {uid} was removed but spawner {spawner} no longer exists!");
            return;
        }

        StartRespawnTimer(spawner);
    }

    private void StartRespawnTimer(EntityUid uid)
    {
        if (!TryComp<SmartMobSpawnerComponent>(uid, out var component))
        {
            Log.Error($"Tried to start a timer on an entity {uid} that has no SmartMobSpawnerComponent!");
            return;
        }

        component.TokenSource?.Cancel();
        component.TokenSource = new CancellationTokenSource();
        component.RespawnTime = (int) (3600f / component.SpawnRate); //_random.Next(component.MinRespawnTimeSeconds, component.MaxRespawnTimeSeconds);

        Log.Debug($"Mob spawner {uid} started a {component.RespawnTime} seconds respawn timer for a {component.MobPrototype}");
        uid.SpawnTimer(TimeSpan.FromSeconds(component.RespawnTime), () => OnTimerFired(uid, component), component.TokenSource.Token);

    }

    private void OnTimerFired(EntityUid uid, SmartMobSpawnerComponent component)
    {
        if (Exists(component.SpawnedMob))
        {
            Log.Debug($"Spawner {uid} is spawning {component.MobPrototype} while {component.SpawnedMob} still exists.");

            // Can only happen if the SmartMobSpawnerSpawnedComponent
            // Was removed from the entity without deleting the entity itself.
            // In which case it was probably intended to allow spawning a new mob.
            // Uncomment return if it ever wasn't.

            //return;
        }

        SpawnMob(uid, component);
    }

    private void SpawnMob(EntityUid uid, SmartMobSpawnerComponent component)
    {
        var coordinates = Transform(uid).Coordinates;
        component.SpawnedMob = SpawnAtPosition(component.MobPrototype, coordinates);
        EnsureComp<SmartMobSpawnerSpawnedComponent>(component.SpawnedMob, out var spawnedMobComp);
        spawnedMobComp.SpawnedBy = uid;
    }
}
