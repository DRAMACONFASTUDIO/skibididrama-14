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
    public bool SpawnOnInit = true;

    [DataField]
    public bool TrySpawnOnceAndDelete = false;

    [DataField]
    public float Chance = 1.0f;

    [DataField]
    public int IntervalSeconds = 60;

    [DataField]
    public int MinimumEntitiesSpawned = 1;

    public int MaximumEntitiesSpawned = 1;

    public CancellationTokenSource? TokenSource; // Used by the timer

    // A list of entities this spawner can spawn, used in collision checks if StackMultiple = false.
    public List<string> CollisionBlackList;

    void ISerializationHooks.AfterDeserialization()
    {
        if (MinimumEntitiesSpawned > MaximumEntitiesSpawned)
            throw new ArgumentException("MaximumEntitiesSpawned can't be lower than MinimumEntitiesSpawned!");
    }
}
