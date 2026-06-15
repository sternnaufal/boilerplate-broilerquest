using System;
using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
    [Serializable]
    private class PanelEntry
    {
        public string key = "";
        public GameObject panel = null;
    }

    [SerializeField] private PanelEntry[] panelEntries;

    private readonly Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (panelEntries == null)
            return;

        foreach (PanelEntry entry in panelEntries)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.key) && entry.panel != null)
                panels[entry.key] = entry.panel;
        }
    }

    public void RegisterPanel(string key, GameObject panel)
    {
        if (string.IsNullOrWhiteSpace(key) || panel == null)
            return;

        panels[key] = panel;
    }

    public void ShowOnly(string panelKey)
    {
        foreach (KeyValuePair<string, GameObject> pair in panels)
        {
            if (pair.Value != null)
                pair.Value.SetActive(pair.Key == panelKey);
        }
    }

    public void Show(string panelKey)
    {
        if (panels.TryGetValue(panelKey, out GameObject panel) && panel != null)
            panel.SetActive(true);
    }

    public void Hide(string panelKey)
    {
        if (panels.TryGetValue(panelKey, out GameObject panel) && panel != null)
            panel.SetActive(false);
    }
}
