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
        "Assets/Scenes/KoleksiIoT.unity"
    };

    [MenuItem("Tools/Fix UI Responsiveness")]
    public static void FixAllScenes()
    {
        string initialScenePath = EditorSceneManager.GetActiveScene().path;

        foreach (string scenePath in ScenesToFix)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"[UIResponsiveFixer] Fixing scene: {scene.name}");

            bool isDirty = false;

            // 1. Adjust all CanvasScalers
            var scalers = GameObject.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            foreach (var scaler in scalers)
            {
                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    isDirty = true;
                }
                if (scaler.referenceResolution != new Vector2(1920, 1080))
                {
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    isDirty = true;
                }
                if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
                {
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    isDirty = true;
                }
                if (scaler.matchWidthOrHeight != 1.0f)
                {
                    scaler.matchWidthOrHeight = 1.0f; // Height matching for landscape
                    isDirty = true;
                }
            }

            // 2. Adjust anchors of specific UI components
            isDirty |= FixSceneUIElements(scene.name);

            if (isDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[UIResponsiveFixer] Scene {scene.name} saved with updates.");
            }
        }

        // Restore initial scene
        if (!string.IsNullOrEmpty(initialScenePath))
        {
            EditorSceneManager.OpenScene(initialScenePath, OpenSceneMode.Single);
        }

        Debug.Log("[UIResponsiveFixer] UI responsiveness fix completed successfully!");
    }

    private static bool FixSceneUIElements(string sceneName)
    {
        bool changed = false;

        if (sceneName == "MainMenu")
        {
            var bg = GameObject.Find("Background");
            if (bg != null) changed |= FixStretch(bg);

            // Add SafeAreaAdjuster to MainMenu panels (finding them via UICanvas to include inactive panels)
            var canvasObj = GameObject.Find("UICanvas");
            if (canvasObj != null)
            {
                var mainPanel = canvasObj.transform.Find("MainScreenPanel")?.gameObject;
                var optionPanel = canvasObj.transform.Find("OptionScreenPanel")?.gameObject;
                var hudPanel = canvasObj.transform.Find("HUDPanel")?.gameObject;
                var pausePanel = canvasObj.transform.Find("PauseScreenPanel")?.gameObject;

                if (mainPanel != null) changed |= EnsureSafeAreaAdjuster(mainPanel, true);
                if (optionPanel != null) changed |= EnsureSafeAreaAdjuster(optionPanel, true);
                if (hudPanel != null) changed |= EnsureSafeAreaAdjuster(hudPanel, true);
                if (pausePanel != null) changed |= EnsureSafeAreaAdjuster(pausePanel, true);
            }
        }
        else if (sceneName == "SelectLevel")
        {
            var bg = GameObject.Find("Background");
            if (bg != null) changed |= FixStretch(bg);

            var starter = GameObject.Find("PanelStarter");
            var beginner = GameObject.Find("PanelBeginner");
            var intermediate = GameObject.Find("PanelIntermediate");

            if (starter != null) changed |= FixLevelPanel(starter, -600f);
            if (beginner != null) changed |= FixLevelPanel(beginner, 0f);
            if (intermediate != null) changed |= FixLevelPanel(intermediate, 600f);

            // Add SafeAreaAdjuster to SelectLevelCanvas to handle adaptive matching only (adjustSafeArea = false)
            var canvasObj = GameObject.Find("SelectLevelCanvas");
            if (canvasObj != null) changed |= EnsureSafeAreaAdjuster(canvasObj, false);
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

            // Add SafeAreaAdjuster to BQ_KoleksiIoTCanvas to handle adaptive matching only (adjustSafeArea = false)
            var canvasObj = GameObject.Find("BQ_KoleksiIoTCanvas");
            if (canvasObj != null) changed |= EnsureSafeAreaAdjuster(canvasObj, false);
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
            if (hud != null) changed |= EnsureSafeAreaAdjuster(hud, true);
        }

        return changed;
    }

    private static bool EnsureSafeAreaAdjuster(GameObject go, bool adjustSafeArea)
    {
        if (go == null) return false;
        var adjuster = go.GetComponent<SafeAreaAdjuster>();
        bool changed = false;
        if (adjuster == null)
        {
            adjuster = go.AddComponent<SafeAreaAdjuster>();
            changed = true;
            Debug.Log($"[UIResponsiveFixer] Added SafeAreaAdjuster to {go.name}");
        }

        var so = new SerializedObject(adjuster);
        var prop = so.FindProperty("adjustSafeArea");
        if (prop != null && prop.boolValue != adjustSafeArea)
        {
            prop.boolValue = adjustSafeArea;
            so.ApplyModifiedProperties();
            changed = true;
            Debug.Log($"[UIResponsiveFixer] Updated adjustSafeArea to {adjustSafeArea} on {go.name}");
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

        var parentRect = rect.parent.GetComponent<RectTransform>();
        Vector2 parentSize = parentRect != null ? parentRect.rect.size : new Vector2(1920, 1080);
        
        Vector2 localPos = rect.anchoredPosition;
        Vector2 oldMin = rect.anchorMin;
        Vector2 oldMax = rect.anchorMax;
        
        float oldAnchorX = (oldMin.x + oldMax.x) * 0.5f;
        float oldAnchorY = (oldMin.y + oldMax.y) * 0.5f;
        float posX = localPos.x + oldAnchorX * parentSize.x;
        float posY = localPos.y + oldAnchorY * parentSize.y;

        float newAnchorX = (min.x + max.x) * 0.5f;
        float newAnchorY = (min.y + max.y) * 0.5f;
        float newX = posX - newAnchorX * parentSize.x;
        float newY = posY - newAnchorY * parentSize.y;

        Vector2 newPos = new Vector2(newX, newY);

        bool changed = false;
        if (rect.anchorMin != min) { rect.anchorMin = min; changed = true; }
        if (rect.anchorMax != max) { rect.anchorMax = max; changed = true; }
        if (rect.pivot != pivot) { rect.pivot = pivot; changed = true; }
        if (Vector2.Distance(rect.anchoredPosition, newPos) > 0.01f) { rect.anchoredPosition = newPos; changed = true; }
        
        return changed;
    }
}
