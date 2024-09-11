using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement.Components;
using Content.Server.IdentityManagement;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Player;

namespace Content.Server._White.CombatLogs;

public sealed class CombatLogsSystem: EntitySystem
{
    //[Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IdentitySystem _identitySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatLogsComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<DamageableComponent, HitScanReflectAttemptEvent>(OnHitscan);
    }

    public void OnAttacked(EntityUid entity, CombatLogsComponent comp, AttackedEvent args)
    {
        if (!TryComp<ActorComponent>(entity, out var actor))
            return;

        if (!TryComp<MobStateComponent>(entity, out var playermobstate) || playermobstate.CurrentState == MobState.Dead)
            return;

        // Get attacker's identity or entity metadata name if there is none
        string attacker;

        if (HasComp<IdentityComponent>(args.User))
            attacker = _identitySystem.GetIdentityName(args.User);
        else
            attacker = MetaData(args.User).EntityName;

        // Get weapon's name
        string weapon = MetaData(args.Used).EntityName;

        // Get the attack verb from the melee weapon if there is one
        string verbRoot = "hit";
        string verbPresent = "hits";

        if (TryComp<MeleeWeaponComponent>(args.Used, out var weaponComponent))
        {
            verbRoot = weaponComponent.ChatLogVerbRoot;
            verbPresent = weaponComponent.ChatLogVerbPresent;
        }

        if (playermobstate.CurrentState == MobState.Critical) // Cant see shit in crit
        {
            LogMessage($"Something {verbPresent} you!");
            return;
        }

        bool unarmed = weapon == MetaData(args.User).EntityName;

        var a_or_an = "a";
        string vowels = "aeioAEIO";

        if (vowels.Contains(weapon[0]))
            a_or_an = "an";

        if (args.User == entity)
        {
            if (unarmed)
            {
                LogMessage($"You {verbRoot} yourself!");
                return;
            }
            LogMessage($"You {verbRoot} yourself with {a_or_an} {weapon}!");
            return;
        }

        if (unarmed)
        {
            LogMessage($"{attacker} {verbPresent} you!");
            return;
        }
        LogMessage($"{attacker} {verbPresent} you with {a_or_an} {weapon}!");
        return;

        void LogMessage(string message)
        {
            var intensity = 12; // Base font size (i think)

            if (weaponComponent != null)
                foreach (var damagevalue in weaponComponent.Damage.DamageDict)
                {
                    if (damagevalue.Key != "Structural")
                        intensity += (int)damagevalue.Value;
                }

            LogToChat(message, actor, intensity);
        }

    }

    public void OnProjectileHit(EntityUid entity, ProjectileComponent comp, ProjectileHitEvent args)
    {
        if (!TryComp<ActorComponent>(args.Target, out var actor))
            return;

        if (!TryComp<MobStateComponent>(args.Target, out var playermobstate) || playermobstate.CurrentState == MobState.Dead)
            return;

        string projectile = MetaData(entity).EntityName;
        string message = $"A {projectile} hits you!";

        var intensity = 12; // Base font size (i think)

        if (playermobstate.CurrentState == MobState.Critical) // Cant see shit in crit
            message = "Something hits you!";

        foreach (var damagevalue in comp.Damage.DamageDict)
        {
            if (damagevalue.Key != "Structural")
                intensity += (int)damagevalue.Value;
        }
        LogToChat(message, actor, intensity);
    }

    public void OnHitscan(EntityUid entity, DamageableComponent comp, HitScanReflectAttemptEvent args)
    {
        if (!TryComp<ActorComponent>(args.Target, out var actor))
            return;

        if (!TryComp<MobStateComponent>(entity, out var playermobstate) || playermobstate.CurrentState == MobState.Dead)
            return;

        string message = args.Reflected
            ? $"A beam of energy reflects off of you!"
            : $"A beam of energy hits you!"; // todo WD lasers

        LogToChat(message, actor, 25);

    }

    public void LogToChat(string message, ActorComponent actor, int intensity)
    {
        intensity = 12; // Requires engine RichTextEntry change

        //var wrappedMessage = Loc.GetString("chat-manager-combat-log-wrap-message", ("message", message), ("size", intensity));
        var wrap = $"[font size={intensity}][bold]{message}[/bold][/font]";
        _chatManager.ChatMessageToOne(ChatChannel.Local, message, wrap, EntityUid.Invalid,
            false, actor.PlayerSession.Channel, Color.Red);
    }

    public int DamageToFontSize(float intensity)
    {
        int minsize = 12;
        int maxsize = 30;
        int fontsize = (int)float.Lerp(minsize, maxsize, intensity);

        return fontsize;
    }
}
