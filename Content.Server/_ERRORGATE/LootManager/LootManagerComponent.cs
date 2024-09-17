using Robust.Shared.Serialization;

namespace Content.Server._ERRORGATE.LootManager;

[RegisterComponent]
public sealed partial class LootManagerComponent : Component, ISerializationHooks
{
    [DataField(required: true)]
    public string GlobalLootTablePrototype = string.Empty;

    public Dictionary<string, int> Entries { get; private set; } = new();
}
