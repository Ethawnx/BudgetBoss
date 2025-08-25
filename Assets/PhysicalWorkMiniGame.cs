using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Michsky.MUIP;

public class PhysicalWorkMiniGame : MiniGameBase
{
    [Header("UI References")]
    public Slider progressBar;
    [SerializeField] SliderManager TimeSlider;
    public Button mashButton; // Optional, for mobile/touch

    [Header("Settings")]
    public float timeLimit = 5f; // seconds
    public int pressesToWin = 30;

    private int currentPresses = 0;
    private float timer = 0f;
    private bool isActive = false;

    public override void Initialize(float difficulty)
    {
        pressesToWin = Mathf.RoundToInt(pressesToWin * difficulty);
        timer = timeLimit;
        currentPresses = 0;
        progressBar.value = 0;
        isActive = true;
        if (TimeSlider != null)
        {
            TimeSlider.mainSlider.maxValue = timeLimit;
            TimeSlider.mainSlider.value = timeLimit;
        }
        if (mashButton != null)
            mashButton.onClick.AddListener(OnMash);
    }

    void Update()
    {
        if (!isActive) return;

        timer -= Time.deltaTime;
        if (TimeSlider != null)
            TimeSlider.mainSlider.value = Mathf.Max(timer, 0f);

        TimeSlider.UpdateUI();

        if (Input.GetKeyDown(KeyCode.Space))
            OnMash();

        if (timer <= 0f)
            EndMiniGame();
    }

    void OnMash()
    {
        if (!isActive) return;
        currentPresses++;
        progressBar.value = (float)currentPresses / pressesToWin;

        if (currentPresses >= pressesToWin)
            EndMiniGame();
    }

    void EndMiniGame()
    {
        isActive = false;
        bool success = currentPresses >= pressesToWin;
        OnMiniGameCompleted.Invoke(success);
        if (mashButton != null)
            mashButton.onClick.RemoveListener(OnMash);
    }
}
