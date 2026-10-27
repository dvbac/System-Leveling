using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PlayableCharacterBuilder
{
    private const string Art = "Assets/MyAssets/Character/Playable";
    private const string Prefabs = "Assets/Prefab/Characters";
    private const string Data = "Assets/Scriptable Object/Character";
    private static readonly string[] Names = { "Knight", "Archer", "Hunter" };
    private static readonly float[] Health = { 120f, 80f, 100f };
    private static readonly float[] Recovery = { 0.4f, 0.15f, 0.25f };
    private static readonly float[] Speed = { 2.8f, 3.6f, 3.2f };
    private static readonly float[] Magnet = { 1.4f, 1.8f, 2f };

    [MenuItem("Tools/System Leveling/Build Playable Characters")]
    public static void Build()
    {
        Require(!Application.isPlaying, "Build characters outside Play Mode.");
        PlayerController template = Object.FindAnyObjectByType<PlayerController>();
        Require(template != null && template.GetComponent<PlayerStats>() != null, "Open the gameplay scene with its configured Player first.");
        foreach (string name in Names)
        {
            Require(File.Exists($"{Art}/{name}/Idle.png") && File.Exists($"{Art}/{name}/Walk.png"), "Missing copied sprites for " + name);
            Require(!File.Exists($"{Prefabs}/{name}.prefab") && !File.Exists($"{Data}/{name}.asset") &&
                !File.Exists($"{Art}/{name}/{name}.controller") && !File.Exists($"{Art}/{name}/Idle.anim") &&
                !File.Exists($"{Art}/{name}/Walk.anim"), "Character output already exists; refusing to overwrite edits: " + name);
        }
        Directory.CreateDirectory(Prefabs);
        AssetDatabase.Refresh();
        for (int i = 0; i < Names.Length; i++)
        {
            string name = Names[i];
            AnimationClip idle = Clip(name, "Idle", 8);
            AnimationClip walk = Clip(name, "Walk", 10);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath($"{Art}/{name}/{name}.controller");
            controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Idle"); idleState.motion = idle;
            AnimatorState walkState = machine.AddState("Walk"); walkState.motion = walk;
            machine.defaultState = idleState;
            Transition(idleState, walkState, AnimatorConditionMode.If);
            Transition(walkState, idleState, AnimatorConditionMode.IfNot);

            CharacterScriptableObject data = ScriptableObject.CreateInstance<CharacterScriptableObject>();
            SerializedObject values = new SerializedObject(data);
            values.FindProperty("maxHealth").floatValue = Health[i];
            values.FindProperty("recovery").floatValue = Recovery[i];
            values.FindProperty("moveSpeed").floatValue = Speed[i];
            values.FindProperty("magnet").floatValue = Magnet[i];
            // These legacy fields are stored but are not consumed by the current combat code.
            values.FindProperty("might").floatValue = 4f;
            values.FindProperty("projectileSpeed").floatValue = 2f;
            values.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(data, $"{Data}/{name}.asset");

            GameObject instance = Object.Instantiate(template.gameObject);
            try
            {
                instance.name = name;
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;
                instance.GetComponent<PlayerStats>().characterData = data;
                SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                renderer.sprite = Frames(name, "Idle")[0]; renderer.color = Color.white; renderer.flipX = false;
                Animator animator = instance.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                CapsuleCollider2D body = instance.GetComponent<CapsuleCollider2D>();
                body.size = new Vector2(0.36f, 0.7f); body.offset = new Vector2(0f, -0.04f);
                // Keep the existing input, collector, weapon hierarchy and level-up settings intact.
                Require(PrefabUtility.SaveAsPrefabAsset(instance, $"{Prefabs}/{name}.prefab") != null, "Prefab save failed: " + name);
            }
            finally { Object.DestroyImmediate(instance); }
            AssetDatabase.SaveAssetIfDirty(data);
            AssetDatabase.SaveAssetIfDirty(controller);
        }
        Validate();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/Knight.prefab");
        Debug.Log("PLAYABLE CHARACTERS BUILT: Knight, Archer, Hunter. Current scene player and existing data unchanged.");
    }

    private static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode condition)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false; transition.duration = 0;
        transition.AddCondition(condition, 0, "isMoving");
    }

    private static AnimationClip Clip(string character, string state, float fps)
    {
        string path = $"{Art}/{character}/{state}.png";
        AssetDatabase.ImportAsset(path);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Require(texture != null && texture.width == texture.height * 8, "Expected 8 square horizontal frames: " + path);
        int cell = texture.height;
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        // Every sheet has a one-world-unit canvas, including the larger Hunter source art.
        importer.spritePixelsPerUnit = cell;
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true; importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        SpriteDataProviderFactories factory = new SpriteDataProviderFactories(); factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        SpriteRect[] rects = Enumerable.Range(0, 8).Select(index => new SpriteRect
        {
            name = state + "_" + index, rect = new Rect(index * cell, 0, cell, cell),
            alignment = SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f), spriteID = GUID.Generate()
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        Sprite[] frames = Frames(character, state);
        Require(frames.Length == 8, "Slice failed: " + path);
        AnimationClip clip = new AnimationClip { name = state, frameRate = fps };
        ObjectReferenceKeyframe[] keys = Enumerable.Range(0, 9).Select(index => new ObjectReferenceKeyframe
        { time = index / fps, value = frames[index % 8] }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, $"{Art}/{character}/{state}.anim");
        return clip;
    }

    private static Sprite[] Frames(string character, string state) => AssetDatabase.LoadAllAssetsAtPath($"{Art}/{character}/{state}.png")
        .OfType<Sprite>().OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();

    [MenuItem("Tools/System Leveling/Validate Playable Characters")]
    public static void Validate()
    {
        int checks = 0;
        for (int i = 0; i < Names.Length; i++)
        {
            string name = Names[i];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{name}.prefab");
            Require(prefab != null && prefab.CompareTag("Player") && prefab.GetComponent<PlayerController>() != null, "Invalid player prefab: " + name); checks++;
            CharacterScriptableObject data = prefab.GetComponent<PlayerStats>().characterData;
            Require(data != null && data.MaxHealth == Health[i] && data.Recovery == Recovery[i] && data.MoveSpeed == Speed[i] && data.Magnet == Magnet[i], "Invalid stats: " + name); checks++;
            Require(prefab.GetComponent<PlayerStats>().levelRanges.Count > 0 && prefab.GetComponent<Rigidbody2D>().gravityScale == 0 && prefab.GetComponentInChildren<PlayerCollector>() != null && prefab.GetComponentInChildren<WeaponHolder>() != null, "Missing gameplay configuration: " + name); checks++;
            SerializedObject input = new SerializedObject(prefab.GetComponent<PlayerController>());
            Require(input.FindProperty("move").objectReferenceValue != null && input.FindProperty("pointerPosition").objectReferenceValue != null, "Missing input: " + name); checks++;
            AnimatorController controller = prefab.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
            Require(controller != null && controller.parameters.Any(parameter => parameter.name == "isMoving" && parameter.type == AnimatorControllerParameterType.Bool), "Missing animation parameter: " + name); checks++;
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            Require(machine.defaultState.name == "Idle" && machine.states.Length == 2 && machine.states.All(state => state.state.transitions.Length == 1 && !state.state.transitions[0].hasExitTime && state.state.transitions[0].conditions[0].parameter == "isMoving"), "Broken transitions: " + name); checks++;
            foreach (string state in new[] { "Idle", "Walk" })
            {
                Sprite[] frames = Frames(name, state);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Art}/{name}/{state}.anim");
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" });
                Require(frames.Length == 8 && frames.All(frame => Mathf.Approximately(frame.rect.height / frame.pixelsPerUnit, 1f)) && keys.Length == 9 && keys.All(key => key.value != null) && AnimationUtility.GetAnimationClipSettings(clip).loopTime, "Broken clip: " + name + "/" + state); checks++;
            }
            GameObject preview = new GameObject("Character animation validation", typeof(SpriteRenderer), typeof(Animator));
            preview.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Animator animator = preview.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0f);
                Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Idle did not start: " + name); checks++;
                Sprite initial = preview.GetComponent<SpriteRenderer>().sprite;
                animator.Update(0.14f);
                Require(initial != null && preview.GetComponent<SpriteRenderer>().sprite != initial, "Idle sprites did not advance: " + name); checks++;
                animator.SetBool("isMoving", true); animator.Update(0.01f); animator.Update(0.01f);
                Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"), "Idle to Walk did not transition: " + name); checks++;
                initial = preview.GetComponent<SpriteRenderer>().sprite;
                animator.Update(0.14f);
                Require(initial != null && preview.GetComponent<SpriteRenderer>().sprite != initial, "Walk sprites did not advance: " + name); checks++;
                animator.SetBool("isMoving", false); animator.Update(0.01f); animator.Update(0.01f);
                Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Walk to Idle did not transition: " + name); checks++;
            }
            finally { Object.DestroyImmediate(preview); }
        }
        Directory.CreateDirectory("Captures/PlayableCharacters");
        File.WriteAllText("Captures/PlayableCharacters/asset-validation.json", "{\"result\":\"PASS\",\"checks\":" + checks + ",\"characters\":[\"Knight\",\"Archer\",\"Hunter\"]}");
        Debug.Log("PLAYABLE CHARACTER VALIDATION PASS: " + checks + " checks.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
