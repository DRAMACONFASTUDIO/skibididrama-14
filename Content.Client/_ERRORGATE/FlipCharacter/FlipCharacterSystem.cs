using Content.Client._White.Animations;
using Content.Shared._ERRORGATE.FlipCharacter;
using Content.Shared.Damage.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Timing;

namespace Content.Client._ERRORGATE.FlipCharacter;

public sealed class FlipCharacterSystem: EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaminaComponent, FlipCharacterEvent>(PlayShoveTargeFlipAnimation);
    }

    public void PlayShoveTargeFlipAnimation(EntityUid user, StaminaComponent component, FlipCharacterEvent args)
    {
        var target = args.Target;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (TerminatingOrDeleted(target))
            return;

        if (_animationSystem.HasRunningAnimation(target, FlippingComponent.AnimationKey))
        {
            EnsureComp<FlippingComponent>(target);
            return;
        }

        RemComp<FlippingComponent>(target);

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

        _animationSystem.Play(target, animation, FlippingComponent.AnimationKey);
    }
}
