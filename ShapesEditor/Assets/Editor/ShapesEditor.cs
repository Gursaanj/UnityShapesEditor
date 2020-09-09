using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

[CustomEditor(typeof(ShapeCreator))]
public class ShapesEditor : Editor
{
    private const float DiscRadius = 0.5f;
    private const float DottedLineThickness = 4.0f;
    private ShapeCreator _shapeCreator;
    private bool _needsRepaint = false;

    private void OnEnable()
    {
        _shapeCreator = target as ShapeCreator;
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
            Undo.RecordObject(_shapeCreator, "Add Point");
            _shapeCreator.Points.Add(mousePosition);
            _needsRepaint = true;
        }
    }

    private void Draw()
    {
        if (_shapeCreator.Points != null)
        {
            for (int i = 0, len = _shapeCreator.Points.Count; i < len; i++)
            {
                Vector3 currentPoint = _shapeCreator.Points[i];
                Vector3 nextPoint = _shapeCreator.Points[(i + 1) % len];
                Handles.color = Color.black;
                Handles.DrawDottedLine(currentPoint, nextPoint, DottedLineThickness);
                Handles.color = Color.white;;
                Handles.DrawSolidDisc(currentPoint, Vector3.up, DiscRadius);
            }

            _needsRepaint = false;
        }
    }
}
