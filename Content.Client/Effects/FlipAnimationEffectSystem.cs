using Content.Shared.Effects;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Effects;

public sealed class FlipAnimationEffectSystem : SharedFlipAnimationEffectSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;

    /// <summary>
    /// It's a little on the long side but given we use multiple colours denoting what happened it makes it easier to register.
    /// </summary>
    private const float AnimationLength = 0.30f;
    private const string AnimationKey = "color-flash-effect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<FlipAnimationEffectEvent>(Flip);
        //SubscribeLocalEvent<FlipAnimationEffectComponent, AnimationCompletedEvent>(OnEffectAnimationCompleted);
    }

    public override void AnimateFlip(EntityUid entity)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        Flip(new FlipAnimationEffectEvent(GetNetEntity(entity)));
    }

    private void OnEffectAnimationCompleted(EntityUid uid, FlipAnimationEffectComponent component, AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey)
            return;

        RemCompDeferred<FlipAnimationEffectComponent>(uid);
    }

    public void Flip(FlipAnimationEffectEvent args)
    {
        var target = GetEntity(args.Entity);

        if (!_timing.IsFirstTimePredicted || target == EntityUid.Invalid)
            return;

        if (TerminatingOrDeleted(target))
            return;

        if (_animationSystem.HasRunningAnimation(target, "flip"))
        {
            return;
        }

        var baseAngle = Angle.Zero;
        if (EntityManager.TryGetComponent(target, out SpriteComponent? sprite))
            baseAngle = sprite.Rotation;

        var degrees = baseAngle.Degrees;

        var animation = new Animation
        {
            Length = TimeSpan.FromMilliseconds(400),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees - 10), 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 180), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees + 360), 0.2f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(degrees), 0f)
                    }
                }
            }
        };
        _animationSystem.Play(target, animation, "flip");
    }
}
