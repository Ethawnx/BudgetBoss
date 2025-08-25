using UnityEngine;

[CreateAssetMenu(fileName = "NewGameEvent", menuName = "Events/Game Event")]
public class EventSO : ScriptableObject
{
    [Header("Stat Requirements")]
    public int minIntelligence;
    public int minCharisma;
    public int minPhysicalStrength;
    public int minMentalStrength;
    public int minReputaion;
    public int minFriends;
    public int minMoney;
    public enum EventType { Positive, Negative }
    // ... existing fields ...
    [Range(0f, 1f)]
    public float chance = 1f; // Probability for this event to occur (0 = never, 1 = always)

    public string eventName;
    [TextArea] public string description;
    public EventType type;
    //public Sprite icon;
    public bool fromFriend;
    public int minFriendshipLevel; // Only trigger from friends above this level
                                   // In EventSO.cs

    // Stat/resource/social changes
    public int moneyChange;
    public int energyChange;
    public int timeChange;
    public int mentalStrengthChange;
    public int physicalStrengthChange;
    public int charismaChange;
    public int intelligenceChange;

    // Social effects
    public bool loseFriend;
    public bool gainFriend;
    public bool affectReputation;
    public int reputationChange;

    public JobsSO jobOffer;
    // Add more fields as needed for your game
    public bool AreRequirementsMet(PlayerStatistics stats, RelationsManager relations, int currentMoney)
    {
        if (stats.CurrentIntelligence < minIntelligence) return false;
        if (stats.CurrentCharisma < minCharisma) return false;
        if (stats.CurrentPhysicalStrength < minPhysicalStrength) return false;
        if (stats.CurrentMentalStrength < minMentalStrength) return false;
        if (relations.Reputations < minReputaion) return false; // Reputation check
        if (relations.Friends < minFriends) return false;       // Friends check
        if (currentMoney < minMoney) return false;              // Money check
        return true;
    }
}