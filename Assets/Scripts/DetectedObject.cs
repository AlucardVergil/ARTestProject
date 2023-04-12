using UnityEngine;

public class DetectedObject
{
    public int classIndex { get; set; }
    public float probability { get; set; }
    public Rect boundingBox { get; set; }

    public DetectedObject() { }

    public DetectedObject(int ClassIndex, float Probability, Rect BoundingBox)
    {
        classIndex = ClassIndex;
        probability = Probability;
        boundingBox = BoundingBox;
    }
}