using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public AudioManager audioManager;
    public GameObject quitButton;
    public GameObject dropdownMenu;

    private void Awake()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            if (quitButton == null)
            {
                Transform quitTransform = canvas.transform.Find("Button (Quit)");
                if (quitTransform != null) quitButton = quitTransform.gameObject;
            }
            
            if (dropdownMenu == null)
            {
                Transform dropdownTransform = canvas.transform.Find("Dropdown Mode");
                if (dropdownTransform != null) dropdownMenu = dropdownTransform.gameObject;
            }
        }
    }

    public void PlayGame()
    {
        if (audioManager != null)
            audioManager.PlayClickSound();

        if (quitButton != null)
            quitButton.SetActive(false);
            
        if (dropdownMenu != null)
            dropdownMenu.SetActive(false);

        GameManager.Instance?.Play();
    }

    public void QuitGame()
    {
        if (audioManager != null)
            audioManager.PlayClickSound();

        Application.Quit();
    }
}
