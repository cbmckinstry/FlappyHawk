using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VolumeSliderBinder : MonoBehaviour
{
    public enum Kind { Master, SFX, Music }
    public Kind kind;
    public Slider slider;
    public TMP_Text valueText; // optional: displays numeric value

    private void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        if (!valueText) valueText = GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        if (!slider) return;

        // Load saved volume
        float start = kind switch
        {
            Kind.Master => PlayerPrefs.GetFloat("MasterVolume", 5f),
            Kind.SFX => PlayerPrefs.GetFloat("SFXVolume", 5f),
            _ => PlayerPrefs.GetFloat("MusicVolume", 5f),
        };

        start = Mathf.Clamp(start, 0f, 10f);
        slider.SetValueWithoutNotify(start);
        UpdateAudioManager(start);
        UpdateValueText(start);

        slider.onValueChanged.AddListener(Handle);
    }

    private void OnDisable()
    {
        if (slider)
            slider.onValueChanged.RemoveListener(Handle);
    }

    private void Handle(float v)
    {
        UpdateAudioManager(v);
        UpdateValueText(v);
    }

    private void UpdateAudioManager(float v)
    {
        if (AudioManager.Instance == null) return;

        switch (kind)
        {
            case Kind.Master: AudioManager.Instance.SetMasterVolume(v); break;
            case Kind.SFX: AudioManager.Instance.SetSFXVolume(v); break;
            case Kind.Music: AudioManager.Instance.SetMusicVolume(v); break;
        }
    }

    private void UpdateValueText(float v)
    {
        if (valueText)
            valueText.text = v.ToString("F0");
    }
}
