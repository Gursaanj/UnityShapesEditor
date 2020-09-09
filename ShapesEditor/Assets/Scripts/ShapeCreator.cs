using System.Collections.Generic;
using UnityEngine;
using Sebastian.Geometry;

public class ShapeCreator : MonoBehaviour
{
    [HideInInspector] public List<Shape> shapes = new List<Shape>();
    [HideInInspector] public bool showShapesList = true;

    public MeshFilter meshFilter;
    [SerializeField] private float _handleRadius = 0.5f;

    public float HandleRadius => _handleRadius;

    public void UpdateMeshDisplay()
    {
        CompositeShape compositeShape = new CompositeShape(shapes);
        meshFilter.mesh = compositeShape.GetMesh();
    }
}