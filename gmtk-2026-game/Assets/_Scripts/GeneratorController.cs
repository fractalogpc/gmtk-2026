using UnityEngine;
using UnityEngine.Events;

public class GeneratorController : MonoBehaviour
{
    [Tooltip("Indicators, in the order they should re-light while cranking.")]
    [SerializeField] private GameObject[] indicators;

    [SerializeField] private DragDial crank;

    [Tooltip("Material shown when an indicator is dead (red).")]
    [SerializeField] private Material offMaterial;
    [Tooltip("Material shown when an indicator is powered (green).")]
    [SerializeField] private Material onMaterial;

    [Tooltip("Degrees of crank travel required to re-light one indicator.")]
    [SerializeField] private float degreesPerIndicator = 360f;
    [Tooltip("Degrees of progress lost per second when the crank is idle — stop cranking and indicators eventually revert.")]
    [SerializeField] private float regressSpeed = 90f;

    [Header("Breaker Switch")]
    [SerializeField] private DragLever breakerLever;
    [Tooltip("NormalizedValue >= this counts as 'up' (breaker engaged).")]
    [SerializeField, Range(0f, 1f)] private float breakerUpThreshold = 0.75f;
    [Tooltip("NormalizedValue <= this counts as 'down' (breaker released).")]
    [SerializeField, Range(0f, 1f)] private float breakerDownThreshold = 0.25f;
    [Tooltip("Angle the breaker lever snaps to when reset (i.e. the 'up' position, in degrees).")]
    [SerializeField] private float breakerUpAngle = 45f;
    [Tooltip("Speed passed to lever.SetAngle when the breaker snaps back up. -1 uses the lever's own setting.")]
    [SerializeField] private float breakerResetSpeed = -1f;

    [Header("Events")]
    [SerializeField] private UnityEvent onGeneratorRestored;
    [SerializeField] private UnityEvent onGeneratorKilled;
    [Tooltip("Fires after the generator is restored AND the player has cycled the breaker down then back up.")]
    [SerializeField] private UnityEvent onPowerRestored;

    private enum BreakerState { Powered, GeneratorDead, NeedsCycle, Primed }

    private bool isDead;
    private float cumulativeCranked;
    private float previousCrankAngle;
    private int litCount;
    private BreakerState breakerState = BreakerState.Powered;

    public bool IsDead => isDead;
    public bool IsPowered => breakerState == BreakerState.Powered;

    private void Awake()
    {
        SetAllIndicators(onMaterial);
        litCount = indicators.Length;
    }

    private void Start()
    {
        KillGenerator();
    }

    public void KillGenerator()
    {
        if (isDead) return;
        isDead = true;
        litCount = 0;
        cumulativeCranked = 0f;
        previousCrankAngle = crank != null ? crank.Angle : 0f;
        SetAllIndicators(offMaterial);

        breakerState = BreakerState.GeneratorDead;
        ResetBreakerToUp();

        onGeneratorKilled.Invoke();
    }

    private void Update()
    {
        UpdateCrank();
        UpdateBreaker();
    }

    private void UpdateCrank()
    {
        if (!isDead || crank == null) return;

        float current = crank.Angle;
        float crankDelta = Mathf.Abs(current - previousCrankAngle);
        previousCrankAngle = current;

        float maxProgress = indicators.Length * degreesPerIndicator;
        cumulativeCranked = Mathf.Clamp(
            cumulativeCranked + crankDelta - regressSpeed * Time.deltaTime,
            0f,
            maxProgress);

        int shouldBeLit = Mathf.Min(indicators.Length, Mathf.FloorToInt(cumulativeCranked / degreesPerIndicator));

        while (litCount < shouldBeLit)
        {
            SetIndicator(litCount, onMaterial);
            litCount++;
        }
        while (litCount > shouldBeLit)
        {
            litCount--;
            SetIndicator(litCount, offMaterial);
        }

        if (litCount >= indicators.Length)
        {
            isDead = false;
            breakerState = BreakerState.NeedsCycle;
            ResetBreakerToUp();
            onGeneratorRestored.Invoke();
        }
    }

    private void UpdateBreaker()
    {
        if (breakerLever == null) return;
        float v = breakerLever.NormalizedValue;

        switch (breakerState)
        {
            case BreakerState.NeedsCycle:
                if (v <= breakerDownThreshold) breakerState = BreakerState.Primed;
                break;
            case BreakerState.Primed:
                if (v >= breakerUpThreshold)
                {
                    breakerState = BreakerState.Powered;
                    onPowerRestored.Invoke();
                }
                break;
        }
    }

    private void ResetBreakerToUp()
    {
        if (breakerLever == null) return;
        if (breakerResetSpeed <= 0f) breakerLever.SetAngle(breakerUpAngle);
        else breakerLever.SetAngle(breakerUpAngle, breakerResetSpeed);
    }

    private void SetAllIndicators(Material material)
    {
        foreach (GameObject indicator in indicators)
        {
            if (indicator != null) indicator.GetComponent<Renderer>().material = material;
        }
    }

    private void SetIndicator(int index, Material material)
    {
        if (index < 0 || index >= indicators.Length) return;
        if (indicators[index] != null) indicators[index].GetComponent<Renderer>().material = material;
    }
}
