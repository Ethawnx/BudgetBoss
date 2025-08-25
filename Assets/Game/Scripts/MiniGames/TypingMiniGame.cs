using Michsky.MUIP;
using TMPro;
using UnityEngine;

public class TypingMiniGame : MiniGameBase
{
    [SerializeField] TMP_Text targetText;
    [SerializeField] TMP_InputField inputField;
    [SerializeField] float timeLimit = 30f;
    [SerializeField] SliderManager TimeSlider;
    private static readonly string[] wordPool = new string[]
{
    "apple", "banana", "orange", "grape", "melon",
    "keyboard", "monitor", "computer", "mouse", "screen",
    "unity", "script", "object", "component", "project",
    "challenge", "success", "random", "typing", "minigame",
    // Technology
    "algorithm", "binary", "network", "server", "client", "database", "variable", "function", "array", "loop",
    // Nature
    "forest", "mountain", "river", "ocean", "desert", "valley", "island", "thunder", "breeze", "blossom",
    // Everyday Objects
    "pencil", "notebook", "wallet", "bottle", "window", "mirror", "pillow", "blanket", "ladder", "hammer",
    // Colors
    "crimson", "turquoise", "violet", "indigo", "amber", "emerald", "sapphire", "coral", "beige", "maroon",
    // Animals
    "elephant", "giraffe", "dolphin", "squirrel", "rabbit", "falcon", "penguin", "otter", "jaguar", "turtle",
    // Food
    "sandwich", "pizza", "burger", "salad", "sushi", "pancake", "omelette", "sausage", "muffin", "cookie",
    // Adjectives
    "clever", "bright", "silent", "rapid", "gentle", "fierce", "smooth", "rough", "eager", "patient"

};

    private float timer;
    private bool _isActive;
    public override void Initialize(float difficulty)
    {
        int wordCount = Mathf.Clamp(Mathf.RoundToInt(difficulty * 3), 4, 10); // 1-5 words
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < wordCount; i++)
        {
            if (i > 0) sb.Append(" ");
            sb.Append(wordPool[Random.Range(0, wordPool.Length)]);
        }
        targetText.text = sb.ToString();

        timeLimit *= difficulty; // Harder jobs = less time
        inputField.onValueChanged.AddListener(CheckText);
        timer = timeLimit;
        TimeSlider.mainSlider.maxValue = timeLimit;
        _isActive = true;
    }

    void Update()
    {
        if (!_isActive) return;

        timer -= Time.deltaTime;

        TimeSlider.mainSlider.value = timer;
        TimeSlider.UpdateUI();

        if (timer <= 0)
        {
            _isActive = false;
            EndGame(false);
        }
    }

    void CheckText(string input)
    {
        if (input == targetText.text)
        {
            _isActive = false;
            EndGame(true);
        }
    }
}