using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised whenever a muzzle flash client-side entity needs to be spawned.
/// </summary>
[Serializable, NetSerializable]
public sealed class MuzzleFlashEvent : EntityEventArgs
{
    public NetEntity Uid;
    public string Prototype;

    /// <summary>
    /// Should the effect match the rotation of the entity.
    /// </summary>
    public bool MatchRotation;

    /// <summary>
    /// Should the effect match the rotation of the entity.
    /// </summary>
    public float MuzzleEffectRadius;

    public MuzzleFlashEvent(NetEntity uid, string prototype, float muzzleEffectRadius, bool matchRotation = false)
    {
        Uid = uid;
        Prototype = prototype;
        MatchRotation = matchRotation;
        MuzzleEffectRadius = muzzleEffectRadius;
    }
}
