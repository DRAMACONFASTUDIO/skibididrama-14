using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Add to an entity to paralyze it whenever it reaches critical amounts of Stamina DamageType.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class StaminaComponent : Component
{
    public bool SlowdownReset;

    /// <summary>
    /// Have we reached peak stamina damage and been paralyzed?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool Critical;

    /// <summary>
    /// How much stamina damage reduces per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float Decay = 3f;

    /// <summary>
    /// How much time after receiving damage until stamina damage starts decreasing.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float Cooldown = 1f;

    /// <summary>
    /// How much stamina damage this entity has taken.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float StaminaDamage;

    /// <summary>
    /// How much stamina damage is required to entire stam crit.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float CritThreshold = 100f;

    /// <summary>
    /// How long will this mob be stunned for?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// A dictionary of active stamina drains, with the key being the source of the drain
    /// and the value being the drain rate per second.
    /// </summary>
    [DataField("activeDrains"), AutoNetworkedField]
    public Dictionary<EntityUid, float> ActiveDrains = new();

    /// <summary>
    /// How much stamina per second is decreased by sprinting.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float SprintingStaminaDrainRate = 5f;

    /// <summary>
    /// To avoid continuously updating our data we track the last time we updated so we can extrapolate our current stamina.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
