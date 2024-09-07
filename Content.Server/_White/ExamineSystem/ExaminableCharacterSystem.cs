using Content.Server.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server._White.ExamineSystem
{

    //^.^
    public sealed class ExaminableCharacterSystem : EntitySystem
    {
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly EntityManager _entityManager = default!;
        [Dependency] private readonly IdCardSystem _idCard = default!;
        [Dependency] private readonly IConsoleHost _consoleHost = default!;
        [Dependency] private readonly INetConfigurationManager _netConfigManager = default!;


        public override void Initialize()
        {
            SubscribeLocalEvent<ExaminableCharacterComponent, ExaminedEvent>(HandleExamine);
        }

        private void SendNoticeMessage(ActorComponent actorComponent, string message)
        {
            var should = _netConfigManager.GetClientCVar(actorComponent.PlayerSession.ConnectedClient, CCVars.LogCharacterExamine);

            if (!should)
                return;

            _consoleHost.RemoteExecuteCommand(actorComponent.PlayerSession, $"notice [font size=10][color=#aeabc4]{message}[/color][/font]");
        }

        private void HandleExamine(EntityUid uid, ExaminableCharacterComponent comp, ExaminedEvent args)
        {
            var infoLines = new List<string>();

            var name = Name(uid);

            var ev = new SeeIdentityAttemptEvent();
            RaiseLocalEvent(uid, ev);
            if (ev.Cancelled)
            {
                if (_idCard.TryFindIdCard(uid, out var id) && !string.IsNullOrWhiteSpace(id.Comp.FullName))
                {
                    name = id.Comp.FullName;
                }
                else
                {
                    name = "unknown";
                }
            }
            infoLines.Add($"It's [bold]{name}[/bold]!");

            var idInfoString = GetInfo(uid);
            if (!string.IsNullOrEmpty(idInfoString))
            {
                infoLines.Add(idInfoString);
                args.PushMarkup(idInfoString);
            }

            var slotLabels = new Dictionary<string, string>
            {
                { "head", "head-" },
                { "eyes", "eyes-" },
                { "mask", "mask-" },
                { "neck", "neck-" },
                { "ears", "ears-" },
                { "jumpsuit", "jumpsuit-" },
                { "outerClothing", "outer-" },
                { "back", "back-" },
                { "gloves", "gloves-" },
                { "belt", "belt-" },
                { "shoes", "shoes-" },
                { "suitstorage", "suitstorage-" }
            };

            var priority = 13;

            if (slotLabels.Count > 0)
            {
                var cansee = Loc.GetString("examine-can-see", ("ent", uid));
                args.PushMarkup(cansee, priority);
                infoLines.Add(cansee);
                priority--;
            }

            foreach (var slotEntry in slotLabels)
            {
                var slotName = slotEntry.Key;
                var slotLabel = slotEntry.Value;

                slotLabel += "examine";

                if (!_inventorySystem.TryGetSlotEntity(uid, slotName, out var slotEntity))
                    continue;

                if (_entityManager.TryGetComponent<MetaDataComponent>(slotEntity, out var metaData))
                {
                    var item = Loc.GetString(slotLabel, ("item", metaData.EntityName), ("ent", uid));
                    args.PushMarkup(item, priority);
                    priority--;
                    infoLines.Add(item);
                }
            }

            var combinedInfo = string.Join("\n", infoLines);

            if (TryComp(args.Examiner, out ActorComponent? actorComponent))
            {
                SendNoticeMessage(actorComponent, combinedInfo);
            }
        }

        private string GetInfo(EntityUid uid)
        {
            if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
            {
                // PDA
                if (EntityManager.TryGetComponent(idUid, out PdaComponent? pda) &&
                    TryComp(pda.ContainedId, out IdCardComponent? idCard))
                {
                    return GetNameAndJob(idCard);
                }
                // ID Card
                if (EntityManager.TryGetComponent(idUid, out IdCardComponent? id))
                {
                    return GetNameAndJob(id);
                }
            }
            return "";
        }

        private string GetNameAndJob(IdCardComponent id)
        {
            var jobSuffix = string.IsNullOrWhiteSpace(id.JobTitle) ? "" : $" ({id.JobTitle})";

            var val = string.IsNullOrWhiteSpace(id.FullName)
                ? Loc.GetString("access-id-card-component-owner-name-job-title-text",
                    ("jobSuffix", jobSuffix))
                : Loc.GetString("access-id-card-component-owner-full-name-job-title-text",
                    ("fullName", id.FullName),
                    ("jobSuffix", jobSuffix));

            return val;
        }
    }
}
