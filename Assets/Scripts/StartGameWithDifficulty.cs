using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameWithDifficulty : MonoBehaviour
{
    public void startEasy()
    {
      GameManager.StartDifficulty = Difficulty.Easy;
      SceneManager.LoadScene("Flappy Bird");
    }

    public void startNormal(){
      GameManager.StartDifficulty = Difficulty.Normal;
      SceneManager.LoadScene("Flappy Bird");
    }

    public void startHard(){
        GameManager.StartDifficulty = Difficulty.Hard;
        SceneManager.LoadScene("Flappy Bird");
    }

   
}

