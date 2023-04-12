using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class ARObjectInfoPanel : MonoBehaviour
{
    [SerializeField] private ARSessionOrigin arSessionOrigin;
    [SerializeField] private RectTransform infoPanel;

    private ARRaycastManager raycastManager;
    private ARAnchor anchor;
    private ARTrackedImage trackedImage;
    private Vector3 screenPos;
    private List<float> depthValues;


    private void Awake()
    {
        raycastManager = arSessionOrigin.GetComponent<ARRaycastManager>();
        depthValues = new List<float>();
    }


    private void Update()
    {
        if (trackedImage == null)
        {
            return;
        }

        // Convert bounding box coordinates to screen coordinates
        screenPos = arSessionOrigin.camera.WorldToScreenPoint(trackedImage.transform.position);
        screenPos.z = RaycastAverageDepth();
        //screenPos.z = raycastManager.raycastAverageDepth;

        // Set panel position and content
        infoPanel.position = screenPos;
        infoPanel.GetComponentInChildren<TMP_Text>().text = trackedImage.referenceImage.name;
    }


    public void SetTrackedImage(ARTrackedImage trackedImage)
    {
        this.trackedImage = trackedImage;
        depthValues.Clear();
    }


    float RaycastAverageDepth()
    {
        // Get the position of the camera
        Vector3 cameraPosition = arSessionOrigin.camera.transform.position;

        // Get the position of the tracked image
        Vector3 imagePosition = trackedImage.transform.position;

        // Calculate the distance between the camera and the tracked image
        float distance = Vector3.Distance(cameraPosition, imagePosition);

        // Store the distance in an array or list to calculate the average depth
        depthValues.Add(distance);

        // Calculate the average depth of the tracked image
        float averageDepth = depthValues.Average();

        return averageDepth;
    }
}