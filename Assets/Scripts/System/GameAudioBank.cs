using UnityEngine;

public enum GameSound
{
    Hover, Click, Experience, LevelUp, PlayerHurt, MagicShot, MagicHit, Upgrade,
    Sword, Bow, Charge, Reload, Crossbow, Hit, ArrowHit
}

public sealed class GameAudioBank : ScriptableObject
{
    public AudioClip menuMusic, grassMusic, snowMusic;
    public AudioClip[] sounds;

    public AudioClip MusicFor(string path) => path == LoadoutLauncher.GameplayScene ? grassMusic
        : path == LoadoutLauncher.SnowScene ? snowMusic : menuMusic;

    public AudioClip Clip(GameSound sound) => sounds != null && (int)sound >= 0 && (int)sound < sounds.Length ? sounds[(int)sound] : null;

    public static bool IsUI(GameSound sound) => sound == GameSound.Hover || sound == GameSound.Click || sound == GameSound.LevelUp || sound == GameSound.Upgrade;
}
