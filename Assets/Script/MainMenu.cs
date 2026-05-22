using UnityEngine;

public class MainMenu : MonoBehaviour
{
    // This script can be kept as a placeholder or removed since UIManager handles the main menu
    // Keeping it for backward compatibility but making it delegate to UIManager
    
    void Start()
    {
        // Delegate to UIManager if it exists
        if (UIManager.Instance != null)
        {
            // UIManager already shows main menu in its Awake/InitializeUI
            // This script can be removed from the scene if not needed
        }
        else
        {
            Debug.LogWarning("UIManager not found in scene. MainMenu script is deprecated.");
        }
    }
}