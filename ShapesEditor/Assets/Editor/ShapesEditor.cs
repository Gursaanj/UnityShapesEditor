using UnityEditor;
using UnityEngine;

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
    private ShapeCreator _shapeCreator;
    private SelectionInfo _selectionInfo;
    private bool _needsRepaint = false;

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
        _shapeCreator = target as ShapeCreator;
        _selectionInfo = new SelectionInfo();
        Undo.undoRedoPerformed += OnUndoOrRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoOrRedo;
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
            
            if (_needsRepaint)
            {
                _needsRepaint = false;
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
        int newPointIndex = _selectionInfo.isMouseOverLine && isMouseOverSelectedShape ? _selectionInfo.lineIndex + 1 : SelectedShape.Points.Count;
        Undo.RecordObject(_shapeCreator, "Add Point");
        SelectedShape.Points.Insert(newPointIndex, mousePosition);
        _selectionInfo.pointIndex = newPointIndex;
        _selectionInfo.mouseOverShapeIndex = _selectionInfo.selectedShapeIndex;
        _needsRepaint = true;
        
        SelectPointUnderMouse();
    }

    private void SelectPointUnderMouse()
    {
        _selectionInfo.isPointSelected = true;
        _selectionInfo.isMouseOverPoint = true;
        _selectionInfo.isMouseOverLine = false;
        _selectionInfo.lineIndex = -1;

        if (_selectionInfo.pointIndex < SelectedShape.Points.Count)
        {
            _selectionInfo.positionAtStartOfDrag = SelectedShape.Points[_selectionInfo.pointIndex];
        }

        _needsRepaint = true;
    }

    private void SelectShapeUnderMouse()
    {
        if (_selectionInfo.mouseOverShapeIndex != -1)
        {
            _selectionInfo.selectedShapeIndex = _selectionInfo.mouseOverShapeIndex;
            _needsRepaint = true;
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
        CreateNewShape();
        CreateNewPoint(mousePosition);
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
            SelectedShape.Points[_selectionInfo.pointIndex] = _selectionInfo.positionAtStartOfDrag;
            Undo.RecordObject(_shapeCreator, "Move Point");
            SelectedShape.Points[_selectionInfo.pointIndex] = mousePosition; // so that Undo will revert to position of MouseDown
            _selectionInfo.isPointSelected = false;
            _selectionInfo.pointIndex = -1;
            _needsRepaint = true;
        }
    }

    private void HandleLeftMouseDrag(Vector3 mousePosition)
    {
        if (_selectionInfo.isPointSelected)
        {
            SelectedShape.Points[_selectionInfo.pointIndex] = mousePosition;
            _needsRepaint = true;
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
                for (int i = 0, len = shape.Points.Count; i < len; i++)
                {
                    if (Vector3.Distance(mousePosition, shape.Points[i]) < _shapeCreator.HandleRadius)
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

            _needsRepaint = true;
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
                    for (int i = 0, len = shape.Points.Count; i < len; i++)
                    {
                        Vector3 currentPointInShape = shape.Points[i];
                        Vector3 nextPointInShape = shape.Points[(i + 1) % len];
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
                _needsRepaint = true;
            }
        }
    }

    private void OnUndoOrRedo()
    {
        if (_shapeCreator != null && _shapeCreator.shapes != null && _selectionInfo.selectedShapeIndex >= _shapeCreator.shapes.Count)
        {
            _selectionInfo.selectedShapeIndex = _shapeCreator.shapes.Count - 1;
        }
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
                    for (int i = 0, len = shape.Points.Count; i < len; i++)
                    {
                        Vector3 currentPoint = shape.Points[i];
                        Vector3 nextPoint = shape.Points[(i + 1) % len];

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
            
            _needsRepaint = false;
        }
    }
}
