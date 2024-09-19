// © 2024 vanx discord@vanxxxx Vaaankas@mail.ru
// Licensed under GNU Affero General Public License version 3.0

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared._ERRORGATE.LootManager;

/// <summary>
/// A global loot table
/// </summary>
[Prototype("globalLootTable")]
public sealed class GlobalLootTablePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("entries", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<int, EntityPrototype>))]
    public Dictionary<string, int> Entries { get; private set; } = new();
}

