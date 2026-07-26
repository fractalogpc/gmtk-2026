using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WaveVisual : MonoBehaviour
{
    [Header("Dial Sources")]
    [Tooltip("Drives the wave's overall amplitude (uses NormalizedValue).")]
    [SerializeField] private DragDial amplitudeDial;
    [Tooltip("Drives harmonic distortion — blends a 2nd/3rd harmonic into the pure sine.")]
    [SerializeField] private DragDial distortionDial;
    [Tooltip("Drives the wave's phase shift (NormalizedValue 0..1 → 0..2π).")]
    [SerializeField] private DragSlider phaseSlider;

    [Header("Wave Geometry (local space)")]
    [Tooltip("Width the wave spans along local X.")]
    [SerializeField] private float width = 1f;
    [Tooltip("Half-height at max amplitude, along local Y.")]
    [SerializeField] private float height = 0.5f;
    [Tooltip("Number of cycles of the fundamental across the width.")]
    [SerializeField] private float cyclesAcrossWidth = 3f;
    [Tooltip("How many samples to plot. More = smoother line, slower.")]
    [SerializeField, Range(16, 512)] private int sampleCount = 160;

    [Header("Radio Interference Noise")]
    [Tooltip("Overall noise amount as a fraction of height. 0 disables interference.")]
    [SerializeField, Range(0f, 1f)] private float noiseAmount = 0.15f;
    [Tooltip("Chance per frame of a stronger static crackle spike.")]
    [SerializeField, Range(0f, 1f)] private float crackleChance = 0.03f;
    [Tooltip("Multiplier applied to noise during a crackle spike.")]
    [SerializeField] private float crackleStrength = 3f;

    [Header("Target Wave (the setting to match)")]
    [SerializeField] private bool showTarget = true;
    [SerializeField] private LineRenderer targetLine;
    [SerializeField, Range(0f, 1f)] private float targetAmplitude = 0.7f;
    [SerializeField, Range(0f, 1f)] private float targetDistortion = 0.4f;
    [SerializeField, Range(0f, 1f)] private float targetPhase = 0.5f;

    [Header("Match Detection")]
    [Tooltip("Default tolerance if the per-parameter values below are 0.")]
    [SerializeField, Range(0f, 0.5f)] private float matchTolerance = 0.15f;
    [Tooltip("Optional per-parameter tolerances. Leave at 0 to fall back to matchTolerance.")]
    [SerializeField, Range(0f, 0.5f)] private float amplitudeTolerance = 0f;
    [SerializeField, Range(0f, 0.5f)] private float distortionTolerance = 0f;
    [SerializeField, Range(0f, 0.5f)] private float phaseTolerance = 0f;
    [Tooltip("If true, logs how close the player is on each parameter every frame — useful for tuning tolerances.")]
    [SerializeField] private bool debugMatchDiffs = false;
    [SerializeField] private UnityEngine.Events.UnityEvent onMatchEntered;
    [SerializeField] private UnityEngine.Events.UnityEvent onMatchExited;

    [Header("Randomization")]
    [Tooltip("Lower bound for the randomized target amplitude — prevents 'flat wave' targets.")]
    [SerializeField, Range(0f, 1f)] private float minTargetAmplitude = 0.45f;
    [Tooltip("Upper bound for the randomized target amplitude.")]
    [SerializeField, Range(0f, 1f)] private float maxTargetAmplitude = 1f;
    [Tooltip("Minimum distance from the player's CURRENT value that a new random target must be, so the player always has to move each control.")]
    [SerializeField, Range(0f, 0.5f)] private float minChangeFromCurrent = 0.25f;
    [Tooltip("How many rejection samples to try before falling back to a forced-shift value.")]
    [SerializeField] private int randomizeMaxAttempts = 50;
    [SerializeField] private UnityEngine.Events.UnityEvent onTargetRandomized;

    public float NormalizedAccuracy
    {
        get
        {
            float ampDiff = Mathf.Abs(CurrentAmplitude - targetAmplitude);
            float distDiff = Mathf.Abs(CurrentDistortion - targetDistortion);
            float phaseDiff = Mathf.Abs(Mathf.DeltaAngle(CurrentPhase * 360f, targetPhase * 360f)) / 360f;

            float ampTol = amplitudeTolerance > 0f ? amplitudeTolerance : matchTolerance;
            float distTol = distortionTolerance > 0f ? distortionTolerance : matchTolerance;
            float phaseTol = phaseTolerance > 0f ? phaseTolerance : matchTolerance;

            float ampScore = Mathf.Clamp01(1f - (ampDiff / ampTol));
            float distScore = Mathf.Clamp01(1f - (distDiff / distTol));
            float phaseScore = Mathf.Clamp01(1f - (phaseDiff / phaseTol));

            return (ampScore + distScore + phaseScore) / 3f;
        }
    }

    private LineRenderer line;
    private bool isMatched;

    public float CurrentAmplitude { get; private set; }
    public float CurrentDistortion { get; private set; }
    public float CurrentPhase { get; private set; }
    public bool IsMatched => isMatched;

    public event System.Action Matched;
    public event System.Action Unmatched;
    public event System.Action TargetRandomized;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;

        if (targetLine != null)
        {
            targetLine.useWorldSpace = false;
            DrawWave(targetLine, targetAmplitude, targetDistortion, targetPhase);
            targetLine.enabled = showTarget;
        }
    }

    private void Update()
    {
        CurrentAmplitude = amplitudeDial != null ? amplitudeDial.NormalizedValue : 0f;
        CurrentDistortion = distortionDial != null ? distortionDial.NormalizedValue : 0f;
        CurrentPhase = phaseSlider != null ? phaseSlider.NormalizedValue : 0f;
        DrawWave(line, CurrentAmplitude, CurrentDistortion, CurrentPhase, applyNoise: true);
        UpdateMatchState();
    }

    private void DrawWave(LineRenderer target, float amp, float dist, float phaseNorm, bool applyNoise = false)
    {
        if (target.positionCount != sampleCount) target.positionCount = sampleCount;
        float step = width / (sampleCount - 1);
        float baseFreq = cyclesAcrossWidth * 2f * Mathf.PI / width;
        float phaseShift = phaseNorm * Mathf.PI * 2f;

        float noiseMult = applyNoise && noiseAmount > 0f ? noiseAmount : 0f;
        if (noiseMult > 0f && Random.value < crackleChance) noiseMult *= crackleStrength;

        for (int i = 0; i < sampleCount; i++)
        {
            float x = i * step;
            float phase = x * baseFreq + phaseShift;
            float y = Sample(phase, amp, dist);
            if (noiseMult > 0f) y += (Random.value - 0.5f) * noiseMult;
            target.SetPosition(i, new Vector3(x, y * height, 0f));
        }
    }

    private static float Sample(float phase, float amp, float dist)
    {
        // Pure sine at dist=0; asymmetric multi-hump wave as dist grows.
        // Second harmonic is phase-locked so its peak sits on the fundamental's peak,
        // producing a distinct "shark-fin" shape rather than a symmetric distortion.
        float fundamental = Mathf.Sin(phase);
        float second = Mathf.Sin(2f * phase + Mathf.PI * 0.5f);
        float third = Mathf.Sin(3f * phase);
        float distorted = fundamental + dist * 0.6f * second + dist * dist * 0.35f * third;

        // Renormalize so amplitude of the composite stays roughly bounded.
        float norm = 1f + dist * 0.6f + dist * dist * 0.35f;
        return amp * distorted / norm;
    }

    private void UpdateMatchState()
    {
        // Phase is circular — 0.02 and 0.98 are actually 0.04 apart, not 0.96.
        float phaseDiff = Mathf.Abs(Mathf.DeltaAngle(CurrentPhase * 360f, targetPhase * 360f)) / 360f;
        float ampDiff = Mathf.Abs(CurrentAmplitude - targetAmplitude);
        float distDiff = Mathf.Abs(CurrentDistortion - targetDistortion);

        float ampTol = amplitudeTolerance > 0f ? amplitudeTolerance : matchTolerance;
        float distTol = distortionTolerance > 0f ? distortionTolerance : matchTolerance;
        float phaseTol = phaseTolerance > 0f ? phaseTolerance : matchTolerance;

        if (debugMatchDiffs)
        {
            Debug.Log($"[Decoder] amp {ampDiff:F3}/{ampTol:F3}  dist {distDiff:F3}/{distTol:F3}  phase {phaseDiff:F3}/{phaseTol:F3}", this);
        }

        bool nowMatched =
            ampDiff <= ampTol &&
            distDiff <= distTol &&
            phaseDiff <= phaseTol;

        if (nowMatched == isMatched) return;
        isMatched = nowMatched;
        if (isMatched)
        {
            onMatchEntered.Invoke();
            Matched?.Invoke();
        }
        else
        {
            onMatchExited.Invoke();
            Unmatched?.Invoke();
        }
    }

    public void SetTarget(float amplitude, float distortion, float phase)
    {
        targetAmplitude = Mathf.Clamp01(amplitude);
        targetDistortion = Mathf.Clamp01(distortion);
        targetPhase = Mathf.Clamp01(phase);
        if (targetLine != null)
        {
            targetLine.enabled = showTarget;
            DrawWave(targetLine, targetAmplitude, targetDistortion, targetPhase);
        }
    }

    public void ResetState()
    {
        if (targetLine != null) targetLine.enabled = false;
        // Clear match state without firing an "unmatched" event — this is a programmatic reset.
        isMatched = false;
    }

    public void RandomizeTarget()
    {
        float minAmp = Mathf.Min(minTargetAmplitude, maxTargetAmplitude);
        float maxAmp = Mathf.Max(minTargetAmplitude, maxTargetAmplitude);

        float amp = PickWithMinDistance(CurrentAmplitude, minChangeFromCurrent, minAmp, maxAmp, wrap: false);
        float dist = PickWithMinDistance(CurrentDistortion, minChangeFromCurrent, 0f, 1f, wrap: false);
        float phase = PickWithMinDistance(CurrentPhase, minChangeFromCurrent, 0f, 1f, wrap: true);

        SetTarget(amp, dist, phase);
        onTargetRandomized.Invoke();
        TargetRandomized?.Invoke();
    }

    private float PickWithMinDistance(float current, float minDist, float lo, float hi, bool wrap)
    {
        for (int i = 0; i < randomizeMaxAttempts; i++)
        {
            float candidate = Random.Range(lo, hi);
            float d = wrap
                ? Mathf.Abs(Mathf.DeltaAngle(candidate * 360f, current * 360f)) / 360f
                : Mathf.Abs(candidate - current);
            if (d >= minDist) return candidate;
        }

        // Fallback: forcibly shift by minDist. For wrapping (phase) push a half-turn away.
        if (wrap) return Mathf.Repeat(current + 0.5f, 1f);

        float up = current + minDist;
        float down = current - minDist;
        if (up <= hi) return Mathf.Clamp(up, lo, hi);
        if (down >= lo) return Mathf.Clamp(down, lo, hi);
        return Mathf.Clamp(current, lo, hi);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetLine != null && !Application.isPlaying)
        {
            targetLine.useWorldSpace = false;
            DrawWave(targetLine, targetAmplitude, targetDistortion, targetPhase);
        }
    }
#endif
}
