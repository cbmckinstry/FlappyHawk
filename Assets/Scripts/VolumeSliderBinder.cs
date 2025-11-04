using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderBinder : MonoBehaviour
{
    public enum Kind { Master, SFX, Music }
    public Kind kind;
    public Slider slider;

    void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
    }

    void OnEnable()
    {
        // Initialize slider from saved prefs without triggering events
        float start = kind switch
        {
            Kind.Master => PlayerPrefs.GetFloat("MasterVolume", 5f),
            Kind.SFX    => PlayerPrefs.GetFloat("SFXVolume",    5f),
            _           => PlayerPrefs.GetFloat("MusicVolume",  5f),
        };
        if (slider) slider.SetValueWithoutNotify(start);

        if (slider) slider.onValueChanged.AddListener(Handle);
    }

    void OnDisable()
    {
        if (slider) slider.onValueChanged.RemoveListener(Handle);
    }

    void Handle(float v)
    {
        if (AudioManager.Instance == null) return;
        switch (kind)
        {
            case Kind.Master: AudioManager.Instance.SetMasterVolume(v); break;
            case Kind.SFX:    AudioManager.Instance.SetSFXVolume(v);    break;
            case Kind.Music:  AudioManager.Instance.SetMusicVolume(v);  break;
        }
    }
}

