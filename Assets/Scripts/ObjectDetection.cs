#define ENABLED_VIDEO_TEST

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Barracuda;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;
using UnityEngine.Networking;

public class ObjectDetection : MonoBehaviour
{
    const int IMAGE_SIZE = 416;
    const string INPUT_NAME = "input";
    const string OUTPUT_NAME = "boxes";
    const string OUTPUT_NAME_2 = "confs";
    const float DETECTION_THRESHOLD = 0.9f;
    
    public ARCameraManager arCameraManager;
    public Preprocess preprocess;
    public NNModel modelFile;
    
    [Header("The number of object classes of my ONNX model")]
    public int numClasses = 2;// the number of object classes of my ONNX model. You can see it from the 3rd value of the confs tensor       
    private Model model;
    private IWorker worker;

    [Header("The slider bar for object locking")]
    public Slider loadingBar;
    [Header("The time needed before the object locks and it detected")]
    public float loadingTime = 3;
    [Header("How much time passed before last detection frame, so \nthat it will reset the loading bar")]
    public float lastDetectionTimeDifference = 0.2f;
    [Header("How much time should pass after detected object goes \nout of view before the info panel disappears")]
    [SerializeField] private float displayDurationAfterOutOfView = 2f;
    private float lastDetectionTime = 0;
    private bool detectionLocked = false; //bool to check if object was detected for given loading time
    private int previousClassIndex = 0;
    private int camScreenWidth = 853; //this default value is not the correct one. It's just because i wanted to intantiate it with something
    private int camScreenHeight = 480;
    private float screenWidth = Screen.width;
    private float screenHeight = Screen.height;

    [Space][Space]
    [Header("Canvas UI")]
    public TMP_Text textMeshPro;
    public TMP_Text sizeText;

    #region Code To Draw Boxes With onGUI() Instead of a Box Prefab
    //private int drawClassIndex = 0;
    //private Rect drawRect;
    #endregion

    private bool drawBoxBool = false;
    private GameObject box;
    public GameObject boundingBoxPrefab;
    public Canvas canvas;

    [SerializeField] private ARTrackedImageManager trackedImageManager;
    //[SerializeField] private ARObjectInfoPanel infoPanelPrefab;
    [SerializeField] private Camera arCamera;

    private ARObjectInfoPanel infoPanel;

    #region Skip Frames For Optimization
    [Space][Space]
    [Header("Optimization Settings")]
    [Min(1)]
    public int framesToSkip = 1;
    private bool isProcessing = false; // Check if the YOLOv4-tiny model is currently processing an image
    private int frames = 0;

    private int framesIEnum = 0; //same as frames variable above but for IEnumarator for testing video

    [Header("How many more loops until it breaks the loop, \nafter the first detection in the channels loop")]
    public int breakLoopCount = 30;
    #endregion

    #region FPS Monitor
    [Space][Space]
    [Header("FPS Monitor Settings")]
    public bool enableFpsDisplay = true;
    public TMP_Text fpsText;    
    public float updateInterval = 0.5f; //How often the fps monitor display refreshes 

    private float fpsAccumulator = 0f;
    private float fpsNextUpdate = 0f;
    private int frames2 = 0;
    #endregion


    public Tensor tensor;
    public Tensor detectionBoxes;
    public Tensor detectionScores;
    private Dictionary<string, Tensor> inputs;
    private float[,,,] scores4D;
    private float[,,,] boxes4D;
    private List<DetectedObject> detectedObjects;
    private float[] scoresArray;
    private TensorShape scoresShape;
    private float[] boxesArray;
    private TensorShape boxesShape;

    private readonly Dictionary<int, string> classObjDictionary = new Dictionary<int, string>()
    {
        {0, "Turbine"},
        {1, "Heater"},
        {2, "Pliers"}
    };

    [Header("The info panels that will display when an object is detected")]
    public GameObject[] uiPanelPrefab;
    private bool QRViewBool = true;
    [Header("The button that switches from QR to Yolo")]
    public GameObject switchButton;

    private readonly Dictionary<string, int> trackedImageDictionary = new Dictionary<string, int>()
    {
        {"Turbine", 0},
        {"Heater", 1},
        {"Pliers", 2}
    };


    #region Video For Testing Only (To Remove Later)
    
    [Header("The raw image that the video frames will display on and the \nvideo player component (These are for testing with video and \nto be removed later)")]
    public RawImage display; //I didn't place this inside the #if UNITY_EDITOR because i need this to destroy the gameobject on build

//#if UNITY_EDITOR
    public VideoPlayer videoPlayer;
    private Texture2D arVideoTexture;    
    private Texture frame; //Texture frame only for testing video
//#endif//*/

    #endregion

    /*
    IEnumerator PlayVideoFromServer(string url)
    {
        UnityWebRequest videoRequest = UnityWebRequest.Get(url);
        yield return videoRequest.SendWebRequest();

        if (videoRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(videoRequest.error);
        }
        else
        {
            videoPlayer = videoPlayer.GetComponent<VideoPlayer>();
            videoPlayer.url = url;
            videoPlayer.Prepare();
            videoPlayer.Play();
        }
    }

    test
    //*/

    void Start()
    {
        for (int h = 0; h < uiPanelPrefab.Length; h++)
        {
            uiPanelPrefab[h].SetActive(false);
        }
        
        model = ModelLoader.Load(modelFile);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);

        loadingBar.maxValue = loadingTime;
        loadingBar.gameObject.SetActive(false);

        //#if !UNITY_EDITOR
        //canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Screen.height);
        //#endif

        screenWidth = canvas.GetComponent<RectTransform>().rect.width; //Screen.width;
        screenHeight = canvas.GetComponent<RectTransform>().rect.height; //Screen.height; 

        #region Video For Testing Only (To Remove Later)
        /*
        //#if UNITY_EDITOR
        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
//#endif

//#if !UNITY_EDITOR
        //Destroy(display.gameObject);  
//#endif
        //*/
        #endregion
    }

    #region Video For Testing Only (To Remove Later)
    /*
//#if UNITY_EDITOR
    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.Play();
        StartCoroutine(ExtractFrames());
    }


    private IEnumerator ExtractFrames()
    {
        while (videoPlayer.isPlaying)
        {
            frame = videoPlayer.texture; //get vide frame
            display.texture = frame; //display frame on raw image UI in scene

            #region Skip Frames For Optimization
            //Skip a number of frames set by the framesToSkip variable, for optimization purposes
            framesIEnum++;
            if (framesIEnum < framesToSkip)
            {
                
            }
            else
            {
                framesIEnum = 0;
                #endregion

                RenderTexture rt = RenderTexture.GetTemporary(frame.width, frame.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

                // Copy the contents of the source texture to the temporary RenderTexture.
                Graphics.Blit(frame, rt);

                // Save the currently active RenderTexture and set the temporary RenderTexture as active.
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;

                // Create a new Texture2D with the desired width and height, and copy the contents of the temporary RenderTexture to it.
                Texture2D newTexture = new Texture2D(frame.width, frame.height);
                newTexture.ReadPixels(new Rect(0, 0, frame.width, frame.height), 0, 0);
                newTexture.Apply();

                // Restore the previously active RenderTexture and release the temporary one.
                RenderTexture.active = previous;

                RenderTexture.ReleaseTemporary(rt);
                RenderTexture.ReleaseTemporary(previous);

                arVideoTexture = newTexture;
            }
            

            yield return new WaitForEndOfFrame();
        }

    }
//#endif//*/
    #endregion


    void Update()
    {
        #region FPS Monitor
        if (enableFpsDisplay)
        {
            frames2++;

            fpsAccumulator += Time.deltaTime;
            if (Time.time > fpsNextUpdate)
            {
                float currentFPS = frames2 / fpsAccumulator;
                fpsNextUpdate += updateInterval;
                fpsAccumulator = 0f;
                frames2 = 0;

                fpsText.text = "FPS: " + currentFPS.ToString("F0"); //F0 for zero decimal
            }
        }        
        #endregion

        // Check if the YOLOv4-tiny model is currently processing an image NOT DONE
        /*if (isProcessing)
        {
            return;
        }
        isProcessing = true; */


        if (QRViewBool) // Don't execute the rest of this code when QR is enabled
            return;


        #region Skip Frames For Optimization
        //Skip a number of frames set by the framesToSkip variable, for optimization purposes
        frames++;
        if (frames < framesToSkip)
        {
            return;
        }
        frames = 0;
        #endregion


        //sizeText.text = "Width: " + screenWidth + " Height: " + screenHeight;


        if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) //853x480
        {
            //sizeText.text = "Camera";
            // Get the current device orientation and determine if the image needs to be rotated, to adjust for landscape mode
            //bool isLandscape = Input.deviceOrientation == DeviceOrientation.LandscapeLeft || Input.deviceOrientation == DeviceOrientation.LandscapeRight;

            // Swap the width and height if necessary based on device's orientation
            //camScreenHeight = isLandscape ? image.height : image.width;
            //camScreenWidth = isLandscape ? image.width : image.height;
            camScreenHeight = image.height;
            camScreenWidth = image.width;

            // Create a Texture2D object with the width and height of the XRCpuImage
            Texture2D arTexture = new Texture2D(camScreenWidth, camScreenHeight, TextureFormat.RGBA32, false);
            
            //sizeText.text = "W: " + screenWidth + " H: " + screenHeight;

            // Load the raw image data from the XRCpuImage into the Texture2D
            var conversionParams = new XRCpuImage.ConversionParams
            {
                outputDimensions = new Vector2Int(camScreenWidth, camScreenHeight), // Set the output dimensions to the size of the XRCpuImage
                outputFormat = TextureFormat.RGBA32, // Set the output format to RGBA32
                inputRect = new RectInt(0, 0, camScreenWidth, camScreenHeight), // Set the input rect to cover the entire XRCpuImage
                transformation = XRCpuImage.Transformation.MirrorX // Set the transformation to Mirror X, because it was inverted
            };

            // Allocate a buffer to hold the converted image data
            int bufferSize = image.GetConvertedDataSize(conversionParams);
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                // Convert the image data and store it in the texture's raw texture data buffer
                image.Convert(conversionParams, buffer, bufferSize);
                arTexture.LoadRawTextureData(buffer, bufferSize);
                arTexture.Apply();
            }
            finally
            {
                // Free the buffer memory 
                Marshal.FreeHGlobal(buffer);
                //Release the XRCpuImage to avoid memory leaks
                image.Dispose();
            }
            //sizeText.text = "Camera 2";
            preprocess.ScaleImage(arTexture, IMAGE_SIZE, IMAGE_SIZE, RunModel);
        }

        #region Code Just For Testing With Video Instead Of The Actual Camera Feed     
        /*
//#if UNITY_EDITOR
        if (videoPlayer.isPlaying)
            preprocess.ScaleImage(arVideoTexture, IMAGE_SIZE, IMAGE_SIZE, RunModel);
//#endif
        //*/
        #endregion
    }


    void RunModel(byte[] pixels)
    {
        sizeText.text = "Width: " + screenWidth + " Height: " + screenHeight
            + "\nWidth: " + Screen.width + " Height: " + Screen.height;
        // Create a new Tensor object with the desired input shape and fill it with the byte[] array
        tensor = TransformPixelsToInput(pixels); //Auto disposes tensors inside 'using'

        inputs = new Dictionary<string, Tensor> {
            { INPUT_NAME, tensor }
        };

        // Pass the Tensor as input to the ONNX model
        worker.Execute(inputs);

        detectionBoxes = worker.PeekOutput(OUTPUT_NAME);
        detectionScores = worker.PeekOutput(OUTPUT_NAME_2);

        //Dispose tensors to avoid GPU resource leak (NOTE: the dispose works here but not at the end of the method. This is probably because if i delay the disposing
        //by allowing to execute all the loops etc first, the next frame is executing while the code from this frame is executed and thus it disposes the tensors from
        //the next frame before they get the chance to execute. This is just my guess, not sure.)
        //Debug.Log("Before: " + inputs["input"].ToString());
        tensor.Dispose();
        //Debug.Log("After: " + inputs["input"].ToString());
        inputs["input"].Dispose();
        // Debug.Log("Before inputs: " + inputs.Count);
        inputs.Clear();
        // Debug.Log("After inputs: " + inputs.Count);
        //worker.Dispose();

        #region Output Handling For Detection And Drawing Of Bounding Boxes etc
        // convert the tensor outputs to arrays
        //The ToReadOnlyArray() method is used to convert the Tensor object to a read-only array. This is because the Tensor object is mutable, which means that it can be changed after it is created. However, in some cases, it may be desirable to use an immutable data structure like a read-only array to prevent accidental modification of the data.
        //Regarding the conversion to a 4D array, it is possible that the boxesTensor has a different shape than what is expected by the rest of the code.By converting the tensor to a read - only array and then to a 4D array with the expected shape, we ensure that the data is in the correct format for the rest of the code to work properly.
        scoresArray = detectionScores.ToReadOnlyArray();
        scoresShape = detectionScores.shape;
        scores4D = new float[scoresShape.batch, scoresShape.height, scoresShape.width, scoresShape.channels];
        //Take the scoresArray, which contains the detection scores as a one-dimensional array, and copy it into a
        //four-dimensional array (scores4D) for easier indexing and manipulation of the data.
        Buffer.BlockCopy(scoresArray, 0, scores4D, 0, scoresArray.Length * sizeof(float));
        
        boxesArray = detectionBoxes.ToReadOnlyArray();
        boxesShape = detectionBoxes.shape;
        // Convert 1D array to 4D array
        boxes4D = new float[boxesShape.batch, boxesShape.height, boxesShape.width, boxesShape.channels];
        Buffer.BlockCopy(boxesArray, 0, boxes4D, 0, boxesArray.Length * sizeof(float));

        #region QuickSort ONNX Output Arrays For Optimization
        
        //var testArray = FindTopElements(scores4D, 20);
        /*
        for (int i = 0; i < testArray.GetLength(3); i++)
        {
            for (int j = 0; j < testArray.GetLength(2); j++)
            {
                Debug.Log("Class " + j + " TOP " + testArray.GetLength(3) + ": " + testArray[0, 0, j, i].ToString("F10"));
            }
            
        }
        */
        #endregion


        //dispose tensors to avoid GPU resource leak
        detectionBoxes.Dispose();
        detectionScores.Dispose();


        // create a list to store the detected objects based on the DetectedObject object class i created
        detectedObjects = new List<DetectedObject>();//this list is for later in case i want to recognize multiple objects at once
        
        //variables for optimization to break loop earlier
        var breakLoop = Mathf.Infinity;
        bool foundFirstDetectedObjectBool = false;
        
        for (int i = 0; i < boxesShape.channels; i++)   //for (int i = 0; i < boxesShape.channels; i++)   //i = boxIndex
        {
            //condition to break loop early for optimization
            if (i > breakLoop)
                break;

            // get the class probability for each object class
            float[] classProbabilities = new float[numClasses];
            for (int k = 0; k < numClasses; k++)
            {
                float classProbability = scores4D[0, 0, k, i];
                classProbabilities[k] = classProbability;
            }

            // find the class with the highest probability
            float maxProbability = classProbabilities.Max();
            int maxIndex = classProbabilities.ToList().IndexOf(maxProbability);
            
            textMeshPro.text = "Max: " + maxProbability.ToString()
            + "\ni = " + i;

            #region Export Values to TXT For Testing
            /*
            // Define the path and filename of the text file
            string filePath = "output.txt";

            // Define the data as a list of lists
            List<List<string>> data = new List<List<string>>();


            if (maxProbability > 1.1f)
            {
                // Create a new row for this item in the first loop
                List<string> rowData = new List<string>();
                rowData.Add(i + "=" + maxProbability.ToString());

                // Add the row to the data list
                data.Add(rowData);

                // Write the data to the text file
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Write each row of data
                    foreach (List<string> row in data)
                    {
                        // Write the row to the text file, with values separated by tabs
                        writer.WriteLine(string.Join("\t", row));
                    }
                }
            }
                */
            #endregion


            // if the highest probability is greater than a certain threshold,
            // add the detected object to the list
            if (maxProbability > DETECTION_THRESHOLD)
            {
                //NOTE: check y axis because i thought it was top to bottom but now i see it is the opposite. Make sure which one is correct
                // get the bounding box coordinates from the tensor (x is the horizontal pixels left to right and y the vertical bottom to top)
                //x1,y1 are the coordinates of bottom-left corner and x2,y2 are the coordinates of top-right corner of the bounding box                
                
                float x1 = Mathf.Clamp01(boxes4D[0, 0, 0, i]);
                float y1 = Mathf.Clamp01(boxes4D[0, 0, 1, i]);
                float x2 = Mathf.Clamp01(boxes4D[0, 0, 2, i]);
                float y2 = Mathf.Clamp01(boxes4D[0, 0, 3, i]);

                // convert the coordinates from relative to absolute                
                int x1Abs = Mathf.RoundToInt(x1 * screenWidth);
                int y1Abs = Mathf.RoundToInt(y1 * screenHeight);
                int x2Abs = Mathf.RoundToInt(x2 * screenWidth);
                int y2Abs = Mathf.RoundToInt(y2 * screenHeight);

                // add the detected object to the list
                DetectedObject obj = new DetectedObject();
                obj.classIndex = maxIndex;
                obj.probability = maxProbability;
                obj.boundingBox = new Rect(x1Abs, y1Abs, x2Abs - x1Abs, y2Abs - y1Abs); //NOTE: check the position to make sure i got the center right //x,y position of top-left corner and width,height of bounding box
                detectedObjects.Add(obj); //this list is for later in case i want to recognize multiple objects at once

                /*
                Debug.Log("x1 = " + x1);
                Debug.Log("y1 = " + y1);
                Debug.Log("x2 = " + x2);
                Debug.Log("y2 = " + y2);

                Debug.Log("abs x1 = " + x1Abs);
                Debug.Log("abs y1 = " + y1Abs);
                Debug.Log("abs x2 = " + x2Abs);
                Debug.Log("abs y2 = " + y2Abs);*/

                //When the first detected object is found in the loop, it counts up to another 30 loops to find the rest (NOTE: assuming that they are clustered together
                //at roughly a range of 30 iterations inside the loop), and then breaks the loop to save up resources because from then on there are no other objects
                if (!foundFirstDetectedObjectBool)
                {
                    breakLoop = i + breakLoopCount;
                    foundFirstDetectedObjectBool = true;
                }
                
            }            
        }


        //loop through all the detected objects and find the one whose bounding box's center is closer the screen center
        float closestDistance = Mathf.Infinity;
        int closestObjIndex = 0;
        for (int j = 0; j < detectedObjects.Count; j++)
        {
            var detectedObjBox = detectedObjects[j].boundingBox;
            var detectedObjCenter = detectedObjBox.center;
            //Debug.Log("Corner: " + detectedObjBox.position);
            //Debug.Log("Center: " + detectedObjBox.center);
            Vector2 screenCenter = new Vector2(screenWidth / 2, screenHeight / 2);//new Vector2(Screen.width / 2, Screen.height / 2);//new Vector2(screenWidth / 2, screenHeight / 2);
            //Vector2 worldCenter = arCamera.ScreenToWorldPoint(screenCenter);  //NOTE TO CHECK: possibly needed along with a reference to the main camera

            //get detected object closest to the center of the screen
            float currentDistance = Vector2.Distance(detectedObjCenter, screenCenter);

            if (currentDistance < closestDistance)
            {
                closestObjIndex = j;
                closestDistance = currentDistance;
            }
        }


        //check how much time passed since last object detected frame and if it exceeded the time limit, reset the loading bar
        //this is to account for negligible lost detection frames in between, meaning to not reset the loading bar if are just
        //a couple of frames in between that didn't detect the object. If for example out of 60 frames the detection misses 3 frames,
        //don't reset the loading bar
        //Also checks to see if the class detected in this frame changed from the previous frame in order to reset the loading bar        
        var tempTimeDifference = Time.time - lastDetectionTime;
        if (tempTimeDifference > lastDetectionTimeDifference)// || detectedObjects[closestObjIndex].classIndex != previousClassIndex)
        {
            loadingBar.value = 0;            
            loadingBar.gameObject.SetActive(false);
        }
        //previousClassIndex = detectedObjects[closestObjIndex].classIndex; //save class to check if the class changed in the next frame



        //start loading to lock onto a detected object        
        if (detectedObjects.Count != 0)
        {
            previousClassIndex = detectedObjects[closestObjIndex].classIndex; //save class to check if the class changed in the next frame
            //save the time that the last object detection frame occured, to use for loading bar
            lastDetectionTime = Time.time;
            if (detectionLocked == false) //if detected object hasn't locked yet, then start the loading bar
                LoadingDetectedObjectUI();
            else
            {
                if (!drawBoxBool)
                {                    
                    //drawBoxBool = true;
                }
                DrawBoxes(detectedObjects[closestObjIndex].boundingBox, detectedObjects[closestObjIndex].classIndex, detectedObjects[closestObjIndex].probability);

                #region Code To Draw Boxes With onGUI() Instead of a Box Prefab
                /*
                drawRect = detectedObjects[closestObjIndex].boundingBox;
                drawClassIndex = detectedObjects[closestObjIndex].classIndex;
                //*/
                #endregion

                for (int h = 0; h < numClasses; h++)
                {
                    if (h != detectedObjects[closestObjIndex].classIndex)
                        uiPanelPrefab[h].SetActive(false);
                    else
                        uiPanelPrefab[detectedObjects[closestObjIndex].classIndex].SetActive(true);
                }
            }
        }
        else
        {
            if (box != null) //destroy GUI box of detected object when there is no object detected in the frame
            {
                DestroyImmediate(box);
            }
              
            var tempTimeDifference2 = Time.time - lastDetectionTime;
            if (tempTimeDifference2 > displayDurationAfterOutOfView && detectionLocked)
            {
                for (int h = 0; h < numClasses; h++)
                {
                    uiPanelPrefab[h].SetActive(false);
                }
                detectionLocked = false;
            }
        }

        #endregion

            // Destroy the Texture2D objects and release their associated memory
            //DestroyImmediate(arTexture);
            //DestroyImmediate(resizedTexture);
            // }
            //isProcessing = false;


            #region Place Object In AR Scene 2
            /*
            // Get the pixel coordinates of the bounding box
            Vector2 boxCenter = detectedObjects[closestObjIndex].boundingBox.center;

            // Convert the pixel coordinates to world coordinates
            Vector3 screenPoint = arCamera.ViewportToScreenPoint(new Vector3(boxCenter.x, boxCenter.y, 0.0f));
            Vector3 worldPoint = arCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, arCamera.nearClipPlane));

            // Calculate the offset position for the UI panel
            Vector3 panelPosition = worldPoint + new Vector3(0.1f, 0.0f, 0.0f);

            // Create the UI panel
            GameObject panelObject = Instantiate(uiPanelPrefab, canvas.transform);

            // Set the position of the UI panel
            RectTransform panelRectTransform = panelObject.GetComponent<RectTransform>();
            panelRectTransform.position = panelPosition;
            */
            #endregion

            #region Place Object In AR Scene
            /*
            // Convert the bounding box coordinates to world coordinates using the AR camera's extrinsic parameters
            Vector3 objectPos = arCamera.ViewportToWorldPoint(new Vector3(detectedObjects[closestObjIndex].boundingBox.x, detectedObjects[closestObjIndex].boundingBox.y, 1f));
            Quaternion objectRot = arCamera.transform.rotation;

            TrackableId id = new TrackableId();
            if (trackedImageManager.trackables.TryGetTrackable(id, out ARTrackedImage trackedImage))
            {

            }




            // Check if the detected object is already being tracked by AR Foundation
            ARTrackedImage trackedImage = trackedImageManager.GetTrackedImage(gameObject);

            // If the detected object is not already being tracked, create a new ARTrackedImage for it
            if (trackedImage == null)
            {
                // Create a new game object for the detected object
                GameObject detectedObject = new GameObject("Detected Object");

                // Set the game object's position and rotation to match the detected object's position and rotation
                detectedObject.transform.position = objectPos;
                detectedObject.transform.rotation = objectRot;

                // Add an ARTrackedImage component to the game object and set its reference image to null
                trackedImage = detectedObject.AddComponent<ARTrackedImage>();
                trackedImage.referenceImage = null;

                // Start tracking the new ARTrackedImage
                trackedImageManager.trackablesParent = detectedObject.transform.parent;
                trackedImageManager.TrackedImagesChanged();
            }

            // Set the tracked image for the info panel to the newly created or updated ARTrackedImage
            infoPanel?.SetTrackedImage(trackedImage);*/
            #endregion
    }


    void DrawBoxes(Rect boundingBox, int classIndex, float probability)
    {
        if (box == null) //this is to instantiate the box prefab only once and then just move it around to avoid overloading with box gameobjects
        {            
            box = Instantiate(boundingBoxPrefab);
            box.transform.SetParent(canvas.gameObject.transform);
        }
        var rectTransform = box.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);

        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        //#if UNITY_EDITOR
        rectTransform.anchoredPosition = boundingBox.center; // * Screen.height / screenHeight;
//#else
        //rectTransform.anchoredPosition = boundingBox.center;// * Screen.height / screenHeight;
//#endif
        //rectTransform.pivot = new Vector2(0, 0);
        //rectTransform.anchoredPosition = boundingBox.position;

        rectTransform.sizeDelta = boundingBox.size * Screen.height / screenHeight;  // 1440 / screenHeight; 639 / screenHeight;
        
        //box.GetComponentInChildren<TMP_Text>().text = (classIndex == 0) ? "turbine" : "heater";
        box.GetComponentInChildren<TMP_Text>().text = classObjDictionary[classIndex] + "\n" + (probability * 100).ToString("F0") + " %";
    }
    

    #region Code To Draw Boxes With OnGUI() Instead of a Box Prefab
    /*
    void OnGUI()
    {
        if(!detectionLocked)
        {
            //drawClassIndex = 0;
            //var drawRect2 = new Rect(1136 / 2, 639 / 2, 300, 300);
            sizeText.text = "Width: " + screenWidth + " Height: " + screenHeight;
            var drawRect2 = new Rect(drawRect.x, drawRect.y, drawRect.size.x, drawRect.size.y);
            Debug.Log("Rect1 " + drawRect);
            Debug.Log("Rect2 " + drawRect2);

            // draw the box outline
            GUI.skin.box.normal.textColor = Color.red;            
            GUI.skin.box.fontSize = 20;
            GUI.Box(drawRect2, "");

            // draw the class label inside the box
            string classLabel = classObjDictionary[drawClassIndex];
            GUI.Label(drawRect2, classLabel);

            //drawBoxBool = false;
            //Debug.Log("Draw Box");
            
            /*
            // set the anchor and pivot to the center of the box
            //Vector2 boxCenter = new Vector2(drawRect.x + drawRect.width / 2f, drawRect.y + drawRect.height / 2f);
            var boxCenter = drawRect.center;
            GUI.BeginGroup(new Rect(boxCenter.x, boxCenter.y, drawRect.size.x, drawRect.size.y), GUI.skin.box);
            GUI.skin.box.normal.textColor = Color.red;
            GUI.skin.box.fontSize = 20;
            GUI.Box(new Rect(boxCenter.x, boxCenter.y, drawRect.width, drawRect.height), "");
            //GUI.Box(drawRect, "");
            // draw the class label inside the box
            string classLabel = classObjDictionary[drawClassIndex];
            GUI.Label(drawRect, classLabel);
            //GUI.Label(new Rect(-drawRect.width / 2f, -drawRect.height / 2f, drawRect.width, drawRect.height), classLabel);

            GUI.EndGroup();
        }        
    }
    //*/
            
    #endregion


    void LoadingDetectedObjectUI()
    {
        //display loading bar and start loading for object detection lock
        loadingBar.gameObject.SetActive(true);
        var temp = loadingBar.value + (Time.deltaTime * framesToSkip);
        temp = Mathf.Clamp(temp, 0f, loadingBar.maxValue);
        loadingBar.value = temp;

        //do something when loading is finished
        if (loadingBar.value == loadingBar.maxValue)
        {
            loadingBar.value = 0;
            loadingBar.gameObject.SetActive(false);
            detectionLocked = true;            
        }
    }

    #region Backup Unused Functions In Case They Are Needed Later
    /*
    Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
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

        // Restore the previously active RenderTexture and release the temporary one.
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        // Return the new resized texture.
        return newTexture;
    }


    //Needed if i take the camera texture from camera component instead of TryAcquireLatestCpuImage
    Texture2D ConvertRenderTextureToTexture2D(RenderTexture rt)
    {
        // Save the currently active RenderTexture and set the temporary RenderTexture as active.
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        // Create a new Texture2D with the desired width and height, copy the contents of the temporary RenderTexture to it and apply the changes to the new texture.
        Texture2D newTexture = new Texture2D(rt.width, rt.height);
        newTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        newTexture.Apply();

        // Restore the previously active RenderTexture and release the temporary one.
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        // Return the new resized texture.
        return newTexture;
    }*/
    #endregion

    Tensor TransformPixelsToInput(byte[] pixels)
    {
        // Create a new float array with the same length as the input byte array
        float[] transformedPixels = new float[pixels.Length];

        // Scale the pixel values from the original range of 0-255 to a new range of -1 to 1
        for (int i = 0; i < pixels.Length; i++)
        {
            transformedPixels[i] = (pixels[i] - 127f) / 128f;
        }

        // Create a new Tensor object with dimensions (1, IMAGE_SIZE, IMAGE_SIZE, 3) and the transformed pixel values
        // The first dimension is set to 1 to represent a batch size of 1, since this function is only transforming one image at a time
        // The second and third dimensions are set to IMAGE_SIZE, which is assumed to be a constant representing the desired input size of the deep learning model
        // The fourth dimension is set to 3 to represent the three color channels (red, green, and blue) of the input image
        return new Tensor(1, IMAGE_SIZE, IMAGE_SIZE, 3, transformedPixels);
    }


    public void SwitchFromQRtoYolo()
    {
        QRViewBool = !QRViewBool;

        if (QRViewBool)
        {
            switchButton.GetComponentInChildren<TMP_Text>().text = "Yolo";
            if (box != null)
                DestroyImmediate(box);
            detectionLocked = false;

            GetComponentInParent<ARTrackedImageManager>().enabled = true;
        }            
        else
        {
            switchButton.GetComponentInChildren<TMP_Text>().text = "QR";

            GetComponentInParent<ARTrackedImageManager>().enabled = false;
        }
            
    }



    #region QR Code
    public void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnImageChanged;
    }

    public void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnImageChanged;
    }

    public void OnImageChanged(ARTrackedImagesChangedEventArgs args)
    {
        if (QRViewBool)
        {
            foreach (ARTrackedImage trackedImage in args.added)
            {
                for (int h = 0; h < numClasses; h++)
                {
                    uiPanelPrefab[h].SetActive(false);
                }

                if (trackedImageDictionary.ContainsKey(trackedImage.referenceImage.name))
                    uiPanelPrefab[trackedImageDictionary[trackedImage.referenceImage.name]].SetActive(true);
            }

            foreach (ARTrackedImage trackedImage in args.updated)
            {
                // check if the image is no longer tracked
                if (trackedImage.trackingState == TrackingState.Limited)
                {
                    if (trackedImageDictionary.ContainsKey(trackedImage.referenceImage.name))
                        uiPanelPrefab[trackedImageDictionary[trackedImage.referenceImage.name]].SetActive(false);
                }
                else if (trackedImage.trackingState == TrackingState.Tracking)
                {
                    if (trackedImageDictionary.ContainsKey(trackedImage.referenceImage.name))
                        uiPanelPrefab[trackedImageDictionary[trackedImage.referenceImage.name]].SetActive(true);
                }
            }
        }
        
    }
    #endregion

    #region Place Object In AR Scene 
    /*
    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            // Instantiate the info panel prefab at the tracked image's position
            infoPanel = Instantiate(infoPanelPrefab, trackedImage.transform.position, Quaternion.identity);
            infoPanel.SetTrackedImage(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            // Update the info panel position and content for the updated tracked image
            infoPanel?.SetTrackedImage(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            // Destroy the info panel for the removed tracked image
            Destroy(infoPanel?.gameObject);
        }


    }

    private void OnTrackedImagesChanged2(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.updated)
        {
            // Check if this is the tracked image you're interested in
            if (trackedImage.referenceImage.name == "YourImageName")
            {
                // Check if you've already added this tracked image to your list
                var existingTrackedImage = trackedImages.Find(ti => ti.referenceImage == trackedImage.referenceImage);

                if (existingTrackedImage != null)
                {
                    // Update the existing tracked image's transform
                    existingTrackedImage.transform.position = trackedImage.transform.position;
                    existingTrackedImage.transform.rotation = trackedImage.transform.rotation;
                }
                else
                {
                    // Create a new ARTrackedImage game object and add it to the scene
                    var newTrackedImageGO = new GameObject("YourImageName");
                    var newTrackedImage = newTrackedImageGO.AddComponent<ARTrackedImage>();
                    newTrackedImage.referenceImage = trackedImage.referenceImage;
                    newTrackedImage.sessionRelativeData = trackedImage.sessionRelativeData;
                    newTrackedImage.transform.SetParent(sessionOrigin.trackablesParent);
                    newTrackedImage.transform.localPosition = Vector3.zero;
                    newTrackedImage.transform.localRotation = Quaternion.identity;

                    trackedImages.Add(newTrackedImage);

                    // Update your info panel with the new tracked image
                    infoPanel?.SetTrackedImage(newTrackedImage);
                }
            }
        }
    }


    private void OnTrackedImagesChanged3(ARTrackedImagesChangedEventArgs eventArgs)
    {
        Dictionary<string, ARTrackedImage> trackedImages = new Dictionary<string, ARTrackedImage>();
        foreach (var trackedImage in eventArgs.updated)
        {
            // Check if this is the tracked image you're interested in
            if (trackedImage.referenceImage.name == "YourImageName")
            {
                // Check if you've already added this tracked image to your dictionary
                if (trackedImages.TryGetValue(trackedImage.referenceImage.name, out ARTrackedImage existingTrackedImage))
                {
                    // Update the existing tracked image's transform
                    existingTrackedImage.transform.position = trackedImage.transform.position;
                    existingTrackedImage.transform.rotation = trackedImage.transform.rotation;
                }
                else
                {
                    // Create a new ARTrackedImage game object and add it to the scene
                    var newTrackedImageGO = new GameObject("YourImageName");
                    var newTrackedImage = newTrackedImageGO.AddComponent<ARTrackedImage>();
                    newTrackedImage.referenceImage = trackedImage.referenceImage;
                    newTrackedImage.sessionRelativeData = trackedImage.sessionRelativeData;
                    newTrackedImage.transform.SetParent(trackedImageManager.trackablesParent);
                    newTrackedImage.transform.localPosition = Vector3.zero;
                    newTrackedImage.transform.localRotation = Quaternion.identity;

                    trackedImages.Add(trackedImage.referenceImage.name, newTrackedImage);
                    //trackedImages.Add(trackedImage.trackableId, newTrackedImage);

                    // Update your info panel with the new tracked image
                    infoPanel?.SetTrackedImage(newTrackedImage);

                    var referenceImageField = typeof(ARTrackedImage).GetField("m_ReferenceImage", BindingFlags.NonPublic | BindingFlags.Instance);
                    referenceImageField.SetValue(newTrackedImage, trackedImage.referenceImage);
                }
            }            
        }        
    }

    void AddImage(Texture2D imageToAdd)
    {
        if (trackedImageManager.referenceLibrary is MutableRuntimeReferenceImageLibrary mutableLibrary)
        {
            mutableLibrary.ScheduleAddImageWithValidationJob(imageToAdd, "my new image", 0.5f /* 50 cm );
        }

    }
    */


    #endregion


    #region QuickSort ONNX Output Arrays For Optimization (Not Used Yet)
    /*
    public float[,,,] FindTopElements(float[,,,] array, int k)
    {
        // Sort the top k elements using quickselect
        for (int classIndex = 0; classIndex < numClasses; classIndex++)
        {
            Quickselect(array, 0, array.GetLength(3) - 1, k, classIndex);
        }        

        // Iterate through the top k elements to find values above the threshold
        float[,,,] topElements = new float[1,1,numClasses,k];
        int count = 0;
        for (int i = 0; i < k; i++) //for (int i = array.GetLength(3) - 1; i >= array.GetLength(3) - k; i--)
        {
            for (int j = 0; j < numClasses; j++)
            {
                if (array[0, 0, j, i] > 0.9f)
                {
                    topElements[0, 0, j, count] = array[0, 0, j, i];

                    if (j == (numClasses - 1))
                        count++;
                }
            }
            
        }

        return topElements;
    }

    public void Quickselect(float[,,,] array, int left, int right, int k, int classIndex)
    {
        if (left == right) return;

        int pivotIndex = (left + right) / 2;
        pivotIndex = Partition(array, left, right, pivotIndex, classIndex);

        if (k == pivotIndex) return;
        else if (k < pivotIndex) Quickselect(array, left, pivotIndex - 1, k, classIndex);
        else Quickselect(array, pivotIndex + 1, right, k, classIndex);
    }

    public int Partition(float[,,,] array, int left, int right, int pivotIndex, int classIndex)
    {
        float pivotValue = array[0,0, classIndex, pivotIndex];
        Swap(array, pivotIndex, right, classIndex);

        int storeIndex = left;
        for (int i = left; i < right; i++)
        {
            if (array[0,0, classIndex, i] > pivotValue)
            {
                Swap(array, i, storeIndex, classIndex);
                storeIndex++;
            }
        }

        Swap(array, storeIndex, right, classIndex);
        return storeIndex;
    }

    public void Swap(float[,,,] array, int index1, int index2, int classIndex)
    {
        /*
        float temp = array[index1];
        array[index1] = array[index2];
        array[index2] = temp;
        
        (array[0, 0, classIndex, index2], array[0, 0, classIndex, index1]) = (array[0, 0, classIndex, index1], array[0, 0, classIndex, index2]); //tuple to swap values, instead of the above comment
        

        for (int i = 0; i < boxes4D.GetLength(2); i++)
        {
            (boxes4D[0, 0, i, index2], boxes4D[0, 0, i, index1]) = (boxes4D[0, 0, i, index1], boxes4D[0, 0, classIndex, index2]); //tuple to swap values, instead of the above comment            
        }
        
    }
    
    */
    ///
    /*
    public static float[] FindTopElements(float[] array, int k)
    {
        // Sort the top k elements using quickselect
        Quickselect(array, 0, array.Length - 1, k);

        // Iterate through the top k elements to find values above the threshold
        float[] topElements = new float[k];
        int count = 0;
        for (int i = array.Length - 1; i >= array.Length - k; i--)
        {
            if (array[i] > 0.9f)
            {
                topElements[count] = array[i];
                count++;
            }
        }

        return topElements;
    }

    public static void Quickselect(float[] array, int left, int right, int k)
    {
        if (left == right) return;

        int pivotIndex = (left + right) / 2;
        pivotIndex = Partition(array, left, right, pivotIndex);

        if (k == pivotIndex) return;
        else if (k < pivotIndex) Quickselect(array, left, pivotIndex - 1, k);
        else Quickselect(array, pivotIndex + 1, right, k);
    }

    public static int Partition(float[] array, int left, int right, int pivotIndex)
    {
        float pivotValue = array[pivotIndex];
        Swap(array, pivotIndex, right);

        int storeIndex = left;
        for (int i = left; i < right; i++)
        {
            if (array[i] > pivotValue)
            {
                Swap(array, i, storeIndex);
                storeIndex++;
            }
        }

        Swap(array, storeIndex, right);
        return storeIndex;
    }

    public static void Swap(float[] array, int index1, int index2)
    {
        /*
        float temp = array[index1];
        array[index1] = array[index2];
        array[index2] = temp;
        
    (array[index2], array[index1]) = (array[index1], array[index2]); //tuple to swap values, instead of the above comment
    }
*/
    #endregion

}