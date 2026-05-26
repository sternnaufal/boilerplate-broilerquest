using System;
using UnityEngine;

public class FeedManager : Singleton<FeedManager>
{
    public event Action<int> FeedChanged;

    private int feedCount;
    private bool hasInitialized;

    protected override void Awake()
    {
        base.Awake();
        LoadFeed();
        GlobalUIOverlay overlay = GlobalUIOverlay.Instance;
    }

    private void LoadFeed()
    {
        feedCount = PlayerPrefs.GetInt(GameConstants.Persistence.FeedCountKey, 0);
        hasInitialized = true;
    }

    private void SaveFeed()
    {
        PlayerPrefs.SetInt(GameConstants.Persistence.FeedCountKey, feedCount);
        PlayerPrefs.Save();
    }

    public void AddFeed(int amount)
    {
        if (amount < 0) return;
        feedCount += amount;
        SaveFeed();
        FeedChanged?.Invoke(feedCount);
    }

    public bool UseFeed(int amount)
    {
        if (amount < 0 || feedCount < amount) return false;
        feedCount -= amount;
        SaveFeed();
        FeedChanged?.Invoke(feedCount);
        return true;
    }

    public int GetFeedCount()
    {
        return feedCount;
    }

    public bool CanUseFeed(int amount)
    {
        return amount >= 0 && feedCount >= amount;
    }

    public void SetFeedCount(int amount)
    {
        feedCount = Mathf.Max(0, amount);
        SaveFeed();
        FeedChanged?.Invoke(feedCount);
    }
}
