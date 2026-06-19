using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SyncStarterScene
{
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

        // 1. Remove Notifikasi
        var notif = canvas?.transform.Find("Notifikasi");
        if (notif != null) Object.DestroyImmediate(notif.gameObject);

        // 2. Remove UIAlertPanel
        var alert = GameObject.Find("UIAlertPanel");
        if (alert != null) Object.DestroyImmediate(alert);

        // 3. Remove StarterSceneInitializer
        var init = GameObject.Find("StarterSceneInitializer");
        if (init != null) Object.DestroyImmediate(init);

        // 4. Remove duplicate StarterGameplayUI on StarterCanvas
        var dup = canvas?.GetComponent<StarterGameplayUI>();
        if (dup != null) Object.DestroyImmediate(dup);

        // 5. Remove EXIT from PausePanel
        var pausePanel = canvas?.transform.Find("PausePanel");
        var exitBut = pausePanel?.Find("EXIT");
        if (exitBut != null) Object.DestroyImmediate(exitBut.gameObject);

        // 6. Add "Hp" background
        if (hpBg != null)
        {
            var clone = Object.Instantiate(hpBg.gameObject, canvas.transform);
            clone.name = "Hp";
        }

        // 7. Add extra RowContainer
        if (extraRow != null)
        {
            var starterCoop = canvas?.transform.Find("CoopStatusManager");
            if (starterCoop != null)
            {
                var rowClone = Object.Instantiate(extraRow.gameObject, starterCoop);
                rowClone.name = "RowContainer";
            }
        }

        // 8. Unpack & fix HPPanel
        var starterHp = GameObject.Find("HPPanel");
        if (starterHp != null)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(starterHp))
            {
                PrefabUtility.UnpackPrefabInstance(starterHp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            // Copy position/size from template
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

        // 9. Update serialized fields
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

        Debug.Log("Done! Starter scene synced to B/I structure.");
    }
}
