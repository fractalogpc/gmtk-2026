using UnityEngine;

public class NixieClock : MonoBehaviour
{

    [System.Serializable]
    public struct NixieTube
    {
        public Renderer[] numbers;
        public bool colon;
        public int value; // Seconds per number
    }

    [SerializeField] private NixieTube[] tubes;
    [SerializeField] private float blinkInterval = 0.5f;
    [SerializeField] private Material offMaterial;
    [SerializeField] private Material onMaterial;

    private float timer = 0f;

    public void StartTimer(float countdown)
    {
        timer = countdown;
    }

    private void Start()
    {
        StartTimer(60f); // Start with a 60-second countdown
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
        {
            // Counting down
            bool colonOn = timer % blinkInterval < blinkInterval / 2;
            for (int i = 0; i < tubes.Length; i++)
            {
                if (tubes[i].colon)
                {
                    for (int j = 0; j < tubes[i].numbers.Length; j++)
                    {
                        tubes[i].numbers[j].material = colonOn ? onMaterial : offMaterial;
                    }
                }
                else
                {
                    int activeNumber = Mathf.FloorToInt(timer / tubes[i].value) % tubes[i].numbers.Length;
                    for (int j = 0; j < tubes[i].numbers.Length; j++)
                    {
                        tubes[i].numbers[j].material = j == activeNumber ? onMaterial : offMaterial;
                    }
                }
            }
        }
        else
        {
            // Expired; set all to 0 and don't blink
            for (int i = 0; i < tubes.Length; i++)
            {
                for (int j = 0; j < tubes[i].numbers.Length; j++)
                {
                    if (tubes[i].colon)
                    {
                        tubes[i].numbers[j].material = onMaterial;
                    }
                    else
                    {
                        tubes[i].numbers[j].material = j == 0 ? onMaterial : offMaterial;
                    }
                }
            }
        }
    }
}
