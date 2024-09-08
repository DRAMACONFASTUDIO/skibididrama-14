namespace Content.Shared.Actions.Events;

public sealed class ShoveAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid TargetUid;
    public readonly EntityUid ShoverUid;

    public ShoveAttemptEvent(EntityUid targetUid, EntityUid shoverUid)
    {
        TargetUid = targetUid;
        ShoverUid = shoverUid;
    }
}
