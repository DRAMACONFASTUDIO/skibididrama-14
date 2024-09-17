using Content.Shared._ERRORGATE.LootManager;
using Robust.Shared.Prototypes;

namespace Content.Server._ERRORGATE.LootManager;

public sealed class LootManagerEntry
{
    public int Count;
    public int MaxCount;
    public List<int> RarityTiers;
    public List<string> Locations;
    public List<string> ChildEntries;

    public LootManagerEntry(int count, int maxCount, List<int> rarityTiers, List<string> locations, List<string> childEntries)
    {
        Count = count;
        MaxCount = maxCount;
        RarityTiers = rarityTiers;
        Locations = locations;
        ChildEntries = childEntries;
    }
}

public sealed class LootManagerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public Dictionary<string, LootManagerEntry> LootManager = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LootManagerComponent, ComponentInit>(OnManagerInit);
    }

    private void OnManagerInit(EntityUid uid, LootManagerComponent component, ComponentInit args)
    {
        LootManager = new();

        // Get all possible spawns

        if (!_prototypeManager.TryIndex<GlobalLootTablePrototype>(component.GlobalLootTablePrototype, out var proto))
        {
            Log.Error("Could not find a global loot table");
            return;
        }

        foreach (var entry in proto.Entries)
        {
            if (!_prototypeManager.TryIndex<LootEntryPrototype>(entry.Key, out var loot))
            {
                Log.Error($"Could not find a loot entry prototype for {entry.Key}");
                continue;
            }

            var parameters = new LootManagerEntry(0, entry.Value, loot.RarityTiers, loot.Locations, loot.ChildEntries);

            LootManager.Add(entry.Key, parameters);
        }
    }
}
