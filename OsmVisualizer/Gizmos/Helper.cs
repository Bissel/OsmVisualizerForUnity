
using UnityEngine;

namespace OsmVisualizer.Gizmos
{
    public static class Helper
    {

        public static void DrawTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            UnityEngine.Gizmos.DrawLine(v0, v1);
            UnityEngine.Gizmos.DrawLine(v1, v2);
            UnityEngine.Gizmos.DrawLine(v2, v0);
        }
    }
}