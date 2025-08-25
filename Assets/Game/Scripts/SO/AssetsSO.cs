using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
public enum AssetsCategories { Inventory, Vehicle, State }
public enum UnlockType { None, Asset, Stat }

[CreateAssetMenu(fileName = "NewAsset", menuName = "Assets/Create Asset")]
public class AssetsSO : ScriptableObject
{
    public AssetsCategories Category;

    [Header("Basic Info")]
    public string AssetName;
    public Sprite Icon;
    [TextArea] public string Description;

    [Header("Cost & Income")]
    public int RequiredBalanceToUnlock;

    //Counts as Passive Income
    public int HourlyIncome;

    // If true, removed after use (e.g., "Business Card")
    public bool consumable; 

    [Header("Unlock Conditions")]
    public List<UnlockCondition> unlockConditions;

    public GameObject AssetGO;

    [Header("Unlocked Features")]
    public List<ActionsSO> unlockedActions; // Social/Actions to unlock
    public List<AssetsSO> unlockedAssets; // Other assets to make available

    [Header("Visibility")]
    public bool IsVisibleByDefault; // Show in asset list from start
    public bool IsUnlockedByDefault; // Owned from start (rare)
}
[System.Serializable]
public struct UnlockCondition
{
    public UnlockType conditionType;

    // If conditionType == Asset
    public AssetsSO requiredAsset;

    // If conditionType == Stat
    public PlayerStatistics.AttributeStats requiredStat; 

    public int minLevel;
}
