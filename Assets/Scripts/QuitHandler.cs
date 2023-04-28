using UnityEditor;
using UnityEngine;

public class QuitHandler : MonoBehaviour
{
    ObjectDetection objDetectionScript;

    private void Start()
    {
        objDetectionScript = GetComponent<ObjectDetection>();
    }

    // Dispose any resources here, because it is possible that if application quits abruptly, it doesn't dispose them from ObjectDetection script,
    //so I added a hook to the Application.quitting event, which is fired when the application is about to quit, even if the quit was initiated from the editor.
    private void OnApplicationQuit()
    {
        objDetectionScript.tensor.Dispose();
        objDetectionScript.detectionBoxes.Dispose();
        objDetectionScript.detectionScores.Dispose();
        //Debug.Log("Application quitting");
    }

    //[MenuItem("Custom/Force Quit")]
    private void OnApplicationQuitting()
    {
        //objDetectionScript.tensor.Dispose();
        //objDetectionScript.detectionBoxes.Dispose();
        //objDetectionScript.detectionScores.Dispose();
        Debug.Log("Application about to quit");
    }

    public void ExitApp()
    {
        Application.Quit();
    }
}