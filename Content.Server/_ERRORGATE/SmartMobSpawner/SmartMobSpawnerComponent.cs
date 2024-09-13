using System.Threading;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server._ERRORGATE.SmartMobSpawner;

/// <summary>
/// Spawns a MobPrototype entity and stores its UID.
/// Starts a respawn timer once the entity was deleted.
/// </summary>
[RegisterComponent]
public sealed partial class SmartMobSpawnerComponent : Component, ISerializationHooks
{
    [DataField(required: true)]
    public EntProtoId MobPrototype = string.Empty;

    [DataField]
    public bool SpawnMobOnInit = true; // If false, will start a timer instead.

    /// <summary>
    /// Used to calculate respawn timers
    /// Will try to spawn that many mobs per hour (3600 seconds)
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int RespawnTime;

    [DataField]
    public float SpawnRate = 1.0f;



    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid SpawnedMob = EntityUid.Invalid;

    public CancellationTokenSource? TokenSource;

}
