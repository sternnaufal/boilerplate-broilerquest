using UnityEngine.Events;
using UnityEngine.UI;

public static class ButtonHelper
{
    public static void AddListenerOnce(Button button, UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    public static void SetSingleListener(Button button, UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();

        if (action != null)
            button.onClick.AddListener(action);
    }

    public static void AddListenerOnce(Slider slider, UnityAction<float> action)
    {
        if (slider == null || action == null)
            return;

        slider.onValueChanged.RemoveListener(action);
        slider.onValueChanged.AddListener(action);
    }
}
