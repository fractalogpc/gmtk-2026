using FMODUnity;
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
    [SerializeField] private StudioEventEmitter tickEmitter;
    [SerializeField] private StudioEventEmitter endEmitter;

    public float Timer => timer;
    private float timer = 0f;

    public void StartTimer(float countdown)
    {
        timer = countdown;
        tickEmitter?.Play();
    }

    public void StopTimer()
    {
        timer = 0f;
        endEmitter?.Play();
    }

    private void Start()
    {
        StartTimer(1f);
    }

    private void Update()
    {
        float oldTimer = timer;
        timer -= Time.deltaTime;

        if (timer > 0f)
        {
            if (Mathf.CeilToInt(oldTimer / 1f) != Mathf.CeilToInt(timer / 1f))
            {
                tickEmitter?.Play();
            }
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
            if (oldTimer > 0f)
            {
                endEmitter?.Play();
            }
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
