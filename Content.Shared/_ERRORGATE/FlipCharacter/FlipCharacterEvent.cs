namespace Content.Shared._ERRORGATE.FlipCharacter;

public sealed class FlipCharacterEvent : EntityEventArgs
{
    /// <summary>
    ///     The entity being flipped.
    /// </summary>
    public EntityUid Target { get; init; }

    public FlipCharacterEvent(EntityUid target)
    {
        Target = target;
    }
}
