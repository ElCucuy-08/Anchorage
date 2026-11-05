using UnityEngine;
using UnityEngine.SceneManagement;


public class LoadScene : MonoBehaviour
{
    public void Update(int indexScene)
    { 
        sceneManager.LoadScene(indexScene)
    }
}
