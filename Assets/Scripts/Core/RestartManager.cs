using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class RestartManager : MonoBehaviour
{
    [SerializeField] private Key restartKey = Key.R;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[restartKey].wasPressedThisFrame)
            RestartScene();
    }

    
    public void RestartScene()
    {
        Time.timeScale = 1f; 
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }
}