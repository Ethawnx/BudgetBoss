using Michsky.MUIP;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ActionsManager : MonoBehaviour, IDailySummaryProvider
{
    [SerializeField] ResourceManager _resourceManager;
    [SerializeField] PlayerStatistics _playerStatistics;
    [SerializeField] JobManager _jobManager;
    [SerializeField] GameManager _gameManager;
    [SerializeField] RelationsManager _relationsManager;

    [Header("UI References")]
    [SerializeField] Transform workAndHustlesListViewTransform;
    [SerializeField] Transform selfCareListViewTransform;
    [SerializeField] Transform socialListViewTransform;

    [SerializeField] GameObject actionButtonPrefab;
    [SerializeField] Transform miniGameContainer;
    [SerializeField] Sprite ErrorSpriteForModalWindow;
    [SerializeField] Sprite NormalSpriteForModalWindow;

    Dictionary<ActionCategories, HashSet<ActionsSO>> visibleActions = new();
    Dictionary<ActionCategories, HashSet<ActionsSO>> unlockedActions = new();

    private GameObject currentMiniGame;
    private bool _miniGameSuccess;
    private JobsSO _currentMiniGameJob;

    [HideInInspector] public List<ActionsSO> allActions;
    private List<ActionsSO> actionsPerformedToday = new();
    private void OnEnable()
    {
        CalendarManager.OnDayEnd += ResetDailyActions;
    }

    private void OnDisable()
    {
        CalendarManager.OnDayEnd -= ResetDailyActions;
    }
    private void ResetDailyActions()
    {
        actionsPerformedToday.Clear();
    }
    private void Awake()
    {
        LoadActions();
        
    }
    void Update()
    {
        
    }

    public void LoadActions()
    {
        allActions = new List<ActionsSO>(Resources.LoadAll<ActionsSO>("Actions"));
        visibleActions.Clear();
        unlockedActions.Clear();
        InitializeDictionary();
    }

    void InitializeDictionary()
    {
        foreach (var category in System.Enum.GetValues(typeof(ActionCategories)))
        {
            visibleActions[(ActionCategories)category] = new HashSet<ActionsSO>();
            unlockedActions[(ActionCategories)category] = new HashSet<ActionsSO>();
        }

        foreach (var action in allActions)
        {
            if (action.IsVisibleByDefault)
                visibleActions[action.Category].Add(action);

            if (action.IsUnlockedByDefault)
                unlockedActions[action.Category].Add(action);

            RefreshCategoryUI(action.Category);
        }
    }

    void PopulateList(Transform listView, ActionCategories category)
    {
        foreach (Transform child in listView)
            Destroy(child.gameObject);

        foreach (var action in visibleActions[category])
        {
            GameObject buttonGO = Instantiate(actionButtonPrefab, listView);
            SetupActionButton(buttonGO, action);
        }
    }

    void PopulateSocialList() => PopulateList(socialListViewTransform, ActionCategories.Social);
    void PopulateSelfCareList() => PopulateList(selfCareListViewTransform, ActionCategories.SelfCare);
    void PopulateWorkAndHustlesList() => PopulateList(workAndHustlesListViewTransform, ActionCategories.WorksAndHustles);

    void SetupActionButton(GameObject buttonGO, ActionsSO action)
    {
        ButtonManager button = buttonGO.GetComponent<ButtonManager>();
        button.SetText("");
        button.SetIcon(action.icon);

        bool isUnlocked = unlockedActions[action.Category].Contains(action);
        //button.interactable = isUnlocked;

        if (isUnlocked)
            button.onClick.AddListener(() => ShowModalWindow(action));
    }

    public bool CanPerformAction(ActionsSO action)
    {
        if (action.actionName == "Work")
        {
            return _resourceManager.GameTimeResource >= _jobManager.currentJob.WorkingHours &&
                   _resourceManager.Energy >= _jobManager.currentJob.EnergyCost;        
        }
        return _resourceManager.HasEnoughResources(action.timeCost, action.energyCost, action.moneyCost);
    }

    public void ShowModalWindow(ActionsSO action)
    {
        if (!CanPerformAction(action))
        {
            if (action.actionName == "Work")
            {
                JobsSO job = _jobManager.currentJob;
                if (_resourceManager.Energy < job.EnergyCost)
                {
                    _gameManager.ShowErrorModal("Too Tired", $"Need {job.EnergyCost} Energy to work as {job.JobName}\nCurrent: {_resourceManager.Energy}");
                    return;
                }
                if (_resourceManager.GameTimeResource < job.WorkingHours)
                {
                    _gameManager.ShowErrorModal("Not Enough Time", $"Need {job.WorkingHours} free hours\nCurrent: {_resourceManager.GameTimeResource} hours");
                    return;
                }
                if (_jobManager.HasWorkedToday)
                {
                    _gameManager.ShowErrorModal("Exhausted!", "You've already worked today!\nRest or do other activities.");
                    return;
                }
            }
            _gameManager.ShowErrorModal("Insufficient resources for " + action.actionName, "you do not have enough resources");
            return;
        }
        _gameManager.ShowActionConfirmModal(
            "Confirm: " + action.actionName,
            BuildActionDescription(action),
            () => PerformConfirmedAction(action) // onConfirm
            // Optionally, you can pass a cancel action as the 4th argument
            );
    }

    string BuildActionDescription(ActionsSO action)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (action.actionName == "Work" && _jobManager.currentJob != null)
        {
            sb.AppendLine($"<color=#191923>Job:</color> {_jobManager.currentJob.JobName}");
            sb.AppendLine($"Income: {_jobManager.currentJob.BaseIncome}$");
            sb.AppendLine($"Working Hours: {_jobManager.currentJob.WorkingHours}");
            sb.AppendLine($"Energy Cost: {_jobManager.currentJob.EnergyCost}");
            return sb.ToString();
        }
        // Show costs
        sb.AppendLine("<color=#191923>Cost:</color>");
        if (action.timeCost > 0)
        {
            float timeModifier = _resourceManager.GetTimeCostModifier();
            int displayedTimeCost = Mathf.CeilToInt(action.timeCost * timeModifier);
            string reductionNote = timeModifier < 1f ? " (reduced by vehicle)" : "";
            sb.AppendLine($"- {displayedTimeCost} hours{reductionNote}");
        }
        if (action.energyCost > 0) sb.AppendLine($"- {action.energyCost} energy");
        if (action.moneyCost > 0) sb.AppendLine($"- ${action.moneyCost}");

        // Show stat boosts
        if (action.statBoosts != null && action.statBoosts.Count > 0)
        {
            sb.AppendLine("\n<color=#006D77>Stat Boosts:</color>");
            foreach (var boost in action.statBoosts)
                sb.AppendLine($"- {boost.statType}: +{boost.GetRandomReward()}");
        }

        // Show resource boosts
        if (action.resourceBoosts != null && action.resourceBoosts.Count > 0)
        {
            sb.AppendLine("\n<color=#006D77>Resource Rewards:</color>");
            foreach (var boost in action.resourceBoosts)
                sb.AppendLine($"- {boost.resourceType}: +{boost.GetRandomReward()}");
        }

        return sb.ToString();
    }

    void PerformConfirmedAction(ActionsSO action)
    {
        if (action.actionName == "Work")
        {
            JobsSO currentJob = _jobManager.currentJob;
            if (currentMiniGame != null)
                Destroy(currentMiniGame);

            currentMiniGame = Instantiate(currentJob.miniGamePrefab, miniGameContainer);
            MiniGameBase miniGame = currentMiniGame.GetComponent<MiniGameBase>();
            miniGame.Initialize(currentJob.miniGameDifficulty);
            miniGame.OnMiniGameCompleted.AddListener(OnWorkMiniGameComplete);
            _jobManager.SetHasWorkedToday(true);
        }
        else if (action.actionName == "Sleep")
        {
            _gameManager.ProceedNextDay();
            PerformAction(action);
        }
        else
        {
            PerformAction(action);
            _gameManager.ShowActionResultModal(action, "Action performed successfully!");
        }
    }

    void OnWorkMiniGameComplete(bool success)
    {
        _miniGameSuccess = success;
        _currentMiniGameJob = _jobManager.currentJob;
        _gameManager.ShowActionConfirmModal(
            success ? "Work Complete!" : "Work Incomplete",
            GetMiniGameResultDescription(success),
            ApplyMiniGameResults // onConfirm
        );
    }

    string GetMiniGameResultDescription(bool success)
    {
        if (success)
        {
            return "You completed the mini-game successfully!\nYou earned your full reward and spent the required time and energy.";
        }
        else
        {
            return "You did not complete the mini-game.\nYou only receive partial rewards and spent less time and energy.";
        }
    }

    void ApplyMiniGameResults()
    {
        if (_miniGameSuccess)
        {
            _resourceManager.DecreaseHour(_currentMiniGameJob.WorkingHours);
            _resourceManager.SpendEnergy(_currentMiniGameJob.EnergyCost);
        }
        else
        {
            _resourceManager.DecreaseHour(_currentMiniGameJob.WorkingHours / 2);
            _resourceManager.SpendEnergy(_currentMiniGameJob.EnergyCost / 2);
        }
        Destroy(currentMiniGame);
        _currentMiniGameJob = null;
    }

    public void PerformAction(ActionsSO action)
    {
        if (!CanPerformAction(action)) return;

        if (action.actionName == "Eat")
        {
            _playerStatistics.OnEat();
        }
        else if (action.actionName == "Hygine")
        {
            _playerStatistics.OnHygiene();
        }
        else if (action.actionName == "Sleep")
        {
            _resourceManager.OnSleep();
        }

        // Example: Spend resources
        if (action.actionName == "Work")
        {
            _resourceManager.DecreaseHourWithModifier(action.timeCost);
        }
        else
        {
            _resourceManager.DecreaseHour(action.timeCost);
        }

        _resourceManager.SpendEnergy(action.energyCost);
        _resourceManager.SpendMoney(action.moneyCost);

        int baseMental = 5; // or your game's default
        float perPoint = 0.01f; // 1% per point
        int mental = _playerStatistics.CurrentMentalStrength;
        float mentalMultiplier = 1f + (mental - baseMental) * perPoint;
        mentalMultiplier = Mathf.Max(0.5f, mentalMultiplier); // Prevent negative/zero

        // Example: Apply stat boosts
        if (action.statBoosts != null)
        {
            foreach (var boost in action.statBoosts)
            {
                int reward = Mathf.RoundToInt(boost.GetRandomReward() * mentalMultiplier);
                _playerStatistics.AddAttributeStat(boost.statType, reward);
            }
        }

        // Example: Apply resource boosts
        if (action.resourceBoosts != null)
        {
            foreach (var boost in action.resourceBoosts)
            {
                int reward = Mathf.RoundToInt(boost.GetRandomReward() * mentalMultiplier);
                _resourceManager.AddResource(boost.resourceType, reward);
            }
        }

        if (action.socialsRewards != null)
        {
            foreach (var boost in action.socialsRewards)
            {
                _relationsManager.AddSocialStat(boost.SocialType, boost.GetRandomReward());
            }
        }
        if (!actionsPerformedToday.Contains(action))
            actionsPerformedToday.Add(action);
    }

    public void AddAvailableAction(ActionsSO action)
    {
        visibleActions[action.Category].Add(action);
        unlockedActions[action.Category].Add(action);
        RefreshCategoryUI(action.Category);
    }

    private void RefreshCategoryUI(ActionCategories category)
    {
        switch (category)
        {
            case ActionCategories.WorksAndHustles:
                PopulateWorkAndHustlesList();
                break;
            case ActionCategories.SelfCare:
                PopulateSelfCareList();
                break;
            case ActionCategories.Social:
                PopulateSocialList();
                break;
            default:
                Debug.LogWarning($"No UI implementation for category {category}");
                break;
        }
    }

    public string GetDailySummary()
    {
        if (actionsPerformedToday.Count == 0)
            return "No actions performed today.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Actions performed today:");
        foreach (var action in actionsPerformedToday)
        {
            sb.AppendLine($"- {action.actionName}");
        }
        return sb.ToString();
    }
}