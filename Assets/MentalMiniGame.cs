using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;

public class MentalWorkMiniGame : MiniGameBase
{
    [Header("UI References")]
    public List<Button> sequenceButtons; // Assign 4+ buttons in Inspector
    public TextMeshProUGUI infoText;
    public Slider progressBar;

    [Header("Settings")]
    public int sequenceLength = 4;
    public float showTime = 1.0f; // seconds per item

    private List<int> sequence = new();
    private int currentInputIndex = 0;
    private int currentRound = 0;
    private bool isShowing = false;
    private bool isActive = false;

    public override void Initialize(float difficulty)
    {
        sequenceLength = Mathf.Clamp(Mathf.RoundToInt(sequenceLength * difficulty), 3, sequenceButtons.Count);
        currentRound = 0;
        progressBar.value = 0;
        StartCoroutine(NextRound());
    }

    System.Collections.IEnumerator NextRound()
    {
        isActive = false;
        isShowing = true;
        sequence.Clear();
        currentInputIndex = 0;

        // Generate random sequence
        for (int i = 0; i < sequenceLength; i++)
            sequence.Add(Random.Range(0, sequenceButtons.Count));

        infoText.text = "Memorize the sequence!";
        // Show sequence
        for (int i = 0; i < sequence.Count; i++)
        {
            HighlightButton(sequence[i], true);
            yield return new WaitForSeconds(showTime);
            HighlightButton(sequence[i], false);
            yield return new WaitForSeconds(0.2f);
        }

        infoText.text = "Repeat the sequence!";
        isShowing = false;
        isActive = true;
        currentInputIndex = 0;

        // Enable input
        for (int i = 0; i < sequenceButtons.Count; i++)
        {
            int idx = i;
            sequenceButtons[i].onClick.RemoveAllListeners();
            sequenceButtons[i].onClick.AddListener(() => OnButtonPressed(idx));
        }
    }

    void HighlightButton(int index, bool highlight)
    {
        var colors = sequenceButtons[index].colors;
        sequenceButtons[index].image.color = highlight ? colors.highlightedColor : colors.normalColor;
    }

    void OnButtonPressed(int index)
    {
        if (!isActive || isShowing) return;

        if (index == sequence[currentInputIndex])
        {
            currentInputIndex++;
            progressBar.value = (float)currentInputIndex / sequenceLength;
            if (currentInputIndex >= sequenceLength)
            {
                EndMiniGame(true);
            }
        }
        else
        {
            EndMiniGame(false);
        }
    }

    void EndMiniGame(bool success)
    {
        isActive = false;
        OnMiniGameCompleted.Invoke(success);
        infoText.text = success ? "Success!" : "Failed!";
        // Optionally, disable buttons here
        foreach (var btn in sequenceButtons)
            btn.onClick.RemoveAllListeners();
    }
}

