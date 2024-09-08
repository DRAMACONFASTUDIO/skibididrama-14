namespace Content.Shared.CombatMode
{
    public sealed class ShoveEvent : HandledEntityEventArgs
    {
        /// <summary>
        ///     The entity being shoved.
        /// </summary>
        public EntityUid Target { get; init; }

        /// <summary>
        ///     The entity performing the shove.
        /// </summary>
        public EntityUid Source { get; init; }

        /// <summary>
        ///     Shove stamina cost for attacker
        /// </summary>
        public float StaminaCost { get; init; }

        /// <summary>
        ///     Shove stamina damage for the target
        /// </summary>
        public float StaminaDamage { get; init; }
    }
}
