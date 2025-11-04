using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameWithDifficulty : MonoBehaviour
{
    public void startEasy()
    {
      GameManager.StartDifficulty = Difficulty.Easy;
      SceneManager.LoadScene("IowaMode");
    }

    public void startNormal(){
      GameManager.StartDifficulty = Difficulty.Normal;
      SceneManager.LoadScene("IowaMode");
    }

    public void startHard(){
        GameManager.StartDifficulty = Difficulty.Hard;
        SceneManager.LoadScene("IowaMode");
    }

   
}

