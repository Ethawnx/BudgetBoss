using UnityEngine;
using UnityEngine.Events;

public abstract class MiniGameBase : MonoBehaviour
{
    public UnityEvent<bool> OnMiniGameCompleted; // bool = success

    // Called when mini-game starts
    public abstract void Initialize(float difficulty);
    protected void EndGame(bool success)
    {
        OnMiniGameCompleted.Invoke(success); // Trigger event
        gameObject.SetActive(false); // Hide but don’t destroy yet
    }
}