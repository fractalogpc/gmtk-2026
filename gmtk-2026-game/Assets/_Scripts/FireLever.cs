using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.Events;

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
    [SerializeField] private UnityEvent onFire;
    public bool IsFired => hasFired;
    private bool hasFired = false;

    private void Awake()
    {
        LockFireLever();
    }

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
        LockFireLever();
    }

    private void Update()
    {
        if (!hasFired && lever.NormalizedValue >= fireThreshold)
        {
            hasFired = true;
            Fire();
        }
        // Note: once fired the lever is locked in place by Fire(), so we don't need to
        // handle the "lever came back below threshold" case — the player can't move it.
    }

    private void Fire()
    {
        LockFireLever();
        StartCoroutine(FireCoroutine());
    }

    private IEnumerator FireCoroutine()
    {
        gunMoveImpulse.GenerateImpulse();
        yield return StartCoroutine(LightsCoroutine());
        fireImpulse.GenerateImpulse();
        onFire?.Invoke();
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

    public void UnlockFireLever()
    {
        lever.SetInteractionEnabled(true);
    }

    public void LockFireLever()
    {
        lever.SetInteractionEnabled(false);
    }
}
