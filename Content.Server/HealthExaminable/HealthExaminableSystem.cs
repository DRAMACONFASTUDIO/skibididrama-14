using Content.Server.Traits.Assorted;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.HealthExaminable;

public sealed class HealthExaminableSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealthExaminableComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, HealthExaminableComponent component, ExaminedEvent args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damage))
            return;

        if (!args.IsInDetailsRange)
            return;


        if (args.Examiner == args.Examined)
        {
            CreateMarkupSelfAware(uid, component, damage, args, -5);
            return;
        }

        CreateMarkup(uid, component, damage, args, -5);
    }

    private void CreateMarkup(EntityUid uid, HealthExaminableComponent component, DamageableComponent damage, ExaminedEvent args, int priority)
    {
        var msg = new FormattedMessage();

        // todo make those into cvars
        bool disableFeelingWellOnNoDamage = false; // disables green status messages on 0 damage for all
        bool disableSeeingOthersFeelWellOnNoDamage = true; // disables it for inspecting others

        var hasdamage = disableFeelingWellOnNoDamage;

        var adjustedThresholds = GetAdjustedThresholds(uid, component.Thresholds);

        foreach (var type in component.ExaminableTypes)
        {
            if (!damage.Damage.DamageDict.TryGetValue(type, out var dmg))
                continue;

            if (dmg == FixedPoint2.Zero)
                continue;

            FixedPoint2 closest = FixedPoint2.Zero;

            string chosenLocStr = string.Empty;
            foreach (var threshold in adjustedThresholds)
            {
                var str = $"health-examinable-{component.LocPrefix}-{type}-{threshold}";
                var tempLocStr = Loc.GetString($"health-examinable-{component.LocPrefix}-{type}-{threshold}", ("target", Identity.Entity(uid, EntityManager)));

                // i.e., this string doesn't exist, because theres nothing for that threshold
                if (tempLocStr == str)
                    continue;

                if (dmg > threshold && threshold > closest)
                {
                    chosenLocStr = tempLocStr;
                    closest = threshold;
                }
            }

            if (closest == FixedPoint2.Zero)
                continue;

            args.PushMarkup(chosenLocStr, priority);
            hasdamage = true;
        }

        if (!hasdamage)
        {
            if (args.Examiner == args.Examined)
            {
                args.PushMarkup(Loc.GetString($"health-examinable-selfaware{component.LocPrefix}-none"), priority);
                return;
            }

            if (disableSeeingOthersFeelWellOnNoDamage)
                return;

            args.PushMarkup(Loc.GetString($"health-examinable-{component.LocPrefix}-none"), priority);
            return;
        }

        // Anything else want to add on to this?
        RaiseLocalEvent(uid, new HealthBeingExaminedEvent(msg, false, args, priority), true);
    }


    private void CreateMarkupSelfAware(EntityUid uid, HealthExaminableComponent component, DamageableComponent damage, ExaminedEvent args, int priority)
    {
        var msg = new FormattedMessage();

        var hasdamage = false;

        var adjustedThresholds = GetAdjustedThresholds(uid, component.Thresholds);

        foreach (var type in component.ExaminableTypes)
        {
            if (!damage.Damage.DamageDict.TryGetValue(type, out var dmg))
                continue;

            if (dmg == FixedPoint2.Zero)
                continue;

            FixedPoint2 closest = FixedPoint2.Zero;

            string chosenLocStr = string.Empty;
            foreach (var threshold in adjustedThresholds)
            {
                var str = $"health-examinable-selfaware-{component.LocPrefix}-{type}-{threshold}";
                var tempLocStr = Loc.GetString($"health-examinable-selfaware-{component.LocPrefix}-{type}-{threshold}", ("target", Identity.Entity(uid, EntityManager)));

                // i.e., this string doesn't exist, because theres nothing for that threshold
                if (tempLocStr == str)
                    continue;

                if (dmg > threshold && threshold > closest)
                {
                    chosenLocStr = tempLocStr;
                    closest = threshold;
                }
            }

            if (closest == FixedPoint2.Zero)
                continue;

            args.PushMarkup(chosenLocStr, priority);
            hasdamage = true;
        }

        if (!hasdamage)
        {
            args.PushMarkup(Loc.GetString($"health-examinable-selfaware-{component.LocPrefix}-none"), priority);
            return;
        }

        // Anything else want to add on to this?
        RaiseLocalEvent(uid, new HealthBeingExaminedEvent(msg, true, args, priority), true);
    }

    /// <summary>
    ///     Return thresholds as percentages of an entity's critical threshold.
    /// </summary>
    private List<FixedPoint2> GetAdjustedThresholds(EntityUid uid, List<FixedPoint2> thresholdPercentages)
    {
        FixedPoint2 critThreshold = 0;
        if (TryComp<MobThresholdsComponent>(uid, out var threshold))
            critThreshold = _threshold.GetThresholdForState(uid, Shared.Mobs.MobState.Critical, threshold);

        // Fallback to 100 crit threshold if none found
        if (critThreshold == 0)
            critThreshold = 100;

        return thresholdPercentages.Select(percentage => critThreshold * percentage).ToList();
    }
}

/// <summary>
///     A class raised on an entity whose health is being examined
///     in order to add special text that is not handled by the
///     damage thresholds.
/// </summary>
public sealed class HealthBeingExaminedEvent
{
    public FormattedMessage Message;
    public ExaminedEvent ExamineEvent;
    public bool IsSelfAware;
    public int Priority;

    public HealthBeingExaminedEvent(FormattedMessage message, bool isSelfAware, ExaminedEvent examineEvent, int priority)
    {
        Message = message;
        IsSelfAware = isSelfAware;
        ExamineEvent = examineEvent;
        Priority = priority;
    }
}
