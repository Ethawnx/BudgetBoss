using Michsky.MUIP;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class AssetsManager : MonoBehaviour
{
    Dictionary<AssetsCategories, List<AssetsSO>> categorizedAssets = new();

    [SerializeField] Transform inventoryListViewTransform;
    [SerializeField] Transform stateListViewTransform;
    [SerializeField] Transform vehicleListViewTransform;
    [SerializeField] GameObject assetListButtonObject;
    [SerializeField] Sprite ErrorSpriteForModalWindow;
    [Header("Managers Reference")]
    [SerializeField] GoalManager _goalManager;
    [SerializeField] RelationsManager _relationsManager;
    [SerializeField] ModalWindowManager _modalManager;
    [SerializeField] ResourceManager _resourceManager;
    [SerializeField] PlayerStatistics _playerStatistics;
    [SerializeField] ActionsManager _actionsManager;
    [SerializeField] GameManager _gameManager;
    [SerializeField] JobManager _jobManager;
    [Header("Inventroy Holder Transform References")]
    [SerializeField] Transform phoneHolder;

    private UnityAction _currentConfirmAction;
    private UnityAction _currentCancelAction;

    [SerializeField] List<AssetsSO> allAssets;
    public HashSet<AssetsSO> visibleAssets = new();
    public HashSet<AssetsSO> ownedAssets = new();
    public bool HasMotorcycle { get; set; }
    //Goal 5 variables
    // At the top of AssetsManager
    [SerializeField] private int assetPurchaseGoal = 5; // Set X to your desired number
    private int assetsPurchased = 0;
    private bool assetGoalCompleted = false;
    void Awake()
    {
        LoadAssets();
        InitializeDictionary();
        _modalManager.onOpen.AddListener(_resourceManager.SetUpdateResourcesToFalse);
        _modalManager.onClose.AddListener(_resourceManager.SetUpdateResourcesToTrue);
        _modalManager.onClose.AddListener(RemoveModalListeners); // Always clean up listeners on close
    }

    private void InitializeDictionary()
    {
        foreach (var category in System.Enum.GetValues(typeof(AssetsCategories)))
            categorizedAssets[(AssetsCategories)category] = new List<AssetsSO>();

        foreach (var asset in allAssets)
        {
            if (asset.IsVisibleByDefault && !ownedAssets.Contains(asset))
                categorizedAssets[asset.Category].Add(asset);
        }

        foreach (AssetsCategories category in System.Enum.GetValues(typeof(AssetsCategories)))
            RefreshCategoryUI(category);
    }

    void LoadAssets()
    {
        allAssets = new List<AssetsSO>(Resources.LoadAll<AssetsSO>("Assets"));
        foreach (var asset in allAssets)
        {
            if (asset.IsVisibleByDefault)
                visibleAssets.Add(asset);
            if (asset.IsUnlockedByDefault)
                ownedAssets.Add(asset);
        }
    }

    public void UnlockAssetVisibility(AssetsSO asset)
    {
        if (!visibleAssets.Contains(asset))
            visibleAssets.Add(asset);
    }
    public bool HasAsset(string assetName)
    {
        return ownedAssets.Any(asset => asset.name == assetName);
    }
    string GetLockReason(AssetsSO asset)
    {
        if (_resourceManager.Balance < asset.RequiredBalanceToUnlock)
            return "Not enough money!";

        foreach (var condition in asset.unlockConditions)
        {
            if (condition.conditionType == UnlockType.Asset &&
                !ownedAssets.Contains(condition.requiredAsset))
                return $"Requires {condition.requiredAsset.AssetName}";

            if (condition.conditionType == UnlockType.Stat &&
                _playerStatistics.GetStatLevel(condition.requiredStat) < condition.minLevel)
                return $"Requires {condition.requiredStat} Lv.{condition.minLevel}";
        }
        return "Can't purchase";
    }

    // Centralized listener removal, now only called by onClose
    private void RemoveModalListeners()
    {
        if (_currentConfirmAction != null)
        {
            _modalManager.onConfirm.RemoveListener(_currentConfirmAction);
            _currentConfirmAction = null;
        }
        if (_currentCancelAction != null)
        {
            _modalManager.onCancel.RemoveListener(_currentCancelAction);
            _currentCancelAction = null;
        }
    }
    public int GetTotalPassiveIncomePerHour()
    {
        int total = 0;
        foreach (var asset in ownedAssets)
        {
            total += asset.HourlyIncome;
        }
        return total;
    }
    public void ShowModalWindow(AssetsSO asset)
    {
        // No need to call RemoveModalListeners here, onClose will handle it

        if (!ValidatePurchase(asset, out string errorMessage))
        {
            ShowPurchaseError(asset, errorMessage);
            return;
        }

        _currentConfirmAction = () => { PurchaseConfirmedAssets(asset); _modalManager.Close(); };
        _currentCancelAction = () => { _modalManager.Close(); };

        _modalManager.onConfirm.AddListener(_currentConfirmAction);
        _modalManager.onCancel.AddListener(_currentCancelAction);

        _modalManager.titleText = $"Confirm: {asset.AssetName}";
        _modalManager.descriptionText = BuildAssetDescription(asset);
        _modalManager.showCancelButton = true;
        _modalManager.UpdateUI();
        _modalManager.Open();
    }

    private bool ValidatePurchase(AssetsSO asset, out string errorMessage)
    {
        errorMessage = "";
        List<string> missingRequirements = new List<string>();

        if (_resourceManager.Balance < asset.RequiredBalanceToUnlock)
            missingRequirements.Add($"- ${asset.RequiredBalanceToUnlock}");

        foreach (var condition in asset.unlockConditions)
        {
            if (condition.conditionType == UnlockType.Stat)
            {
                int currentLevel = _playerStatistics.GetStatLevel(condition.requiredStat);
                if (currentLevel < condition.minLevel)
                    missingRequirements.Add($"- {condition.requiredStat} Lv.{condition.minLevel} (Current: {currentLevel})");
            }
            if (condition.conditionType == UnlockType.Asset &&
                !ownedAssets.Contains(condition.requiredAsset))
                missingRequirements.Add($"- {condition.requiredAsset.AssetName}");
        }

        if (missingRequirements.Count > 0)
        {
            errorMessage = GetMissingRequirementsMessage(asset, missingRequirements);
            return false;
        }
        return true;
    }

    private string GetMissingRequirementsMessage(AssetsSO asset, List<string> missing)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Can't buy {asset.AssetName}:");
        sb.AppendLine("<color=#CD5334>Missing Requirements:</color>");
        sb.AppendJoin("\n", missing);
        return sb.ToString();
    }

    private void ShowPurchaseError(AssetsSO asset, string message)
    {
        // No need to call RemoveModalListeners here, onClose will handle it

        _currentConfirmAction = () => { _modalManager.Close(); };
        _modalManager.onConfirm.AddListener(_currentConfirmAction);

        _modalManager.titleText = "Purchase Failed";
        _modalManager.descriptionText = message;
        _modalManager.icon = ErrorSpriteForModalWindow;
        _modalManager.showCancelButton = false;
        _modalManager.UpdateUI();
        _modalManager.Open();
    }

    private void ShowPurchaseSuccessModal(AssetsSO asset)
    {
        // No need to call RemoveModalListeners here, onClose will handle it

        _currentConfirmAction = () => { _modalManager.Close(); };
        _modalManager.onConfirm.AddListener(_currentConfirmAction);

        _modalManager.titleText = "Purchased!";
        _modalManager.descriptionText = $"Successfully bought {asset.AssetName}!\nRemaining balance: ${_resourceManager.Balance}";
        _modalManager.icon = null; // Set a success icon if you have one
        _modalManager.showCancelButton = false;
        _modalManager.UpdateUI();
        _modalManager.Open();
    }

    private void PurchaseConfirmedAssets(AssetsSO asset)
    {
        if (!CanPurchaseAsset(asset)) return;

        _resourceManager.SpendMoney(asset.RequiredBalanceToUnlock);
        ownedAssets.Add(asset);

        if (asset.AssetName == "Phone" && asset.AssetGO != null)
        {
            var phoneGO = Instantiate(asset.AssetGO, phoneHolder);
            var phoneManager = phoneGO.GetComponent<PhoneManager>();
            _relationsManager.SetPhoneManager(phoneManager);
        }

        if (visibleAssets.Contains(asset))
            visibleAssets.Remove(asset);

        UnlockNewActions(asset);
        UnlockConnectedAssets(asset);

        RefreshCategoryUI(asset.Category);
        foreach (var unlockedAsset in asset.unlockedAssets)
            RefreshCategoryUI(unlockedAsset.Category);

        assetsPurchased++;
        CheckAssetPurchaseGoal();
        _jobManager.UpdateFinancialStats();

        ShowPurchaseSuccessModal(asset);
    }
    private void CheckAssetPurchaseGoal()
    {
        if (!assetGoalCompleted && assetsPurchased >= assetPurchaseGoal)
        {
            assetGoalCompleted = true;
            Debug.Log("Goal achieved: Bought enough assets!");
            _goalManager.CompleteGoal(4); // Use the correct index for your asset purchase goal
        }
    }
    private bool CanPurchaseAsset(AssetsSO asset)
    {
        if (_resourceManager.Balance < asset.RequiredBalanceToUnlock)
            return false;
        return CheckUnlockConditions(asset);
    }

    private bool CheckUnlockConditions(AssetsSO asset)
    {
        foreach (var condition in asset.unlockConditions)
        {
            switch (condition.conditionType)
            {
                case UnlockType.Asset:
                    if (!ownedAssets.Contains(condition.requiredAsset))
                        return false;
                    break;
                case UnlockType.Stat:
                    if (_playerStatistics.GetStatLevel(condition.requiredStat) < condition.minLevel)
                        return false;
                    break;
            }
        }
        return true;
    }

    void RefreshCategoryUI(AssetsCategories category)
    {
        switch (category)
        {
            case AssetsCategories.Inventory:
                PopulateList(inventoryListViewTransform, AssetsCategories.Inventory);
                break;
            case AssetsCategories.Vehicle:
                PopulateList(vehicleListViewTransform, AssetsCategories.Vehicle);
                break;
            case AssetsCategories.State:
                PopulateList(stateListViewTransform, AssetsCategories.State);
                break;
            default:
                Debug.LogWarning($"No UI implementation for category {category}");
                break;
        }
    }

    private void PopulateList(Transform listView, AssetsCategories category)
    {
        foreach (Transform child in listView)
            Destroy(child.gameObject);

        foreach (var asset in categorizedAssets[category])
        {
            if (visibleAssets.Contains(asset) && !ownedAssets.Contains(asset))
            {
                GameObject buttonGO = Instantiate(assetListButtonObject, listView);
                SetupAssetButton(buttonGO, asset);
            }
        }
    }
    private void SetupAssetButton(GameObject buttonGO, AssetsSO asset)
    {
        ButtonManager button = buttonGO.GetComponent<ButtonManager>();
        button.SetText("");
        button.SetIcon(asset.Icon);
        bool canBuy = CanPurchaseAsset(asset);
        //button.interactable = canBuy;

        if (!canBuy)
        {
            string tooltip = GetLockReason(asset);
            //button.GetComponent<TooltipContent>().tooltipText = tooltip;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ShowModalWindow(asset));
    }
    private void UnlockNewActions(AssetsSO asset)
    {
        if (_actionsManager == null)
        {
            Debug.LogError("ActionsManager reference missing!");
            return;
        }

        foreach (var action in asset.unlockedActions)
        {
            if (!_actionsManager.allActions.Contains(action))
            {
                _actionsManager.allActions.Add(action);
                _actionsManager.LoadActions();
            }
            _actionsManager.AddAvailableAction(action);
        }
    }

    private void UnlockConnectedAssets(AssetsSO asset)
    {
        foreach (var unlockedAsset in asset.unlockedAssets)
        {
            if (!allAssets.Contains(unlockedAsset))
                allAssets.Add(unlockedAsset);

            UnlockAssetVisibility(unlockedAsset);

            if (!ownedAssets.Contains(unlockedAsset) &&
                !categorizedAssets[unlockedAsset.Category].Contains(unlockedAsset))
            {
                categorizedAssets[unlockedAsset.Category].Add(unlockedAsset);
            }
        }
    }

    private string BuildAssetDescription(AssetsSO asset)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<color=#191923>Cost:</color>");
        sb.AppendLine($"- ${asset.RequiredBalanceToUnlock}");

        if (asset.unlockedActions.Count > 0 || asset.unlockedAssets.Count > 0)
        {
            sb.AppendLine("\n<color=#006D77>Unlocks:</color>");
            foreach (var action in asset.unlockedActions)
                sb.AppendLine($"- {action.actionName} action");
            foreach (var unlockedAsset in asset.unlockedAssets)
                sb.AppendLine($"- {unlockedAsset.AssetName} availability");
        }
        return sb.ToString();
    }
}
