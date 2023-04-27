using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DatasetEvaluation : MonoBehaviour
{
    public bool validationDatasetReady = false;

    private ClassMetrics[] classMetrics;


    #region Read And Save Validation Dataset Texture And File Names

    private List<ImageData> imageDataList = new List<ImageData>(); //ImageData is a nested class inside this script

    private void Awake()
    {
        //intantiate the array with the number of class of the model
        classMetrics = new ClassMetrics[GetComponent<ObjectDetection>().numClasses];
        for (int i = 0; i < classMetrics.Length; i++)
        {
            classMetrics[i] = new ClassMetrics(0, 0, 0, 0);
        }

        string folderPath = Application.dataPath + "/ValidationDataset"; // Path to the folder containing the images
        string[] imagePaths = Directory.GetFiles(folderPath, "*.jpg"); // Change the extension if your images have a different format

        foreach (string imagePath in imagePaths)
        {
            // Read and process each image
            Texture2D imageTexture = LoadImage(imagePath);
            string txtPath = folderPath + "/" + Path.GetFileNameWithoutExtension(imagePath) + ".txt";
            ImageData imageData = new ImageData(txtPath, imageTexture);
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
        public string TxtPath { get; }
        public Texture2D Texture { get; }

        public ImageData(string txtPath, Texture2D texture)
        {
            TxtPath = txtPath;
            Texture = texture;
        }
    }

    public List<ImageData> GetValidationDataset()
    {
        return imageDataList;
    }
    #endregion



    #region Evaluate Validation Dataset

    //Nested class to save TP, FP, TN and FN for each class separately
    public class ClassMetrics
    {
        public int TP { get; set; }
        public int FP { get; set; }
        public int TN { get; set; }
        public int FN { get; set; }

        public ClassMetrics(int truePositive, int falsePositive, int trueNegative, int falseNegative)
        {
            TP = truePositive;
            FP = falsePositive;
            TN = trueNegative;
            FN = falseNegative;
        }
    }



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

        int classID = 0; //this is only to save classID because this line `classMetrics[classID].FN = groundTruth.Count;` is outside of foreach

        foreach (var prediction in predictions)
        {
            classID = prediction.classIndex;

            //convert bounding box from Rect to Tuple 
            Tuple<int, float, float, float, float> boundingBox = new Tuple<int, float, float, float, float>(
                prediction.classIndex, 
                prediction.boundingBox.x, 
                prediction.boundingBox.y, 
                prediction.boundingBox.width + prediction.boundingBox.x, 
                prediction.boundingBox.height + prediction.boundingBox.y);


            float bestIoU = 0;
            Tuple<int, float, float, float, float> bestMatch = null;

            foreach (var annotation in groundTruth)
            {
                // Check if the class ID matches
                if (annotation.Item1 == boundingBox.Item1)
                {
                    float iou = CalculateIoU(annotation, boundingBox);
                    if (iou > bestIoU)
                    {
                        bestIoU = iou;
                        bestMatch = annotation;
                    }
                }
            }


            if (bestMatch != null)
            {
                if (bestIoU >= 0.5)
                {
                    classMetrics[classID].TP++;
                    groundTruth.Remove(bestMatch);
                }
                else
                {
                    classMetrics[classID].FP++;
                }
            }
            else
            {
                classMetrics[classID].FP++;
            }
        }
        classMetrics[classID].FN = groundTruth.Count;
    }


    //Calculates Precision, Recall, F1_Score based on the TP,FP,FN that it gathered from the Evaluate method
    //The out float variables are to extract the variables easier like this:
    //float precision, recall, f1score;
    //"CalculateMetrics(out precision, out recall, out f1score);"
    public void CalculateMetricsByClass(int classIndex, out float precision, out float recall, out float f1Score)
    {
        precision = (float)classMetrics[classIndex].TP / (classMetrics[classIndex].TP + classMetrics[classIndex].FP);
        recall = (float)classMetrics[classIndex].TP / (classMetrics[classIndex].TP + classMetrics[classIndex].FN);
        f1Score = 2 * (precision * recall) / (precision + recall);

        Debug.Log("Class " + classIndex + ": Precision = " + precision.ToString("F3") + " Recall = " + recall.ToString("F3") + " F1_Score = " + f1Score.ToString("F3"));
    }


    //Calculate metrics for each class and then add them and divide them by the number of classes to get total metrics for the model
    public void CalculateMetricsTotal(int classCount, out float totalPrecision, out float totalRecall, out float totalF1score)
    {
        totalPrecision = 0;
        totalRecall = 0;
        totalF1score = 0;
        float precision, recall, f1Score;

        for (int i = 0; i < classCount; i++)
        {            
            CalculateMetricsByClass(i, out precision, out recall, out f1Score);
            totalPrecision += precision;
            totalRecall += recall;
            totalF1score += f1Score;
        }

        totalPrecision = totalPrecision / classCount;
        totalRecall = totalRecall / classCount;
        totalF1score = totalF1score / classCount;

        Debug.Log("Total: Precision = " + totalPrecision.ToString("F3") + " Recall = " + totalRecall.ToString("F3") + " F1_Score = " + totalF1score.ToString("F3"));
    }

    #endregion
}