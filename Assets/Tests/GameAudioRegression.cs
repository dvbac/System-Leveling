#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class GameAudioRegression
{
    [MenuItem("Tools/System Leveling/Validate Game Audio")]
    public static void Validate()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Validate audio outside Play Mode.");
        var checks = new List<string>();
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException(label); checks.Add(label); }
        Scene original = SceneManager.GetActiveScene(); bool dirty = original.isDirty;
        float volume = AudioListener.volume;
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var bank = Resources.Load<GameAudioBank>("GameAudio");
            Check(bank != null, "Runtime Resources bank exists and is included in builds");
            Check(bank.MusicFor(MainMenu.ScenePath) == bank.menuMusic && bank.MusicFor(LoadoutLauncher.SelectionScene) == bank.menuMusic, "Menu and selection share music");
            Check(bank.MusicFor(LoadoutLauncher.GameplayScene) == bank.grassMusic && bank.MusicFor(LoadoutLauncher.SnowScene) == bank.snowMusic, "Each gameplay map selects its own track");
            foreach (var clip in new[] { bank.menuMusic, bank.grassMusic, bank.snowMusic })
            {
                Check(clip != null && clip.length > 100, "Full music track assigned");
                Check(((AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip))).defaultSampleSettings.loadType == AudioClipLoadType.Streaming, "Music streams instead of decompressing minutes of PCM");
            }
            var sounds = (GameSound[])Enum.GetValues(typeof(GameSound));
            Check(bank.sounds.Length == sounds.Length && bank.sounds.Distinct().Count() == sounds.Length, "All 15 supplied SFX have distinct mappings");
            foreach (GameSound sound in sounds)
            {
                AudioClip clip = bank.Clip(sound);
                Check(clip != null && clip.length > 0, "Valid clip: " + sound);
                Check(((AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip))).defaultSampleSettings.loadType == AudioClipLoadType.DecompressOnLoad, "Ready for low-latency playback: " + sound);
            }
            var root = new GameObject("Audio validation fixture") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(root, preview);
            var audio = root.AddComponent<GameAudio>(); audio.Initialize(bank);
            var sources = root.GetComponents<AudioSource>();
            Check(sources.Length == GameAudio.VoiceCount + 1, "Exactly 12 effect voices and one music source");
            Check(sources.All(s => !s.playOnAwake && s.spatialBlend == 0), "2D playback without accidental play-on-awake");
            audio.SetSceneMusic(LoadoutLauncher.GameplayScene);
            Check(sources[0].clip == bank.grassMusic && sources[0].loop, "Production music switch sets looping grass track");
            audio.SetSceneMusic(LoadoutLauncher.SnowScene);
            Check(sources[0].clip == bank.snowMusic, "Production music switch selects snow track");
            audio.SetSceneMusic(MainMenu.ScenePath);
            Check(sources[0].clip == bank.menuMusic, "Production music switch returns to menu track");
            Check(audio.PlaySound(GameSound.Experience, 0), "First EXP sound accepted");
            Check(!audio.PlaySound(GameSound.Experience, 0.01f), "EXP burst throttled");
            Check(audio.PlaySound(GameSound.Experience, 0.08f), "EXP sound rearmed after cooldown");
            foreach (GameSound sound in sounds)
            {
                Check(audio.PlaySound(sound, 1), "Production voice allocator accepts: " + sound);
                Check(sources.Skip(1).Any(s => s.clip == bank.Clip(sound)), "Voice receives mapped clip: " + sound);
            }
            for (int i = 0; i < 100; i++) audio.PlaySound(GameSound.Hit, 2 + i);
            Check(root.GetComponents<AudioSource>().Length == sources.Length, "100 impacts never grow the voice budget");
            Check(!audio.PlaySound((GameSound)(-1), 200), "Invalid sound safely ignored");
            audio.UpdateMix(true, 1);
            Check(Mathf.Approximately(sources[0].volume, 0.12f), "Pause/results duck music using unscaled delta");
            audio.UpdateMix(false, 1);
            Check(Mathf.Approximately(sources[0].volume, 0.28f), "Resume restores music mix");
            Check(GameAudioBank.IsUI(GameSound.LevelUp) && GameAudioBank.IsUI(GameSound.Upgrade) && !GameAudioBank.IsUI(GameSound.Sword), "Level-up/upgrade sounds remain available while game is paused");
            var weaponTests = root.AddComponent<WeaponAmmoRegression>();
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(WeaponAmmoRegression).GetMethod("PureTests", fields).Invoke(weaponTests, null);
            int weaponChecks = ((List<string>)typeof(WeaponAmmoRegression).GetField("checks", fields).GetValue(weaponTests)).Count;
            Check(weaponChecks > 30, "Existing real weapon scheduler regression passes: " + weaponChecks + " checks");
            Check(AudioListener.volume == volume, "Master volume unchanged");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        Check(SceneManager.GetActiveScene() == original && original.isDirty == dirty, "Open scene and unsaved edits preserved");
        Directory.CreateDirectory("Captures/GameAudio");
        File.WriteAllText("Captures/GameAudio/validation.json", JsonUtility.ToJson(new Report
        { result = "PASS", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), note = "Edit Mode mapping, mixer, voice allocation and weapon scheduler checks; no listening or full Play Mode/build test." }, true));
        Debug.Log("GAME AUDIO VALIDATION PASS: " + checks.Count);
    }

    [Serializable] private class Report { public string result, utc, note; public string[] checks; }
}
#endif
