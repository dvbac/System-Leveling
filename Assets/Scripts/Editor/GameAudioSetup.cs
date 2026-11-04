using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameAudioSetup
{
    public const string BankPath = "Assets/Resources/GameAudio.asset";
    private const string Root = "Assets/MyAssets/Sound/";
    private static readonly string[] SoundFiles =
    {
        "A_subtle_dark_fantas_#1-1793594188708.wav",
        "A_single_premium_dar_#4-1793594238489.wav",
        "A_tiny_magical_exper_#4-1793594279337.wav",
        "A_dark_fantasy_level_#4-1793594310731.wav",
        "A_single_compact_pla_#2-1793594348898.wav",
        "A_single_dark_magic__#1-1793594424465.wav",
        "A_single_magical_pro_#3-1793594471636.wav",
        "A_magical_upgrade_ca_#1-1793594510152.wav",
        "A_single_fast_sword__#4-1793596044689.wav",
        "A_single_wooden_bow__#2-1793596120706.wav",
        "A_seamless_looping_m_#3-1793596507434.wav",
        "One_single_clean,_br_#3-1793596609017.wav",
        "A_single_medieval_cr_#4-1793596642117.wav",
        "A_single_universal_a_#2-1793596830142.wav",
        "A_single_subtle_arro_#2-1793597011034.wav"
    };

    [MenuItem("Tools/System Leveling/Setup Game Audio")]
    public static void Setup()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Set up audio outside Play Mode.");
        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();
        var bank = AssetDatabase.LoadAssetAtPath<GameAudioBank>(BankPath);
        if (bank == null) { bank = ScriptableObject.CreateInstance<GameAudioBank>(); AssetDatabase.CreateAsset(bank, BankPath); }
        bank.menuMusic = Load("Music/Awakening the Ancient System - usesuno.com.wav", true);
        bank.grassMusic = Load("Music/Cursed Wilderness - usesuno.com.wav", true);
        bank.snowMusic = Load("Music/Frozen Moonlight - usesuno.com.wav", true);
        bank.sounds = new AudioClip[SoundFiles.Length];
        for (int i = 0; i < SoundFiles.Length; i++) bank.sounds[i] = Load("sfx/" + SoundFiles[i], false);
        EditorUtility.SetDirty(bank); AssetDatabase.SaveAssetIfDirty(bank);
        Debug.Log("GAME AUDIO SETUP: 3 streaming music tracks, 15 SFX; scenes and WAV data untouched.");
    }

    private static AudioClip Load(string relative, bool music)
    {
        string path = Root + relative;
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) throw new InvalidOperationException("Missing audio: " + path);
        var settings = importer.defaultSampleSettings;
        settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
        settings.quality = 0.75f;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path) ?? throw new InvalidOperationException("Audio failed to import: " + path);
    }
}
