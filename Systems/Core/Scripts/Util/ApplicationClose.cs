using UnityEngine;

public class ApplicationClose : MonoBehaviour
{
    public void CloseApp()
    {
        Debug.Log("Application is closing...");
        Application.Quit();

        // If running in the editor, stop playing
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
