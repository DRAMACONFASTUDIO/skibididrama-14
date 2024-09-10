using Robust.Shared.Serialization;

namespace Content.Shared.Effects;

/// <summary>
/// Raised on the server and sent to a client to play the flip animation.
/// </summary>
[Serializable, NetSerializable]
public sealed class FlipAnimationEffectEvent : EntityEventArgs
{
    /// <summary>
    /// Entity to flip.
    /// </summary>
    public NetEntity Entity;

    public FlipAnimationEffectEvent(NetEntity entity)
    {
        Entity = entity;
    }
}
