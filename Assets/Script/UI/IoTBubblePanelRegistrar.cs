// IoTBubblePanelRegistrar.cs
using UnityEngine;

public class IoTBubblePanelRegistrar : MonoBehaviour
{
    private void Start() // ← ganti Awake ke Start
    {
        if (IoTReminderController.Instance != null)
        {
            IoTReminderController.Instance.RegisterBubblePanel(gameObject);
        }
        else
        {
            Debug.LogWarning("[IoTBubble] IoTReminderController.Instance masih null di Start!");
        }
    }
}