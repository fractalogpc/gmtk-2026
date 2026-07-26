using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

public class MainMenuMenuController : MonoBehaviour
{
    private const string MasterSoundPrefKey = "settings.masterSound";
    private const string MusicSoundPrefKey = "settings.musicSound";
    private const string SFXSoundPrefKey = "settings.sfxSound";
    private const string AmbientSoundPrefKey = "settings.ambientSound";

    private const float DefaultMasterSound = 0.5f;
    private const float DefaultMusicSound = 1f;
    private const float DefaultSFXSound = 1f;
    private const float DefaultAmbientSound = 1f;

    [Header("UI Controls")]
    [SerializeField] private Slider masterSoundSlider;
    [SerializeField] private Slider musicSoundSlider;
    [SerializeField] private Slider sfxSoundSlider;
    [SerializeField] private Slider ambientSoundSlider;
    [Header("FMOD Volumes")]
    [SerializeField] private string masterVcaPath = "vca:/Master";
    [SerializeField] private string musicVcaPath = "vca:/Music";
    [SerializeField] private string sfxVcaPath = "vca:/SFX";
    [SerializeField] private string ambientVcaPath = "vca:/Ambient";

    private FMOD.Studio.VCA masterVca;
    private FMOD.Studio.VCA musicVca;
    private FMOD.Studio.VCA sfxVca;
    private FMOD.Studio.VCA ambientVca;

    private void Awake()
    {
        masterVca = ResolveVca(masterVcaPath);
        musicVca = ResolveVca(musicVcaPath);
        sfxVca = ResolveVca(sfxVcaPath);
        ambientVca = ResolveVca(ambientVcaPath);

        BindSettingsUi();
        ApplySavedSettings();
        SyncSettingsUI();
    }

    private void OnDestroy()
    {
        UnbindSettingsUi();
    }

    public void QuitButton()
    {
        Application.Quit();
    }

    public void SetMasterSound(float value)
    {
        SetVcaVolume(ref masterVca, masterVcaPath, value);
        PlayerPrefs.SetFloat(MasterSoundPrefKey, value);
    }

    public void SetMusicSound(float value)
    {
        SetVcaVolume(ref musicVca, musicVcaPath, value);
        PlayerPrefs.SetFloat(MusicSoundPrefKey, value);
    }

    public void SetSFXSound(float value)
    {
        SetVcaVolume(ref sfxVca, sfxVcaPath, value);
        PlayerPrefs.SetFloat(SFXSoundPrefKey, value);
    }

    public void SetAmbientSound(float value)
    {
        SetVcaVolume(ref ambientVca, ambientVcaPath, value);
        PlayerPrefs.SetFloat(AmbientSoundPrefKey, value);
    }

    private void ApplySavedSettings()
    {
        SetMasterSound(PlayerPrefs.GetFloat(MasterSoundPrefKey, DefaultMasterSound));
        SetMusicSound(PlayerPrefs.GetFloat(MusicSoundPrefKey, DefaultMusicSound));
        SetSFXSound(PlayerPrefs.GetFloat(SFXSoundPrefKey, DefaultSFXSound));
        SetAmbientSound(PlayerPrefs.GetFloat(AmbientSoundPrefKey, DefaultAmbientSound));

        PlayerPrefs.Save();
    }

    private void SyncSettingsUI()
    {
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
