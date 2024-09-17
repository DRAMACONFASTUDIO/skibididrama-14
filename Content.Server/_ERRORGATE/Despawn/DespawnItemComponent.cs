using System.Threading;
using Robust.Shared.Serialization;

namespace Content.Server._ERRORGATE.Despawn;

/// <summary>
/// Despawns entities that are parented to the grid for DespawnAfterSeconds.
/// </summary>
[RegisterComponent]
public sealed partial class DespawnItemComponent : Component, ISerializationHooks
{
    [DataField]
    public int DespawnAfterSeconds = 1200; // 20 minutes

    [DataField]
    public bool Despawn = true;

    public CancellationTokenSource? TokenSource;
}
