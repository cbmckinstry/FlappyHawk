using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VolumeSlider : MonoBehaviour
{
    public enum VolumeType { Master, SFX, Music }

    [Header("Bindings")]
    public VolumeType type;
    public Slider slider;
    public TMP_Text valueText;

    private void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        if (!valueText) valueText = GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        if (!slider) return;

        float startValue;

        // Prefer PlayerPrefs if saved, otherwise use AudioManager defaults
        if (PlayerPrefs.HasKey($"{type}Volume"))
        {
            startValue = PlayerPrefs.GetFloat($"{type}Volume");
        }
        else if (AudioManager.Instance != null)
        {
            switch (type)
            {
                case VolumeType.Master: startValue = AudioManager.Instance.masterVolume; break;
                case VolumeType.SFX: startValue = AudioManager.Instance.sfxVolume; break;
                case VolumeType.Music: startValue = AudioManager.Instance.musicVolume; break;
                default: startValue = 5f; break;
            }
        }
        else
        {
            startValue = 5f;
        }

        startValue = Mathf.Clamp(startValue, 0f, 10f);
        slider.SetValueWithoutNotify(startValue);
        UpdateValueText(startValue);
        slider.onValueChanged.AddListener(HandleValueChanged);
    }

    private void OnDisable()
    {
        if (slider)
            slider.onValueChanged.RemoveListener(HandleValueChanged);
    }

    private void HandleValueChanged(float value)
    {
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
    }

    private void UpdateValueText(float value)
    {
        if (valueText)
            valueText.text = value.ToString("F0");
    }
}
