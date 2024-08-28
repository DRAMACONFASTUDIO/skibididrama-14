using System.Linq;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Player;

namespace Content.Server._White.CombatLogs;

public sealed class CombatLogsSystem: EntitySystem
{
    //[Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatLogsComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<DamageableComponent, HitScanReflectAttemptEvent>(OnHitscan);
    }

    public void OnAttacked(EntityUid entity, CombatLogsComponent comp, AttackedEvent args)
    {
        if (TryComp<ActorComponent>(entity, out var actor))
        {
            string attacker = MetaData(args.User).EntityName;
            string weapon = MetaData(args.Used).EntityName;
            string message;

            if (args.User == entity)
                message = attacker == weapon ? $"You punch yourself!" : $"You hit yourself with a {weapon}!";
            else
                message = attacker == weapon ? $"{attacker} punches you!" : $"{attacker} hits you with a {weapon}!";


            LogToChat(message, actor, 20);
        }
    }

    public void OnProjectileHit(EntityUid entity, ProjectileComponent comp, ProjectileHitEvent args)
    {
        if (TryComp<ActorComponent>(args.Target, out var actor))
        {

            string projectile = MetaData(entity).EntityName;
            string message = $"A {projectile} hits you!";

            float damage = 0;

            foreach (FixedPoint2 damagevalue in comp.Damage.DamageDict.Values)
            {
                damage += (float)damagevalue;
            }

            LogToChat(message, actor, 50);
        }
    }

    public void OnHitscan(EntityUid entity, DamageableComponent comp, HitScanReflectAttemptEvent args)
    {
        if (TryComp<ActorComponent>(args.Target, out var actor))
        {
            string message = args.Reflected
                ? $"A beam of energy reflects off of you!"
                : $"A beam of energy hits you!"; // todo WD lasers

            LogToChat(message, actor, 25);
        }
    }

    public void LogToChat(string message, ActorComponent actor, int intensity)
        {
            var wrappedMessage = Loc.GetString("chat-manager-combat-log-wrap-message", ("message", message), ("size", intensity));
            var wrap = $"[font size={intensity}][bold]{message}[/bold][/font] bruh";
            _chatManager.ChatMessageToOne(ChatChannel.Local, message, wrap, EntityUid.Invalid, false, actor.PlayerSession.Channel,
                    Color.Red);
        }

    public int DamageToFontSize(float intensity)
    {
        int minsize = 12;
        int maxsize = 30;
        int fontsize = (int)float.Lerp(minsize, maxsize, intensity);

        return fontsize;
    }
}
