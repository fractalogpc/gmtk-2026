using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EventManager : MonoBehaviour
{

    public static EventManager Instance { get; private set; }

    [Header("Fire Event")]
    public GameObject HeldExtinguisherObj;
    public GameObject InteractableExtinguisherObj;
    public Interactable InteractableExtinguisherScript;

    public GameObject smallFirePrefab;
    public GameObject mediumFirePrefab;
    public GameObject bigFirePrefab;
    public GameObject explosionPrefab;

    private E_FireComponent[] fireComponents;

    bool isExtinguisherHeld = false;
    bool isExtinguisherInteractable = false;

    [Header("Power Outage Event")]
    [SerializeField] private GameObject[] lightsToTurnOff;
    [SerializeField] private GameObject[] emergencyLightsToTurnOn;


    float updateInterval = 0.25f; // Update every second
    float timer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void Start()
    {
        fireComponents = FindObjectsByType<E_FireComponent>(FindObjectsSortMode.None);

        foreach (var fireComponent in fireComponents)
        {
            fireComponent.Initialize(smallFirePrefab, mediumFirePrefab, bigFirePrefab, explosionPrefab);
        }

        // TriggerFireEvent(4); // Example trigger with intensity 2
        // Invoke(nameof(TriggerPowerOutageEvent), 5f); // Example trigger after 5 seconds
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            HandleActiveFireComponents();

            if (AllFiresExtinguished())
            {
                DropExtinguisher();
            }
        }
    }

    #region Fire

    public void TriggerFireEvent(int intensity)
    {
        if (!isExtinguisherHeld && !isExtinguisherInteractable)
        {
            isExtinguisherInteractable = true;
            InteractableExtinguisherScript.enabled = true;
        }

        int numberOfComponentsToTrigger = Mathf.Clamp(intensity, 0, fireComponents.Length);

        var indices = new List<int>();
        for (int i = 0; i < fireComponents.Length; i++)
        {
            indices.Add(i);
        }

        for (int i = 0; i < numberOfComponentsToTrigger; i++)
        {
            int randomIdx = Random.Range(0, indices.Count);
            int componentIndex = indices[randomIdx];

            // Trigger the fire event on the selected component at a designated intensity
            int fireIntensity = Random.Range(intensity / 2, intensity + 1); // idk this is random placeholder
            fireComponents[componentIndex].Trigger(fireIntensity);
            indices.RemoveAt(randomIdx);
        }
    }

    public void PickUpExtinguisher()
    {
        isExtinguisherHeld = true;
        HeldExtinguisherObj.SetActive(true);
        InteractableExtinguisherObj.SetActive(false);
        InteractableExtinguisherScript.enabled = false;
    }

    private void DropExtinguisher()
    {
        isExtinguisherHeld = false;
        isExtinguisherInteractable = false;
        HeldExtinguisherObj.SetActive(false);
        InteractableExtinguisherObj.SetActive(true);
        InteractableExtinguisherScript.enabled = false;

        // All fires are out — clear the fire tutorial lesson.
        if (TutorialManager.Instance != null) TutorialManager.Instance.CompleteLesson("fire");
    }

    private bool AllFiresExtinguished()
    {
        foreach (var fireComponent in fireComponents)
        {
            if (fireComponent.currentFireIntensity > 0)
            {
                return false;
            }
        }
        return true;
    }

    private void HandleActiveFireComponents()
    {
        for (int i = 0; i < fireComponents.Length; i++)
        {
            float intensity = 0f;
            Vector3 posI = fireComponents[i].transform.position;

            for (int j = 0; j < fireComponents.Length; j++)
            {
                if (i == j) continue;
                if (fireComponents[j].BeingExtinguished) continue;
                int otherFireIntensity = fireComponents[j].currentFireIntensity;
                if (otherFireIntensity == 0) continue;

                float distance = Vector3.Distance(posI, fireComponents[j].transform.position);

                switch (otherFireIntensity)
                {
                    case 1:
                        if (distance < 2f) intensity += 2f;
                        break;
                    case 2:
                        if (distance < 4f) intensity += 3f;
                        break;
                    case 3:
                        if (distance < 6f) intensity += 6f;
                        break;
                }
            }
            fireComponents[i].SetExternalFlameInfluence(intensity);
        }
    }
    #endregion

    #region Power

    public void TriggerPowerOutageEvent()
    {
        List<GameObject> lightsToTurnOffList = new List<GameObject>(lightsToTurnOff);
        for (int i = lightsToTurnOffList.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            GameObject temp = lightsToTurnOffList[i];
            lightsToTurnOffList[i] = lightsToTurnOffList[randomIndex];
            lightsToTurnOffList[randomIndex] = temp;
        }

        StartCoroutine(TurnOffLightsCoroutine(lightsToTurnOffList));
    }

    private IEnumerator TurnOffLightsCoroutine(List<GameObject> lightsToTurnOffList)
    {
        foreach (var light in lightsToTurnOffList)
        {
            light.SetActive(false);
            yield return new WaitForSeconds(Random.Range(0.01f, 0.1f)); // Adjust the delay as needed
        }

        yield return new WaitForSeconds(1f); // Wait for a moment before turning on emergency lights

        foreach (var emergencyLight in emergencyLightsToTurnOn)
        {
            emergencyLight.SetActive(true);
            yield return new WaitForSeconds(Random.Range(0.2f, 0.25f)); // Adjust the delay as needed
        }
    }

    public void RestorePower()
    {
        StopCoroutine(nameof(TurnOffLightsCoroutine));
        StopCoroutine(nameof(RestorePowerCoroutine));

        List<GameObject> lightsToTurnOnList = new List<GameObject>(lightsToTurnOff);
        for (int i = lightsToTurnOnList.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (lightsToTurnOnList[i], lightsToTurnOnList[randomIndex]) = (lightsToTurnOnList[randomIndex], lightsToTurnOnList[i]);
        }

        StartCoroutine(RestorePowerCoroutine(lightsToTurnOnList));
    }

    private IEnumerator RestorePowerCoroutine(List<GameObject> lightsToTurnOnList)
    {
        foreach (var emergencyLight in emergencyLightsToTurnOn)
        {
            emergencyLight.SetActive(false);
            yield return new WaitForSeconds(Random.Range(0.2f, 0.25f));
        }

        yield return new WaitForSeconds(1f);

        foreach (var light in lightsToTurnOnList)
        {
            light.SetActive(true);
            yield return new WaitForSeconds(Random.Range(0.01f, 0.1f));
        }
    }

    #endregion
}
