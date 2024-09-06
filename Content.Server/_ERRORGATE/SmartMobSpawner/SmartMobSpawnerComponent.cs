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
    /// Respawn time randomly generated between Min and Max,
    /// starts counting from entity deletion, dont forget about the Despawn timer.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int RespawnTime;
    [DataField]
    public int MinRespawnTimeSeconds = 60;
    [DataField]
    public int MaxRespawnTimeSeconds = 120;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid SpawnedMob = EntityUid.Invalid;

    public CancellationTokenSource? TokenSource;

    void ISerializationHooks.AfterDeserialization()
    {
        if (MinRespawnTimeSeconds > MaxRespawnTimeSeconds)
            throw new ArgumentException("MaxRespawnTimeSeconds can't be lower than MinRespawnTimeSeconds!");
    }
}
