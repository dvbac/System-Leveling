using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class HudTimerSetup
{
    [MenuItem("Tools/System Leveling/Add Gameplay Timer")]
    public static void Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Add the timer outside Play Mode.");
        foreach (string path in new[] { LoadoutLauncher.GameplayScene, LoadoutLauncher.SnowScene })
        {
            string backup = "Captures/RunTimer/Before/" + path;
            if (!File.Exists(backup)) { Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(path, backup); }
            Scene scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            if (scene.isDirty)
            {
                string live = "Captures/RunTimer/Before/Live-" + scene.name + ".unity";
                if (!File.Exists(live) && !EditorSceneManager.SaveScene(scene, live, true)) throw new IOException(live);
            }
            try
            {
                PlayerHud hud = Find<PlayerHud>(scene); EnemySpawner spawner = Find<EnemySpawner>(scene);
                Transform bar = hud.transform.Find("HUD");
                if (bar.Find("Run Timer") != null) throw new InvalidOperationException("Timer already exists; existing layout will not be overwritten.");
                GameObject timer = Object.Instantiate(bar.Find("Level Badge").gameObject, bar, false); timer.name = "Run Timer";
                RectTransform rect = timer.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
                rect.pivot = new Vector2(0.5f, 1); rect.anchoredPosition = new Vector2(0, -24); rect.sizeDelta = new Vector2(180, 90);
                Text caption = timer.transform.Find("Caption").GetComponent<Text>(); caption.text = "SURVIVAL TIME"; caption.fontSize = 12;
                Place(caption.rectTransform, 12, 8, 156, 22); caption.alignment = TextAnchor.MiddleCenter;
                Text value = timer.transform.Find("Level Value").GetComponent<Text>(); value.name = "Time Value";
                value.text = "00:00"; value.fontSize = 32; value.alignment = TextAnchor.MiddleCenter;
                Place(value.rectTransform, 12, 32, 156, 46);
                foreach (Graphic graphic in timer.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                SerializedObject so = new SerializedObject(hud);
                so.FindProperty("runClock").objectReferenceValue = spawner; so.FindProperty("timeText").objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.IsPartOfPrefabInstance(hud)) PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException(path);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        Debug.Log("GAMEPLAY TIMER READY: top-center MM:SS on both maps, sharing the survival spawner clock; pause/death stop it and scene restart resets it.");
    }

    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); }
}
