using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    public enum VolumeType { Master, SFX, Music }

    [Header("Bindings")]
    public VolumeType type;
    public Slider slider;
    public TMP_Text valueText;

    private bool initialized = false;

    private void Awake()
    {
        slider ??= GetComponent<Slider>();
        valueText ??= GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        if (!slider) return;

        float startValue = LoadStartingValue();
        startValue = Mathf.Clamp(startValue, 0f, 10f);

        // Initialize without triggering event
        slider.SetValueWithoutNotify(startValue);
        UpdateValueText(startValue);

        slider.onValueChanged.AddListener(HandleValueChanged);
        initialized = true;
    }

    private void OnDisable()
    {
        if (slider && initialized)
        {
            slider.onValueChanged.RemoveListener(HandleValueChanged);
            initialized = false;
        }
    }

    private float LoadStartingValue()
    {
        string prefKey = $"{type}Volume";

        if (PlayerPrefs.HasKey(prefKey))
            return PlayerPrefs.GetFloat(prefKey);

        if (AudioManager.Instance != null)
        {
            return type switch
            {
                VolumeType.Master => AudioManager.Instance.masterVolume,
                VolumeType.SFX => AudioManager.Instance.sfxVolume,
                VolumeType.Music => AudioManager.Instance.musicVolume,
                _ => 5f
            };
        }

        return 5f;
    }

    private void HandleValueChanged(float value)
    {
        if (!initialized) return;

        UpdateValueText(value);

        if (AudioManager.Instance == null) return;

        switch (type)
        {
            case VolumeType.Master:
                AudioManager.Instance.SetMasterVolume(value);
                break;

            case VolumeType.SFX:
                AudioManager.Instance.SetSFXVolume(value);
                break;

            case VolumeType.Music:
                AudioManager.Instance.SetMusicVolume(value);
                break;
        }

        // Persist the setting
        PlayerPrefs.SetFloat($"{type}Volume", value);
        PlayerPrefs.Save();
    }

    private void UpdateValueText(float value)
    {
        if (valueText)
            valueText.text = Mathf.RoundToInt(value).ToString();
    }
}
