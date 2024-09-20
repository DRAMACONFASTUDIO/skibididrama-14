using System.Threading;
using Robust.Shared.Serialization;

namespace Content.Server._ERRORGATE.LootManager;


[RegisterComponent]
public sealed partial class LootSpawnerComponent : Component, ISerializationHooks
{
    // Loot Rarity used by the loot manager to determine possible loot
    [DataField]
    public int Rarity = 1;

    // Loot Location used by the loot manager to determine possible loot
    [DataField]
    public string Location = "Living";

    // Try to spawn that many entities per hour (3600 seconds)
    [DataField]
    public float SpawnRate = 3f;

    // The interval between the attempts to spawn an entity. Does not affect the spawn probability.
    [DataField]
    public int IntervalSeconds = 600;

    public CancellationTokenSource? TokenSource; // Used by the timer

    // A list of entities this spawner can spawn, used in collision checks if StackMultiple = false.
    public Dictionary<string, int> LootTable;

}
