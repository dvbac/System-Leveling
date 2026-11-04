using System;

// Only two rolls per run; a missed first roll guarantees the second opportunity.
public sealed class RainbowSpawnSchedule
{
    private bool checkedSeven, checkedTwelve;
    public int Pending { get; private set; }
    public int Spawned { get; private set; }

    public void Advance(float elapsed, float chance, Func<float> roll)
    {
        if (float.IsNaN(elapsed) || float.IsInfinity(elapsed) || elapsed < 0f) return;
        if (!checkedSeven && elapsed >= 420f)
        {
            checkedSeven = true;
            if (roll() < chance) Pending++;
        }
        if (!checkedTwelve && elapsed >= 720f)
        {
            checkedTwelve = true;
            if (Spawned + Pending == 0 || roll() < chance) Pending++;
        }
    }

    public bool RecordSpawn()
    {
        if (Pending <= 0 || Spawned >= 2) return false;
        Pending--; Spawned++; return true;
    }
}
