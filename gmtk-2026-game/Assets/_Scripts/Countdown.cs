using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Countdown : MonoBehaviour
{
    [SerializeField] private float countdownTime = 10f;
    [SerializeField] private float currentCountdownTime = 10f;
    [SerializeField] private float countdownSpeed = 1f;
    [SerializeField] private TextMeshProUGUI countdownText;
    public UnityEvent onCountdownFinished;
    public float CountdownTime => countdownTime;
    public float CurrentCountdownTime => currentCountdownTime;
    public float CountdownSpeed => countdownSpeed;

    public void RestartCountdown()
    {
        currentCountdownTime = countdownTime;
        UpdateCountdownText();
    }
    
    public void SetCountdownTime(float time)
    {
        countdownTime = time;
        currentCountdownTime = time;
        UpdateCountdownText();
    }

    public void SetCountdownRunningSpeed(float speed)
    {
        countdownSpeed = speed;
    }

    private void UpdateCountdownText()
    {
        if (countdownText != null)
        {
            countdownText.text = currentCountdownTime.ToString("F2");
        }
    }

    void Start()
    {
        SetCountdownTime(countdownTime);
        UpdateCountdownText();
    }

    void Update()
    {
        currentCountdownTime -= countdownSpeed * Time.deltaTime;
        UpdateCountdownText();
        if (currentCountdownTime <= 0)
        {
            currentCountdownTime = 0;
            UpdateCountdownText();
            onCountdownFinished?.Invoke();
            // Countdown has reached zero, you can trigger an event or perform any action here
        }
    }
}
