using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    private const string LookSensitivityPrefKey = "settings.lookSensitivity";
    private const string InvertYPrefKey = "settings.invertY";
    private const string MasterSoundPrefKey = "settings.masterSound";
    private const string MusicSoundPrefKey = "settings.musicSound";
    private const string SFXSoundPrefKey = "settings.sfxSound";
    private const string AmbientSoundPrefKey = "settings.ambientSound";

    private const float DefaultLookSensitivity = 0.5f;
    private const bool DefaultInvertY = false;
    private const float DefaultMasterSound = 0.5f;
    private const float DefaultMusicSound = 1f;
    private const float DefaultSFXSound = 1f;
    private const float DefaultAmbientSound = 1f;

    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private PlayerController playerController;
    [Header("UI Controls")]
    [SerializeField] private Slider lookSensitivitySlider;
    [SerializeField] private Toggle invertYToggle;
    [SerializeField] private Slider masterSoundSlider;
    [SerializeField] private Slider musicSoundSlider;
    [SerializeField] private Slider sfxSoundSlider;
    [SerializeField] private Slider ambientSoundSlider;
    [Header("FMOD Volumes")]
    [SerializeField] private string masterVcaPath = "vca:/Master";
    [SerializeField] private string musicVcaPath = "vca:/Music";
    [SerializeField] private string sfxVcaPath = "vca:/SFX";
    [SerializeField] private string ambientVcaPath = "vca:/Ambient";
    [Tooltip("If true, sets Time.timeScale = 0 while paused.")]
    [SerializeField] private bool freezeTime = true;

    private bool isPaused;
    private CursorLockMode savedCursorLockMode;
    private bool savedCursorVisible;
    private FMOD.Studio.VCA masterVca;
    private FMOD.Studio.VCA musicVca;
    private FMOD.Studio.VCA sfxVca;
    private FMOD.Studio.VCA ambientVca;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (playerInteractor == null)
        {
            playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        masterVca = ResolveVca(masterVcaPath);
        musicVca = ResolveVca(musicVcaPath);
        sfxVca = ResolveVca(sfxVcaPath);
        ambientVca = ResolveVca(ambientVcaPath);

        BindSettingsUi();

        ApplySavedSettings();
        SyncSettingsUI();

        SetPauseCanvasVisible(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Called by the escape / cancel input path once other cancel targets (interaction,
    /// static view) have been ruled out. Toggles the pause menu open/closed.
    /// </summary>
    public void TogglePause()
    {
        if (playerInteractor != null && playerInteractor.IsInStaticView)
        {
            return;
        }

        SetPaused(!isPaused);
    }

    public void SetPaused(bool value)
    {
        if (isPaused == value) return;
        isPaused = value;

        SetPauseCanvasVisible(isPaused);
        if (playerController != null)
        {
            playerController.SetLookEnabled(!isPaused);
        }
        if (freezeTime) Time.timeScale = isPaused ? 0f : 1f;

        if (isPaused)
        {
            savedCursorLockMode = Cursor.lockState;
            savedCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = savedCursorLockMode;
            Cursor.visible = savedCursorVisible;
        }
    }

    private void OnDestroy()
    {
        UnbindSettingsUi();
    }

    private void SetPauseCanvasVisible(bool visible)
    {
        if (pauseCanvasGroup == null) return;

        pauseCanvasGroup.alpha = visible ? 1f : 0f;
        pauseCanvasGroup.interactable = visible;
        pauseCanvasGroup.blocksRaycasts = visible;
    }

    // Convenience methods to wire directly to UI buttons on the pause canvas.
    public void ResumeButton() => SetPaused(false);
    public void QuitButton() => SceneManager.LoadScene("MainMenu");

    public void InvertY(bool invert)
    {
        if (playerController != null)
        {
            playerController.InvertY = invert;
        }

        PlayerPrefs.SetInt(InvertYPrefKey, invert ? 1 : 0);
    }

    public void SetLookSensitivity(float value)
    {
        if (playerController != null)
        {
            playerController.LookSensitivity = value / 6 + 0.01f; // Scale slider value (0-1) to sensitivity range (0.5-1.5)
        }

        PlayerPrefs.SetFloat(LookSensitivityPrefKey, value);
    }

    public void SetMasterSound(float value) {
        SetVcaVolume(ref masterVca, masterVcaPath, value);
        PlayerPrefs.SetFloat(MasterSoundPrefKey, value);
    }

    public void SetMusicSound(float value) {
        SetVcaVolume(ref musicVca, musicVcaPath, value);
        PlayerPrefs.SetFloat(MusicSoundPrefKey, value);
    }

    public void SetSFXSound(float value) {
        SetVcaVolume(ref sfxVca, sfxVcaPath, value);
        PlayerPrefs.SetFloat(SFXSoundPrefKey, value);
    }

    public void SetAmbientSound(float value) {
        SetVcaVolume(ref ambientVca, ambientVcaPath, value);
        PlayerPrefs.SetFloat(AmbientSoundPrefKey, value);
    }

    private void ApplySavedSettings()
    {
        SetLookSensitivity(PlayerPrefs.GetFloat(LookSensitivityPrefKey, DefaultLookSensitivity));
        InvertY(PlayerPrefs.GetInt(InvertYPrefKey, DefaultInvertY ? 1 : 0) != 0);
        SetMasterSound(PlayerPrefs.GetFloat(MasterSoundPrefKey, DefaultMasterSound));
        SetMusicSound(PlayerPrefs.GetFloat(MusicSoundPrefKey, DefaultMusicSound));
        SetSFXSound(PlayerPrefs.GetFloat(SFXSoundPrefKey, DefaultSFXSound));
        SetAmbientSound(PlayerPrefs.GetFloat(AmbientSoundPrefKey, DefaultAmbientSound));

        PlayerPrefs.Save();
    }

    private void SyncSettingsUI()
    {
        if (lookSensitivitySlider != null)
        {
            lookSensitivitySlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(LookSensitivityPrefKey, DefaultLookSensitivity));
        }

        if (invertYToggle != null)
        {
            invertYToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(InvertYPrefKey, DefaultInvertY ? 1 : 0) != 0);
        }

        if (masterSoundSlider != null)
        {
            masterSoundSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MasterSoundPrefKey, DefaultMasterSound));
        }

        if (musicSoundSlider != null)
        {
            musicSoundSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicSoundPrefKey, DefaultMusicSound));
        }

        if (sfxSoundSlider != null)
        {
            sfxSoundSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SFXSoundPrefKey, DefaultSFXSound));
        }

        if (ambientSoundSlider != null)
        {
            ambientSoundSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(AmbientSoundPrefKey, DefaultAmbientSound));
        }
    }

    private void BindSettingsUi()
    {
        if (lookSensitivitySlider != null)
        {
            lookSensitivitySlider.onValueChanged.AddListener(SetLookSensitivity);
        }

        if (invertYToggle != null)
        {
            invertYToggle.onValueChanged.AddListener(InvertY);
        }

        if (masterSoundSlider != null)
        {
            masterSoundSlider.onValueChanged.AddListener(SetMasterSound);
        }

        if (musicSoundSlider != null)
        {
            musicSoundSlider.onValueChanged.AddListener(SetMusicSound);
        }

        if (sfxSoundSlider != null)
        {
            sfxSoundSlider.onValueChanged.AddListener(SetSFXSound);
        }

        if (ambientSoundSlider != null)
        {
            ambientSoundSlider.onValueChanged.AddListener(SetAmbientSound);
        }
    }

    private void UnbindSettingsUi()
    {
        if (lookSensitivitySlider != null)
        {
            lookSensitivitySlider.onValueChanged.RemoveListener(SetLookSensitivity);
        }

        if (invertYToggle != null)
        {
            invertYToggle.onValueChanged.RemoveListener(InvertY);
        }

        if (masterSoundSlider != null)
        {
            masterSoundSlider.onValueChanged.RemoveListener(SetMasterSound);
        }

        if (musicSoundSlider != null)
        {
            musicSoundSlider.onValueChanged.RemoveListener(SetMusicSound);
        }

        if (sfxSoundSlider != null)
        {
            sfxSoundSlider.onValueChanged.RemoveListener(SetSFXSound);
        }

        if (ambientSoundSlider != null)
        {
            ambientSoundSlider.onValueChanged.RemoveListener(SetAmbientSound);
        }
    }

    private FMOD.Studio.VCA ResolveVca(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return default;
        }

        try
        {
            return RuntimeManager.GetVCA(path);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to resolve FMOD VCA '{path}' on {name}: {e.Message}", this);
            return default;
        }
    }

    private void SetVcaVolume(ref FMOD.Studio.VCA vca, string path, float value)
    {
        if (!vca.isValid())
        {
            vca = ResolveVca(path);
        }

        if (vca.isValid())
        {
            vca.setVolume(value);
        }
    }
}
