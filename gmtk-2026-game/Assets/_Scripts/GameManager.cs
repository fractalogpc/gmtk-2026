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

    private void OnEnable()
    {
        if (decoderController != null) decoderController.SignalMatched += HandleSignalDecoded;
    }

    private void OnDisable()
    {
        if (decoderController != null) decoderController.SignalMatched -= HandleSignalDecoded;
    }

    private void HandleSignalDecoded()
    {
        LogState("Decoder matched → revealing coordinates");
        targetingConsole.RevealCoordinates();
        if (tutorialManager != null) tutorialManager.CompleteLesson("decoder");
    }

    [Header("References")]
    [SerializeField] private TargetingConsole targetingConsole;
    [SerializeField] private DecoderController decoderController;
    [SerializeField] private LoadingStation loadingStation;
    [SerializeField] private GeneratorController generatorController;
    [SerializeField] private EventManager eventManager;

    [SerializeField] private NixieClock countdown;
    [SerializeField] private FireLever fireLever;
    [SerializeField] private SuccessLight successLight;
    private TargetRequirements currentTarget;
    [SerializeField] private UnityEvent onPostImpact;
    [SerializeField] private UnityEvent onImpact;
    [SerializeField] private UnityEvent onTargetHit;
    [SerializeField] private UnityEvent onNewTarget;
    [SerializeField] private UnityEvent onShellAnimation;
    [SerializeField] private UnityEvent onFinalImpact;
    [SerializeField] private Animation shellAnim;
    [SerializeField] private DialogController dialogController;
    [SerializeField] private TutorialManager tutorialManager;

    [Header("Settings")]
    [SerializeField] private float timeToImpact = 10f;
    [SerializeField] private float randomAzimuthMin = -180f;
    [SerializeField] private float randomAzimuthMax = 180;
    [SerializeField] private float elevationVariation = 2f;
    [SerializeField] private float impactViewTime = 3f;
    [SerializeField] private float resultTime = 8f;
    [Tooltip("How long the TIME OUT message stays on the screen before the retry begins.")]
    [SerializeField] private float failMessageDuration = 2f;
    [SerializeField] private Level[] levels;

    private int currentLevel = 0;

    private Coroutine gameCoroutine;

    private void LogState(string state)
    {
        Debug.Log($"[GameManager] Level {currentLevel} → {state}", this);
    }

    private IEnumerator GameCoroutine()
    {
        LogState("BOOT: resetting fire lever");
        fireLever.ResetFireState(0f);

        yield return new WaitForSeconds(15f);

        while (true)
        {
            if (currentLevel >= levels.Length)
            {
                LogState("ALL LEVELS COMPLETE");
                yield break;
            }
            Level levelData = levels[currentLevel];
            LogState($"START level (shell={levelData.requiredShell}, obscured={levelData.obscureCoordinates}, timeLimit={levelData.timeLimit})");

            if (levelData.showStartDialog && levelData.startDialog != null)
            {
                dialogController.StartDialogue(levelData.startDialog);
                levelData.showStartDialog = false;
            }

            // Targeting console lesson always shows on first level.
            if (tutorialManager != null) tutorialManager.TriggerLesson("targeting");

            // Roll and stash the target for this round so the fire check and impact-time
            // calculations later on can reference the exact same values the player was given.
            currentTarget = new TargetRequirements(
                Random.Range(randomAzimuthMin, randomAzimuthMax),
                levelData.elevation(),
                levelData.tolerance);
            LogState($"Target: az={currentTarget.azimuth:F2} el={currentTarget.elevation:F2} tol={currentTarget.tolerance:F2}");

            // Set new coordinates (encrypted until decoded if the level obscures them)
            targetingConsole.SetTargetValues(
                currentTarget.azimuth,
                currentTarget.elevation,
                currentTarget.tolerance,
                levelData.obscureCoordinates);

            LogState("WAITING for player to receive coordinates");
            while (!targetingConsole.HasReceivedCoordinates)
            {
                yield return null;
            }
            LogState("Coordinates received");

            // Trigger countdown
            bool hasTimer = levelData.timeLimit > 0f;
            if (hasTimer)
            {
                LogState($"Starting countdown ({levelData.timeLimit}s)");
                countdown.StartTimer(levelData.timeLimit);
            }
            else
            {
                LogState("No timer this level");
            }

            // Only activate + wait on the loading station when a shell is required.
            bool requiresLoading = levelData.requiredShell != ShellType.None;
            if (requiresLoading)
            {
                LogState($"Activating loading station (shell={levelData.requiredShell})");
                loadingStation.SetRequiredShell(levelData.requiredShell);
                if (tutorialManager != null) tutorialManager.TriggerLesson("loading");
            }
            else
            {
                LogState("Skipping loading step (requiredShell=None)");
            }

            // Check for obscured coordinates
            if (levelData.obscureCoordinates)
            {
                if (tutorialManager != null) tutorialManager.TriggerLesson("decoder");
                LogState("Activating decoder (obscured level)");
                decoderController.RandomizeTarget();
            }

            bool failed = false;

            if (requiresLoading)
            {
                LogState("WAITING for loading station IsReady (or timeout)");
                while (!loadingStation.IsReady && (!hasTimer || countdown.Timer > 0f))
                {
                    yield return null;
                }

                if (hasTimer && countdown.Timer <= 0f && !loadingStation.IsReady)
                {
                    LogState("FAILURE: loading timed out");
                    failed = true;
                }
                else
                {
                    LogState("Loading complete");
                    if (tutorialManager != null) tutorialManager.CompleteLesson("loading");
                }
            }

            if (!failed)
            {
                // Loaded, unlock fire lever
                LogState("Unlocking fire lever");
                fireLever.UnlockFireLever();

                LogState("WAITING for fire lever pulled (or timeout)");
                while (!fireLever.IsFired && (!hasTimer || countdown.Timer > 0f))
                {
                    yield return null;
                }

                if (hasTimer && countdown.Timer <= 0f && !fireLever.IsFired)
                {
                    LogState("FAILURE: fire timed out");
                    failed = true;
                }
                else
                {
                    LogState("Fire lever pulled");
                    if (tutorialManager != null) tutorialManager.CompleteLesson("targeting");
                }
            }

            if (failed)
            {
                if (tutorialManager != null) tutorialManager.TriggerLesson("failure");
                countdown.StopTimer();
                onImpact?.Invoke();
                successLight.SetSuccess(false);

                // Pick a specific reason for the on-screen red message.
                string reason;
                if (requiresLoading && loadingStation.LoadedShell != GameManager.ShellType.None &&
                    loadingStation.LoadedShell != levelData.requiredShell)
                {
                    reason = "WRONG AMMO\nLOADED";
                }
                else if (requiresLoading && !loadingStation.IsReady)
                {
                    reason = "FAILED TO LOAD\nSHELL IN TIME";
                }
                else
                {
                    reason = "FAILED TO FIRE\nIN TIME";
                }
                targetingConsole.ShowFailureMessage(reason);

                yield return new WaitForSeconds(failMessageDuration);

                LogState("RESET after failure — retrying same level");
                targetingConsole.Reset();
                loadingStation.Reset();
                decoderController.Reset();
                fireLever.ResetFireState();
                successLight.Reset();

                // Don't advance currentLevel — the player retries this level (fresh random target).
                continue;
            }

            // At the point the shot has been fired

            // Reset countdown clock
            countdown.StopTimer();

            bool isSuccess = Mathf.Abs(targetingConsole.GunAzimuth - currentTarget.azimuth) <= currentTarget.tolerance &&
                            Mathf.Abs(targetingConsole.GunElevation - currentTarget.elevation) <= currentTarget.tolerance;
            LogState($"Aim check → success={isSuccess} (gun az={targetingConsole.GunAzimuth:F2} el={targetingConsole.GunElevation:F2})");

            LogState("Waiting 3s for firing animation");
            yield return new WaitForSeconds(3f);
            float impactTime = levelData.timeToImpact(levelData.range, currentTarget.elevation);
            if ((hasTimer && countdown.Timer > 0f) || !hasTimer)
            {
                if (levelData.showFireDialog && levelData.fireDialog != null)
                {
                    dialogController.StartDialogue(levelData.fireDialog);
                    levelData.showImpactDialog = false;
                }
            }
            LogState($"Starting impact countdown ({impactTime:F2}s)");
            countdown.StartTimer(impactTime);

            // If success, do success animation
            while (countdown.Timer > 0f)
            {
                if (countdown.Timer < 1f)
                {
                    if (!shellAnim.isPlaying && isSuccess)
                    {
                        onShellAnimation?.Invoke();
                        shellAnim.Play();
                    }
                }
                yield return null;
            }

            LogState("IMPACT");
            onImpact?.Invoke();
            StartCoroutine(TriggerEvents(levelData, Mathf.Max(levelData.soundDelay(currentTarget.elevation) - impactViewTime - resultTime, 0f)));
            if (isSuccess)
            {
                onTargetHit?.Invoke();
                if (currentLevel == levels.Length - 1)
                {
                    onFinalImpact?.Invoke();
                }
                if (tutorialManager != null) tutorialManager.TriggerLesson("success");
            }
            else
            {
                if (tutorialManager != null) tutorialManager.TriggerLesson("failure");
            }
            successLight.SetSuccess(isSuccess);
            if (levelData.showImpactDialog && levelData.impactDialog != null)
            {
                dialogController.StartDialogue(levelData.impactDialog);
                levelData.showImpactDialog = false;
            }
            LogState($"Waiting {impactViewTime}s (impactViewTime)");
            yield return new WaitForSeconds(impactViewTime);
            onPostImpact?.Invoke();

            if (isSuccess)
            {
                targetingConsole.DisplayMessage("SUCCESS");
            }
            else
            {
                targetingConsole.ShowFailureMessage("MISSED\nTARGET");
            }
            LogState($"Displaying result, waiting {resultTime}s");
            yield return new WaitForSeconds(resultTime);

            // Reset everything
            LogState("RESET: cycling all stations");
            targetingConsole.Reset();
            loadingStation.Reset();
            decoderController.Reset();
            fireLever.ResetFireState();
            successLight.Reset();

            // Sound delay
            // yield return new WaitForSeconds(Mathf.Max(levelData.soundDelay(currentTarget.elevation) - impactViewTime - resultTime, 0f));

            if (isSuccess) currentLevel++;

            onNewTarget?.Invoke();

            // onNewTarget?.Invoke();
            // float elev = levelData.elevation();
            // currentTarget = new TargetRequirements(randomAzimuthMin, randomAzimuthMax, elev - elevationVariation, elev + elevationVariation, levelData.tolerance);
            // targetingConsole.SetTargetValues(currentTarget.azimuth, currentTarget.elevation);
            // while (!fireLever.IsFired)
            // {
            //     yield return null;
            // }
            // // Lever has been fired
            // bool isSuccess = Mathf.Abs(targetingConsole.GunAzimuth - currentTarget.azimuth) <= currentTarget.tolerance &&
            //                 Mathf.Abs(targetingConsole.GunElevation - currentTarget.elevation) <= currentTarget.tolerance;

            // targetingConsole.SetLocked(true);
            // yield return new WaitForSeconds(3f); // Wait for firing animations
            // countdown.StartTimer(timeToImpact);
            // while (countdown.Timer > 0f)
            // {
            //     if (countdown.Timer < 1f)
            //     {
            //         if (!shellAnim.isPlaying && isSuccess)
            //         {
            //             onShellAnimation?.Invoke();
            //             shellAnim.Play();
            //         }
            //     }
            //     yield return null;
            // }

            // // Impact
            // onImpact?.Invoke();
            // if (isSuccess)
            // {
            //     onTargetHit?.Invoke();
            // }
            // EventManager.Instance.TriggerFireEvent(1);
            // yield return new WaitForSeconds(impactViewTime);
            // onPostImpact?.Invoke();
            // // Reset the lever
            // successLight.SetSuccess(isSuccess);
            // targetingConsole.DisplayMessage(isSuccess ? "SUCCESS" : "FAILURE");
            // yield return new WaitForSeconds(resultTime);
            // successLight.Reset();
            // fireLever.ResetFireState(3f);
            // targetingConsole.SetLocked(false);
            // currentLevel++;
        }
    }

    private IEnumerator TriggerEvents(Level levelData, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (levelData.fireIntensity > 0)
        {
            eventManager.TriggerFireEvent(levelData.fireIntensity);
            if (tutorialManager != null) tutorialManager.TriggerLesson("fire");
        }
        if (levelData.doBlackout)
        {
            eventManager.TriggerPowerOutageEvent();
            generatorController.KillGenerator();
            if (tutorialManager != null) tutorialManager.TriggerLesson("blackout");
        }
    }

    public void OnPowerCut()
    {
        targetingConsole.SetPowered(false);
        loadingStation.SetPowered(false);
        decoderController.SetPowered(false);
        generatorController.SetPowered(false);
    }

    public void OnPowerRestored()
    {
        targetingConsole.SetPowered(true);
        loadingStation.SetPowered(true);
        decoderController.SetPowered(true);
        generatorController.SetPowered(true);
        if (tutorialManager != null) tutorialManager.CompleteLesson("blackout");
    }

    public void OnSignalDecoded()
    {
        targetingConsole.RevealCoordinates();
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

    public enum ShellType
    {
        AP,
        INC,
        HE,
        None
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


[System.Serializable]
public class Level
{
    public const float MAX_RANGE = 11f;
    public GameManager.ShellType requiredShell; // Set None to not require shell loading
    public bool obscureCoordinates;
    public float timeLimit;
    public float tolerance;
    public float range; // Distance in meters
    public int fireIntensity; // 0 is no fire, don't go over 4
    public bool doBlackout;

    public DialogObject startDialog;
    public bool showStartDialog = true;
    public DialogObject fireDialog;
    public bool showFireDialog = true;
    public DialogObject impactDialog;
    public bool showImpactDialog = true;

    public Level()
    {
        requiredShell = GameManager.ShellType.None;
        obscureCoordinates = false;
        timeLimit = 0f;
        tolerance = 1f;
        range = 10f;
        fireIntensity = 0;
        doBlackout = false;
    }

    public float elevation()
    {
        float exitVelocity = 200f; // m/s
        float gravity = 9.81f; // m/s^2

        float angle = Mathf.Asin(gravity * range / (exitVelocity * exitVelocity)) * Mathf.Rad2Deg / 2f;

        return 90f - angle;
    }

    private const float DELAY_FACTOR = 4f;

    public float soundDelay(float range)
    {
        return range / 343f / DELAY_FACTOR;
    }

    public float timeToImpact(float range, float elevation)
    {
        float exitVelocity = 200f; // m/s
        float time = 2f * exitVelocity * Mathf.Sin(elevation * Mathf.Deg2Rad) / 9.81f;
        return time / DELAY_FACTOR;
    }
}