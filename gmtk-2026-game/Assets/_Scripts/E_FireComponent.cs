using UnityEngine;
using UnityEngine.Events;

public class E_FireComponent : MonoBehaviour
{

    private GameObject smallFirePrefab;
    private GameObject mediumFirePrefab;
    private GameObject bigFirePrefab;
    private GameObject explosionPrefab;

    private GameObject currentFireInstance;
    public int currentFireIntensity = 0;

    bool isTriggered = false;

    float fireAmount = 0f; // if this hits 100 then it upgrades to the next fire level

    private float internalFlameInfluence = 0f;
    public float externalFlameInfluence = 0f;

    public bool BeingExtinguished = false;

    public UnityEvent OnFireStarted;
    public UnityEvent OnFireIncreased;
    public UnityEvent OnFireExtinguished;

    public void Initialize(GameObject smallFirePrefab, GameObject mediumFirePrefab, GameObject bigFirePrefab, GameObject explosionPrefab)
    {
        this.smallFirePrefab = smallFirePrefab;
        this.mediumFirePrefab = mediumFirePrefab;
        this.bigFirePrefab = bigFirePrefab;
        this.explosionPrefab = explosionPrefab;
    }

    public void Trigger(int intensity)
    {
        OnFireStarted.Invoke();
        isTriggered = true;
        currentFireIntensity = intensity;

        internalFlameInfluence = 6f;

        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        explosion.transform.parent = transform;

        GameObject firePrefabToUse = null;

        switch (intensity)
        {
            case 1:
                firePrefabToUse = smallFirePrefab;
                break;
            case 2:
                firePrefabToUse = mediumFirePrefab;
                break;
            case 3:
                firePrefabToUse = bigFirePrefab;
                break;
        }

        GameObject fire = Instantiate(firePrefabToUse, transform.position, Quaternion.identity);
        fire.transform.parent = transform;

        currentFireInstance = fire;
    }

    private void Update()
    {
        float effectiveExternal = BeingExtinguished ? -80f : externalFlameInfluence;
        fireAmount += (internalFlameInfluence + effectiveExternal) * Time.deltaTime;

        if (fireAmount >= 100f)
        {
            if (currentFireIntensity < 3)
            {
                if (!isTriggered) Trigger(1);
                else IncreaseFlame();
                fireAmount = 0f;
            }
            else
            {
                fireAmount = 100f;
            }
        }
        else if (fireAmount <= 0f)
        {
            if (currentFireIntensity == 0)
            {
                fireAmount = 0f;
            }
            else if (currentFireIntensity == 1)
            {
                ResetFire();
            }
            else
            {
                DecreaseFlame();
            }
        }

        if (internalFlameInfluence == 0f && externalFlameInfluence == 0f && currentFireIntensity == 0)
        {
            fireAmount = 0f;
        }
    }

    private void IncreaseFlame()
    {
        if (currentFireIntensity < 3)
        {
            currentFireIntensity++;
            Destroy(currentFireInstance);

            GameObject firePrefabToUse = null;

            switch (currentFireIntensity)
            {
                case 1:
                    firePrefabToUse = smallFirePrefab;
                    break;
                case 2:
                    firePrefabToUse = mediumFirePrefab;
                    break;
                case 3:
                    firePrefabToUse = bigFirePrefab;
                    break;
            }

            GameObject fire = Instantiate(firePrefabToUse, transform.position, Quaternion.identity);
            fire.transform.parent = transform;

            currentFireInstance = fire;

            OnFireIncreased.Invoke();
        }
    }

    private void DecreaseFlame()
    {
        if (currentFireIntensity <= 1) return;

        currentFireIntensity--;
        Destroy(currentFireInstance);

        GameObject firePrefabToUse = null;
        switch (currentFireIntensity)
        {
            case 1:
                firePrefabToUse = smallFirePrefab;
                break;
            case 2:
                firePrefabToUse = mediumFirePrefab;
                break;
        }

        GameObject fire = Instantiate(firePrefabToUse, transform.position, Quaternion.identity);
        fire.transform.parent = transform;
        currentFireInstance = fire;

        fireAmount = 99f;
    }

    public void ResetFire()
    {
        isTriggered = false;
        currentFireIntensity = 0;
        fireAmount = 0f;
        internalFlameInfluence = 0f;

        if (currentFireInstance != null)
        {
            Destroy(currentFireInstance);
            currentFireInstance = null;
        }

        OnFireExtinguished.Invoke();
    }

    public void SetExternalFlameInfluence(float influence)
    {
        externalFlameInfluence = Mathf.Min(20f, influence);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (internalFlameInfluence + externalFlameInfluence == 0) return;
        Vector3 textPosition = transform.position + Vector3.up * 1f;
        UnityEditor.Handles.Label(
            textPosition,
            $"Intensity: {currentFireIntensity}, Internal influence: {internalFlameInfluence}, External influence: {externalFlameInfluence}, Fire Amount: {fireAmount:F1}"
        );
    }
#endif
}