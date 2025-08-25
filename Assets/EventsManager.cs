using System.Collections.Generic;
using System.Resources;
using TMPro;
using UnityEngine;
public class TriggeredEvent
{
    public EventSO eventSO;
    public Friend sourceFriend; // null if not from a friend
    public Friend lostFriend;  // the friend that was lost (if any)
    public System.DateTime occurredAt;
    public TriggeredEvent(EventSO evt, Friend friend, Friend lost = null)
    {
        eventSO = evt;
        sourceFriend = friend;
        lostFriend = lost;
        occurredAt = System.DateTime.Now;
    }
}
public class EventsManager : MonoBehaviour
{
    [Tooltip("should be between 0,1")]
    [Range(0f, 1f)]
    public float RandomEventChance;
    public List<TriggeredEvent> MessageEvents => messageEvents;

    private List<GameObject> eventEntries = new();

    private List<EventSO> allEvents;

    private List<TriggeredEvent> messageEvents = new();
    private GameManager gameManager;
    private ResourceManager resourceManager;
    private RelationsManager relationsManager;
    private void Awake()
    {
        allEvents = new List<EventSO>(Resources.LoadAll<EventSO>("Events"));
        gameManager = FindAnyObjectByType<GameManager>();
        resourceManager = FindAnyObjectByType<ResourceManager>();
        relationsManager = FindAnyObjectByType<RelationsManager>();
    }

    private void Start()
    {
    }
    public void TryTriggerRandomEvent(List<Friend> friends)
    {
        if (Random.value > RandomEventChance)
            return;

        // Get references to player stats, relations, and money
        var stats = FindAnyObjectByType<PlayerStatistics>();
        var relations = relationsManager;
        int currentMoney = FindAnyObjectByType<ResourceManager>().Balance;

        // Filter all events by stat requirements
        List<EventSO> possibleEvents = allEvents.FindAll(e => e.AreRequirementsMet(stats, relations, currentMoney));

        // Always filter friend-based events by minFriendshipLevel
        Friend trustedFriend = null;
        possibleEvents = possibleEvents.FindAll(e => {
            if (!e.fromFriend) return true;
            // Find a friend who meets the minFriendshipLevel for this event
            var friend = friends.Find(f => f.FriendshipLevel >= e.minFriendshipLevel);
            if (friend != null)
            {
                trustedFriend = friend;
                return true;
            }
            return false;
        });

        // Filter by each event's chance
        List<EventSO> eligibleEvents = new();
        foreach (var evt in possibleEvents)
        {
            if (Random.value <= evt.chance)
                eligibleEvents.Add(evt);
        }

        if (eligibleEvents.Count == 0)
            return; // No event passed its chance check

        var selectedEvent = eligibleEvents[Random.Range(0, eligibleEvents.Count)];
        ApplyEvent(selectedEvent, trustedFriend);
    }

    public void ApplyEvent(EventSO evt, Friend sourceFriend = null)
    {
        Friend lostFriend = null;
        // Resource changes
        if (evt.moneyChange != 0)
            resourceManager.AddMoney(evt.moneyChange);
        if (evt.energyChange != 0)
            resourceManager.AddEnergy(evt.energyChange);
        if (evt.timeChange != 0)
            resourceManager.DecreaseHour(Mathf.Abs(evt.timeChange)); // If negative, lose time; if positive, gain time

        // Stat changes
        var stats = FindAnyObjectByType<PlayerStatistics>();
        if (evt.mentalStrengthChange != 0)
            stats.AddAttributeStat(PlayerStatistics.AttributeStats.MentalStrength, evt.mentalStrengthChange);
        if (evt.physicalStrengthChange != 0)
            stats.AddAttributeStat(PlayerStatistics.AttributeStats.PhysicalStrength, evt.physicalStrengthChange);
        if (evt.charismaChange != 0)
            stats.AddAttributeStat(PlayerStatistics.AttributeStats.Charisma, evt.charismaChange);
        if (evt.intelligenceChange != 0)
            stats.AddAttributeStat(PlayerStatistics.AttributeStats.Intelligence, evt.intelligenceChange);

        // Social effects
        if (evt.loseFriend)
        {
            if (relationsManager.FriendsList.Count > 0)
            {
                int idx = Random.Range(0, relationsManager.FriendsList.Count);
                lostFriend = relationsManager.FriendsList[idx];
                relationsManager.FriendsList.RemoveAt(idx);
                Debug.Log($"Lost friend: {lostFriend.Name}");
            }
        }
        if (evt.gainFriend)
            relationsManager.AddFriends(1);
        //if (evt.affectReputation)
        //relationsManager.Reputations += evt.reputationChange;

        // Log the event with the lost friend
        messageEvents.Add(new TriggeredEvent(evt, sourceFriend, lostFriend));
        // Handle job offer
        if (evt.jobOffer != null)
        {
            string friendName = sourceFriend != null ? sourceFriend.Name : "a friend";
            string jobName = evt.jobOffer.JobName;
            string message = $"{friendName} has recommended you for a new job: <b>{jobName}</b>.\nDo you want to accept the offer?";

            gameManager.ShowActionConfirmModal(
                "Job Offer",
                message,
                onConfirm: () =>
                {
                    var jobManager = FindAnyObjectByType<JobManager>();
                    jobManager.SetCurrentJob(evt.jobOffer);
                    gameManager.ShowActionResultModal("Job Accepted", $"You are now working as a {jobName}!");
                },
                onCancel: () =>
                {
                    gameManager.ShowActionResultModal("Job Declined", "You declined the job offer.");
                }
            );
        }
        // Optionally, show a modal or add to messages
        gameManager.ShowActionResultModal("New Event Happened", "Check out your phone to see what happened exactly");
    }

}
