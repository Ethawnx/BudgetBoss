using System;
using TMPro;
using UnityEngine;

public class CalendarManager : MonoBehaviour
{
    // Add this event
    public static event Action OnDayEnd;

    [SerializeField] TextMeshProUGUI CalendarTextHolder;
    // This field stores the current date and time.
    // It gets initialized to the player's local system time.
    private DateTime currentDate;
    private DateTime startingDate;
    private int dayCounter = 0;
    void Start()
    {
        // Initialize currentDate with the player's local real-life time.
        startingDate = DateTime.Now;
        currentDate = startingDate;
        DisplayCurrentDate();
    }

    // This method prints the current date and time to the Unity Console.
    // You can replace this with code that updates a UI text element if desired.
    void DisplayCurrentDate()
    {
        // "M" format specifier prints the Month/day
        CalendarTextHolder.text = currentDate.ToString("M");
        Debug.Log("Current Date and Time: " + currentDate.ToString("M"));
    }

    // Call this method (e.g., from a UI button) to move the calendar to the next day.
    public void NextDay()
    {
        // Add one day to the current date.
        currentDate = currentDate.AddDays(1);
        dayCounter++;
        //Debug.Log("Moved to Next Day: " + currentDate.ToString("D")); // "D" gives long date format.
        CalendarTextHolder.text = currentDate.ToString("M");
        OnDayEnd?.Invoke();
    }

    // Call this method (e.g., from a UI button) to move the calendar to the previous day.
    public void PreviousDay()
    {
        // Subtract one day from the current date.
        currentDate = currentDate.AddDays(-1);
        Debug.Log("Moved to Previous Day: " + currentDate.ToString("D"));
    }
    public bool IsOneWeekPassed()
    {
        if (dayCounter >= 7)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public void ResetDayCounter()
    {
        dayCounter = 0;
    }
    // Optionally, if you need the time to update continuously (for example, a real-time clock),
    // you can update currentDate each frame. Remove or comment this out if you want the date 
    // to remain fixed until you change it using NextDay/PreviousDay.
    void Update()
    {
        // Uncomment the following line to update currentDate every frame:
        // currentDate = DateTime.Now;
    }
}
