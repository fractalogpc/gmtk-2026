using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class FireLever : MonoBehaviour
{
    [SerializeField] private DragLever lever;
    [SerializeField] private Renderer[] lightRenderers;
    [SerializeField] private Light[] lightSources;
    [SerializeField] private Material onMaterial;
    [SerializeField] private Material offMaterial;
    [SerializeField][Range(0, 1)] private float fireThreshold = 0.8f; 
    [SerializeField] private CinemachineImpulseSource gunMoveImpulse;
    [SerializeField] private CinemachineImpulseSource fireImpulse;
    public bool IsFired => hasFired;
    private bool hasFired = false;

    public void ResetFireState(float speed = -1f)
    {
        hasFired = false;
        lever.ResetLever(speed);
        foreach (var renderer in lightRenderers)
        {
            renderer.material = offMaterial;
        }
        foreach (var light in lightSources)
        {
            light.enabled = false;
        }
    }
    
    private void Update()
    {
        if (!hasFired && lever.NormalizedValue >= fireThreshold)
        {
            hasFired = true;
            Fire();
        }
        else if (hasFired && lever.NormalizedValue < fireThreshold)
        {
            hasFired = false; // Reset the fire state when the lever is released
        }
    }

    private void Fire()
    {
        // play sounds or something
        StartCoroutine(FireCoroutine());
    }

    private IEnumerator FireCoroutine()
    {
        gunMoveImpulse.GenerateImpulse();
        yield return StartCoroutine(LightsCoroutine());
        fireImpulse.GenerateImpulse();
    }

    private IEnumerator LightsCoroutine()
    {
        for (int i = 0; i < lightRenderers.Length; i++)
        {
            lightRenderers[i].material = onMaterial;
            lightSources[i].enabled = true;
            yield return new WaitForSeconds(1f);
        }
    }
}
