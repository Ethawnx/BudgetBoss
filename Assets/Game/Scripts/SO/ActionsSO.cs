using System.Collections.Generic;
using UnityEngine;
public enum ActionCategories { WorksAndHustles, SelfCare, SelfImprovement, Social, Investments }

[CreateAssetMenu(fileName = "NewAction", menuName = "Actions/Create Action")]
public class ActionsSO : ScriptableObject
{
    public ActionCategories Category;
    public string actionName;
    public Sprite icon;
    public int timeCost;
    public int energyCost;
    public int moneyCost;
    public List<StatReward> statBoosts;
    public List<ResourceReward> resourceBoosts;
    public List<SocialsReward> socialsRewards;
    [Header("Visibility")]
    public bool IsVisibleByDefault; // Show in asset list from start
    public bool IsUnlockedByDefault; // Owned from start (rare)
}

[System.Serializable]
public struct StatReward
{
    public PlayerStatistics.AttributeStats statType;
    public int minBoost;
    public int maxBoost;

    public int GetRandomReward() => Random.Range(minBoost, maxBoost + 1);
}
[System.Serializable]
public struct ResourceReward
{
    public ResourceManager.ResourceTypes resourceType;
    public int minAmount;
    public int maxAmount;

    public int GetRandomReward() => Random.Range(minAmount, maxAmount + 1);
}
[System.Serializable]
public struct SocialsReward
{
    public RelationsManager.SocialType SocialType;
    public int minAmount;
    public int maxAmount;

    public int GetRandomReward() => Random.Range(minAmount, maxAmount + 1);
}
