using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private Countdown countdown;
    [SerializeField] private FireLever fireLever;
    [SerializeField] private SuccessLight successLight;
    [SerializeField] private TargetRequirements currentTarget;

    [Header("Settings")]
    [SerializeField] private float timeToImpact = 10f;
    [SerializeField] private float randomAzimuthMin = -180f;
    [SerializeField] private float randomAzimuthMax = 180;
    [SerializeField] private float randomElevationMin = 5f;
    [SerializeField] private float randomElevationMax = 85f;
    [SerializeField] private float resultTime = 3f;

    private Coroutine gameCoroutine;

    private IEnumerator GameCoroutine()
    {
        currentTarget = new TargetRequirements(randomAzimuthMin, randomAzimuthMax, randomElevationMin, randomElevationMax);
        targetingConsole.SetTargetValues(currentTarget.azimuth, currentTarget.elevation);
        while (!fireLever.IsFired)
        {
            yield return null;
        }



    }

    private void Start()
    {
        gameCoroutine = StartCoroutine(GameCoroutine());
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
        this.elevation = elevation;
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