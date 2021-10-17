using OsmVisualizer.Data.Utils;
using UnityEditor;
using UnityEngine;

namespace OsmVisualizer.Gizmos
{
    
    [RequireComponent(typeof(Data.MapTile))]
    public class Intersection : MonoBehaviour
    {
        
        public bool OnSelect = true;
        public Color Color = Color.red;
        public Color CenterColor = Color.cyan;
        
        [Min(2)]
        public int MinRoadCount = 3;
        [Min(2)]
        public int MaxRoadCount = 100;
#if (UNITY_EDITOR)
        public void OnDrawGizmosSelected()
        {
            if(OnSelect)
                DrawGizmos();
        }
        
        public void OnDrawGizmos()
        {
            if(!OnSelect)
                DrawGizmos();
        }

        private void DrawGizmos()
        {
            var mt = gameObject.GetComponent<Data.MapTile>();
            var tileOffset = Vector3.zero; // gameObject.transform.position; // (mt.bounds.Min.inWorldCoords() - mt.sp.StartPosition.inWorldCoords()).ToVector3xz();
            
            foreach (var i in mt.Intersections.Values)
            {
                if(i == null || i.Radius > 100f)
                    continue;
                
                UnityEngine.Gizmos.color = Color;
                UnityEngine.Gizmos.DrawSphere(i.Center.ToVector3xz(), i.Radius);
                
                var inCnt = i.LanesIn.Count;
                var outCnt = i.LanesOut.Count;
                var totalLC = inCnt + outCnt;
                
                if(totalLC < MinRoadCount || totalLC > MaxRoadCount)
                    continue;
                
                // Debug.Log($"Intersection {i.Node} has {i.Points.Count} Points, {i.LanesIn} in, {i.LanesOut} out");

                var center = i.Center.ToVector3xz() + tileOffset;
                
                UnityEngine.Gizmos.color = CenterColor;
                UnityEngine.Gizmos.DrawSphere(center, .25f);
                
                for (var index = 0; index < i.Points.Count; index++)
                {
                    var a = i.Points[index].ToVector3xz() + tileOffset;
                    var b = i.Points[(index + 1) % i.Points.Count].ToVector3xz() + tileOffset;
                    
                    UnityEngine.Gizmos.color = CenterColor;
                    UnityEngine.Gizmos.DrawLine(a, center);
                    
                    UnityEngine.Gizmos.color = Color;
                    // UnityEngine.Gizmos.DrawSphere(a, .5f);
                    // UnityEngine.Gizmos.DrawSphere(b, .5f);
                    UnityEngine.Gizmos.DrawLine(a, b);
                    
                    
                    // Handles.Label(a + Vector3.up * .25f * index, "" + index);
                }

                for (var index = 0; index < totalLC; index++)
                {
                    var a = (
                        index < inCnt
                            ? i.LanesIn[index].GetPointLE1()
                            : i.LanesOut[index - inCnt].GetPointRS1()
                        ).ToVector3xz() + tileOffset;

                    var otherDir = i.GetNthLaneCollection(index).OtherDirection;
                    var otherDirIndex = -1;
                    if(otherDir != null)
                        otherDirIndex = i.IsNthLaneCollectionDirIn(index) ? i.LanesOut.IndexOf(otherDir) + i.LanesIn.Count : i.LanesIn.IndexOf(otherDir);
                    
                    Handles.Label(a + Vector3.up * .15f * index, index + " " + i.OrientationNumberByCount(index) + " " + otherDirIndex);
                    // Handles.Label(a + Vector3.up * .15f * index + Vector3.up, "" + i.Angles[index] + "°");
                }
            }
        }
        
        
#endif   
    }

}