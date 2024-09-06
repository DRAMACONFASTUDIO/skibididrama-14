using System.Threading;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Spawners.Components;

/// <summary>
/// Added on entites spawned by SmartMobSpawner.
/// Stores the UID of the spawner so the spawner
/// can be called when the entity is deleted.
/// </summary>
[RegisterComponent]
public sealed partial class SmartMobSpawnerSpawnedComponent : Component, ISerializationHooks
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid SpawnedBy = EntityUid.Invalid;
}
