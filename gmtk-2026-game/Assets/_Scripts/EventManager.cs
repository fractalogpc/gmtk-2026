using UnityEngine;
using System.Collections.Generic;

public class EventManager : MonoBehaviour
{

    public GameObject smallFirePrefab;
    public GameObject mediumFirePrefab;
    public GameObject bigFirePrefab;
    public GameObject explosionPrefab;

    public E_FireComponent[] fireComponents;

    float updateInterval = 0.25f; // Update every second
    float timer = 0f;

    public void Start()
    {
        foreach (var fireComponent in fireComponents)
        {
            fireComponent.Initialize(smallFirePrefab, mediumFirePrefab, bigFirePrefab, explosionPrefab);
        }


        TriggerFireEvent(2); // Example trigger with intensity 2
    }

    public void TriggerFireEvent(int intensity)
    {
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

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            HandleActiveFireComponents();
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
                int otherFireIntensity = fireComponents[j].currentFireIntensity;
                if (otherFireIntensity == 0) continue;

                float distance = Vector3.Distance(posI, fireComponents[j].transform.position);

                switch (otherFireIntensity)
                {
                    case 1:
                        if (distance < 5f) intensity += 2.5f;
                        break;
                    case 2:
                        if (distance < 8f) intensity += 4f;
                        break;
                    case 3:
                        if (distance < 10f) intensity += 7f;
                        break;
                }
            }
            fireComponents[i].SetExternalFlameInfluence(intensity);
        }
    }
}
