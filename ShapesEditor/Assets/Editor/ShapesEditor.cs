using UnityEditor;
using UnityEngine;
using Sebastian.Geometry;

[CustomEditor(typeof(ShapeCreator))]
public class ShapesEditor : Editor
{
    public class SelectionInfo
    {
        public int selectedShapeIndex = -1;
        public int mouseOverShapeIndex = -1;
        
        public int pointIndex = -1;
        public bool isMouseOverPoint = false;
        public bool isPointSelected = false;
        public Vector3 positionAtStartOfDrag;
        
        public int lineIndex = -1;
        public bool isMouseOverLine = false;

    }
    
    private const float DottedLineThickness = 4.0f;
    private const float PropertySpacing = 20f;

    private const string _helpMessage = "Left click to add points.\nShift + Left Click on point to delete.\n Shift + Left Click on empty space to create new shape";
    private ShapeCreator _shapeCreator;
    private SelectionInfo _selectionInfo;
    private bool _hasShapeChangedSinceLastRepaint;

    private Shape SelectedShape
    {
        get
        {
            if (_shapeCreator != null && _shapeCreator.shapes != null && _selectionInfo.selectedShapeIndex < _shapeCreator.shapes.Count)
            {
                return _shapeCreator.shapes[_selectionInfo.selectedShapeIndex];
            }
            else
            {
                return  new Shape();
            }
        }
    }

    private void OnEnable()
    {
        _hasShapeChangedSinceLastRepaint = true;
        _shapeCreator = target as ShapeCreator;
        _selectionInfo = new SelectionInfo();
        Undo.undoRedoPerformed += OnUndoOrRedo;
        Tools.hidden = true;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoOrRedo;
        Tools.hidden = false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        int shapeDeleteIndex = -1;

        if (_shapeCreator != null && _shapeCreator.shapes != null)
        {
            GUILayout.Space(PropertySpacing);
            EditorGUILayout.HelpBox(_helpMessage, MessageType.Info);
            _shapeCreator.showShapesList = EditorGUILayout.Foldout(_shapeCreator.showShapesList, "Show List of Shapes");

            if (_shapeCreator.showShapesList)
            {
                for (int i = 0, len = _shapeCreator.shapes.Count; i < len; i++)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label($"Shape {i+1}");

                        GUI.enabled = i != _selectionInfo.selectedShapeIndex;
                    
                        if (GUILayout.Button("Select"))
                        {
                            _selectionInfo.selectedShapeIndex = i;
                        }

                        GUI.enabled = true;

                        if (GUILayout.Button("Delete"))
                        {
                            shapeDeleteIndex = i;
                        }  
                    }
                }
            }
            
            if (shapeDeleteIndex != -1)
            {
                Undo.RecordObject(_shapeCreator, "Delete Shape");
                _shapeCreator.shapes.RemoveAt(shapeDeleteIndex);
                _selectionInfo.selectedShapeIndex = Mathf.Clamp(_selectionInfo.selectedShapeIndex, 0, _shapeCreator.shapes.Count - 1);
            }

            if (GUI.changed)
            {
                _hasShapeChangedSinceLastRepaint = true;
                SceneView.RepaintAll();
            }
        }
    }

    private void OnSceneGUI()
    {
        Event guiEvent = Event.current;

        if (guiEvent.type == EventType.Repaint)
        {
            Draw();
        }
        else if (guiEvent.type == EventType.Layout)
        {
            //Will ensure that the selected gameobject will be picked in the hierarchy if nothing else is. which will be the
            //case when clicking things in the scene. Thus ensuring this object is always chosen
            // FocustType.Passive means it can be selected via Keyboard
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
        else
        {
            HandleInput(guiEvent);
            
            if (_hasShapeChangedSinceLastRepaint)
            {
                HandleUtility.Repaint();
            }
            
        }
    }

    private void CreateNewShape()
    {
        if (_shapeCreator != null && _shapeCreator.shapes != null)
        {
            Undo.RecordObject(_shapeCreator, "Create Shape");
            _shapeCreator.shapes.Add(new Shape());
            _selectionInfo.selectedShapeIndex = _shapeCreator.shapes.Count - 1;
        }
    }

    private void CreateNewPoint(Vector3 mousePosition)
    {
        bool isMouseOverSelectedShape = _selectionInfo.mouseOverShapeIndex == _selectionInfo.selectedShapeIndex;
        int newPointIndex = _selectionInfo.isMouseOverLine && isMouseOverSelectedShape ? _selectionInfo.lineIndex + 1 : SelectedShape.points.Count;
        Undo.RecordObject(_shapeCreator, "Add Point");
        SelectedShape.points.Insert(newPointIndex, mousePosition);
        _selectionInfo.pointIndex = newPointIndex;
        _selectionInfo.mouseOverShapeIndex = _selectionInfo.selectedShapeIndex;
        _hasShapeChangedSinceLastRepaint = true;
        
        SelectPointUnderMouse();
    }

    private void DeletePointUnderMouse()
    {
        Undo.RecordObject(_shapeCreator, "Delete Point");
        SelectedShape.points.RemoveAt(_selectionInfo.pointIndex);
        _selectionInfo.isPointSelected = false;
        _selectionInfo.isMouseOverPoint = false;
        _hasShapeChangedSinceLastRepaint = true;
    }

    private void SelectPointUnderMouse()
    {
        _selectionInfo.isPointSelected = true;
        _selectionInfo.isMouseOverPoint = true;
        _selectionInfo.isMouseOverLine = false;
        _selectionInfo.lineIndex = -1;


        _selectionInfo.positionAtStartOfDrag = SelectedShape.points[_selectionInfo.pointIndex];
        _hasShapeChangedSinceLastRepaint = true;
    }

    private void SelectShapeUnderMouse()
    {
        if (_selectionInfo.mouseOverShapeIndex != -1)
        {
            _selectionInfo.selectedShapeIndex = _selectionInfo.mouseOverShapeIndex;
            _hasShapeChangedSinceLastRepaint = true;
        }
    }

    private void HandleInput(Event guiEvent)
    {
        // Event.mousePosition : Screen Coordinates, not world coordinates - turn into world ray
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);

        // Origin of HandleDrawing (O) + direction of HandleDrawing(dir) x distance to DrawPoint (d) = Point where drawing takes place (P)
        // O + dir*d = P
        // O.y + dir.y(d) = Height of DrawPoint (h) ==>  d = (h - O.y)/(dir.y)
        // So O + dir(h - O.y)/(dir.y) = P

        float drawPlaneHeight = 0f; //h
        float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y; // d
        Vector3 mousePosition = mouseRay.GetPoint(dstToDrawPlane); // P

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Shift)
        {
            HandleShiftLeftMouseDown(mousePosition);
        }
        
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
        {
            HandleLeftMouseDown(mousePosition);
        }

        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
        {
            HandleLeftMouseUp(mousePosition);
        }

        if (guiEvent.type == EventType.MouseDrag && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
        {
            HandleLeftMouseDrag(mousePosition);
        }

        if (!_selectionInfo.isPointSelected)
        {
            UpdateMouseOverInfo(mousePosition);
        }
    }

    private void HandleShiftLeftMouseDown(Vector3 mousePosition)
    {
        if (_selectionInfo.isMouseOverPoint)
        {
            SelectShapeUnderMouse();
            DeletePointUnderMouse();
        }
        else
        {
            CreateNewShape();
            CreateNewPoint(mousePosition);
        }
    }

    // if mouse is over currently existing point, select that point. if not then add new point
    private void HandleLeftMouseDown(Vector3 mousePosition)
    {
        if (_shapeCreator != null && _shapeCreator.shapes != null && _shapeCreator.shapes.Count == 0)
        {
            CreateNewShape();
        }
        
        SelectShapeUnderMouse();

        if (_selectionInfo.isMouseOverPoint)
        {
            SelectPointUnderMouse();
        }
        else
        {
            CreateNewPoint(mousePosition);
        }
    }

    private void HandleLeftMouseUp(Vector3 mousePosition)
    {
        if (_selectionInfo.isPointSelected) 
        {
            SelectedShape.points[_selectionInfo.pointIndex] = _selectionInfo.positionAtStartOfDrag;
            Undo.RecordObject(_shapeCreator, "Move Point");
            SelectedShape.points[_selectionInfo.pointIndex] = mousePosition; // so that Undo will revert to position of MouseDown
            _selectionInfo.isPointSelected = false;
            _selectionInfo.pointIndex = -1;
            _hasShapeChangedSinceLastRepaint = true;
        }
    }

    private void HandleLeftMouseDrag(Vector3 mousePosition)
    {
        if (_selectionInfo.isPointSelected)
        {
            SelectedShape.points[_selectionInfo.pointIndex] = mousePosition;
            _hasShapeChangedSinceLastRepaint = true;
        }
    }



    private void UpdateMouseOverInfo(Vector3 mousePosition)
    {
        int mouseOverPointIndex = -1;
        int mouseOverShapeIndex = -1;

        if (_shapeCreator == null || _shapeCreator.shapes == null)
        {
            return;
        }

        for (int shapeIndex = 0, count = _shapeCreator.shapes.Count; shapeIndex < count; shapeIndex++)
        {
            Shape shape = _shapeCreator.shapes[shapeIndex];

            if (shape != null)
            {
                for (int i = 0, len = shape.points.Count; i < len; i++)
                {
                    if (Vector3.Distance(mousePosition, shape.points[i]) < _shapeCreator.HandleRadius)
                    {
                        mouseOverPointIndex = i;
                        mouseOverShapeIndex = shapeIndex;
                        break;
                    }
                }
            }
        }
        

        if (mouseOverPointIndex != _selectionInfo.pointIndex || mouseOverShapeIndex != _selectionInfo.mouseOverShapeIndex)
        {
            _selectionInfo.mouseOverShapeIndex = mouseOverShapeIndex;
            _selectionInfo.pointIndex = mouseOverPointIndex;
            _selectionInfo.isMouseOverPoint = mouseOverPointIndex != -1;

            _hasShapeChangedSinceLastRepaint = true;
        }

        if (_selectionInfo.isMouseOverPoint)
        {
            _selectionInfo.isMouseOverLine = false;
            _selectionInfo.lineIndex = -1;
        }
        else
        {
            int mouseOverLineIndex = -1;
            float closestLineDistance = _shapeCreator.HandleRadius;
            
            for (int shapeIndex = 0, count = _shapeCreator.shapes.Count; shapeIndex < count; shapeIndex++)
            {
                Shape shape = _shapeCreator.shapes[shapeIndex];

                if (shape != null)
                {
                    for (int i = 0, len = shape.points.Count; i < len; i++)
                    {
                        Vector3 currentPointInShape = shape.points[i];
                        Vector3 nextPointInShape = shape.points[(i + 1) % len];
                        float dstFromMouseToLine = HandleUtility.DistancePointToLineSegment(mousePosition.ToXZ(), currentPointInShape.ToXZ(), nextPointInShape.ToXZ());

                        if (dstFromMouseToLine < closestLineDistance)
                        {
                            closestLineDistance = dstFromMouseToLine;
                            mouseOverLineIndex = i;
                            mouseOverShapeIndex = shapeIndex;
                        }
                    }
                }
            }
            
            
            if (_selectionInfo.lineIndex != mouseOverLineIndex || mouseOverShapeIndex != _selectionInfo.mouseOverShapeIndex)
            {
                _selectionInfo.mouseOverShapeIndex = mouseOverShapeIndex;
                _selectionInfo.lineIndex = mouseOverLineIndex;
                _selectionInfo.isMouseOverLine = mouseOverLineIndex != -1;
                _hasShapeChangedSinceLastRepaint = true;
            }
        }
    }

    private void OnUndoOrRedo()
    {
        if (_shapeCreator != null && _shapeCreator.shapes != null && (_selectionInfo.selectedShapeIndex >= _shapeCreator.shapes.Count || _selectionInfo.selectedShapeIndex == -1))
        {
            _selectionInfo.selectedShapeIndex = _shapeCreator.shapes.Count - 1;
        }

        _hasShapeChangedSinceLastRepaint = true;
    }

    private void Draw()
    {
        if (_shapeCreator != null && _shapeCreator.shapes != null)
        {
            for (int shapeIndex = 0, count = _shapeCreator.shapes.Count; shapeIndex < count; shapeIndex++)
            {
                Shape shape = _shapeCreator.shapes[shapeIndex];
                bool isShapeSelected = shapeIndex == _selectionInfo.selectedShapeIndex;
                bool isMouseOverShape = shapeIndex == _selectionInfo.mouseOverShapeIndex;
                Color deselectedShapeColor = Color.grey;

                if (shape != null)
                {
                    for (int i = 0, len = shape.points.Count; i < len; i++)
                    {
                        Vector3 currentPoint = shape.points[i];
                        Vector3 nextPoint = shape.points[(i + 1) % len];

                        if (i == _selectionInfo.lineIndex && isMouseOverShape)
                        {
                            Handles.color = Color.red;
                            Handles.DrawLine(currentPoint, nextPoint);
                        }
                        else
                        {
                            Handles.color = isShapeSelected ? Color.black : deselectedShapeColor;
                            Handles.DrawDottedLine(currentPoint, nextPoint, DottedLineThickness);
                        }

                        if (i == _selectionInfo.pointIndex && isMouseOverShape)
                        {
                            Handles.color = _selectionInfo.isPointSelected ? Color.black : Color.red;
                        }
                        else
                        {
                            Handles.color = isShapeSelected ? Color.white : deselectedShapeColor;
                        }

                        Handles.DrawSolidDisc(currentPoint, Vector3.up, _shapeCreator.HandleRadius);
                    }
                }
            }
            
            // using this to check to see if the shape has changes since the last repaint call
            if (_hasShapeChangedSinceLastRepaint)
            {
                _shapeCreator.UpdateMeshDisplay();
                SceneView.RepaintAll();
            }

            _hasShapeChangedSinceLastRepaint = false;
        }
    }
}
