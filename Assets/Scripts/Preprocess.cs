using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class Preprocess : MonoBehaviour
{
    UnityAction<byte[]> callback;

    public void ScaleImage(Texture2D source, int width, int height, UnityAction<byte[]> callback)
    {
        this.callback = callback;

        // Create a temporary RenderTexture with the desired width and height.
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

        // Copy the contents of the source texture to the temporary RenderTexture.
        Graphics.Blit(source, rt);

        // Save the currently active RenderTexture and set the temporary RenderTexture as active.
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        // Create a new Texture2D with the desired width and height, and copy the contents of the temporary RenderTexture to it.
        Texture2D newTexture = new Texture2D(width, height);
        newTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        newTexture.Apply();

        //byte[] bytes = newTexture.EncodeToPNG();
        //File.WriteAllBytes("resizedtest.png", bytes);

        //To save texture on android device for testing
        //string filePath = Path.Combine(Application.persistentDataPath, "resizedtest.png");
        //ile.WriteAllBytes(filePath, bytes);

        // Restore the previously active RenderTexture and release the temporary one.
        RenderTexture.active = previous;


        AsyncGPUReadback.Request(rt, 0, TextureFormat.RGB24, OnCompleteReadback);

        RenderTexture.ReleaseTemporary(rt);
        RenderTexture.ReleaseTemporary(previous);
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.Log("GPU readback error detected.");
            return;
        }
        
        callback.Invoke(request.GetData<byte>().ToArray());
    }



    //Scale and crop instead of just scale
    /*
    RenderTexture renderTexture;
    Vector2 scale = new Vector2(1, 1);
    Vector2 offset = Vector2.zero;

    UnityAction<byte[]> callback;

    public void ScaleAndCropImage(Texture2D arTexture, int desiredSize, UnityAction<byte[]> callback)
    {

        this.callback = callback;

        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(desiredSize, desiredSize, 0, RenderTextureFormat.ARGB32);
        }

        scale.x = (float)arTexture.height / (float)arTexture.width;
        offset.x = (1 - scale.x) / 2f;
        Graphics.Blit(arTexture, renderTexture, scale, offset);
        AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, OnCompleteReadback);
    }
    
    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.Log("GPU readback error detected.");
            return;
        }

        callback.Invoke(request.GetData<byte>().ToArray());
    }
     */



}