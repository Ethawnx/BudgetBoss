using UnityEngine;
using Michsky.MUIP;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections.Generic;
using System;
using UnityEngine.Events;
using Unity.VisualScripting;
using Unity.Mathematics;
public interface IDailySummaryProvider
{
    string GetDailySummary();
}
public class GameManager : MonoBehaviour
{
    [Space(2f)]
    [Header("Modal References")]
    [SerializeField] ModalWindowManager _startingModal;
    [SerializeField] ModalWindowManager _tutorialModal;
    [SerializeField] ModalWindowManager _errorModal;
    [SerializeField] ModalWindowManager _daySummaryModal;
    [SerializeField] ModalWindowManager _actionConfirmModal;
    [SerializeField] ModalWindowManager _actionResultsModal;
    [SerializeField] ModalWindowManager _assetConfirmModal;
    [SerializeField] ModalWindowManager _gameOverModal;
    [Space(2f)]
    [Header("Manager's Reference")]
    [SerializeField] GoalManager _goalManager;
    [SerializeField] EventsManager _eventsManager;
    [SerializeField] WindowManager _windowManager;
    [SerializeField] JobManager _jobManager;
    [SerializeField] ResourceManager _resourceManager;
    [SerializeField] CalendarManager _calendarManager;
    [SerializeField] RelationsManager _relationsManager;
    [SerializeField] PlayerStatistics _playerStatistics;
    [SerializeField] GameObject QuestionsGO;
    [SerializeField] Sprite ErrorSpriteForModalWindow;
    [SerializeField] Sprite SuccessSpriteForModalWindow;
    public string PlayerName { get; private set; }

    private GameObject[] _questionsGO;
    private int _currentQuestionIndex;

    private bool canAnalyzeQuiz = true;
    private TMP_InputField nameInputField;

    private List<IDailySummaryProvider> summaryProviders = new();
    private void OnEnable()
    {
        CalendarManager.OnDayEnd += ShowDaySummaryModal;
        _actionConfirmModal.onOpen.AddListener(_resourceManager.SetUpdateResourcesToFalse);
        _actionConfirmModal.onClose.AddListener(_resourceManager.SetUpdateResourcesToTrue);
    }
    private void OnDisable()
    {
        CalendarManager.OnDayEnd -= ShowDaySummaryModal;
    }
    private void Awake()
    {
        // Register all managers that implement IDailySummaryProvider
        summaryProviders.Add(FindFirstObjectByType<JobManager>());
        summaryProviders.Add(FindFirstObjectByType<ActionsManager>());
        // Add others as needed

        CacheDirectChildren();
        if (_resourceManager == null)
            _resourceManager = GetComponent<ResourceManager>();

        nameInputField = _startingModal.GetComponentInChildren<TMP_InputField>(true);
    }
    void Start()
    {
        ShowWelcomeModal();
        OnlyShowQuizPanel();
        ShowFirstQuestion();
    }
    void Update()
    {
        if (_windowManager.currentWindowIndex == 1)
        {
            _playerStatistics.UpdateAttributesStats();
            _jobManager.UpdateFinancialStats();
            _relationsManager.UpdateSocialStats();
            //To be Added
        }

        if (_resourceManager.GameTimeResource <= 0)
        {
            ProceedNextDay();
        }

        if (_resourceManager.Energy <= 0)
        {
            ShowGameOverModal("You ran out of energy or money!");
            // Optionally, disable further actions here
        }
    }
    private void ShowDaySummaryModal()
    {
        var sb = new StringBuilder();
        foreach (var provider in summaryProviders)
        {
            sb.AppendLine(provider.GetDailySummary());
        }
        _daySummaryModal.titleText = "Day Summary";
        _daySummaryModal.descriptionText = sb.ToString();
        _daySummaryModal.UpdateUI();
        _daySummaryModal.Open();
    }
    void ShowWelcomeModal()
    {
        RemoveStartingModalListeners();
        _startingModal.titleText = "Welcome!";
        _startingModal.descriptionText = "Hello We are Glad to have you here\nWhat should we call you?";
        _startingModal.showCancelButton = false;
        _startingModal.showConfirmButton = false;
        nameInputField.onValueChanged.AddListener(CheckName);
        _startingModal.onConfirm.AddListener(SetPlayerName);
        _startingModal.Open();
        _startingModal.UpdateUI();
    }
    private void RemoveStartingModalListeners()
    {
        _startingModal.onConfirm.RemoveListener(SetPlayerName);
        nameInputField.onValueChanged.RemoveListener(CheckName);
    }
    private void ShowStatisticsTutorialModal()
    {
        RemoveTutorialModalListeners();
        _tutorialModal.titleText = $"Congratulations";
        _tutorialModal.descriptionText = $"You have got a job as {_jobManager.currentJob.JobName}\nYou also will see some of you main stats on next page\nNow your main goal is to achieve financial independency\n Have Fun!";
        _tutorialModal.showCancelButton = false;
        _tutorialModal.onConfirm.AddListener(_resourceManager.SetUpdateResourcesToTrue);
        _tutorialModal.onConfirm.AddListener(ShowResourcesUI);
        _tutorialModal.Open();
        _tutorialModal.UpdateUI();
    }
    private void RemoveTutorialModalListeners()
    {
        _tutorialModal.onConfirm.RemoveListener(_resourceManager.SetUpdateResourcesToTrue);
        _tutorialModal.onConfirm.RemoveListener(ShowResourcesUI);
    }
    void CheckName(string input)
    {
        var valid = !string.IsNullOrWhiteSpace(input) && input.Length <= 20;

        _startingModal.showConfirmButton = valid;
        _startingModal.UpdateUI();

        if (!valid && input.Length > 20)
        {
            ShowErrorModal("Invalid Input", "Name must be 20 characters or less");
        }
    }
    void SetPlayerName()
    {
        string input = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(input))
        {
            ShowErrorModal("Invalid Name", "Please enter a valid name!");
            return;
        }

        if (input.Length > 20)
        {
            ShowErrorModal("Too Long", "Name must be under 20 characters!");
            return;
        }

        PlayerName = input;
        ShowStartingTutorialModal();
    }
    public void ShowErrorModal(string title, string message)
    {
        _errorModal.titleText = title;
        _errorModal.descriptionText = message;
        _errorModal.icon = ErrorSpriteForModalWindow; // Set this in inspector
        _errorModal.showCancelButton = false;
        _errorModal.Open();
        _errorModal.UpdateUI();
    }
    
    public void ShowActionConfirmModal(string title, string message, UnityAction onConfirm, UnityAction onCancel = null)
    {
        _actionConfirmModal.titleText = title;
        _actionConfirmModal.descriptionText = message;

        _actionConfirmModal.showCancelButton = true;
        // Remove previous listeners to prevent stacking
        _actionConfirmModal.onConfirm.RemoveAllListeners();
        _actionConfirmModal.onCancel.RemoveAllListeners();

        // Add new listeners
        if (onConfirm != null)
            _actionConfirmModal.onConfirm.AddListener(onConfirm);
        _actionConfirmModal.onConfirm.AddListener(_actionConfirmModal.Close);

        if (onCancel != null)
            _actionConfirmModal.onCancel.AddListener(onCancel);
        _actionConfirmModal.onCancel.AddListener(_actionConfirmModal.Close);

        _actionConfirmModal.UpdateUI();
        _actionConfirmModal.Open();
    }
    public void ShowActionResultModal(string title, string message)
    {
        _actionResultsModal.titleText = title;
        _actionResultsModal.descriptionText = message;
        _actionResultsModal.icon = SuccessSpriteForModalWindow;
        _actionResultsModal.showCancelButton = false;
        _actionResultsModal.onConfirm.RemoveAllListeners();
        _actionResultsModal.onConfirm.AddListener(_actionResultsModal.Close);
        _actionResultsModal.UpdateUI();
        _actionResultsModal.Open();
    }
    public void ShowActionResultModal(ActionsSO action, string message)
    {
        _actionResultsModal.titleText = $"{action.actionName} Complete";
        _actionResultsModal.descriptionText = message;
        _actionResultsModal.icon = SuccessSpriteForModalWindow;
        _actionResultsModal.showCancelButton = false;
        _actionResultsModal.onConfirm.RemoveAllListeners();
        _actionResultsModal.onConfirm.AddListener(_actionResultsModal.Close);
        _actionResultsModal.UpdateUI();
        _actionResultsModal.Open();
    }
    private void ShowStartingTutorialModal()
    {
        _tutorialModal.titleText = $"Dear {PlayerName}";
        _tutorialModal.descriptionText = "We know it sounds hard but we are gonna take a quick quiz from you :)\nYour performance here will influence available resources in future scenarios.";
        _tutorialModal.showCancelButton = false;
        _tutorialModal.Open();
        _tutorialModal.UpdateUI();
    }
    public void ShowGameOverModal(string message = "You have lost the game!")
    {
        _gameOverModal.titleText = "Game Over";
        _gameOverModal.descriptionText = message;
        _gameOverModal.showCancelButton = false;
        _gameOverModal.onConfirm.RemoveAllListeners();
        _gameOverModal.onConfirm.AddListener(_goalManager.RestartGame);
        _gameOverModal.UpdateUI();
        _gameOverModal.Open();
    }
    private void ShowResourcesUI()
    {
        _resourceManager.ShowResourcesUI();
    }
    
    public void ProceedNextDay()
    {
        _resourceManager.ResetTimeandEnergy();
        _calendarManager.NextDay();
        _jobManager.SetHasWorkedToday(false);
        _eventsManager.TryTriggerRandomEvent(_relationsManager.FriendsList);
        _playerStatistics.OnDayPassed();
        CheckNeeds();
    }
    public void CheckNeeds()
    {
        if (_playerStatistics.DaysSinceLastEat >= 2)
            ShowErrorModal("Warning", "You haven't eaten for 2 days! Eat soon or you will lose.");
        if (_playerStatistics.DaysSinceLastEat >= 3)
        {
            ShowGameOverModal(PlayerName + ", you have lost the game due to starvation! Please try again.");
            return;
        }
        if (_playerStatistics.DaysSinceLastHygiene >= 5)
            ShowErrorModal("Warning", "You haven't taken care of hygiene for 5 days! Take care soon or you will get penalties.");
    }
    private void CacheDirectChildren()
    {
        if (QuestionsGO == null)
        {
            Debug.LogWarning("Parent object is not assigned!");
            return;
        }

        Transform parentTransform = QuestionsGO.transform;
        int childCount = parentTransform.childCount;

        // Initialize the array with the correct size
        _questionsGO = new GameObject[childCount];

        // Fill the array with direct children
        for (int i = 0; i < childCount; i++)
        {
            _questionsGO[i] = parentTransform.GetChild(i).gameObject;
            _questionsGO[i].SetActive(false);
        }
    }
    private void ShowFirstQuestion()
    {
        //Show the first QuestionGO
        _currentQuestionIndex = 0;
        _questionsGO[_currentQuestionIndex].SetActive(true);
    }
    private void GoToNextQuestion()
    {
        //Check the list of objects first
        if (_currentQuestionIndex < 0 || _questionsGO == null)
        {
            Debug.LogError("Questions not initialized!");
            return;
        }

        // Check if there's a next question 
        if (_currentQuestionIndex + 1 >= _questionsGO.Length)
        {
            Debug.Log("No more questions!");
            return;
        }

        // Turn off the current question
        _questionsGO[_currentQuestionIndex].SetActive(false);

        // Move to the next question
        _currentQuestionIndex++;
        _questionsGO[_currentQuestionIndex].SetActive(true);
    }
    void OnlyShowQuizPanel()
    {
        //Deactivate All other Window Buttons
        for (int i = 1; i < _windowManager.windows.Count; i++)
        {
            _windowManager.windows[i].buttonObject.SetActive(false);
        }

        //Turn Off All the Answers Check Toggles
        ToggleGroup[] toggleGroups = QuestionsGO.GetComponentsInChildren<ToggleGroup>(true);
        foreach (ToggleGroup _groups in toggleGroups)
        {
            _groups.SetAllTogglesOff();
        }

        //Open Quiz Window
        _windowManager.OpenWindowByIndex(0);
    }

    private void DisableQuiz()
    {
        _windowManager.windows[0].buttonObject.SetActive(false);
    }

    //This Function is invoked from AnalyzeBtn inside the Quiz;
    public void AnalyzeQuiz()
    {
        var eligibleJobs = _jobManager.GetEligibleJobs();

        if (eligibleJobs.Count == 0)
        {
            ShowErrorModal("No Jobs Available", "Improve your skills to unlock jobs!");
            return;
        }

        if (!canAnalyzeQuiz)
        {
            return;
        }
        for (int i = 1; i < _windowManager.windows.Count; i++)
        {
            _windowManager.windows[i].buttonObject.SetActive(true);
        }

        // Assign the closest eligible job
        var closestJob = _jobManager.GetClosestEligibleJob();
        if (closestJob != null)
            _jobManager.SetCurrentJob(closestJob);

        _jobManager.UpdateFinancialStats();
        _playerStatistics.UpdateAttributesStats();
        _windowManager.OpenWindowByIndex(1);

        DisableQuiz();
        ShowStatisticsTutorialModal();
    }

    //This is invoked from ConfirmButton inside the Quiz;
    public void GetCurrentSelection(ToggleGroup _tgroup)
    {
        if (!_tgroup.AnyTogglesOn())
        {
            ShowErrorModal("Answer Required", "Please select an answer to proceed!");
            canAnalyzeQuiz = false;
            return;
        }
        else
        {
            canAnalyzeQuiz = true;
        }

        foreach (Toggle toggle in _tgroup.GetComponentsInChildren<Toggle>())
        {
            if (toggle.isOn)
            {
                if (toggle.gameObject.name == "A")
                {
                    _playerStatistics.AddAttributeStat(PlayerStatistics.AttributeStats.Intelligence, 1);
                }
                else if (toggle.gameObject.name == "B")
                {
                    _playerStatistics.AddAttributeStat(PlayerStatistics.AttributeStats.Charisma, 1);
                }
                else if (toggle.gameObject.name == "C")
                {
                    _playerStatistics.AddAttributeStat(PlayerStatistics.AttributeStats.PhysicalStrength, 1);
                }
                else if (toggle.gameObject.name == "D")
                {
                    _playerStatistics.AddAttributeStat(PlayerStatistics.AttributeStats.MentalStrength, 1);
                }
                GoToNextQuestion();
            }
        }
    }

    public void ShowTransactionReport(string title, string costs, string rewards)
    {
        _errorModal.titleText = title;

        StringBuilder description = new StringBuilder();
        if (!string.IsNullOrEmpty(costs))
        {
            description.AppendLine("<color=#191923>Costs:</color>");
            description.AppendLine(costs);
        }
        if (!string.IsNullOrEmpty(rewards))
        {
            description.AppendLine("<color=#006D77>Rewards:</color>");
            description.AppendLine(rewards);
        }

        _errorModal.descriptionText = description.ToString();
        _errorModal.Open();
        _errorModal.UpdateUI();
    }
}
