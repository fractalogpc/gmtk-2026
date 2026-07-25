using UnityEngine;
using System.Collections.Generic;

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
    }

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
}
