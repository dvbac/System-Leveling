using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EliteTreasureChestBuilder
{
    public const string Path = "Assets/Resources/EliteTreasureChest.prefab";

    [MenuItem("Tools/System Leveling/Build Elite Treasure Chest")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build outside Play Mode.");
        const string imagePath = "Assets/MyAssets/Other/Drop/treasure-chest.png";
        var importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing treasure-chest texture.");
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Path) != null) return;
        var sprite = AssetDatabase.LoadAllAssetsAtPath(imagePath).OfType<Sprite>().FirstOrDefault();
        if (sprite == null) throw new InvalidOperationException("Missing treasure-chest sprite.");
        Directory.CreateDirectory("Assets/Resources"); AssetDatabase.Refresh();
        Scene preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var go = new GameObject("Elite Treasure Chest", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(EliteTreasureChest));
            SceneManager.MoveGameObjectToScene(go, preview);
            // Render the supplied tiny pixel-art chest at a readable world size.
            go.transform.localScale = Vector3.one * (0.6f / Mathf.Max(0.01f, sprite.bounds.size.x));
            var visual = go.GetComponent<SpriteRenderer>(); visual.sprite = sprite; visual.sortingOrder = 6;
            var collider = go.GetComponent<CircleCollider2D>(); collider.isTrigger = true; collider.radius = sprite.bounds.size.x * 0.6f;
            if (PrefabUtility.SaveAsPrefabAsset(go, Path) == null) throw new IOException("Could not save chest prefab.");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        Debug.Log("ELITE TREASURE CHEST BUILT: 2-3 levels worth of EXP at pickup and 50 bonus Soul. Source art and scenes untouched.");
    }
}
