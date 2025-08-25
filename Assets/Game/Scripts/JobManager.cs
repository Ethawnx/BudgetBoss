using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JobManager : MonoBehaviour, IDailySummaryProvider
{
    [SerializeField] PlayerStatistics _playerStatistics;
    [SerializeField] GoalManager _goalManager;
    [SerializeField] RelationsManager _relationsManager;
    [SerializeField] GameObject FinancialAmountHolder;
    [SerializeField] AssetsManager AssetsManager;
    public JobsSO currentJob { get; private set; }

    private int promotionLevel = 0;
    public int PromotionLevel => promotionLevel;

    private List<JobsSO> allJobs;
    private List<JobsSO> eligibleJobs;

    private TextMeshProUGUI[] _financialAmountHolders;

    public bool HasWorkedToday { get; private set; }
    private int consecutiveWorkDays = 0;
    private int daysWorkedThisPeriod = 0;
    private int daysInPeriod = 7;
    private int currentDayInPeriod = 0;
    private void OnEnable()
    {
        CalendarManager.OnDayEnd += OnDayEnd;
    }
    private void OnDisable()
    {
        CalendarManager.OnDayEnd -= OnDayEnd;
    }
    private void OnDayEnd()
    {
        currentDayInPeriod++;
        if (HasWorkedToday)
        {
            daysWorkedThisPeriod++;
            consecutiveWorkDays++;
        }
        else
        {
            consecutiveWorkDays = 0;
        }

        HasWorkedToday = false;

        if (currentDayInPeriod >= daysInPeriod)
        {
            CheckWorkGoal();
            currentDayInPeriod = 0;
            daysWorkedThisPeriod = 0;
        }
    }
    private void CheckWorkGoal()
    {
        if (daysWorkedThisPeriod == daysInPeriod)
        {
            Debug.Log("Goal achieved: Worked every day this period!");
            PromoteCurrentJob(1.2f, 0.2f); 
        }
        else
        {
            Debug.Log("Goal not achieved: Missed some work days.");
        }
    }
    public void PromoteCurrentJob(float incomeMultiplier = 1.2f, float difficultyIncrement = 0.2f)
    {
        if (currentJob == null)
        {
            Debug.LogWarning("No current job to promote.");
            return;
        }

        // Increase active income
        currentJob.BaseIncome = Mathf.RoundToInt(currentJob.BaseIncome * incomeMultiplier);

        promotionLevel++; // Increment promotion level

        // Increase mini-game difficulty
        currentJob.miniGameDifficulty += difficultyIncrement;

        Debug.Log($"Promoted to higher position in {currentJob.JobName}! New income: {currentJob.BaseIncome}, New difficulty: {currentJob.miniGameDifficulty}");
        UpdateFinancialStats();
        ShowPromotionModal();
        _relationsManager.AddReputation(500);
        _goalManager.CompleteGoal(0);
    }
    private void ShowPromotionModal()
    {
        var gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            string title = "Promotion!";
            string message = $"Congratulations! You have been promoted in your job: <b>{currentJob.JobName}</b>.\n" +
                             $"Promotion Level: <b>{promotionLevel}</b>\n" +
                             $"New Income: <b>{currentJob.BaseIncome}$</b>\n" +
                             $"Mini-game Difficulty: <b>{currentJob.miniGameDifficulty:F1}</b>";
            gameManager.ShowActionResultModal(title, message);
        }
    }
    private void Awake()
    {
        // Load all JobSO assets from the "Resources/Jobs" folder
        allJobs = new List<JobsSO>(Resources.LoadAll<JobsSO>("Jobs"));
        _financialAmountHolders = FinancialAmountHolder?.GetComponentsInChildren<TextMeshProUGUI>(true);
    }
    public void SetHasWorkedToday(bool amount)
    {
        HasWorkedToday = amount;
    }
    public List<JobsSO> GetEligibleJobs()
    {
        eligibleJobs = new List<JobsSO>();
        foreach (JobsSO job in allJobs)
        {
            if (_playerStatistics.CurrentIntelligence >= job.RequiredIntelligence &&
                _playerStatistics.CurrentCharisma >= job.RequiredCharisma &&
                _playerStatistics.CurrentPhysicalStrength >= job.RequiredPhysical &&
                _playerStatistics.CurrentMentalStrength >= job.RequiredMental)
            {
                eligibleJobs.Add(job);
            }
        }
        return eligibleJobs;
    }
    public JobsSO GetClosestEligibleJob()
    {
        var eligible = GetEligibleJobs();
        if (eligible.Count == 0)
            return null;

        int minDiff = int.MaxValue;
        JobsSO closest = eligible[0];

        foreach (var job in eligible)
        {
            int diff =
                Mathf.Abs(_playerStatistics.CurrentIntelligence - job.RequiredIntelligence) +
                Mathf.Abs(_playerStatistics.CurrentCharisma - job.RequiredCharisma) +
                Mathf.Abs(_playerStatistics.CurrentPhysicalStrength - job.RequiredPhysical) +
                Mathf.Abs(_playerStatistics.CurrentMentalStrength - job.RequiredMental);

            if (diff < minDiff)
            {
                minDiff = diff;
                closest = job;
            }
        }
        return closest;
    }
    public void UpdateFinancialStats()
    {
        if (_financialAmountHolders == null || currentJob == null) return;
        if (_financialAmountHolders.Length >= 5)
        {
            _financialAmountHolders[0].text = currentJob.JobName;
            _financialAmountHolders[1].text = CalculateActiveIncome().ToString() + "$";
            _financialAmountHolders[2].text = CalculatePassiveIncome();
            _financialAmountHolders[3].text = CalculateWorkingHours();
            _financialAmountHolders[4].text = CalculateAssets();
        }
    }
    public void SetCurrentJob(JobsSO newJob)
    {
        currentJob = newJob;
        if (_financialAmountHolders != null && _financialAmountHolders.Length > 0 && currentJob != null)
            _financialAmountHolders[0].text = currentJob.JobName;
    }

    public int CalculateActiveIncome()
    {
        return currentJob != null ? currentJob.BaseIncome : 0;
    }

    private string CalculatePassiveIncome()
    {
        if (AssetsManager == null)
            return "0$/hr";

        int passiveIncome = AssetsManager.GetTotalPassiveIncomePerHour();
        return passiveIncome.ToString() + "$/hr";
    }

    private string CalculateWorkingHours()
    {
        // TODO: Add logic for other working hours if needed
        return currentJob != null ? currentJob.WorkingHours.ToString() : "0";
    }

    private string CalculateAssets()
    {
        // TODO: Implement asset calculation
        return "0";
    }

    public string GetDailySummary()
    {
        return HasWorkedToday
            ? $"\nYou worked as {currentJob?.JobName ?? "Unemployed"} today."
            : "\nYou did not work today.";
    }
}
