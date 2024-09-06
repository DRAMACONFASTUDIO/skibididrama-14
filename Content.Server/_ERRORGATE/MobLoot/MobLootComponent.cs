using Robust.Shared.Serialization;

namespace Content.Server._ERRORGATE.MobLoot;

[RegisterComponent]
public sealed partial class MobLootComponent : Component, ISerializationHooks
{
    [DataField(required: true)]
    public string LootTablePrototype = string.Empty;

    [DataField]
    public float DropChance = 1.0f;

    [DataField]
    public bool GibOnDrop = false;

    public bool HasDropped = false;
}
