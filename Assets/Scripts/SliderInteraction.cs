using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderInteraction : MonoBehaviour
{
    private Slider slider;
    private TMP_Text textField;

    private void Reset()
    {
        // Auto-assign components when you add the script
        slider = GetComponent<Slider>();
        textField = GetComponentInChildren<TMP_Text>();
    }

    private void Awake()
    {
        // Assign components manually in Awake if not already done
        if (slider == null)
            slider = GetComponent<Slider>();

        if (textField == null)
            textField = GetComponentInChildren<TMP_Text>();

        // Subscribe to value change event
        slider.onValueChanged.AddListener(HandleSliderValueChanged);

        // Initialize text
        HandleSliderValueChanged(slider.value);
    }

    private void OnDestroy()
    {
        // Clean up listener
        if (slider != null)
            slider.onValueChanged.RemoveListener(HandleSliderValueChanged);
    }

    public void HandleSliderValueChanged(float value)
    {
        // Display rounded or decimal values
        textField.text = value.ToString("F0");
    }
}
