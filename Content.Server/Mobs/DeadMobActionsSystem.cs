using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Speech.Muting;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Console;
using Robust.Shared.Player;
using Content.Shared.Speech.Muting;

namespace Content.Server.Mobs;

/// <summary>
///     Handles performing dead-specific actions.
/// </summary>
public sealed class DeadMobActionsSystem : EntitySystem
{
    [Dependency] private readonly IServerConsoleHost _host = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateActionsComponent, DeadRespawnEvent>(OnRespawn);
    }

    private void OnRespawn(EntityUid uid, MobStateActionsComponent component, DeadRespawnEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor) || !_mobState.IsDead(uid))
            return;

        _host.ExecuteCommand(actor.PlayerSession, "respawn");
        args.Handled = true;
    }

}
