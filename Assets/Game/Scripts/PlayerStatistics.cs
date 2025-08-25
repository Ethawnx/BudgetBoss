using System;
using TMPro;
using UnityEngine;

public class PlayerStatistics : MonoBehaviour
{
    public int DefaultIntelligence = 5;
    public int DefaultCharisma = 5;
    public int DefaultPhysicalStrength = 5;
    public int DefaultMentalStrength = 5;

    [SerializeField] RelationsManager _relationsManager;

    [Header("UI GameObject References")]
    [SerializeField] GameObject AttributesAmountHolder;
    

    public int CurrentIntelligence { get; private set; }
    public int CurrentCharisma { get; private set; }
    public int CurrentPhysicalStrength { get; private set; }
    public int CurrentMentalStrength { get; private set; }

    public int DaysSinceLastEat { get; private set; } = 0;
    public int DaysSinceLastHygiene { get; private set; } = 0;

    private TextMeshProUGUI[] _attributesAmountHolders;
    

    public enum AttributeStats
    {
        None,
        Intelligence,
        Charisma,
        PhysicalStrength,
        MentalStrength
    }

    private void Awake()
    {
        CurrentIntelligence = DefaultIntelligence;
        CurrentCharisma = DefaultCharisma;
        CurrentPhysicalStrength = DefaultPhysicalStrength;
        CurrentMentalStrength = DefaultMentalStrength;

        _attributesAmountHolders = AttributesAmountHolder?.GetComponentsInChildren<TextMeshProUGUI>(true);
    }
    public void OnDayPassed()
    {
        DaysSinceLastEat++;
        DaysSinceLastHygiene++;
    }
    public void OnEat()
    {
        DaysSinceLastEat = 0;
    }

    public void OnHygiene()
    {
        DaysSinceLastHygiene = 0;
    }
    // Attributes Stats Functions
    public void UpdateAttributesStats()
    {
        if (_attributesAmountHolders == null) return;
        if (_attributesAmountHolders.Length >= 4)
        {
            _attributesAmountHolders[0].text = CurrentIntelligence.ToString();
            _attributesAmountHolders[1].text = CurrentCharisma.ToString();
            _attributesAmountHolders[2].text = CurrentPhysicalStrength.ToString();
            _attributesAmountHolders[3].text = CurrentMentalStrength.ToString();
        }
    }

    public void AddAttributeStat(AttributeStats stat, int amount)
    {
        switch (stat)
        {
            case AttributeStats.Intelligence:
                CurrentIntelligence += amount;
                break;
            case AttributeStats.Charisma:
                CurrentCharisma += amount;
                break;
            case AttributeStats.PhysicalStrength:
                CurrentPhysicalStrength += amount;
                break;
            case AttributeStats.MentalStrength:
                CurrentMentalStrength += amount;
                break;
        }
    }

    public int GetStatLevel(AttributeStats stat)
    {
        return stat switch
        {
            AttributeStats.Intelligence => CurrentIntelligence,
            AttributeStats.Charisma => CurrentCharisma,
            AttributeStats.PhysicalStrength => CurrentPhysicalStrength,
            AttributeStats.MentalStrength => CurrentMentalStrength,
            _ => 0
        };
    }
}
