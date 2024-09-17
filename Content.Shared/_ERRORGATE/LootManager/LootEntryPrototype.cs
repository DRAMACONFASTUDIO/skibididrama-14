using Robust.Shared.Prototypes;

namespace Content.Shared._ERRORGATE.LootManager;

/// <summary>
/// A loot entry
/// </summary>
[Prototype("lootEntry")]
public sealed class LootEntryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<int> RarityTiers = [];

    [DataField]
    public List<string> Locations = [];

    [DataField]
    public List<string> ChildEntries = [];
}

