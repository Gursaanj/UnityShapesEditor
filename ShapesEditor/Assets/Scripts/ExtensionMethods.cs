using UnityEngine;

public static class ExtensionMethods 
{
    public static Vector2 ToXZ(this Vector3 positon)
    {
        return  new Vector2(positon.x, positon.z);
    }
}
