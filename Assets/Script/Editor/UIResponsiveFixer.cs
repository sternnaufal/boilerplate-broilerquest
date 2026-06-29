using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class UIResponsiveFixer : EditorWindow
{
    private static readonly string[] ScenesToFix = new string[]
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/SelectLevel.unity",
        "Assets/Scenes/Starter.unity",
        "Assets/Scenes/Beginner.unity",
        "Assets/Scenes/Intermediate.unity",
        "Assets/Scenes/KoleksiIoT.unity",
        "Assets/Scenes/TesMinigame.unity",
    };

    private static readonly string[] CanvasPrefabsToFix = new string[]
    {
        "Assets/Prefab/Canvas.prefab",
        "Assets/Prefab/SceneTransitionCanvas.prefab",
        "Assets/Prefab/StarterCanvas.prefab",
        "Assets/Prefab/GlobalUIOverlay.prefab",
    };

    [MenuItem("Tools/Fix UI Responsiveness")]
    public static void FixAllScenes()
    {
        FixCanvasPrefabs();

        string initialScenePath = EditorSceneManager.GetActiveScene().path;

        foreach (string scenePath in ScenesToFix)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"[UIResponsiveFixer] Scene not found, skipping: {scenePath}");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"[UIResponsiveFixer] Fixing scene: {scene.name}");

            bool isDirty = false;

            var scalers = GameObject.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            foreach (var scaler in scalers)
            {
                isDirty |= ApplyExpandMode(scaler);
            }

            isDirty |= FixSceneUIElements(scene.name);

            if (isDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[UIResponsiveFixer] Scene {scene.name} saved.");
            }
        }

        if (!string.IsNullOrEmpty(initialScenePath))
            EditorSceneManager.OpenScene(initialScenePath, OpenSceneMode.Single);

        Debug.Log("[UIResponsiveFixer] Done.");
    }

    private static void FixCanvasPrefabs()
    {
        foreach (string prefabPath in CanvasPrefabsToFix)
        {
            if (!System.IO.File.Exists(prefabPath))
            {
                Debug.LogWarning($"[UIResponsiveFixer] Prefab not found, skipping: {prefabPath}");
                continue;
            }

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null) continue;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;
                bool changed = false;

                foreach (var scaler in root.GetComponentsInChildren<CanvasScaler>(true))
                    changed |= ApplyExpandMode(scaler);

                if (changed)
                    Debug.Log($"[UIResponsiveFixer] Fixed prefab: {prefabPath}");
            }
        }
    }

    private static bool ApplyExpandMode(CanvasScaler scaler)
    {
        bool changed = false;

        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            changed = true;
        }
        if (scaler.referenceResolution != new Vector2(1920, 1080))
        {
            scaler.referenceResolution = new Vector2(1920, 1080);
            changed = true;
        }
        if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand)
        {
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            changed = true;
        }

        return changed;
    }

    private static bool FixSceneUIElements(string sceneName)
    {
        bool changed = false;

        if (sceneName == "MainMenu")
        {
            var bg = GameObject.Find("Background");
            if (bg != null) changed |= FixStretch(bg);

            var canvasObj = GameObject.Find("UICanvas");
            if (canvasObj != null)
            {
                string[] panels = { "MainScreenPanel", "OptionScreenPanel", "HUDPanel", "PauseScreenPanel" };
                foreach (var panelName in panels)
                {
                    var panel = canvasObj.transform.Find(panelName)?.gameObject;
                    if (panel != null) changed |= EnsureContentSafeZone(panel);
                }
            }
        }
        else if (sceneName == "SelectLevel")
        {
            var bg = GameObject.Find("Background");
            if (bg != null) changed |= FixStretch(bg);

            if (GameObject.Find("PanelStarter") is var starter && starter != null)
                changed |= FixLevelPanel(starter, -600f);
            if (GameObject.Find("PanelBeginner") is var beginner && beginner != null)
                changed |= FixLevelPanel(beginner, 0f);
            if (GameObject.Find("PanelIntermediate") is var intermediate && intermediate != null)
                changed |= FixLevelPanel(intermediate, 600f);
        }
        else if (sceneName == "KoleksiIoT")
        {
            var bg = GameObject.Find("Background");
            if (bg != null) changed |= FixStretch(bg);

            var backButton = GameObject.Find("BackButton");
            if (backButton != null) changed |= FixAnchor(backButton, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f));

            var container = GameObject.Find("ProductContainer");
            if (container != null)
            {
                changed |= FixAnchor(container, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                var rect = container.GetComponent<RectTransform>();
                if (rect != null && rect.sizeDelta != new Vector2(1800, 306))
                {
                    rect.sizeDelta = new Vector2(1800, 306);
                    changed = true;
                }
            }
        }
        else if (sceneName == "Starter" || sceneName == "Beginner" || sceneName == "Intermediate")
        {
            var bg = GameObject.Find("Background");
            if (bg != null) changed |= FixStretch(bg);

            var pauseBtn = GameObject.Find("PAUSE");
            if (pauseBtn != null) changed |= FixAnchor(pauseBtn, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));

            var hpBtn = GameObject.Find("HP");
            if (hpBtn != null) changed |= FixAnchor(hpBtn, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f));

            var timerText = GameObject.Find("TimerText");
            if (timerText != null) changed |= FixAnchor(timerText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            var coopStatus = GameObject.Find("CoopStatusPanel");
            if (coopStatus != null) changed |= FixAnchor(coopStatus, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));

            var hpPanel = GameObject.Find("HPPanel");
            if (hpPanel != null) changed |= FixAnchor(hpPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f));

            var hud = GameObject.Find("HUD");
            if (hud != null) changed |= EnsureContentSafeZone(hud);
        }

        return changed;
    }

    // Adds ContentSafeZone to a panel (direct child of Canvas).
    // Removes SafeAreaAdjuster if present to avoid anchor conflicts.
    private static bool EnsureContentSafeZone(GameObject go)
    {
        if (go == null) return false;
        bool changed = false;

        var old = go.GetComponent<SafeAreaAdjuster>();
        if (old != null)
        {
            Object.DestroyImmediate(old);
            changed = true;
            Debug.Log($"[UIResponsiveFixer] Removed SafeAreaAdjuster from {go.name} (replaced by ContentSafeZone)");
        }

        if (go.GetComponent<ContentSafeZone>() == null)
        {
            go.AddComponent<ContentSafeZone>();
            changed = true;
            Debug.Log($"[UIResponsiveFixer] Added ContentSafeZone to {go.name}");
        }

        return changed;
    }

    private static bool FixStretch(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null) return false;

        bool changed = false;
        if (rect.anchorMin != Vector2.zero) { rect.anchorMin = Vector2.zero; changed = true; }
        if (rect.anchorMax != Vector2.one) { rect.anchorMax = Vector2.one; changed = true; }
        if (rect.offsetMin != Vector2.zero) { rect.offsetMin = Vector2.zero; changed = true; }
        if (rect.offsetMax != Vector2.zero) { rect.offsetMax = Vector2.zero; changed = true; }
        if (rect.sizeDelta != Vector2.zero) { rect.sizeDelta = Vector2.zero; changed = true; }
        if (rect.anchoredPosition != Vector2.zero) { rect.anchoredPosition = Vector2.zero; changed = true; }
        return changed;
    }

    private static bool FixLevelPanel(GameObject go, float targetX)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null) return false;

        bool changed = false;
        if (rect.anchorMin != new Vector2(0.5f, 0.5f)) { rect.anchorMin = new Vector2(0.5f, 0.5f); changed = true; }
        if (rect.anchorMax != new Vector2(0.5f, 0.5f)) { rect.anchorMax = new Vector2(0.5f, 0.5f); changed = true; }
        if (rect.sizeDelta != new Vector2(520, 482)) { rect.sizeDelta = new Vector2(520, 482); changed = true; }
        if (rect.anchoredPosition != new Vector2(targetX, 0f)) { rect.anchoredPosition = new Vector2(targetX, 0f); changed = true; }
        return changed;
    }

    private static bool FixAnchor(GameObject go, Vector2 min, Vector2 max, Vector2 pivot)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null) return false;

        bool changed = false;
        if (rect.anchorMin != min) { rect.anchorMin = min; changed = true; }
        if (rect.anchorMax != max) { rect.anchorMax = max; changed = true; }
        if (rect.pivot != pivot) { rect.pivot = pivot; changed = true; }
        return changed;
    }
}
