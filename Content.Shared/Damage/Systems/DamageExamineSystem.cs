using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Damage.Systems;

public sealed class DamageExamineSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        // ERRORGATE NO EXAMINE VERBS
        //SubscribeLocalEvent<DamageExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(EntityUid uid, DamageExaminableComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var ev = new DamageExamineEvent(new FormattedMessage(), args.User);
        RaiseLocalEvent(uid, ref ev);
        if (!ev.Message.IsEmpty)
        {
            _examine.AddDetailedExamineVerb(args, component, ev.Message,
                Loc.GetString("damage-examinable-verb-text"),
                "/Textures/Interface/VerbIcons/smite.svg.192dpi.png",
                Loc.GetString("damage-examinable-verb-message")
            );
        }
    }

    public FormattedMessage AddDamageExamine(DamageSpecifier damageSpecifier, string? type = null)
    {
        var markup = GetDamageExamine(damageSpecifier, type);
        var message = new FormattedMessage();

        if (!message.IsEmpty)
        {
            message.PushNewline();
        }
        message.AddMessage(markup);

        return message;
    }

    /// <summary>
    /// Retrieves the damage examine values.
    /// </summary>
    private FormattedMessage GetDamageExamine(DamageSpecifier damageSpecifier, string? type = null)
    {
        var msg = new FormattedMessage();

        if (string.IsNullOrEmpty(type))
        {
            msg.AddMarkup(Loc.GetString("damage-examine"));
        }
        else
        {
            msg.AddMarkup(Loc.GetString("damage-examine-type", ("type", type)));
        }

        foreach (var damage in damageSpecifier.DamageDict)
        {
            if (damage.Value != FixedPoint2.Zero)
            {
                msg.PushNewline();
                msg.AddMarkup(Loc.GetString("damage-value", ("type", damage.Key), ("amount", damage.Value)));
            }
        }

        return msg;
    }
}
