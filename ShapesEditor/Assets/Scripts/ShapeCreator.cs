using System.Collections.Generic;
using UnityEngine;

public class ShapeCreator : MonoBehaviour
{
    [HideInInspector] public List<Shape> shapes = new List<Shape>();

    [SerializeField] private float _handleRadius = 0.5f;

    public float HandleRadius => _handleRadius;
}


[System.Serializable]
public class Shape
{
    public List<Vector3> Points = new List<Vector3>();
}