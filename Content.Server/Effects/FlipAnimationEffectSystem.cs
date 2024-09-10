using Content.Shared.Effects;

namespace Content.Server.Effects;

public sealed class FlipAnimationEffectSystem : SharedFlipAnimationEffectSystem
{
    public override void AnimateFlip(EntityUid entity)
    {
        RaiseNetworkEvent(new FlipAnimationEffectEvent(GetNetEntity(entity)));
    }
}
