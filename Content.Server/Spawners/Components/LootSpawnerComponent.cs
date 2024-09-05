using System.Threading;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Spawners.Components;


[RegisterComponent]
public sealed partial class LootSpawnerComponent : Component, ISerializationHooks
{

    [DataField(required: true)]
    public string LootTablePrototype = string.Empty;

    [DataField]
    public bool StackMultiple = false;

    [DataField]
    public float Chance = 1.0f;

    [DataField]
    public int IntervalSeconds = 60;

    [DataField]
    public int MinimumEntitiesSpawned = 1;

    public int MaximumEntitiesSpawned = 1;

    public CancellationTokenSource? TokenSource;

    void ISerializationHooks.AfterDeserialization()
    {
        if (MinimumEntitiesSpawned > MaximumEntitiesSpawned)
            throw new ArgumentException("MaximumEntitiesSpawned can't be lower than MinimumEntitiesSpawned!");
    }
}
