using Michsky.MUIP;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoalManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Transform goalsToggleListParent; // Parent object for toggles

    private Toggle[] goalToggles;

    private bool[] goalsCompleted;
    [SerializeField] private int totalGoals = 7; // Set to your actual number of goals
    [SerializeField] private ModalWindowManager _winModal;
    [SerializeField] private GameManager _gameManager;
    private void Awake()
    {
        goalsCompleted = new bool[totalGoals];
    }
    void Start()
    {
        CacheGoalToggles();
    }

    private void CacheGoalToggles()
    {
        int childCount = goalsToggleListParent.childCount;
        goalToggles = new Toggle[childCount];
        for (int i = 0; i < childCount; i++)
        {
            goalToggles[i] = goalsToggleListParent.GetChild(i).GetComponent<Toggle>();
            goalToggles[i].isOn = false; // Ensure all are off at start
        }
    }

    // Call this method to complete a goal by index
    public void CompleteGoal(int goalIndex)
    {

        if (goalIndex < 0 || goalIndex >= goalsCompleted.Length)
            return;

        if (goalsCompleted[goalIndex])
            return; // Already completed

        goalsCompleted[goalIndex] = true;

        // Update UI toggle
        if (goalToggles != null && goalIndex < goalToggles.Length)
            goalToggles[goalIndex].isOn = true;

        if (AllGoalsCompleted())
            ShowWinModal();
    }
    private bool AllGoalsCompleted()
    {
        foreach (var completed in goalsCompleted)
            if (!completed) return false;
        return true;
    }
    private void ShowWinModal()
    {
        _winModal.titleText = "You Win!";
        _winModal.descriptionText = "Congratulations! You have completed all goals.";
        _winModal.icon = null; // Set a win icon if you have one
        _winModal.showCancelButton = true;
        _winModal.showConfirmButton = true;

        _winModal.onConfirm.RemoveAllListeners();
        _winModal.onCancel.RemoveAllListeners();

        _winModal.onConfirm.AddListener(RestartGame);
        _winModal.onCancel.AddListener(_winModal.Close);

        _winModal.UpdateUI();
        _winModal.Open();
    }
    public void RestartGame()
    {
        // Option 1: Reload the scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);

        // Option 2: If you want to reset only game data, call your own reset methods here
        //_gameManager.ResetGame();
    }
}
