using System;
using UnityEngine;

public sealed class EliteTreasureChest : MonoBehaviour
{
    [SerializeField, Range(2, 3)] private int levelsGranted = 2;
    [SerializeField, Min(0)] private int soulGranted = 50;
    private bool collected;
    public int LevelsGranted => Mathf.Clamp(levelsGranted, 2, 3);
    public int SoulGranted => Mathf.Max(0, soulGranted);

    public void RollReward() => levelsGranted = UnityEngine.Random.Range(2, 4);

    // Award 2-3 threshold costs; lower later thresholds can roll carried XP into an extra level.
    public static int ExperienceReward(int currentLevel, int levels)
    {
        // shortcut: EXP saturates at int.MaxValue like PlayerStats; widen both if progression ever reaches that limit.
        long reward = 0;
        for (int i = 0; i < Mathf.Clamp(levels, 2, 3); i++)
            reward += PlayerStats.ExperienceForLevel((int)Math.Min(int.MaxValue, (long)Math.Max(1, currentLevel) + i));
        return (int)Math.Min(int.MaxValue, reward);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
    private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    private void TryCollect(Collider2D other)
    {
        if (other.GetComponent<PlayerCollector>() != null) return;
        var player = other.GetComponentInParent<PlayerStats>();
        if (player != null) Collect(player);
    }

    public bool Collect(PlayerStats player)
    {
        if (collected || player == null || player.IsDead || player.experienceCap <= 0 || Time.timeScale <= 0f || GameOverController.GameplayStopped) return false;
        collected = true;
        foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
        // Award Soul before EXP opens the paused upgrade UI; SoulWallet retains rewards if saving fails.
        SoulWallet.Add(SoulGranted);
        player.IncreaseExperience(ExperienceReward(player.level, LevelsGranted));
        GameAudio.Play(GameSound.LevelUp);
        Destroy(gameObject);
        return true;
    }
}
