#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CompactHudRegression : MonoBehaviour
{
    private const string Output="Captures/CompactHud";
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    private readonly List<string> checks=new List<string>();
    private readonly List<int> sizes=new List<int>();
    private int originalSize=-1;
    private bool background,cleaned;
    private Mouse mouse;
    private string sourceHash;
    private int souls;

    [MenuItem("Tools/System Leveling/Run Compact HUD Regression (Play Mode)")]
    public static void Begin()
    {
        if(!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if(FindAnyObjectByType<CompactHudRegression>()!=null) return;
        var go=new GameObject("Compact HUD Regression"); DontDestroyOnLoad(go);
        var runner=go.AddComponent<CompactHudRegression>();
        runner.background=Application.runInBackground; Application.runInBackground=true;
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private IEnumerator Start()
    {
        sourceHash=Hash("Assets/MyAssets/Other/Cursor/Cursor.png");
        souls=SoulWallet.Total;
        mouse=InputSystem.AddDevice<Mouse>();
        Directory.CreateDirectory(Output);
        IEnumerator run=Tests(); string failure="";
        while(true)
        {
            object current;
            try { if(!run.MoveNext()) break; current=run.Current; }
            catch(Exception error) { failure=error.ToString(); break; }
            yield return current;
        }
        Cleanup();
        File.WriteAllText(Output+"/runtime-regression.json",JsonUtility.ToJson(new Report
        { result=failure==""?"PASS":"FAIL",checks=checks.ToArray(),failure=failure },true));
        if(failure=="") Debug.Log($"COMPACT HUD REGRESSION PASS: {checks.Count} checks, both maps and aspect ratios.");
        else Debug.LogError("COMPACT HUD REGRESSION FAIL: "+failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        var cursor=PlayerSettings.defaultCursor;
        Check(cursor!=null && File.Exists(AssetDatabase.GetAssetPath(cursor)),"Cursor setting resolves to an existing asset, not a deleted generated texture");
        Check(cursor!=null && cursor.width==16 && cursor.height==16 && cursor.format==TextureFormat.RGBA32 &&
            PlayerSettings.cursorHotspot==new Vector2(8,8),"AC-002: cursor is exactly 16x16 RGBA32 with a centered hotspot");
        Check(((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(cursor))).textureType==TextureImporterType.Cursor && cursor.filterMode==FilterMode.Point,
            "Cursor uses the native import path and point sampling, not sliced Sprite rendering");
        // Compare original PNG pixels, not the cursor importer's transformed texture data.
        var original=new Texture2D(2,2); var reduced=new Texture2D(2,2);
        try
        {
            original.LoadImage(File.ReadAllBytes("Assets/MyAssets/Other/Cursor/Cursor.png"));
            reduced.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(cursor)));
            int scale=original.width/16;
            Check((scale==1 || scale==2) && reduced.GetPixels32().Where((pixel,index)=>
                pixel.Equals((Color32)original.GetPixel(index%16*scale,index/16*scale))).Count()==16*16,
                "Cursor stays 16x16 without padding, enlargement, or interpolated pixels");
        }
        finally { Destroy(original); Destroy(reduced); }

        foreach(string path in new[]{LoadoutLauncher.GameplayScene,LoadoutLauncher.SnowScene})
        {
            yield return SceneManager.LoadSceneAsync(path); yield return null;
            var player=PlayerController.Instance.GetComponent<PlayerStats>();
            Check(!player.IsDead,"Fresh player alive on "+path);
            player.GetComponent<PlayerController>().enabled=false;
            player.GetComponent<Rigidbody2D>().simulated=false;
            foreach(var spawner in FindObjectsByType<EnemySpawner>()) spawner.enabled=false;
            foreach(var enemy in FindObjectsByType<Enemy>()) enemy.enabled=false;
            var hud=FindAnyObjectByType<PlayerHud>(); var weaponHud=FindAnyObjectByType<WeaponHud>();
            var canvas=hud.GetComponent<Canvas>(); var holder=player.GetComponentInChildren<WeaponHolder>();
            var ammo=Read<Text>(weaponHud,"label"); var pointer=Read<Text>(weaponHud,"cursorAmmo");
            var reload=Read<Text>(weaponHud,"reloadIndicator");
            var soul=hud.GetComponentInChildren<SoulBalanceView>();
            Check(weaponHud.Compact && Read<PlayerStats>(weaponHud,"player")==player && Read<PlayerStats>(hud,"player")==player,
                "AC-005: current loadout player bindings intact on "+path);
            Check(soul!=null && soul.amount.text==FindAnyObjectByType<GameOverController>().SoulEarned.ToString("N0"),
                "AC-001: compact Soul retains per-run value");
            var health=Read<Image>(hud,"healthFill"); var exp=Read<Image>(hud,"experienceFill");
            var healthPanel=(RectTransform)hud.transform.Find("HUD/Health Panel");
            var experiencePanel=(RectTransform)hud.transform.Find("Experience Panel");
            Check(healthPanel.sizeDelta==new Vector2(240,18) && healthPanel.anchoredPosition==new Vector2(8,-8),
                "AC-001: short readable HP top-left, not full-width top strip");
            Check(experiencePanel.anchorMin==Vector2.zero && experiencePanel.anchorMax==Vector2.right &&
                experiencePanel.anchoredPosition==Vector2.zero && experiencePanel.sizeDelta==new Vector2(0,6),
                "AC-001: EXP stretches flush along bottom");
            Check(!healthPanel.GetComponent<Image>().enabled && !experiencePanel.GetComponent<Image>().enabled &&
                !weaponHud.GetComponent<Image>().enabled && !soul.GetComponent<Image>().enabled,
                "AC-001: oversized opaque panels removed");
            var hpText=Read<Text>(hud,"healthText");
            Check(hpText.transform.GetSiblingIndex()==healthPanel.childCount-1 && hpText.enabled,
                "AC-001: HP value drawn over the fill, not hidden behind it");
            player.TakeDamage(1); hud.Refresh();
            Check(Mathf.Approximately(health.fillAmount,player.currentHealth/player.MaxHealth),"AC-001: real HP changes update bar");
            player.IncreaseExperience(1); hud.Refresh();
            Check(Mathf.Approximately(exp.fillAmount,(float)player.experience/player.experienceCap),"AC-001: real EXP changes update bar");
            Check(hud.transform.Find("HUD/Run Timer/Time Value").GetComponent<Text>().text.Contains(":"),"AC-001: survival timer preserved");
            var encounter=FindAnyObjectByType<RotwoodEncounter>();
            if(encounter!=null)
            {
                Check(encounter.SpawnNow(),"AC-005: boss can still spawn with compact HUD");
                encounter.Boss.enabled=false;
            }

            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1024,768)})
            {
                SetResolution(size.x,size.y); yield return null; yield return null;
                Canvas.ForceUpdateCanvases();
                if(encounter!=null)
                {
                    var bossBar=Read<GameObject>(encounter,"hud").GetComponent<RectTransform>();
                    Check(!Bounds(bossBar).Overlaps(Bounds(healthPanel)) &&
                        !Bounds(bossBar).Overlaps(Bounds((RectTransform)weaponHud.transform)),"AC-005: boss bar does not overlap compact stats");
                }
                foreach(string name in new[]{"MagicBook","Bow","Crossbow"})
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Weapons/"+name+".prefab");
                    Check(holder.Equip(prefab),"Equips "+name);
                    yield return null;
                    var ranged=holder.CurrentWeapon as RangedWeaponController;
                    Check(ranged!=null && ranged.State!=null,"Live ranged state initialized: "+name);
                    ranged.enabled=false;
                    RangedWeaponState state=ranged.State;
                    Vector2 center=new Vector2(Screen.width*0.55f,Screen.height*0.5f);
                    Pointer(center); weaponHud.SendMessage("LateUpdate");
                    Check(ammo.text==$"{state.Ammo} / {state.Capacity}" && pointer.text==$"{state.Ammo}/{state.Capacity}",
                        "AC-003: header and cursor counts match "+name);
                    Check(pointer.gameObject.activeSelf && !reload.gameObject.activeSelf && pointer.fontSize*canvas.scaleFactor<16,
                        "AC-003: ammo text is visible and smaller than cursor");
                    foreach(Vector2 edge in new[]{Vector2.zero,new Vector2(Screen.width,0),new Vector2(0,Screen.height),new Vector2(Screen.width,Screen.height)})
                    {
                        Pointer(edge); weaponHud.SendMessage("LateUpdate");
                        Check(Inside(pointer.rectTransform),"AC-003: cursor count clamped to viewport at "+edge);
                    }
                    Pointer(center); weaponHud.SendMessage("LateUpdate");
                    // Equipping/UI interruption requires release before the next valid trigger.
                    state.Tick(0,false);
                    state.Tick(0,true);
                    if(name=="Crossbow")
                    {
                        state.Tick(0.2f,true); weaponHud.SendMessage("LateUpdate");
                        Check(state.IsCharging && !state.IsReloading && reload.gameObject.activeSelf && reload.color==new Color(1f,0.72f,0.2f),"Crossbow charge displays the amber bracket progress bar");
                        string earlyCharge=reload.text;
                        state.Tick(state.ChargeDuration*0.5f-state.ChargeDuration*state.ChargeFraction,true); weaponHud.SendMessage("LateUpdate");
                        Check(reload.text!=earlyCharge && Mathf.Approximately(state.ChargeFraction,0.5f),"Charge marker follows actual half-charge progress");
                        var pauseMenu=FindAnyObjectByType<RunMenu>(); int firedBeforePause=ranged.ShotsFired;
                        pauseMenu.OpenPause(); ranged.Tick(10,true); weaponHud.SendMessage("LateUpdate");
                        Check(!state.IsCharging && !reload.gameObject.activeSelf && ranged.ShotsFired==firedBeforePause,"Pause cancels crossbow charge and hides its bar without a ghost shot");
                        pauseMenu.Continue(); yield return null; yield return null;
                        state.Tick(0,false); state.Tick(0,true); state.Tick(state.ChargeDuration*0.5f,true); weaponHud.SendMessage("LateUpdate");
                        Check(state.IsCharging && reload.gameObject.activeSelf,"A fresh crossbow charge shows feedback after resuming");
                        yield return new WaitForEndOfFrame();
                        Capture((path==LoadoutLauncher.GameplayScene?"grass":"snow")+"-crossbow-charge-"+size.x+"x"+size.y+".png");
                        state.Tick(state.ChargeDuration,true); weaponHud.SendMessage("LateUpdate");
                        Check(state.ChargeFraction==1f && reload.text.IndexOf('|')==22,"Fully charged crossbow keeps its marker at the end of the bar");
                        state.Tick(0,false);
                    }
                    else if(name=="Bow") state.Tick(ranged.weaponData.BowRecoveryDelay+state.ReloadDuration*0.5f,false);
                    else state.Tick(state.ReloadDuration*0.5f,false);
                    weaponHud.SendMessage("LateUpdate");
                    Check(state.IsReloading && reload.gameObject.activeSelf && reload.color==Color.white && reload.text.StartsWith("[") && reload.text.EndsWith("]") && reload.text.Contains("|"),
                        "AC-004: bracket reload only during real reload: "+name);
                    string before=reload.text;
                    state.Tick(state.ReloadDuration*0.2f,false); weaponHud.SendMessage("LateUpdate");
                    Check(reload.text!=before,"AC-004: reload marker follows actual progress: "+name);
                    var p=reload.rectTransform.position;
                    player.transform.position+=Vector3.right*0.5f;
                    weaponHud.SendMessage("LateUpdate");
                    Check(reload.rectTransform.position.x>p.x && reload.rectTransform.position.y>Camera.main.WorldToScreenPoint(player.transform.position).y,
                        "AC-004: reload follows above the character");
                    Check(Inside(reload.rectTransform),"AC-004: reload indicator inside viewport");
                    if(name=="MagicBook")
                    {
                        yield return new WaitForEndOfFrame();
                        Capture((path==LoadoutLauncher.GameplayScene?"grass":"snow")+"-reload-"+size.x+"x"+size.y+".png");
                    }
                    var menu=FindAnyObjectByType<RunMenu>();
                    float pausedFraction=state.ReloadFraction;
                    menu.OpenPause(); ranged.Tick(10,false); weaponHud.SendMessage("LateUpdate");
                    Check(menu.IsOpen && state.IsReloading && Mathf.Approximately(state.ReloadFraction,pausedFraction) &&
                        !pointer.gameObject.activeSelf && !reload.gameObject.activeSelf,"AC-003/004: pause freezes real reload and hides feedback");
                    menu.OpenSettings(); weaponHud.SendMessage("LateUpdate");
                    Check(!pointer.gameObject.activeSelf && !reload.gameObject.activeSelf,"AC-003/004: settings hides combat feedback");
                    menu.CloseSettings(); menu.Continue(); yield return null; yield return null;
                    Pointer(center); weaponHud.SendMessage("LateUpdate");
                    Check(pointer.gameObject.activeSelf && reload.gameObject.activeSelf,"AC-003/004: real reload feedback resumes after pause");
                    state.Tick(state.ReloadDuration+1,false); weaponHud.SendMessage("LateUpdate");
                    Check(!state.IsReloading && !reload.gameObject.activeSelf,"AC-004: reload hides on completion");
                    if(name!="Crossbow")
                    {
                        state.SetUpgrades(3,0); weaponHud.SendMessage("LateUpdate");
                        Check(pointer.text==$"{state.Ammo}/{state.Capacity}","AC-003: upgraded capacity reflected without changing weapon data");
                    }
                }
                Check(holder.Equip(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Weapons/Sword.prefab")),"Equips melee");
                yield return null; weaponHud.SendMessage("LateUpdate");
                Check(ammo.text=="MELEE" && !pointer.gameObject.activeSelf && !reload.gameObject.activeSelf,"AC-003/004: melee hides ammo/reload feedback");
                Check(Inside(healthPanel)&&Inside(experiencePanel),"AC-001: both bar bounds valid at "+size);
                var statGraphics=new[]{healthPanel,(RectTransform)weaponHud.transform,(RectTransform)soul.transform}
                    .SelectMany(t=>t.GetComponentsInChildren<Graphic>(true));
                Check(statGraphics.All(g=>!g.raycastTarget) && !pointer.raycastTarget && !reload.raycastTarget,"AC-003: HUD never intercepts shooting input");
            }
            // Real modal/restart flow without changing persistent Soul.
            holder.Equip(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Weapons/MagicBook.prefab"));
            yield return null;
            var gun=(RangedWeaponController)holder.CurrentWeapon; gun.enabled=false;
            Pointer(new Vector2(Screen.width/2f,Screen.height/2f));
            player.IncreaseExperience(player.experienceCap-player.experience);
            weaponHud.SendMessage("LateUpdate");
            var upgrades=FindAnyObjectByType<LevelUpSelection>();
            Check(upgrades.IsOpen&&!pointer.gameObject.activeSelf&&!reload.gameObject.activeSelf,"AC-005: level-up remains usable and hides feedback");
            var buttons=Read<Button[]>(upgrades,"buttons");
            Check(buttons.All(b=>b.gameObject.activeInHierarchy && b.interactable),"AC-005: level-up buttons remain enabled");
            player.Kill(); weaponHud.SendMessage("LateUpdate");
            var result=FindAnyObjectByType<GameOverController>();
            Check(result.IsGameOver&&!pointer.gameObject.activeSelf&&!reload.gameObject.activeSelf,"AC-005: death keeps dialogs and hides feedback");
            result.Restart();
            float timeout=Time.realtimeSinceStartup+15;
            while(FindAnyObjectByType<GameOverController>()==result&&Time.realtimeSinceStartup<timeout) yield return null;
            yield return null;
            Check(!FindAnyObjectByType<GameOverController>().IsGameOver&&FindAnyObjectByType<WeaponHud>().Compact,"AC-005: restart retains compact HUD with fresh gameplay");
        }
        Check(SoulWallet.Total==souls,"AC-005: tests do not change saved Soul balance");
        Check(Hash("Assets/MyAssets/Other/Cursor/Cursor.png")==sourceHash,"AC-002: original cursor PNG unchanged");
    }

    private void Pointer(Vector2 position)
    { InputSystem.QueueStateEvent(mouse,new MouseState{position=position}); InputSystem.Update(); }
    private void Check(bool pass,string label) { if(!pass) throw new InvalidOperationException(label); checks.Add(label); }
    private static T Read<T>(object target,string field)=>(T)target.GetType().GetField(field,Flags).GetValue(target);
    private static bool Inside(RectTransform rect)
    {
        var points=new Vector3[4]; rect.GetWorldCorners(points);
        return points.All(p=>p.x>=-0.5f&&p.y>=-0.5f&&p.x<=Screen.width+0.5f&&p.y<=Screen.height+0.5f);
    }
    private static Rect Bounds(RectTransform rect)
    { var p=new Vector3[4]; rect.GetWorldCorners(p); return Rect.MinMaxRect(p[0].x,p[0].y,p[2].x,p[2].y); }
    private static string Hash(string path)
    { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))); }
    private static void Capture(string name)
    { var texture=ScreenCapture.CaptureScreenshotAsTexture(); try{File.WriteAllBytes(Output+"/"+name,texture.EncodeToPNG());}finally{Destroy(texture);} }
    private void SetResolution(int width,int height)
    {
        Assembly assembly=typeof(Editor).Assembly; Type type=assembly.GetType("UnityEditor.GameViewSizes");
        object collection=typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
        object group=type.GetProperty("currentGroup",Flags).GetValue(collection);
        EditorWindow window=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        PropertyInfo property=window.GetType().GetProperty("selectedSizeIndex",Flags);
        if(originalSize<0) originalSize=(int)property.GetValue(window);
        int index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        object size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{
            Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,height,"Compact HUD temporary"});
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size}); sizes.Add(index);
        property.SetValue(window,index); window.Show(); window.Focus(); window.Repaint();
    }
    private void Cleanup()
    {
        if(cleaned) return; cleaned=true;
        if(mouse!=null) InputSystem.RemoveDevice(mouse);
        Application.runInBackground=background;
        if(originalSize<0) return;
        Assembly assembly=typeof(Editor).Assembly;
        EditorWindow window=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        window.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(window,originalSize);
        Type type=assembly.GetType("UnityEditor.GameViewSizes");
        object collection=typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
        object group=type.GetProperty("currentGroup",Flags).GetValue(collection);
        for(int i=sizes.Count-1;i>=0;i--) group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{sizes[i]});
    }
    private void OnDestroy()=>Cleanup();
    [Serializable] private class Report { public string result; public string[] checks; public string failure; }
}
#endif
