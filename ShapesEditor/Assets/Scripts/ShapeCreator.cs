using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeCreator : MonoBehaviour
{
    [HideInInspector]
    public List<Vector3> Points = new List<Vector3>();

    [SerializeField] private float _handleRadius = 0.5f;

    public float HandleRadius => _handleRadius;
}
