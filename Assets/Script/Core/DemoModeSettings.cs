using UnityEngine;

[CreateAssetMenu(fileName = "DemoModeConfig", menuName = "BroilerQuest/Build/Demo Mode Config")]
public class DemoModeSettings : ScriptableObject
{
    [SerializeField] public bool demoModeEnabled;
}
