using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var updateFrequency = 0.25f;

        if (!Timing.IsFirstTimePredicted)
            return;

        var query2 = EntityQueryEnumerator<GunComponent>();
        var curTime = Timing.CurTime;

        while (query2.MoveNext(out var uid, out var comp))
        {

            if (comp.NextUpdate > curTime)
                continue;


            var movespeed = Physics.GetMapVelocities(uid).Item1.Length() / (10f * comp.Ergonomics * updateFrequency);

            comp.CurrentAngle = Math.Min(comp.CurrentAngle + movespeed, comp.MaxAngleModified);
            comp.CurrentAngle = Math.Max(comp.CurrentAngle - comp.AngleDecay * updateFrequency, comp.MinAngleModified);

            comp.NextUpdate = curTime + TimeSpan.FromSeconds(updateFrequency);

            Dirty(uid, comp);
        }


        /*
         * On server because client doesn't want to predict other's guns.
         */

        // Automatic firing without stopping if the AutoShootGunComponent component is exist and enabled
        var query = EntityQueryEnumerator<AutoShootGunComponent, GunComponent>();

        while (query.MoveNext(out var uid, out var autoShoot, out var gun))
        {
            if (!autoShoot.Enabled)
                continue;

            if (gun.NextFire > Timing.CurTime)
                continue;

            AttemptShoot(uid, gun);
        }
    }
}
