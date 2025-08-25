using UnityEngine;

[CreateAssetMenu(fileName = "NewJob", menuName = "Jobs/Create Job")]
public class JobsSO : ScriptableObject
{
    public string JobName;

    public int RequiredIntelligence;
    public int RequiredCharisma;
    public int RequiredPhysical;
    public int RequiredMental;

    //The Income is Per Month
    public int BaseIncome;
    public int WorkingHours;
    public int EnergyCost;

    public GameObject miniGamePrefab;
    public float miniGameDifficulty = 1f;
}
