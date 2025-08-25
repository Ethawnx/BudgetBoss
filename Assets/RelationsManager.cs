using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static ResourceManager;
using static UnityEngine.Rendering.DebugUI;

[System.Serializable]
public class Friend
{
    public string Name;
    public Sprite Icon;
    public int FriendshipLevel; // 0-100
    public int Debt;
    public int DebtDayCounter;
    // Add more fields as needed (e.g., trust, debt, etc.)
}
public class RelationsManager : MonoBehaviour
{
    public enum SocialType
    {
        Friend,
        Associate,
        Debt,
        Investments,
        Reputations
    }

    private static readonly string[] firstNames = { "Alex", "Sam", "Jamie", "Taylor", "Jordan", "Morgan", "Casey", "Riley" };
    private static readonly string[] lastNames = { "Smith", "Lee", "Patel", "Kim", "Garcia", "Brown", "Nguyen", "Martinez" };
    private Sprite[] friendIcons;
    public int Friends { get; private set; }
    public int Associates { get; private set; }
    public int Debts { get; private set; }
    public int Investments { get; private set; }
    public int Reputations { get; private set; }

    [Header("Manager References")]
    PhoneManager _phoneManager;
    [SerializeField] private GoalManager _goalManager;

    [SerializeField] GameObject SocialAmountHolder;

    private TextMeshProUGUI[] _socialAmountHolders;

    [HideInInspector] public List<Friend> FriendsList = new();

    //Goal 6 variables
    private bool fameGoalCompleted = false;
    private int fameGoalValue = 10000; // Fame threshold
    public void SetPhoneManager(PhoneManager phoneManager)
    {
        _phoneManager = phoneManager;
    }
    public void AddSocialStat(SocialType SocialStatToAdd, int amountToAdd)
    {
        switch (SocialStatToAdd)
        {
            case SocialType.Friend:
                AddFriends(amountToAdd);
                break;
            case SocialType.Associate:
                AddAssociates(amountToAdd);
                break;
        }
    }
    private void Awake()
    {
        LoadFriendIcons();
        _phoneManager = FindAnyObjectByType<PhoneManager>();
        Friends = 0;
        Associates = 0;
        Debts = 0;
        Investments = 0;
        Reputations = 0;
        _socialAmountHolders = SocialAmountHolder?.GetComponentsInChildren<TextMeshProUGUI>(true);
    }
    public List<Friend> CreateRandomFriend(int amount)
    {
        var createdFriends = new List<Friend>();
        for (int i = 0; i < amount; i++)
        {
            var friend = new Friend
            {
                Name = GenerateRandomName(),
                Icon = GetRandomIcon(),
                FriendshipLevel = Random.Range(10, 30)
            };
            FriendsList.Add(friend);
            createdFriends.Add(friend);
        }
        UpdateSocialStats();
        if (_phoneManager != null)
            _phoneManager.PopulateContacts();
        return createdFriends;
    }
    public void UpdateSocialStats()
    {
        if (_socialAmountHolders == null) return;
        if (_socialAmountHolders.Length >= 3)
        {
            _socialAmountHolders[0].text = Friends.ToString();
            _socialAmountHolders[1].text = Associates.ToString();
            _socialAmountHolders[2].text = Debts.ToString();
            _socialAmountHolders[3].text = Investments.ToString();
            _socialAmountHolders[4].text = Reputations.ToString();
        }
    }
    public void AddReputation(int amount)
    {
        Reputations += amount;
        UpdateSocialStats();
        CheckFameGoal();
    }
    private void CheckFameGoal()
    {
        if (!fameGoalCompleted && Reputations >= fameGoalValue)
        {
            fameGoalCompleted = true;
            Debug.Log("Goal achieved: Fame 10,000+!");
            _goalManager.CompleteGoal(5); // Use the correct index for your fame goal
        }
    }
    public void AddFriends(int amount)
    {
        Friends += amount;
        CreateRandomFriend(amount);
    }
    public void AddAssociates(int amount)
    {
        Associates += amount;
    }
    public void AddDebts(Friend friend, int amount)
    {
        if (friend == null || amount <= 0)
            return;

        friend.Debt += amount;
        Debts += amount; // Update total debts counter

        UpdateSocialStats();
    }
    public bool HasDebt(Friend friend)
    {
        return friend != null && friend.Debt > 0;
    }

    public int GetDebtAmount(Friend friend)
    {
        return friend != null ? friend.Debt : 0;
    }

    public void PayDebt(Friend friend, int amount)
    {
        if (friend == null || amount <= 0 || friend.Debt < amount)
            return;

        friend.Debt -= amount;
        if (friend.Debt < 0) friend.Debt = 0;
        friend.DebtDayCounter = 0;
        Debts -= amount;
        if (Debts < 0) Debts = 0;
        UpdateSocialStats();
        AddReputation(amount);
    }
    private void LoadFriendIcons()
    {
        friendIcons = Resources.LoadAll<Sprite>("FriendIcons");
    }
    private string GenerateRandomName()
    {
        string first = firstNames[Random.Range(0, firstNames.Length)];
        string last = lastNames[Random.Range(0, lastNames.Length)];
        return $"{first} {last}";
    }
    private Sprite GetRandomIcon()
    {
        if (friendIcons == null || friendIcons.Length == 0) LoadFriendIcons();
        var test = friendIcons[Random.Range(0, friendIcons.Length)];
        Debug.Log(test.name);
        return test;
    }
}
