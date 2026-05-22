using UnityEngine;

public class MainMenu : MonoBehaviour
{
    void Start()
    {
        // Ensure UIManager exists
        if (UIManager.Instance == null)
        {
            GameObject uiManagerObj = new GameObject("UIManager");
            uiManagerObj.AddComponent<UIManager>();
        }
    }
}