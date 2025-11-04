using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderInteraction : MonoBehaviour
{
    private Slider slider;
    private TMP_Text textField;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        textField = GetComponentInChildren<TMP_Text>();
        slider.onValueChanged.AddListener(UpdateLabel);
        UpdateLabel(slider.value);
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(UpdateLabel);
    }

    private void UpdateLabel(float value)
    {
        if (textField) textField.text = value.ToString("F0");
    }
}
