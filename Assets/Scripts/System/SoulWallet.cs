using System;
using UnityEngine;

public static class SoulWallet
{
    private const string DefaultKey = "SystemLeveling.Soul.Total.v1";
    private static string saveKey = DefaultKey;
    private static int? pendingBalance;
    public static int Total => pendingBalance ?? Mathf.Max(0, PlayerPrefs.GetInt(saveKey, 0));
    public static bool HasUnsavedChanges => pendingBalance.HasValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        saveKey = DefaultKey; pendingBalance = null;
#if UNITY_EDITOR
        SimulateSaveFailure = false;
#endif
    }

    public static void Add(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Soul rewards cannot be negative.");
        if (amount == 0) return;
        pendingBalance = (int)Math.Min(int.MaxValue, (long)Total + amount);
        GameOverController.RecordSoulReward(amount);
        Save();
    }

    public static bool Save()
    {
        if (!pendingBalance.HasValue) return true;
        try
        {
#if UNITY_EDITOR
            if (SimulateSaveFailure) throw new PlayerPrefsException("Simulated Soul save failure (isolated test).");
#endif
            PlayerPrefs.SetInt(saveKey, pendingBalance.Value);
            PlayerPrefs.Save();
            pendingBalance = null;
            return true;
        }
        catch (Exception error)
        {
            // Keep earned Soul in memory for retry; a storage failure must not interrupt enemy death/loot.
            Debug.LogError("Soul could not be saved. Balance is retained for retry: " + error.Message);
            return false;
        }
    }

#if UNITY_EDITOR
    internal static bool SimulateSaveFailure;
    internal static IDisposable UseTestSave() => new TestSave();
    private sealed class TestSave : IDisposable
    {
        private static int users;
        private static string previousKey, testKey;
        private static int? previousPending;
        private static bool previousFailure;
        private bool disposed;
        internal TestSave()
        {
            if (users++ > 0) return;
            previousKey = saveKey; previousPending = pendingBalance; previousFailure = SimulateSaveFailure;
            testKey = "SystemLeveling.Tests.Soul." + Guid.NewGuid().ToString("N");
            saveKey = testKey; pendingBalance = null; SimulateSaveFailure = false;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (--users > 0) return;
            saveKey = previousKey; pendingBalance = previousPending; SimulateSaveFailure = previousFailure;
            PlayerPrefs.DeleteKey(testKey); PlayerPrefs.Save();
        }
    }
#endif
}
