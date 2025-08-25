using Michsky.MUIP;
using System;
using System.Collections.Generic;
using System.Resources;
using TMPro;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public enum ResourceTypes { None, Money, Energy }

    [SerializeField] CalendarManager _calendarManager;
    [SerializeField] PlayerStatistics _playerStatistics;
    [SerializeField] GameManager _gameManager;
    [SerializeField] JobManager _jobManager;
    [SerializeField] AssetsManager _assetsManager;
    [SerializeField] GoalManager _goalManager;
    [Header("UI References")]
    [SerializeField] RadialSlider RealTimeCounter;
    [SerializeField] RadialSlider GameRemainingTimeCounter;
    [SerializeField] RadialSlider EnergyCounter;
    [SerializeField] TextMeshProUGUI BalanceCounter;
    [SerializeField] GameObject _resourcesContainer;

    public float AmountOfSecondsPerGameHour = 60f;

    [Header("Debug")]
    [SerializeField] bool _shouldChangeResources = false;

    public int GameTimeResource { get; private set; }
    public int Energy { get; private set; }
    public int Balance { get; private set; }
    public int InitialBalance = 100;

    private int _currentRemainingHours = 24;
    private float _elapsedTimeT;
    private float _elapsedTimeE;
    [SerializeField] float _interval = 1f;
    //Goal 2 check variables
    private int daysInPeriod = 7; // or 30 for a month
    private int currentDayInPeriod = 0;
    //Goal 3 check variables
    private int g3_daysInPeriod = 7; // Use 1 for daily, 7 for weekly, 30 for monthly
    private int g3_currentDayInPeriod = 0;
    private int minEnergyThreshold = 50; // Set your desired minimum energy
    private bool maintainedEnergyThisPeriod = true;
    // Goal 4 check variables
    private int g4_daysInPeriod = 7; // or 30 for a month
    private int g4_currentDayInPeriod = 0;
    private bool savedTimeThisPeriod = true;
    // Goal 7 check variables
    private bool independencyGoalCompleted = false;
    private int independencyGoalValue = 100000; // $100,000
    private void OnEnable()
    {
        CalendarManager.OnDayEnd += OnDayEnd;
    }
    private void OnDisable()
    {
        CalendarManager.OnDayEnd -= OnDayEnd;
    }
    void Awake()
    {
        ResetTimeandEnergy();
        _resourcesContainer.SetActive(false);
    }

    void Start()
    {
        // Ensure Balance is set on start
        Balance = InitialBalance;
        UpdateUI();
    }

    void Update()
    {
        UpdateTime();
        UpdateEnergy();
        UpdateMoney();
    }
    public void OnSleep()
    {
        // Check if player has more than 8 hours left at sleep
        if (GameTimeResource <= 8)
            savedTimeThisPeriod = false;

        g4_currentDayInPeriod++;

        if (g4_currentDayInPeriod >= g4_daysInPeriod)
        {
            CheckSaveTimeGoal();
            g4_currentDayInPeriod = 0;
            savedTimeThisPeriod = true; // Reset for next period
        }
    }
    private void CheckSaveTimeGoal()
    {
        if (savedTimeThisPeriod)
        {
            Debug.Log("Goal achieved: Slept with >8 hours left every day this period!");
            _goalManager.CompleteGoal(3); // Use the correct index for your time-saving goal
        }
        else
        {
            Debug.Log("Goal not achieved: Did not save enough time on some days.");
        }
    }
    public void OnDayEnd()
    {
        // If energy is below threshold, mark as not maintained
        if (Energy < minEnergyThreshold)
            maintainedEnergyThisPeriod = false;

        g3_currentDayInPeriod++;

        if (g3_currentDayInPeriod >= g3_daysInPeriod)
        {
            CheckMaintainEnergyGoal();
            g3_currentDayInPeriod = 0;
            maintainedEnergyThisPeriod = true; // Reset for next period
        }

        currentDayInPeriod++;

        if (currentDayInPeriod >= daysInPeriod)
        {
            CheckSavingsGoal();
            currentDayInPeriod = 0;
        }
    }
    private void CheckMaintainEnergyGoal()
    {
        if (maintainedEnergyThisPeriod)
        {
            Debug.Log("Goal achieved: Maintained energy every day this period!");
            _goalManager.CompleteGoal(2); // Use the correct index for your energy goal
        }
        else
        {
            Debug.Log("Goal not achieved: Missed energy threshold on some days.");
        }
    }
    private void CheckSavingsGoal()
    {
        if (Balance >= 10000)
        {
            Debug.Log("Goal achieved: Saved $10,000 in a week!");
            _goalManager.CompleteGoal(1); // Use the correct index for your savings goal
        }
        else
        {
            Debug.Log("Goal not achieved: Did not save $10,000.");
        }
    }
    // Check if enough resources are available
    public bool HasEnoughResources(int time, int energy, int money)
    {
        List<string> missing = new List<string>();

        if (GameTimeResource < time) missing.Add($"<color=#191923>- {time} hours</color>");
        if (Energy < energy) missing.Add($"<color=#191923>- {energy} energy</color>");
        if (Balance < money) missing.Add($"<color=#191923>- ${money}</color>");

        if (missing.Count > 0)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=#006D77>Missing Resources:</color>");
            foreach (var m in missing) sb.AppendLine(m);
            _gameManager.ShowErrorModal("Not Enough Resources", sb.ToString());
            return false;
        }
        return true;
    }

    // Add resource by type
    public void AddResource(ResourceTypes resourceToAdd, int amountToAdd)
    {
        switch (resourceToAdd)
        {
            case ResourceTypes.Money:
                AddMoney(amountToAdd);
                break;
            case ResourceTypes.Energy:
                AddEnergy(amountToAdd);
                break;
        }
    }

    public void SetUpdateResourcesToTrue() => _shouldChangeResources = true;
    public void SetUpdateResourcesToFalse() => _shouldChangeResources = false;
    public void CanNowUpdateTheResources(bool amount) => _shouldChangeResources = amount;

    public void ResetTimeandEnergy()
    {
        RealTimeCounter.maxValue = AmountOfSecondsPerGameHour;
        _elapsedTimeT = RealTimeCounter.maxValue;
        _currentRemainingHours = 24;
        GameTimeResource = 24;
        Energy = 100;
        UpdateUI();
    }
    public float GetTimeCostModifier()
    {
        if (_assetsManager.HasAsset("Bycycle"))
            return 0.95f;
        else if (_assetsManager.HasAsset("Motorcycle"))
            return 0.85f;
        else if (_assetsManager.HasAsset("Car"))
            return 0.75f;
        else
            return 1f;
    }
    public void DecreaseHourWithModifier(int baseTimeCost)
    {
        int finalTimeCost = Mathf.CeilToInt(baseTimeCost * GetTimeCostModifier());
        DecreaseHour(finalTimeCost);
    }
    public void DecreaseHour(int baseamount)
    {
        if (_currentRemainingHours - baseamount < 0)
        {
           _gameManager.ShowErrorModal("Time Paradox",
                "You can't spend more time than you have!\n" +
                $"Tried to spend {baseamount} hours, only {_currentRemainingHours} left.");
            return;
        }

        _currentRemainingHours -= baseamount;
        GameTimeResource = _currentRemainingHours;
        UpdateUI();
    }
    private void CheckIndependencyGoal()
    {
        if (!independencyGoalCompleted && Balance >= independencyGoalValue)
        {
            independencyGoalCompleted = true;
            Debug.Log("Goal achieved: $100,000 balance!");
            _goalManager.CompleteGoal(6); // Use the correct index for your Independency goal
        }
    }
    public void SpendMoney(int amount)
    {
        Balance -= amount;
        Balance = Mathf.Max(Balance, 0);
        UpdateUI();
        CheckIndependencyGoal();
    }

    public void AddMoney(int amount)
    {
        Balance += amount;
        UpdateUI();
        CheckIndependencyGoal();
    }

    public void AddEnergy(int amount)
    {
        Energy += amount;
        Energy = Mathf.Clamp(Energy, 0, 100);
        UpdateUI();
    }

    public void SpendEnergy(int amount)
    {
        Energy -= amount;
        Energy = Mathf.Clamp(Energy, 0, 100);
        UpdateUI();
    }

    // Decrease hour by 1 (internal use)
    void DecreaseHour()
    {
        DecreaseHour(1);
    }

    // Update all resource UI elements
    private void UpdateUI()
    {
        if (BalanceCounter != null)
            BalanceCounter.text = Balance.ToString() + "$";
        if (EnergyCounter != null)
        {
            EnergyCounter.SliderValueRaw = Energy;
            EnergyCounter.UpdateUI();
        }
        if (GameRemainingTimeCounter != null)
        {
            GameRemainingTimeCounter.SliderValueRaw = GameTimeResource;
            GameRemainingTimeCounter.UpdateUI();
        }
        if (RealTimeCounter != null)
        {
            RealTimeCounter.SliderValueRaw = _elapsedTimeT;
            RealTimeCounter.UpdateUI();
        }
    }

    private void UpdateMoney()
    {
        if (!_shouldChangeResources)
            return;

        Balance = Mathf.Max(Balance, 0);
        if (_calendarManager.IsOneWeekPassed())
        {
            Balance += _jobManager.CalculateActiveIncome();
            _calendarManager.ResetDayCounter();
        }
        UpdateUI();
    }

    private void UpdateEnergy()
    {
        if (!_shouldChangeResources)
            return;

        _elapsedTimeE += Time.deltaTime;

        float currentInterval = GetEnergyInterval();

        if (_elapsedTimeE >= currentInterval)
        {
            Energy--;
            Energy = Mathf.Clamp(Energy, 0, 100);
            UpdateUI();
            _elapsedTimeE = 0f;
        }
    }
    private float GetEnergyInterval()
    {
        // Example: Each point above 5 increases interval by 0.05f, below 5 decreases it
        int baseStrength = 5; // Use your game's default
        float intervalPerPoint = 0.05f; // Tune as needed
        int strength = _playerStatistics.CurrentPhysicalStrength;
        float interval = _interval + (strength - baseStrength) * intervalPerPoint;
        return Mathf.Max(0.2f, interval); // Prevent interval from going too low
    }
    private void AddPassiveIncome()
    {
        if (_assetsManager != null)
        {
            int passiveIncome = _assetsManager.GetTotalPassiveIncomePerHour();
            if (passiveIncome > 0)
            {
                AddMoney(passiveIncome);
            }
        }
    }
    private void UpdateTime()
    {
        if (!_shouldChangeResources)
            return;

        _currentRemainingHours = Mathf.Max(_currentRemainingHours, 0);
        if (_currentRemainingHours <= 0)
        {
            _gameManager.ShowErrorModal("Forced Rest",
                "You've run out of time!\nThe system will automatically sleep.");
            _gameManager.ProceedNextDay();
        }
        _elapsedTimeT -= Time.deltaTime;

        if (_elapsedTimeT <= 0f)
        {
            DecreaseHour();
            AddPassiveIncome();
            _elapsedTimeT = RealTimeCounter.maxValue;
        }
        UpdateUI();
    }
    public void ShowResourcesUI()
    {
        _resourcesContainer.SetActive(true);
    }
}
