using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CreateHealthCheckOverlayPrefab
{
    private const string PrefabPath = "Assets/Resources/HealthCheckResultOverlay.prefab";
    private const string SuccessSpritePath = "Assets/Gambar/berhasil.png";
    private const string FailSpritePath = "Assets/Gambar/gagal.png";

    [MenuItem("Tools/Create HealthCheck Overlay Prefab")]
    public static void Create()
    {
        var successSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SuccessSpritePath);
        var failSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FailSpritePath);

        if (successSprite == null || failSprite == null)
        {
            Debug.LogError("[HealthCheckPrefab] berhasil.png atau gagal.png tidak ditemukan di Assets/Gambar/");
            return;
        }

        var root = new GameObject("HealthCheckResultOverlay");
        var overlay = root.AddComponent<HealthCheckResultOverlay>();

        var so = new SerializedObject(overlay);
        so.FindProperty("successSprite").objectReferenceValue = successSprite;
        so.FindProperty("failSprite").objectReferenceValue = failSprite;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        Debug.Log($"[HealthCheckPrefab] Prefab dibuat di {PrefabPath}");
    }
}
