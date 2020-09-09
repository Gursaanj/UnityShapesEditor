using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

[CustomEditor(typeof(ShapeCreator))]
public class ShapesEditor : Editor
{
    public class SelectionInfo
    {
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

    private void OnEnable()
    {
        _shapeCreator = target as ShapeCreator;
        _selectionInfo = new SelectionInfo();
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

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
        {
            HandleLeftMouseDown(mousePosition);
        }

        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
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
    
    // if mouse is over currently existing point, select that point. if not then add new point
    private void HandleLeftMouseDown(Vector3 mousePosition)
    {
        if (!_selectionInfo.isMouseOverPoint)
        {
            int newPointIndex = _selectionInfo.isMouseOverLine ? _selectionInfo.lineIndex + 1 : _shapeCreator.Points.Count;
            Undo.RecordObject(_shapeCreator, "Add Point");
            _shapeCreator.Points.Insert(newPointIndex, mousePosition);
            _selectionInfo.pointIndex = newPointIndex;
        }

        _selectionInfo.isPointSelected = true;
        _selectionInfo.positionAtStartOfDrag = mousePosition;
        _needsRepaint = true;
    }

    private void HandleLeftMouseUp(Vector3 mousePosition)
    {
        if (_selectionInfo.isPointSelected)
        {
            _shapeCreator.Points[_selectionInfo.pointIndex] = _selectionInfo.positionAtStartOfDrag;
            Undo.RecordObject(_shapeCreator, "Move Point");
            _shapeCreator.Points[_selectionInfo.pointIndex] = mousePosition; // so that Undo will revert to position of MouseDown
            _selectionInfo.isPointSelected = false;
            _selectionInfo.pointIndex = -1;
            _needsRepaint = true;
        }
    }

    private void HandleLeftMouseDrag(Vector3 mousePosition)
    {
        if (_selectionInfo.isPointSelected)
        {
            _shapeCreator.Points[_selectionInfo.pointIndex] = mousePosition;
            _needsRepaint = true;
        }
    }



    private void UpdateMouseOverInfo(Vector3 mousePosition)
    {
        int mouseOverPointIndex = -1;

        if (_shapeCreator == null || _shapeCreator.Points == null)
        {
            return;
        }

        for (int i = 0, len = _shapeCreator.Points.Count; i < len; i++)
        {
            if (Vector3.Distance(mousePosition, _shapeCreator.Points[i]) < _shapeCreator.HandleRadius)
            {
                mouseOverPointIndex = i;
                break;
            }
        }

        if (mouseOverPointIndex != _selectionInfo.pointIndex)
        {
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
            for (int i = 0, len = _shapeCreator.Points.Count; i < len; i++)
            {
                Vector3 currentPointInShape = _shapeCreator.Points[i];
                Vector3 nextPointInShape = _shapeCreator.Points[(i + 1) % len];
                float dstFromMouseToLine = HandleUtility.DistancePointToLineSegment(mousePosition.ToXZ(), currentPointInShape.ToXZ(), nextPointInShape.ToXZ());

                if (dstFromMouseToLine < closestLineDistance)
                {
                    closestLineDistance = dstFromMouseToLine;
                    mouseOverLineIndex = i;
                }
            }

            if (_selectionInfo.lineIndex != mouseOverLineIndex)
            {
                _selectionInfo.lineIndex = mouseOverLineIndex;
                _selectionInfo.isMouseOverLine = mouseOverLineIndex != -1;
                _needsRepaint = true;
            }
        }
    }

    private void Draw()
    {
        if (_shapeCreator != null && _shapeCreator.Points != null)
        {
            for (int i = 0, len = _shapeCreator.Points.Count; i < len; i++)
            {
                Vector3 currentPoint = _shapeCreator.Points[i];
                Vector3 nextPoint = _shapeCreator.Points[(i + 1) % len];

                if (i == _selectionInfo.lineIndex)
                {
                    Handles.color = Color.red;
                    Handles.DrawLine(currentPoint, nextPoint);
                }
                else
                {
                    Handles.color = Color.black;
                    Handles.DrawDottedLine(currentPoint, nextPoint, DottedLineThickness);
                }
                
                Handles.color = i == _selectionInfo.pointIndex ? _selectionInfo.isPointSelected ? Color.black : Color.red : Color.white;
                Handles.DrawSolidDisc(currentPoint, Vector3.up, _shapeCreator.HandleRadius);
            }

            _needsRepaint = false;
        }
    }
}
