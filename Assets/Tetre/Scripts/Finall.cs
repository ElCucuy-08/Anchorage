using UnityEngine;
using UnityEngine.SceneManagement;

public class Finall : MonoBehaviour
{
    public void RestartGame()
    {
        SceneManager.LoadScene("Game");
    }
}