using TMPro;
using UnityEngine;
using FMODUnity;

public class TargetingConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI gunText;
    [SerializeField] private DragLever leverAzimuth;
    [SerializeField] private DragLever leverElevation;
    [SerializeField] private float azimuthForceMin, azimuthForceMax;
    [SerializeField] private float elevationForceMin, elevationForceMax;
    [SerializeField] private float gunMass = 100f;
    [SerializeField][Range(0, 1)] private float gunDrag = 0.1f;
    [SerializeField] private float gunMaxAzimuthVelocity = 30f;
    [SerializeField] private float gunMaxElevationVelocity = 15f;
    [SerializeField] private StudioEventEmitter gunMovementEmitter;
    [SerializeField] private Transform gunBase;
    [SerializeField] private Transform gunBarrel;
    [SerializeField] private float stopEmitterAfterSecondsAtZero = 0.2f;
    public float GunAzimuth => gunAzimuth;
    public float GunElevation => gunElevation;
    private float gunAzimuth, gunElevation;
    private float gunVelocityAzimuth, gunVelocityElevation;

    private bool locked = false;

    public void SetLocked(bool isLocked)
    {
        ((IInteractable)leverAzimuth).SetInteractionEnabled(!isLocked);
        ((IInteractable)leverElevation).SetInteractionEnabled(!isLocked);
        if (isLocked)
        {
            gunText.text = "CONTROLS LOCKED\nFIRE COMMAND SENT";
        }
        else
        {
            UpdateGunText();
        }
        locked = isLocked;
    }

    public void DisplayMessage(string message)
    {
        gunText.text = message;
    }

    public void SetTargetValues(float azimuth, float elevation)
    {
        targetText.text = $"FIRING ORDERS\n_______________\n{azimuth:F2} AZIM\n{elevation:F2} ELEV";
    }

    private float MapNormalizedToRange(float normalizedValue, float min, float max)
    {
        return Mathf.Lerp(min, max, normalizedValue);
    }

    private void UpdateGunText()
    {
        gunText.text = $"GUN ORIENTATION\n_______________\n{gunAzimuth:F2} AZIM\n{gunElevation:F2} ELEV";
    }

    private void UpdateAccelFromLevers(float deltaTime)
    {
        float azimuthForce = MapNormalizedToRange(leverAzimuth.NormalizedValue, azimuthForceMin, azimuthForceMax);
        float elevationForce = MapNormalizedToRange(leverElevation.NormalizedValue, elevationForceMin, elevationForceMax);
        gunVelocityAzimuth += (azimuthForce / gunMass) * deltaTime;
        gunVelocityElevation += (elevationForce / gunMass) * deltaTime;
        gunVelocityAzimuth = Mathf.Clamp(gunVelocityAzimuth, -gunMaxAzimuthVelocity, gunMaxAzimuthVelocity);
        gunVelocityElevation = Mathf.Clamp(gunVelocityElevation, -gunMaxElevationVelocity, gunMaxElevationVelocity);
        if (azimuthForce == 0f) gunVelocityAzimuth = 0f;
        if (elevationForce == 0f) gunVelocityElevation = 0f;
    }

    private void UpdateGunFromVelocity(float deltaTime)
    {
        gunAzimuth += gunVelocityAzimuth * deltaTime;
        if (gunAzimuth > 180f) {
            gunAzimuth = -180f;
            gunVelocityAzimuth = 0f;
        }
        if (gunAzimuth < -180f) {
            gunAzimuth = 180f;
            gunVelocityAzimuth = 0f;
        }

        gunElevation += gunVelocityElevation * deltaTime;
        if (gunElevation > 90f) {
            gunElevation = 90f;
            gunVelocityElevation = 0f;
        }
        if (gunElevation < 0f) {
            gunElevation = 0f;
            gunVelocityElevation = 0f;
        }

        gunVelocityAzimuth *= (1f - gunDrag * deltaTime);
        gunVelocityElevation *= (1f - gunDrag * deltaTime);
    }

    private float secondsAtZeroSpeed = 0f;
    private void UpdateTraverseSound(float deltaTime)
    {
        float azimuthSpeed = Mathf.Abs(gunVelocityAzimuth) / gunMaxAzimuthVelocity;
        float elevationSpeed = Mathf.Abs(gunVelocityElevation) / gunMaxElevationVelocity;
        float speed = Mathf.Max(azimuthSpeed, elevationSpeed);
        gunMovementEmitter.SetParameter("TraverseSpeed", speed);
        if (speed > 0f)
        {
            secondsAtZeroSpeed = 0f;
            if (!gunMovementEmitter.IsPlaying())
            {
                gunMovementEmitter.Play();
            }
        }
        else
        {
            secondsAtZeroSpeed += deltaTime;
            if (secondsAtZeroSpeed >= stopEmitterAfterSecondsAtZero && gunMovementEmitter.IsPlaying())
            {
                gunMovementEmitter.Stop();
            }
        }
    }

    private void Update()
    {
        if (locked) return;
        float deltaTime = Time.deltaTime;
        UpdateAccelFromLevers(deltaTime);
        UpdateGunFromVelocity(deltaTime);
        UpdateTraverseSound(deltaTime);
        gunBase.localRotation = Quaternion.Euler(0f, -gunAzimuth, 0f);
        gunBarrel.localRotation = Quaternion.Euler(-gunElevation, 0f, 0f);
        UpdateGunText();
    }

}
