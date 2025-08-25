using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhoneManager : MonoBehaviour
{
    [SerializeField] private GameObject eventMessagePrefab;
    [SerializeField] private Transform messagesContainer;
    [SerializeField] Transform contactsContainer; // The parent for friend entries
    [SerializeField] GameObject ContactsPanel;
    [SerializeField] GameObject MessagesPanel;
    [SerializeField] private GameObject friendPrefab;
    [SerializeField] private GameObject friendActionPanel;
    [SerializeField] private Button askOutButton;
    [SerializeField] private Button askForDebtButton;
    [SerializeField] private Button payDebtButton;
    [SerializeField] private TMP_Text friendNameText;

    private RelationsManager relationsManager;
    private GameManager _gamemanager;
    private EventsManager _eventsManager;
    private ResourceManager _resourceManager;
    private List<GameObject> friendEntries = new();
    private List<GameObject> eventEntries = new();

    private Friend selectedFriend;
    private void Awake()
    {
        MessagesPanel.SetActive(false);
        ContactsPanel.SetActive(false);
    }
    private void Start()
    {
        relationsManager = FindAnyObjectByType<RelationsManager>();
        _gamemanager = FindAnyObjectByType<GameManager>();
        _resourceManager = FindAnyObjectByType<ResourceManager>(); 
        _eventsManager = FindAnyObjectByType<EventsManager>();
    }
    public void OnContactsButtonClicked()
    {
        if (!ContactsPanel.activeSelf)
        {
            ContactsPanel.SetActive(true);
        }

        if (MessagesPanel.activeSelf)
        {
            MessagesPanel.SetActive(false);
        }
    }
    public void OnMessagesButtonClicked()
    {
        if (!MessagesPanel.activeSelf)
        {
            MessagesPanel.SetActive(true);
        }

        if (ContactsPanel.activeSelf)
        {
            ContactsPanel.SetActive(false);
        }
        PopulateEvents();
    }
    public void PopulateEvents()
    {
        // Clear old entries
        foreach (Transform child in messagesContainer)
            Destroy(child.gameObject);
        eventEntries.Clear();

        // Add new entries
        foreach (var triggered in _eventsManager.MessageEvents)
        {
            GameObject entry = Instantiate(eventMessagePrefab, messagesContainer);

            entry.transform.Find("Title").GetComponent<TMP_Text>().text = triggered.eventSO.eventName;
            entry.transform.Find("Description").GetComponent<TMP_Text>().text = triggered.eventSO.description;

            var friendNameObj = entry.transform.Find("FriendName");
            if (friendNameObj != null)
                friendNameObj.GetComponent<TMP_Text>().text = triggered.sourceFriend != null
                    ? $"From: {triggered.sourceFriend.Name}"
                    : "";
            var lostFriendObj = entry.transform.Find("FriendName");
            if (lostFriendObj != null)
            {
                lostFriendObj.GetComponent<TMP_Text>().text = triggered.lostFriend != null
                    ? $"Lost: {triggered.lostFriend.Name}"
                    : "";
            }
            var dateObj = entry.transform.Find("Date");
            if (dateObj != null)
                dateObj.GetComponent<TMP_Text>().text = triggered.occurredAt.ToString("M");

            eventEntries.Add(entry);
        }
    }
    public void PopulateContacts()
    {
        // Clear old entries
        foreach (Transform child in contactsContainer)
            Destroy(child.gameObject);
        friendEntries.Clear();

        // Add new entries
        foreach (var friend in relationsManager.FriendsList)
        {
            GameObject entry = Instantiate(friendPrefab, contactsContainer);

            // Set icon
            entry.transform.Find("Icon").GetComponent<Image>().sprite = friend.Icon;
            // Set name
            entry.transform.Find("Name").GetComponent<TMP_Text>().text = friend.Name;
            // Set friendship level
            entry.transform.Find("Slider").GetComponent<Slider>().value = friend.FriendshipLevel;

            // Debt indicator logic
            var debtIndicator = entry.transform.Find("DebtIndicator");
            if (debtIndicator != null)
                debtIndicator.gameObject.SetActive(friend.Debt > 0);
            debtIndicator.transform.Find("DebtAmount").GetComponent<TMP_Text>().text = $"{friend.Debt}$";

            // Add highlight/select/click logic
            Button button = entry.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnFriendSelected(friend));
            }

            friendEntries.Add(entry);
        }
    }
    public void OnCallButtonPressed()
    {
        if (selectedFriend == null)
        {
            _gamemanager.ShowErrorModal("No friend selected","Please select a friend to call.");
            return;
        }

        _resourceManager.SpendEnergy(1);

        float chance = Mathf.Clamp01(selectedFriend.FriendshipLevel / 100f);
        // Random chance to increase relationship
        if (Random.value < chance)
        {
            ShowFriendActionPanel(selectedFriend);
        }
        else
        {
            _gamemanager.ShowErrorModal("No answer", $"{selectedFriend.Name} was busy. No change.");
        }
        PopulateContacts(); // Refresh UI
        selectedFriend = null;
    }
    private void ShowFriendActionPanel(Friend friend)
    {
        friendActionPanel.SetActive(true);
        if (friendNameText != null)
            friendNameText.text = $"{friend.Name} Answered, what do you want to do?";

        // Remove previous listeners to avoid stacking
        askOutButton.onClick.RemoveAllListeners();
        askForDebtButton.onClick.RemoveAllListeners();
        payDebtButton.onClick.RemoveAllListeners();

        // Add new listeners
        askOutButton.onClick.AddListener(() => { ShowAskOutConfirmation(friend); friendActionPanel.SetActive(false); });
        askForDebtButton.onClick.AddListener(() => { AskForDebt(friend); friendActionPanel.SetActive(false); });
        payDebtButton.onClick.AddListener(() => { PayDebt(friend); friendActionPanel.SetActive(false); });
    }
    private void ShowAskOutConfirmation(Friend friend)
    {
        int timeCost = 2;
        int moneyCost = 50;
        string message = $"This action will cost:\n" +
                         $"<color=#006D77>Time: {timeCost} hours\nMoney: ${moneyCost}</color>\n\n" +
                         $"Do you want to proceed?";

        _gamemanager.ShowActionConfirmModal(
            $"Ask {friend.Name} Out",
            message,
            onConfirm: () => {
                AskOut(friend);
                friendActionPanel.SetActive(false);
            }
        );
    }
    private void AskOut(Friend friend)
    {
        int timeCost = 2;
        int moneyCost = 50;
        if (_resourceManager.HasEnoughResources(timeCost, 0, moneyCost))
        {
            _resourceManager.DecreaseHour(timeCost);
            _resourceManager.SpendMoney(moneyCost);

            friend.FriendshipLevel = Mathf.Min(friend.FriendshipLevel + 10, 100);
            _gamemanager.ShowActionResultModal(
            $"Asked {friend.Name} Out",
            $"{friend.Name} enjoyed your time together! Relationship increased."
            );
        }
        else
        {
            _gamemanager.ShowErrorModal("Not enough resources", "You don't have enough time or money.");
        }
        PopulateContacts();
        friendActionPanel.SetActive(false);
    }
   
    private void AskForDebt(Friend friend)
    {
        if (friend.Debt > 0)
        {
            _gamemanager.ShowErrorModal("Loan Exists", $"You already owe {friend.Name} {friend.Debt}$! Pay it back first.");
            friendActionPanel.SetActive(false);
            return;
        }

        int maxDebt = 500;
        int debtAmount = Mathf.RoundToInt((friend.FriendshipLevel / 100f) * maxDebt);

        if (debtAmount <= 0)
        {
            _gamemanager.ShowErrorModal("Too Low Relationship", $"{friend.Name} doesn't trust you enough to lend money.");
            friendActionPanel.SetActive(false);
            return;
        }

        string message = $"You can borrow <color=#006D77>${debtAmount}</color> from {friend.Name} based on your relationship.\nDo you want to proceed?";

        _gamemanager.ShowActionConfirmModal(
            $"Ask {friend.Name} For Money",
            message,
            onConfirm: () => {
                TryBorrowDebt(friend, debtAmount);
                friendActionPanel.SetActive(false);
            }
        );
    }
    private void TryBorrowDebt(Friend friend, int amount)
    {
        float chance = friend.FriendshipLevel / 100f;
        if (Random.value < chance)
        {
            relationsManager.AddDebts(friend, amount); // This should set friend.Debt += amount;
            friend.DebtDayCounter = 0; // Reset day counter
            _resourceManager.AddMoney(amount);
            PopulateContacts();
            _gamemanager.ShowActionResultModal(
                $"Asked {friend.Name} For Money",
                $"{friend.Name} lent you <color=#006D77>${amount}</color>!"
            );
        }
        else
        {
            _gamemanager.ShowActionResultModal(
                $"Asked {friend.Name} For Money",
                $"{friend.Name} refused to lend you money."
            );
        }
    }
    public void UpdateDebtsDaily()
    {
        foreach (var friend in relationsManager.FriendsList)
        {
            if (friend.Debt > 0)
            {
                friend.DebtDayCounter++;

                if (friend.DebtDayCounter > 7) // e.g., after 7 days
                {
                    // Strong penalty
                    friend.FriendshipLevel = Mathf.Max(friend.FriendshipLevel - 20, 0);
                    // Optionally, decrease reputation here as well
                    // playerStatistics.Reputation = Mathf.Max(playerStatistics.Reputation - 10, 0);
                }
            }
        }
    }
    private void PayDebt(Friend friend)
    {
        if (friend.Debt <= 0)
        {
            _gamemanager.ShowErrorModal("No debt", $"You don't owe {friend.Name} any money.");
            friendActionPanel.SetActive(false);
            return;
        }

        int debtAmount = friend.Debt;

        // Check if player has enough money
        if (!_resourceManager.HasEnoughResources(0, 0, debtAmount))
        {
            _gamemanager.ShowErrorModal("Not enough money", "You don't have enough money to pay the debt.");
            friendActionPanel.SetActive(false);
            return;
        }

        // Confirmation modal
        string message = $"Are you sure you want to pay back <color=#006D77>${debtAmount}</color> to {friend.Name}?";
        _gamemanager.ShowActionConfirmModal(
            $"Pay Debt to {friend.Name}",
            message,
            onConfirm: () =>
            {
                // Deduct money
                _resourceManager.SpendMoney(debtAmount);

                // Remove debt
                relationsManager.PayDebt(friend, debtAmount);
                friend.Debt = 0;
                friend.DebtDayCounter = 0;

                // Optionally, reward relationship/reputation
                friend.FriendshipLevel = Mathf.Min(friend.FriendshipLevel + 5, 100);

                PopulateContacts();
                _gamemanager.ShowActionResultModal(
                    $"Paid Debt to {friend.Name}",
                    $"You paid back your debt to {friend.Name}."
                );
                friendActionPanel.SetActive(false);
            }
        );
    }
    private void OnFriendSelected(Friend friend)
    {
        selectedFriend = friend;
        // Show call/ask money UI for this friend
        // Example: Open a panel with "Call" and "Ask for Money" buttons
        Debug.Log($"Selected friend: {friend.Name}");
        // Implement your call/ask logic here
    }
}
