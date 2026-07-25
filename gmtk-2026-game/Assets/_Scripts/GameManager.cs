using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance { get; private set; }
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

    [Header("References")]
    [SerializeField] private TargetingConsole targetingConsole;
    [SerializeField] private NixieClock countdown;
    [SerializeField] private FireLever fireLever;
    [SerializeField] private SuccessLight successLight;
    [SerializeField] private TargetRequirements currentTarget;
    [SerializeField] private UnityEvent onPostImpact;
    [SerializeField] private UnityEvent onImpact;
    [SerializeField] private UnityEvent onNewTarget;
    [SerializeField] private UnityEvent onShellAnimation;
    [SerializeField] private Animation shellAnim;

    [Header("Settings")]
    [SerializeField] private float timeToImpact = 10f;
    [SerializeField] private float randomAzimuthMin = -180f;
    [SerializeField] private float randomAzimuthMax = 180;
    [SerializeField] private float elevationVariation = 2f;
    [SerializeField] private float impactViewTime = 3f;
    [SerializeField] private float resultTime = 8f;
    [SerializeField] private Level[] levels;

    private int currentLevel = 0;

    private Coroutine gameCoroutine;

    private IEnumerator GameCoroutine()
    {
        fireLever.ResetFireState(0f);

        while (true)
        {
            if (currentLevel >= levels.Length)
                yield break;
            Level levelData = levels[currentLevel];
            
            onNewTarget?.Invoke();
            float elev = levelData.elevation();
            currentTarget = new TargetRequirements(randomAzimuthMin, randomAzimuthMax, elev - elevationVariation, elev + elevationVariation, levelData.tolerance);
            targetingConsole.SetTargetValues(currentTarget.azimuth, currentTarget.elevation);
            while (!fireLever.IsFired)
            {
                yield return null;
            }
            // Lever has been fired

            targetingConsole.SetLocked(true);
            yield return new WaitForSeconds(3f); // Wait for firing animations
            countdown.StartTimer(timeToImpact);
            while (countdown.Timer > 0f)
            {
                if (countdown.Timer < 1f)
                {
                    if (!shellAnim.isPlaying)
                    {
                        onShellAnimation?.Invoke();
                        shellAnim.Play();
                    }
                }
                yield return null;
            }

            // Impact
            onImpact?.Invoke();
            EventManager.Instance.TriggerFireEvent(1);
            yield return new WaitForSeconds(impactViewTime);
            onPostImpact?.Invoke();
            // Reset the lever
            bool isSuccess = Mathf.Abs(targetingConsole.GunAzimuth - currentTarget.azimuth) <= currentTarget.tolerance &&
                            Mathf.Abs(targetingConsole.GunElevation - currentTarget.elevation) <= currentTarget.tolerance;
            successLight.SetSuccess(isSuccess);
            targetingConsole.DisplayMessage(isSuccess ? "SUCCESS" : "FAILURE");
            yield return new WaitForSeconds(resultTime);
            successLight.Reset();
            fireLever.ResetFireState(3f);
            targetingConsole.SetLocked(false);
            currentLevel++;
        }
    }

    private void Start()
    {
        gameCoroutine = StartCoroutine(GameCoroutine());
    }

    private void OnDestroy()
    {
        if (gameCoroutine != null)
        {
            StopCoroutine(gameCoroutine);
        }
    }
}

public class TargetRequirements
{
    public float azimuth;
    public float elevation;
    public float tolerance;

    public TargetRequirements(float azimuth, float elevation, float tolerance = 1f)
    {
        this.azimuth = azimuth;
        this.elevation = Mathf.Clamp(elevation, 0f, 90f);
        this.tolerance = tolerance;
    }
    
    /// <summary>
    /// Generates target values within the specified ranges for azimuth and elevation, and a given tolerance.
    /// </summary>
    public TargetRequirements(float azimuthMin, float azimuthMax, float elevationMin, float elevationMax, float tolerance = 1f)
    {
        this.azimuth = Random.Range(azimuthMin, azimuthMax);
        this.elevation = Random.Range(elevationMin, elevationMax);
        this.tolerance = tolerance;
    }
}

public enum ShellType
{
    Normal,
    AP,
    HE
}

[System.Serializable]
public class Level
{
    public const float MAX_RANGE = 11f;
    public ShellType requiredShell;
    public bool obscureCoordinates;
    public float timeLimit;
    public float tolerance;
    public float range;

    public Level()
    {
        requiredShell = ShellType.Normal;
        obscureCoordinates = false;
        timeLimit = 0f;
        tolerance = 1f;
        range = 10f;
    }

    public float elevation()
    {
        return 90f - Mathf.Asin(range / MAX_RANGE) * Mathf.Rad2Deg;
    }
}