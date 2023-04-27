using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DatasetEvaluation : MonoBehaviour
{
    private int truePositives = 0;
    private int falsePositives = 0;
    private int trueNegatives = 0;
    private int falseNegatives = 0;
    public bool validationDatasetReady = false;

    #region Read And Save Validation Dataset Texture And File Names
    private List<ImageData> imageDataList = new List<ImageData>();

    private void Awake()
    {
        string folderPath = Application.dataPath + "/ValidationDataset"; // Path to the folder containing the images
        string[] imagePaths = Directory.GetFiles(folderPath, "*.jpg"); // Change the extension if your images have a different format

        foreach (string imagePath in imagePaths)
        {
            // Read and process each image
            Texture2D imageTexture = LoadImage(imagePath);
            string fileName = Path.GetFileName(imagePath);
            ImageData imageData = new ImageData(fileName, imageTexture);
            imageDataList.Add(imageData);
        }
        validationDatasetReady = true;
    }

    private Texture2D LoadImage(string imagePath)
    {
        byte[] imageData = File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2); // Create a new texture
        texture.LoadImage(imageData); // Load the image data into the texture
        return texture;
    }

    //nested class
    public class ImageData
    {
        public string FileName { get; }
        public Texture2D Texture { get; }

        public ImageData(string fileName, Texture2D texture)
        {
            FileName = fileName;
            Texture = texture;
        }
    }

    public List<ImageData> GetValidationDataset()
    {
        return imageDataList;
    }
    #endregion



    #region Evaluate Validation Dataset
    // Function to parse the annotation text files
    private List<Tuple<int, float, float, float, float>> ParseAnnotationTxt(string txtFile)
    {
        string[] lines = File.ReadAllLines(txtFile);

        List<Tuple<int, float, float, float, float>> annotations = new List<Tuple<int, float, float, float, float>>();
        foreach (string line in lines)
        {
            string[] parts = line.Trim().Split(' ');
            int classId = int.Parse(parts[0]);
            float x1 = float.Parse(parts[1]);
            float y1 = float.Parse(parts[2]);
            float x2 = float.Parse(parts[3]);
            float y2 = float.Parse(parts[4]);

            Tuple<int, float, float, float, float> annotation = new Tuple<int, float, float, float, float>(classId, x1, y1, x2, y2);
            annotations.Add(annotation);
        }

        return annotations;
    }

    // Function to calculate IoU (Intersection over Union) between two bounding boxes
    private float CalculateIoU(Tuple<int, float, float, float, float> box1, Tuple<int, float, float, float, float> box2)
    {
        float x1 = Mathf.Max(box1.Item2, box2.Item2);
        float y1 = Mathf.Max(box1.Item3, box2.Item3);
        float x2 = Mathf.Min(box1.Item4, box2.Item4);
        float y2 = Mathf.Min(box1.Item5, box2.Item5);

        float intersection = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
        float areaBox1 = (box1.Item4 - box1.Item2) * (box1.Item5 - box1.Item3);
        float areaBox2 = (box2.Item4 - box2.Item2) * (box2.Item5 - box2.Item3);
        float union = areaBox1 + areaBox2 - intersection;

        return intersection / union;
    }

    // Function to evaluate mAP, precision, recall, TP, FP, TN, FN
    public void Evaluate(string txtFile, List<DetectedObject> predictions)
    {
        List<Tuple<int, float, float, float, float>> groundTruth = ParseAnnotationTxt(txtFile);
        // Parse your predictions from the corresponding predictions file (format depending on your implementation)

        foreach (var prediction in predictions)
        {
            //convert bounding box from Rect to Tuple 
            Tuple<int, float, float, float, float> boundingBox = 
                new Tuple<int, float, float, float, float>(prediction.classIndex, prediction.boundingBox.x, prediction.boundingBox.y, 
                prediction.boundingBox.width + prediction.boundingBox.x, prediction.boundingBox.height + prediction.boundingBox.y);


            float bestIoU = 0;
            Tuple<int, float, float, float, float> bestMatch = null;

            foreach (var annotation in groundTruth)
            {
                float iou = CalculateIoU(annotation, boundingBox);
                if (iou > bestIoU)
                {
                    bestIoU = iou;
                    bestMatch = annotation;
                }
            }

            if (bestMatch != null)
            {
                if (bestIoU >= 0.5)
                {
                    truePositives++;
                    groundTruth.Remove(bestMatch);
                }
                else
                {
                    falsePositives++;
                }
            }
            else
            {
                falsePositives++;
            }
        }

        falseNegatives = groundTruth.Count;
        // Calculate precision, recall, TN based on your requirements

        // Calculate mAP based on your requirements

        // Output
    }
    #endregion
}