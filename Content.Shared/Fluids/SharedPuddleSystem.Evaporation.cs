using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    [ValidatePrototypeId<ReagentPrototype>]
    private const string Water = "Water";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string Blood = "Blood";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string InsectBlood = "InsectBlood";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string Vomit = "Vomit";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string CopperBlood = "CopperBlood";

    public static readonly string[] EvaporationReagents = [Water, Blood, InsectBlood, Vomit, CopperBlood];

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(EvaporationReagents) == solution.Volume;
    }
}
