using UnityEngine;

[CreateAssetMenu(fileName = "MyScriptableObject", menuName = "My Scriptable Objects/MyData")]
public class MyScriptableObject : ScriptableObject
{
    public int myInt;
    public float myFloat;
    public string myString;
}