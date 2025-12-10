using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Slider))]
public class CustomSpawnSlider : MonoBehaviour
{
    public enum WeightType
    {
        ObstacleChance,
        Balloon,
        Silo,
        Turbine,
        CycloneBird,
        GravityFlipChance
    }

    [Header("Settings")]
    public WeightType type;

    [Tooltip("Optional text to show the current value as a percent.")]
    public TMP_Text valueText;

    private Slider slider;
    private bool initialized = false;

    private void Awake()
    {
        slider ??= GetComponent<Slider>();
        // If you want: auto-grab a child TMP_Text if you don’t manually assign one
        if (valueText == null)
            valueText = GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        if (!slider) return;

        float startValue = GetCurrentValue();
        startValue = Mathf.Clamp01(startValue);

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

    private void HandleValueChanged(float value)
    {
        if (!initialized) return;

        value = Mathf.Clamp01(value);
        SetValue(value);
        UpdateValueText(value);
    }

    private float GetCurrentValue()
    {
        switch (type)
        {
            case WeightType.ObstacleChance: return CustomSpawnSettings.obstacleSpawnChance;

            case WeightType.Balloon:        return CustomSpawnSettings.balloonWeight;
            case WeightType.Silo:           return CustomSpawnSettings.siloWeight;
            case WeightType.Turbine:        return CustomSpawnSettings.turbineWeight;
            case WeightType.CycloneBird:    return CustomSpawnSettings.cycloneBirdWeight;
            case WeightType.GravityFlipChance: return CustomSpawnSettings.gravityFlipChance;
        }
        return 0.5f;
    }

    private void SetValue(float value)
    {
        switch (type)
        {
            case WeightType.ObstacleChance: CustomSpawnSettings.obstacleSpawnChance = value; break;

            case WeightType.Balloon:        CustomSpawnSettings.balloonWeight      = value; break;
            case WeightType.Silo:           CustomSpawnSettings.siloWeight         = value; break;
            case WeightType.Turbine:        CustomSpawnSettings.turbineWeight      = value; break;
            case WeightType.CycloneBird:    CustomSpawnSettings.cycloneBirdWeight  = value; break;
            case WeightType.GravityFlipChance: CustomSpawnSettings.gravityFlipChance = value; break;
        }
    }

    private void UpdateValueText(float value)
    {
        if (valueText != null)
        {
            valueText.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }
}
