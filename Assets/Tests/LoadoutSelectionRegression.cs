#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LoadoutSelectionRegression : MonoBehaviour
{
    private const string Output = "Captures/LoadoutSelection";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<int> temporarySizes = new List<int>();
    private int originalSize = -1;
    private bool background, cleaned;
    private readonly List<string> checks = new List<string>();
    private LoadoutLauncher launcher;
    private bool launchEnabled;

    [MenuItem("Tools/System Leveling/Capture and Test Loadout Selection")]
    public static void Run()
    {
        if (!Application.isPlaying || FindAnyObjectByType<LoadoutSelection>() == null)
            throw new InvalidOperationException("Enter Play Mode in LoadoutSelection first.");
        if (FindAnyObjectByType<LoadoutSelectionRegression>() != null)
            throw new InvalidOperationException("Selection regression is already running.");
        new GameObject("Loadout Regression").AddComponent<LoadoutSelectionRegression>();
    }

    private IEnumerator Start()
    {
        background = Application.runInBackground;
        Application.runInBackground = true;
        Directory.CreateDirectory(Output);
        IEnumerator run = Check();
        string failure = null;
        while (true)
        {
            object current;
            try { if (!run.MoveNext()) break; current = run.Current; }
            catch (Exception error) { failure = error.ToString(); break; }
            yield return current;
        }
        Cleanup();
        File.WriteAllText(Output + "/regression.json", JsonUtility.ToJson(new Report { result = failure == null ? "PASS" : "FAIL", checks = checks.ToArray(), failure = failure }, true));
        if (failure == null) Debug.Log("LOADOUT VISUAL REGRESSION PASS: 16:9 and 4:3, real UI raycasts, navigation and captures.");
        else Debug.LogError("LOADOUT VISUAL REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Check()
    {
        LoadoutSelection ui = FindAnyObjectByType<LoadoutSelection>();
        launcher = ui.GetComponent<LoadoutLauncher>();
        launchEnabled = launcher != null && launcher.enabled;
        if (launcher != null) launcher.enabled = false;
        GameObject[] originalCharacters = ui.characterPrefabs;
        GameObject[] originalWeapons = ui.weaponPrefabs;
        Button previous = ui.characterView.cards[0];
        GameObject[] shortCharacters = { originalCharacters[0], originalCharacters[originalCharacters.Length - 1] };
        GameObject[] shortWeapons = { originalWeapons[0], originalWeapons[originalWeapons.Length - 1], originalWeapons[0] };
        ui.SetPrefabs(shortCharacters, shortWeapons);
        if (ui.characterView.cards.Length != 2 || ui.weaponView.cards.Length != 3 || ui.characterView.options.childCount != 2 || ui.weaponView.options.childCount != 3 ||
            previous != null && previous.gameObject.activeInHierarchy)
            throw new Exception("SetPrefabs must replace, not append, both generated lists.");
        foreach (GameObject[] invalid in new[] { null, Array.Empty<GameObject>(), new GameObject[] { null }, new[] { originalWeapons[0] } })
        {
            bool rejected = false;
            try { ui.SetPrefabs(invalid, shortWeapons); }
            catch (ArgumentException) { rejected = true; }
            if (!rejected || ui.characterView.cards.Length != 2 || ui.weaponView.cards.Length != 3)
                throw new Exception("Invalid prefab input must preserve the working catalog.");
        }
        bool invalidWeaponRejected = false;
        try { ui.SetPrefabs(shortCharacters, new[] { originalCharacters[0] }); }
        catch (ArgumentException) { invalidWeaponRejected = true; }
        if (!invalidWeaponRejected) throw new Exception("A character cannot be listed as a weapon.");
        GameObject fixture = new GameObject("Scene object catalog rejection"); fixture.SetActive(false);
        try
        {
            GameObject sceneCharacter = Instantiate(originalCharacters[0], fixture.transform);
            GameObject sceneWeapon = Instantiate(originalWeapons[0], fixture.transform);
            bool rejectedCharacter = false, rejectedWeapon = false;
            try { ui.SetPrefabs(new[] { sceneCharacter }, shortWeapons); } catch (ArgumentException) { rejectedCharacter = true; }
            try { ui.SetPrefabs(shortCharacters, new[] { sceneWeapon }); } catch (ArgumentException) { rejectedWeapon = true; }
            if (!rejectedCharacter || !rejectedWeapon || ui.characterView.cards.Length != 2 || ui.weaponView.cards.Length != 3)
                throw new Exception("Scene instances must be rejected before they can be destroyed by scene loading.");
        }
        finally { Destroy(fixture); }
        ui.SetPrefabs(Enumerable.Repeat(originalCharacters[0], 19).ToArray(), shortWeapons);
        yield return null;
        yield return new WaitForEndOfFrame();
        ui.characterView.cards[18].Select();
        Canvas.ForceUpdateCanvases();
        ScrollRect scroll = ui.characterView.options.GetComponentInParent<ScrollRect>();
        if (!scroll.vertical || scroll.content.rect.height <= scroll.viewport.rect.height || scroll.verticalNormalizedPosition > 0.01f)
            throw new Exception("Long catalogs must scroll the focused item into view.");
        Vector2 lastCenter = RectTransformUtility.WorldToScreenPoint(null, ui.characterView.cards[18].transform.TransformPoint(ui.characterView.cards[18].GetComponent<RectTransform>().rect.center));
        if (!RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, lastCenter)) throw new Exception("Focused grid item is still outside the viewport.");
        checks.Add("SetPrefabs: different counts, rebuild without duplicates, invalid input and scene instances rejected, 19-item vertical grid scrolling");
        ui.SetPrefabs(originalCharacters, originalWeapons);
        yield return null;
        EditorApplication.ExecuteMenuItem("Tools/System Leveling/Validate Loadout Selection");
        int characterIndex = Mathf.Min(1, originalCharacters.Length - 1);
        int weaponIndex = Mathf.Min(1, originalWeapons.Length - 1);
        foreach (Vector2Int size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1024, 768) })
        {
            SetResolution(size.x, size.y);
            ui.Back();
            int oldCharacter = ui.CharacterIndex;
            ExecuteEvents.Execute(ui.characterView.cards[characterIndex].gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            if (ui.CharacterIndex != oldCharacter || ui.ChoosingWeapon) throw new Exception("Hover selected a character.");
            yield return null;
            yield return new WaitForEndOfFrame();
            Verify(ui.characterView);
            // Canvas scaling after a Game View resize must reach the next rendered frame before capture.
            yield return null;
            yield return new WaitForEndOfFrame();
            Capture("characters-" + size.x + "x" + size.y + ".png");
            // Use the real pointer event handler, not only the onClick delegate.
            Click(ui.characterView.cards[characterIndex]);
            if (!ui.ChoosingWeapon || ui.CharacterIndex != characterIndex) throw new Exception("Character click did not select and advance.");
            ExecuteEvents.Execute(ui.weaponView.cards[weaponIndex].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.selectHandler);
            if (ui.Confirmed) throw new Exception("Keyboard focus selected a weapon.");
            yield return null;
            yield return new WaitForEndOfFrame();
            Verify(ui.weaponView, ui.back);
            ExecuteEvents.Execute(ui.weaponView.cards[weaponIndex].gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            if (!ui.Confirmed || ui.WeaponIndex != weaponIndex) throw new Exception("Submitting weapon did not confirm immediately.");
            yield return null;
            yield return new WaitForEndOfFrame();
            Capture("weapons-" + size.x + "x" + size.y + ".png");
            checks.Add(size + ": compact circles visible; hover/focus only preview; character click advances; weapon submit confirms; raycasts and text fit");
        }
        ui.SelectCharacter(0); ui.SelectWeapon(0); ui.Back();
    }

    private static void Click(Button button) => ExecuteEvents.Execute(button.gameObject,
        new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);

    private static void Verify(LoadoutSelection.View view, params Button[] navigation)
    {
        Canvas.ForceUpdateCanvases();
        GridLayoutGroup grid = view.options.GetComponent<GridLayoutGroup>();
        if (grid == null || grid.constraintCount != 6 || grid.cellSize != new Vector2(96, 96))
            throw new Exception("Reference layout needs six columns of 96-unit icons.");
        if (view.stats.rectTransform.anchoredPosition.x < 900 || view.statValues.rectTransform.anchoredPosition.x <= view.stats.rectTransform.anchoredPosition.x)
            throw new Exception("Stats must occupy the right-hand portrait region.");
        var buttons = new List<Button>(view.cards); buttons.AddRange(navigation);
        foreach (Button button in buttons)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != button)
                throw new Exception("Pointer blocked: " + button.name);
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (Vector3 p in corners)
                if (p.x < 0 || p.x > Screen.width || p.y < 0 || p.y > Screen.height)
                    throw new Exception("Button outside viewport: " + button.name);
        }
        foreach (Text label in view.root.GetComponentsInChildren<Text>())
            if (label.preferredHeight > label.rectTransform.rect.height + 1)
                throw new Exception("Text is clipped: " + label.name + " / " + label.text);
    }

    private void SetResolution(int width, int height)
    {
        Assembly assembly = typeof(Editor).Assembly;
        Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        object group = sizesType.GetProperty("currentGroup", Flags).GetValue(sizes);
        EditorWindow window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        PropertyInfo property = window.GetType().GetProperty("selectedSizeIndex", Flags);
        if (originalSize < 0) originalSize = (int)property.GetValue(window);
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        object size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), new object[]
            { Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), width, height, "Loadout temporary" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        temporarySizes.Add(index); property.SetValue(window, index);
        window.Show(); window.Focus(); window.Repaint();
    }

    private static void Capture(string filename)
    {
        Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(Output + "/" + filename, texture.EncodeToPNG()); }
        finally { Destroy(texture); }
    }

    private void Cleanup()
    {
        if (cleaned) return;
        cleaned = true; Application.runInBackground = background;
        if (launcher != null) launcher.enabled = launchEnabled;
        if (originalSize < 0) return;
        Assembly assembly = typeof(Editor).Assembly;
        EditorWindow window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        window.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(window, originalSize);
        Type type = assembly.GetType("UnityEditor.GameViewSizes");
        object sizes = typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
        object group = type.GetProperty("currentGroup", Flags).GetValue(sizes);
        for (int i = temporarySizes.Count - 1; i >= 0; i--)
            group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { temporarySizes[i] });
    }

    private void OnDestroy() => Cleanup();
    [Serializable] private class Report { public string result; public string[] checks; public string failure; }
}
#endif
