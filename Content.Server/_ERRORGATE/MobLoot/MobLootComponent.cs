using Robust.Shared.Serialization;

namespace Content.Server._ERRORGATE.MobLoot;

[RegisterComponent]
public sealed partial class MobLootComponent : Component, ISerializationHooks
{
    // Loot Rarity used by the loot manager to determine possible loot
    [DataField]
    public int Rarity = 1;

    // Loot Location used by the loot manager to determine possible loot
    [DataField]
    public string Location = "Living";

    [DataField]
    public float DropChance = 1.0f;

    [DataField]
    public bool GibOnDrop = false;

    public bool HasDropped = false;

    // A list of entities this mob can drop
    public Dictionary<string, int> LootTable;
}
