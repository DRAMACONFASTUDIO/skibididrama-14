using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Speech.Muting;
using Content.Shared.Administration.Notes;
using Content.Shared.Chat;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Speech.Muting;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Mobs;

/// <see cref="DeathgaspComponent"/>
public sealed class DeathgaspSystem: EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeathgaspComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, DeathgaspComponent component, MobStateChangedEvent args)
    {
        // don't deathgasp if they arent going straight from crit to dead
        if (args.NewMobState != MobState.Dead || args.OldMobState != MobState.Critical)
            return;

        Deathgasp(uid, component);
    }

    /// <summary>
    ///     Causes an entity to perform their deathgasp emote, if they have one.
    /// </summary>
    public bool Deathgasp(EntityUid uid, DeathgaspComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (HasComp<MutedComponent>(uid))
            return false;

        _chat.TryEmoteWithChat(uid, component.Prototype, ignoreActionBlocker: true);

        if (TryComp(uid, out ActorComponent? actor) && TryComp(uid, out MobStateComponent? mobstate) && mobstate.CurrentState == MobState.Dead) // ERRORGATE >>> SEND "YOU DIED" MESSAGE IN CHAT
        {
            var message = "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n";
            var wrappedMessage = message;
            Send(message, wrappedMessage);

            message = "ERROR: YOU ARE DEAD!";
            wrappedMessage = $"[font size=48][bold]{message}[/bold][/font]"; //XD
            Send(message, wrappedMessage);

            message = "\nYOU FAILED TO ESCAPE THE MACHINATION OF RUIN. RISE AND TRY AGAIN.";
            wrappedMessage = $"\n[bold]>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>\n {message} [/bold]\n\n\n\n\n\n\n\n\n\n\n\n";
            Send(message, wrappedMessage);


            void Send(string msg, string wrappedmsg)
            {
                _chatManager.ChatMessageToOne(ChatChannel.OOC,
                    msg,
                    wrappedmsg,
                    EntityUid.Invalid,
                    false,
                    actor.PlayerSession.Channel,
                    Color.Red);
            }
        }

        return true;
    }
}
