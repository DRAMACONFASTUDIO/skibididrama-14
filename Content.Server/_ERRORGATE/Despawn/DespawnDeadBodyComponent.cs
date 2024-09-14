using System.Threading;
using Robust.Shared.Serialization;

namespace Content.Server._ERRORGATE.Despawn;

/// <summary>
/// Despawns entities that are in the dead mobstate for DespawnAfterSeconds.
/// </summary>
[RegisterComponent]
public sealed partial class DespawnDeadBodyComponent : Component, ISerializationHooks
{
    [DataField]
    public int DespawnAfterSeconds = 300; // 5 minutes

    [DataField]
    public bool GibInsteadOfDeleting = true; // Gibbing drops all items so its preferred for players

    public CancellationTokenSource? TokenSource;
}
