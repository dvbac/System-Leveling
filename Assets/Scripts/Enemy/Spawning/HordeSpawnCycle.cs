using System;

public enum HordeSpawnState { CheckingLayer1, WaitingLayer2, Spawning, WaitingForClear }

// Shared production/test state machine. No scene lookups, coroutines, or global random calls.
public sealed class HordeSpawnCycle
{
    private readonly HordeSpawnSettings settings;
    public HordeSpawnState State { get; private set; }
    public float Chance { get; private set; }
    public float CheckTimer { get; private set; }
    public float WaitElapsed { get; private set; }
    public float Elapsed { get; private set; }
    public Action<string> Log;

    public HordeSpawnCycle(HordeSpawnSettings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Reset();
    }

    public void Reset()
    {
        State = HordeSpawnState.CheckingLayer1;
        Chance = SpawnSelection.Percent(settings.initialChance);
        CheckTimer = 0;
        WaitElapsed = 0;
        Elapsed = 0;
    }

    private void NextCycle()
    {
        State = HordeSpawnState.CheckingLayer1;
        Chance = SpawnSelection.Percent(settings.initialChance);
        CheckTimer = 0;
        WaitElapsed = 0;
        Log?.Invoke("Horde cleared; restarting layer 1 timer.");
    }

    // At most one probability check per Tick. A long frame never causes a burst of checks.
    // True means deployment must begin now; capacity/geometry remain the spawner's responsibility.
    public bool Tick(float delta, bool canReserve, int livingHorde, Func<float> percentRoll)
    {
        if (!settings.enabled || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return false;
        Elapsed += delta;
        if (State == HordeSpawnState.WaitingForClear)
        {
            if (livingHorde == 0) NextCycle();
            return false;
        }
        if (State == HordeSpawnState.Spawning) return false;
        if (State == HordeSpawnState.CheckingLayer1)
        {
            CheckTimer += delta;
            if (CheckTimer < SpawnSelection.Positive(settings.activationInterval, 20)) return false;
            CheckTimer = 0;
            if (!canReserve)
            {
                Log?.Invoke("Layer 1 deferred: no valid horde pool or reservable population slots.");
                return false;
            }
            float roll = percentRoll();
            Log?.Invoke($"Layer 1 roll={roll:F1}%, chance={settings.activationChance:F1}%.");
            float activationChance = SpawnSelection.Percent(settings.activationChance);
            if (activationChance < 100f && roll >= activationChance) return false;
            State = HordeSpawnState.WaitingLayer2;
            Chance = SpawnSelection.Percent(settings.initialChance);
            WaitElapsed = 0;
            Log?.Invoke("Layer 1 passed; waiting for the FIRST layer 2 interval (no immediate spawn).");
            return false;
        }

        WaitElapsed += delta;
        if (WaitElapsed >= SpawnSelection.Positive(settings.maxWait, 30))
        {
            State = HordeSpawnState.Spawning;
            Log?.Invoke($"Layer 2 deadline reached at {WaitElapsed:F1}s; forced deployment.");
            return true;
        }
        CheckTimer += delta;
        if (CheckTimer < SpawnSelection.Positive(settings.checkInterval, 5)) return false;
        CheckTimer = 0;
        float spawnRoll = percentRoll();
        Log?.Invoke($"Layer 2 roll={spawnRoll:F1}%, chance={Chance:F1}%, wait={WaitElapsed:F1}s.");
        if (Chance >= 100f || spawnRoll < Chance)
        {
            State = HordeSpawnState.Spawning;
            Log?.Invoke("Layer 2 passed; deploying horde.");
            return true;
        }
        Chance = SpawnSelection.Percent(Chance + SpawnSelection.Percent(settings.increasePerFailure));
        Log?.Invoke($"Layer 2 failed; next chance={Chance:F1}%.");
        return false;
    }

    public void DeploymentFinished(int livingHorde)
    {
        if (State != HordeSpawnState.Spawning) return;
        if (livingHorde > 0)
        {
            State = HordeSpawnState.WaitingForClear;
            Log?.Invoke($"Deployment complete; waiting for {livingHorde} living horde enemies to clear.");
        }
        else NextCycle();
    }
}
