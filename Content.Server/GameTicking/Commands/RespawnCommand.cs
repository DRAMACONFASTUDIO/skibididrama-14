using Content.Shared.Mind;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.GameTicking.Commands
{
    sealed class RespawnCommand : IConsoleCommand
    {
        public string Command => "respawn";
        public string Description => "Respawns a player, kicking them back to the lobby, if they are dead.";
        public string Help => "respawn [player]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (args.Length > 1)
            {
                shell.WriteLine("Must provide <= 1 argument.");
                return;
            }

            var playerMgr = IoCManager.Resolve<IPlayerManager>();
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            var ticker = sysMan.GetEntitySystem<GameTicker>();
            var mind = sysMan.GetEntitySystem<SharedMindSystem>();
            var mobState = sysMan.GetEntitySystem <MobStateSystem>();

            NetUserId userId;
            if (args.Length == 0)
            {
                if (player == null)
                {
                    shell.WriteLine("If not a player, an argument must be given.");
                    return;
                }

                userId = player.UserId;
            }
            else if (!playerMgr.TryGetUserId(args[0], out userId))
            {
                shell.WriteLine("Unknown player");
                return;
            }

            if (!playerMgr.TryGetSessionById(userId, out var targetPlayer))
            {
                if (!playerMgr.TryGetPlayerData(userId, out var data))
                {
                    shell.WriteLine("Unknown player");
                    return;
                }

                mind.WipeMind(data.ContentData()?.Mind);
                shell.WriteLine("Player is not currently online, but they will respawn if they come back online");
                return;
            }

            // ERRORGATE START

            if (targetPlayer.AttachedEntity == null) // RESPAWN IF CHARACTER DELETED
            {
                ticker.Respawn(targetPlayer);
                return;
            }

            var character = targetPlayer.AttachedEntity ?? new EntityUid();

            if (!mobState.IsDead(character))
            {
                if (args.Length == 0)
                    shell.WriteError(Loc.GetString("You can only respawn if you are dead."));
                else
                    shell.WriteError(Loc.GetString("You can only respawn dead players, use forcerespawn instead."));

                return;
            }

            // ERRORGATE END

            ticker.Respawn(targetPlayer);
        }
    }
}
