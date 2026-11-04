using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CompactHudSetup
{
    public const string CursorSource = "Assets/MyAssets/Other/Cursor/Cursor.png";
    private const string WhitePath = "Assets/MyAssets/UI/CompactHud/White.png";
    private const string Output = "Captures/CompactHud";
    private static Sprite white;

    [MenuItem("Tools/System Leveling/Apply Compact Gameplay HUD")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Apply HUD outside Play Mode.");
        string[] paths = { LoadoutLauncher.GameplayScene, LoadoutLauncher.SnowScene };
        Directory.CreateDirectory(Output);
        foreach (string path in paths)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            Scene open = SceneManager.GetSceneByPath(path);
            if (open.isLoaded && open.isDirty)
                throw new InvalidOperationException("Save your live gameplay scene changes first: " + path);
            Backup(path);
        }
        Backup("Assets/Prefab/UI/System HUD.prefab");
        Backup("ProjectSettings/ProjectSettings.asset");
        string before = Hash(CursorSource);
        PrepareCursor();
        const string prefabPath = "Assets/Prefab/UI/System HUD.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try { Apply(contents); PrefabUtility.SaveAsPrefabAsset(contents,prefabPath); }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        foreach (string path in paths)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                PlayerHud hud = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PlayerHud>(true)).Single();
                Apply(hud.gameObject);
                foreach (Component component in hud.GetComponentsInChildren<Component>(true))
                    if (component != null && !(component is CanvasRenderer) && PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + path);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene,true); }
        }
        AssetDatabase.SaveAssets();
        if (before != Hash(CursorSource)) throw new InvalidOperationException("Original cursor changed.");
        GameCursorSetup.Validate();
        File.WriteAllText(Output+"/setup.json", "{\"result\":\"PASS\",\"maps\":2,\"sourceCursorUnchanged\":true,\"sourceSHA256\":\""+before+"\",\"cursorTextureSize\":16,\"cursorArtworkSize\":16}");
        Debug.Log("COMPACT HUD READY: both maps, short inset HP, edge EXP, compact stats, cursor ammo and character reload feedback. Cursor source untouched.");
    }

    // Shared with Build HUD so it cannot reintroduce the old oversized panels.
    public static void Apply(GameObject root)
    {
        white = White();
        var hud = root.GetComponent<PlayerHud>();
        if (hud == null) return;
        Transform row = root.transform.Find("HUD");
        Place((RectTransform)row,0,0,0,60,new Vector2(0,1),new Vector2(1,1),new Vector2(0.5f,1));
        Transform health = row.Find("Health Panel");
        var data = new SerializedObject(hud);
        var hp = (Image)data.FindProperty("healthFill").objectReferenceValue;
        var hpText = (Text)data.FindProperty("healthText").objectReferenceValue;
        Strip(health,hp,hpText,hp.transform.parent.GetComponent<Image>());
        Place((RectTransform)health,8,-8,240,18);
        Stretch((RectTransform)hp.transform.parent);
        Bar(hp,Color.white);
        hp.color = new Color32(232,86,111,255);
        hp.transform.parent.GetComponent<Image>().color = new Color32(35,22,29,210);
        hp.transform.parent.GetComponent<Image>().sprite = white;
        hp.transform.parent.GetComponent<Image>().type = Image.Type.Simple;
        Stretch(hpText.rectTransform);
        TextStyle(hpText,12,TextAnchor.MiddleCenter);
        hpText.transform.SetAsLastSibling(); // The old fill was drawn after the separate HP label.

        Transform exp = root.transform.Find("Experience Panel");
        var xp = (Image)data.FindProperty("experienceFill").objectReferenceValue;
        var xpText = (Text)data.FindProperty("experienceText").objectReferenceValue;
        Strip(exp,xp,xpText,xp.transform.parent.GetComponent<Image>());
        Place((RectTransform)exp,0,0,0,6,new Vector2(0,0),new Vector2(1,0),new Vector2(0.5f,0));
        Stretch((RectTransform)xp.transform.parent);
        Bar(xp,new Color32(61,180,244,255));
        xp.transform.parent.GetComponent<Image>().sprite = white;
        xp.transform.parent.GetComponent<Image>().type = Image.Type.Simple;
        xp.transform.parent.GetComponent<Image>().color = new Color32(10,31,46,180);
        Place(xpText.rectTransform,-6,8,160,14,Vector2.right,Vector2.right,Vector2.right);
        TextStyle(xpText,10,TextAnchor.MiddleRight);

        Transform badge = row.Find("Level Badge");
        var level = (Text)data.FindProperty("levelText").objectReferenceValue;
        Text caption = badge.Find("Caption").GetComponent<Text>();
        Strip(badge,caption,level);
        Place((RectTransform)badge,88,-30,64,18);
        caption.text = "LV";
        Place(caption.rectTransform,0,0,22,18); TextStyle(caption,12);
        Place(level.rectTransform,24,0,36,18); TextStyle(level,12);

        var soul = root.GetComponentInChildren<SoulBalanceView>(true);
        if (soul != null)
        {
            Strip(soul.transform,soul.caption,soul.amount);
            Place((RectTransform)soul.transform,152,-30,200,18);
            Place(soul.caption.rectTransform,0,0,145,18); TextStyle(soul.caption,12);
            Place(soul.amount.rectTransform,36,0,110,18); TextStyle(soul.amount,12);
            var so = new SerializedObject(soul);
            so.FindProperty("compact").boolValue = true; so.ApplyModifiedPropertiesWithoutUndo();
        }
        Transform timer = row.Find("Run Timer");
        if (timer != null)
        {
            Text value = timer.Find("Time Value").GetComponent<Text>();
            Strip(timer,value);
            Place((RectTransform)timer,-8,-8,74,22,Vector2.one,Vector2.one,Vector2.one);
            Stretch(value.rectTransform); TextStyle(value,16,TextAnchor.MiddleRight);
        }

        WeaponHud weapon = root.GetComponentInChildren<WeaponHud>(true);
        if (weapon != null)
        {
            var so = new SerializedObject(weapon);
            Text text = (Text)so.FindProperty("label").objectReferenceValue;
            weapon.transform.SetParent(row,false);
            Strip(weapon.transform,text);
            Place((RectTransform)weapon.transform,8,-30,74,18);
            Stretch(text.rectTransform); TextStyle(text,12);
            so.FindProperty("compact").boolValue = true;
            RectTransform feedback = FindOrCreate(root.transform,"Combat Feedback");
            Stretch(feedback);
            Text pointer = Feedback(feedback,"Cursor Ammo",72,14);
            Text reload = Feedback(feedback,"Reload Indicator",120,18);
            so.FindProperty("cursorAmmo").objectReferenceValue = pointer;
            so.FindProperty("reloadIndicator").objectReferenceValue = reload;
            so.ApplyModifiedPropertiesWithoutUndo();
            pointer.gameObject.SetActive(false);
            reload.gameObject.SetActive(false);
        }
        foreach (Graphic graphic in new[] { health,exp,badge,timer,soul!=null?soul.transform:null,weapon!=null?weapon.transform:null }
            .Where(t=>t!=null).SelectMany(t=>t.GetComponentsInChildren<Graphic>(true))) graphic.raycastTarget=false;
    }

    private static void Strip(Transform panel, params Graphic[] keep)
    {
        foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true))
        { graphic.enabled = keep.Contains(graphic); graphic.raycastTarget = false; }
    }
    private static void TextStyle(Text text,int size,TextAnchor alignment=TextAnchor.MiddleLeft)
    {
        text.enabled=true; text.fontSize=size; text.alignment=alignment; text.color=Color.white;
        text.raycastTarget=false; text.resizeTextForBestFit=false;
        text.horizontalOverflow=HorizontalWrapMode.Overflow;
        var outline=text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
        outline.effectColor=Color.black; outline.effectDistance=new Vector2(1,-1);
    }
    private static Text Feedback(Transform parent,string name,int width,int height)
    {
        RectTransform rect=FindOrCreate(parent,name);
        Place(rect,0,0,width,height,Vector2.one*0.5f,Vector2.one*0.5f,Vector2.zero);
        Text text=rect.GetComponent<Text>() ?? rect.gameObject.AddComponent<Text>();
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        TextStyle(text,9,name=="Cursor Ammo"?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter);
        return text;
    }
    private static RectTransform FindOrCreate(Transform parent,string name)
    {
        Transform existing=parent.Find(name);
        if(existing!=null) return (RectTransform)existing;
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false); return rect;
    }
    private static void Place(RectTransform r,float x,float y,float w,float h,Vector2? min=null,Vector2? max=null,Vector2? pivot=null)
    {
        r.anchorMin=min??new Vector2(0,1); r.anchorMax=max??r.anchorMin; r.pivot=pivot??new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,y); r.sizeDelta=new Vector2(w,h); r.localScale=Vector3.one;
    }
    private static void Stretch(RectTransform r)
    { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; r.localScale=Vector3.one; }
    private static void Bar(Image fill,Color color)
    { Stretch(fill.rectTransform); fill.sprite=white; fill.type=Image.Type.Filled; fill.fillMethod=Image.FillMethod.Horizontal; fill.fillOrigin=0; fill.color=color; fill.enabled=true; fill.raycastTarget=false; }
    private static Sprite White()
    {
        if (white!=null) return white;
        if(!File.Exists(WhitePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(WhitePath));
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white}); texture.Apply();
            File.WriteAllBytes(WhitePath,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(WhitePath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(WhitePath);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.mipmapEnabled=false; importer.filterMode=FilterMode.Point;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        return white=AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
    }
    [MenuItem("Tools/System Leveling/Refresh Small Cursor")]
    public static void PrepareCursor()
    {
        GameCursorSetup.Reset();
    }
    private static void Backup(string path)
    {
        string dest=Output+"/Before/"+path;
        if(File.Exists(dest)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(dest)); File.Copy(path,dest);
    }
    private static string Hash(string path)
    { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-",""); }
}
