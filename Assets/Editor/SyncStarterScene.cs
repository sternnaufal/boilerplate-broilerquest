using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SyncStarterScene
{
    // ── Scene paths ────────────────────────────────────────────────────────
    private static readonly string[] LevelScenePaths = new[]
    {
        "Assets/Scenes/Starter.unity",
        "Assets/Scenes/Beginner.unity",
        "Assets/Scenes/Intermediate.unity"
    };

    // ── Prefab paths ───────────────────────────────────────────────────────
    private const string NotifikasiPrefabPath     = "Assets/Prefab/Notifikasi.prefab";
    private const string UIAlertPanelPrefabPath   = "Assets/Prefab/UIAlertPanel.prefab";
    private const string ExitButtonPrefabPath     = "Assets/Prefab/ExitButton.prefab";

    // ═══════════════════════════════════════════════════════════════════════
    //  TOOL: Sync Starter Scene to B/I structure
    //  ⚠ Neutered – no longer destroys UIAlertPanel, Notifikasi, or EXIT.
    // ═══════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Sync Starter Scene to B/I Structure")]
    public static void Sync()
    {
        const string starterPath = "Assets/Scenes/Starter.unity";
        const string templatePath = "Assets/Scenes/Intermediate.unity";

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // Backup
        string backupPath = starterPath.Replace(".unity", "_Backup.unity");
        AssetDatabase.CopyAsset(starterPath, backupPath);
        Debug.Log($"Backup: {backupPath}");

        // --- Collect template objects ---
        var templateScene = EditorSceneManager.OpenScene(templatePath, OpenSceneMode.Single);
        var tplCanvas = GameObject.Find("StarterCanvas");
        var tplSystems = GameObject.Find("StarterSystems");

        var hpBg = tplCanvas?.transform.Find("Hp");
        var hpPanel = GameObject.Find("HPPanel");

        var coopMgr = tplCanvas?.transform.Find("CoopStatusManager");
        Transform extraRow = null;
        if (coopMgr != null)
        {
            var rows = coopMgr.GetComponentsInChildren<Transform>(true);
            bool first = true;
            foreach (var r in rows)
            {
                if (r.name != "RowContainer" || r == coopMgr) continue;
                if (first) { first = false; continue; }
                extraRow = r; break;
            }
        }

        // --- Open Starter ---
        var starterScene = EditorSceneManager.OpenScene(starterPath, OpenSceneMode.Single);
        var canvas = GameObject.Find("StarterCanvas");

        // ╔════════════════════════════════════════════════════════════════╗
        // ║  NEUTERED: The following destructive operations have been     ║
        // ║  removed to preserve UI elements (Notifikasi, UIAlertPanel,   ║
        // ║  EXIT button, StarterSceneInitializer).                       ║
        // ╚════════════════════════════════════════════════════════════════╝

        // 1. [REMOVED] No longer destroys Notifikasi
        // 2. [REMOVED] No longer destroys UIAlertPanel
        // 3. [REMOVED] No longer destroys StarterSceneInitializer
        // 4. [REMOVED] No longer destroys duplicate StarterGameplayUI
        // 5. [REMOVED] No longer destroys EXIT from PausePanel

        // --- Add "Hp" background (if missing) ---
        if (hpBg != null && canvas != null)
        {
            var existingHp = canvas.transform.Find("Hp");
            if (existingHp == null)
            {
                var clone = Object.Instantiate(hpBg.gameObject, canvas.transform);
                clone.name = "Hp";
            }
        }

        // --- Add extra RowContainer (if missing) ---
        if (extraRow != null && canvas != null)
        {
            var starterCoop = canvas.transform.Find("CoopStatusManager");
            if (starterCoop != null)
            {
                var existing = starterCoop.Find("RowContainer");
                if (existing == null)
                {
                    var rowClone = Object.Instantiate(extraRow.gameObject, starterCoop);
                    rowClone.name = "RowContainer";
                }
            }
        }

        // --- Unpack & fix HPPanel ---
        var starterHp = GameObject.Find("HPPanel");
        if (starterHp != null)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(starterHp))
            {
                PrefabUtility.UnpackPrefabInstance(starterHp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            if (hpPanel != null)
            {
                var srcRt = hpPanel.GetComponent<RectTransform>();
                var dstRt = starterHp.GetComponent<RectTransform>();
                if (srcRt != null && dstRt != null)
                {
                    dstRt.anchorMin = srcRt.anchorMin;
                    dstRt.anchorMax = srcRt.anchorMax;
                    dstRt.pivot = srcRt.pivot;
                    dstRt.sizeDelta = srcRt.sizeDelta;
                    dstRt.anchoredPosition = srcRt.anchoredPosition;
                }
            }
        }

        // --- Update serialized fields ---
        var systems = GameObject.Find("StarterSystems");
        var gui = systems?.GetComponent<StarterGameplayUI>();
        if (gui != null)
        {
            var so = new SerializedObject(gui);
            so.FindProperty("hpPanelAnchorMin").vector2Value = Vector2.one * 0.5f;
            so.FindProperty("hpPanelAnchorMax").vector2Value = Vector2.one * 0.5f;
            so.FindProperty("hpPanelPivot").vector2Value = Vector2.one * 0.5f;
            so.FindProperty("hpPanelSizeDelta").vector2Value = new Vector2(556f, 960f);
            so.FindProperty("hpPanelAnchoredPosition").vector2Value = new Vector2(513f, -86.884f);
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(starterScene);
        EditorSceneManager.SaveScene(starterScene);

        // Clean up template scene (don't save changes)
        EditorSceneManager.OpenScene(starterPath, OpenSceneMode.Single);

        Debug.Log("Done! Starter scene synced to B/I structure. UI elements preserved.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TOOL: Restore Missing UI Elements in All Levels
    // ═══════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Restore UI Elements in All Levels")]
    public static void RestoreUIInAllLevels()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var notifPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>(NotifikasiPrefabPath);
        var alertPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>(UIAlertPanelPrefabPath);
        var exitPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>(ExitButtonPrefabPath);

        if (notifPrefab == null) { Debug.LogError($"Notifikasi prefab not found at {NotifikasiPrefabPath}"); return; }
        if (alertPrefab == null) { Debug.LogError($"UIAlertPanel prefab not found at {UIAlertPanelPrefabPath}"); return; }
        if (exitPrefab == null)  { Debug.LogError($"ExitButton prefab not found at {ExitButtonPrefabPath}"); return; }

        foreach (string scenePath in LevelScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("StarterCanvas");
            if (canvas == null) { Debug.LogWarning($"{scenePath}: StarterCanvas not found, skipping."); continue; }

            bool dirty = false;

            // ── 1. Notifikasi under StarterCanvas ─────────────────────────
            Transform notifTransform = canvas.transform.Find("Notifikasi");
            if (notifTransform == null)
            {
                var notif = Object.Instantiate(notifPrefab, canvas.transform);
                notif.name = "Notifikasi";
                Debug.Log($"{scenePath}: Created Notifikasi under StarterCanvas.");
                dirty = true;
                notifTransform = notif.transform;
            }

            // ── 2. UIAlertPanel at scene root ─────────────────────────────
            var alertObj = GameObject.Find("UIAlertPanel");
            if (alertObj == null)
            {
                alertObj = Object.Instantiate(alertPrefab);
                alertObj.name = "UIAlertPanel";
                Debug.Log($"{scenePath}: Created UIAlertPanel at root.");
                dirty = true;
            }

            // ── 3. Wire UIAlertPanel → Notifikasi children ────────────────
            var alertPanel = alertObj.GetComponent<UIAlertPanel>();
            if (alertPanel != null && notifTransform != null)
            {
                var so = new SerializedObject(alertPanel);
                WireAlertPanelRef(so, "foodOutPanel",          notifTransform, "PakanHabis");
                WireAlertPanelRef(so, "coinOutPanel",          notifTransform, "DuidHabis");
                WireAlertPanelRef(so, "timeOutPanel",          notifTransform, "WaktuHabis");
                WireAlertPanelRef(so, "mainMenuConfirmPanel",  notifTransform, "MainMenu");
                if (so.ApplyModifiedProperties())
                {
                    Debug.Log($"{scenePath}: Wired UIAlertPanel references.");
                    dirty = true;
                }
            }

            // ── 4. EXIT button under PausePanel ───────────────────────────
            var pausePanel = canvas.transform.Find("PausePanel");
            Transform exitTransform = pausePanel?.Find("EXIT");
            if (pausePanel != null && exitTransform == null)
            {
                var exitBtn = Object.Instantiate(exitPrefab, pausePanel);
                exitBtn.name = "EXIT";
                Debug.Log($"{scenePath}: Created EXIT button under PausePanel.");
                dirty = true;
                exitTransform = exitBtn.transform;
            }

            // ── 5. Wire StarterGameplayUI.exitButton → EXIT ───────────────
            var systems = GameObject.Find("StarterSystems");
            var gui = systems?.GetComponent<StarterGameplayUI>();
            if (gui != null && exitTransform != null)
            {
                var btn = exitTransform.GetComponent<Button>();
                if (btn != null)
                {
                    var so = new SerializedObject(gui);
                    var prop = so.FindProperty("exitButton");
                    if (prop != null && prop.objectReferenceValue == null)
                    {
                        prop.objectReferenceValue = btn;
                        so.ApplyModifiedProperties();
                        Debug.Log($"{scenePath}: Wired exitButton on StarterGameplayUI.");
                        dirty = true;
                    }
                }
            }

            // ── 6. Wire StarterGameplayUI.hpPanelRect → HPPanel ───────────
            if (gui != null)
            {
                var hpPanelObj = GameObject.Find("HPPanel");
                if (hpPanelObj != null)
                {
                    var rt = hpPanelObj.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        var so = new SerializedObject(gui);
                        var prop = so.FindProperty("hpPanelRect");
                        if (prop != null && prop.objectReferenceValue == null)
                        {
                            prop.objectReferenceValue = rt;
                            so.ApplyModifiedProperties();
                            Debug.Log($"{scenePath}: Wired hpPanelRect on StarterGameplayUI.");
                            dirty = true;
                        }
                    }
                }
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"{scenePath}: Saved with restored UI elements.");
            }
            else
            {
                Debug.Log($"{scenePath}: All UI elements already present.");
            }
        }

        Debug.Log("Done! Restore UI Elements completed for all levels.");
    }

    private static void WireAlertPanelRef(SerializedObject so, string propertyName, Transform parent, string childName)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null) return;

        var child = parent.Find(childName);
        if (child != null && prop.objectReferenceValue == null)
            prop.objectReferenceValue = child.gameObject;
    }
}
