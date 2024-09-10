namespace Content.Shared.Effects;

public abstract class SharedFlipAnimationEffectSystem : EntitySystem
{
    public abstract void AnimateFlip(EntityUid entity);
}
