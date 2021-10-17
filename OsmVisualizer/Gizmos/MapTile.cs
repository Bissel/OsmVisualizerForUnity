using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Gizmos
{
    
    [RequireComponent(typeof(Data.MapTile))]
    public class MapTile : MonoBehaviour
    {
        
        public bool OnSelect = true;
        public Color Color = Color.green;
        
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

            var center = mt.sp is SettingsProvider provider ? provider.startPosition.InWorldCoords() : Vector2.zero;
            var min = (mt.bounds.Min.InWorldCoords() - center).ToVector3xz();
            var max = (mt.bounds.Max.InWorldCoords() - center).ToVector3xz();

            UnityEngine.Gizmos.color = Color;
            UnityEngine.Gizmos.DrawLine(min, new Vector3(min.x, 0, max.z));
            UnityEngine.Gizmos.DrawLine(max, new Vector3(min.x, 0, max.z));
            UnityEngine.Gizmos.DrawLine(min, new Vector3(max.x, 0, min.z));
            UnityEngine.Gizmos.DrawLine(max, new Vector3(max.x, 0, min.z));
            
            
            center = mt.pos.ToVector2() * 500;
            min = (center - new Vector2(250, 250)).ToVector3xz();
            max = (center + new Vector2(250, 250)).ToVector3xz();
            UnityEngine.Gizmos.color = Color.red;
            UnityEngine.Gizmos.DrawLine(min, new Vector3(min.x, 0, max.z));
            UnityEngine.Gizmos.DrawLine(max, new Vector3(min.x, 0, max.z));
            UnityEngine.Gizmos.DrawLine(min, new Vector3(max.x, 0, min.z));
            UnityEngine.Gizmos.DrawLine(max, new Vector3(max.x, 0, min.z));
            
            UnityEngine.Gizmos.DrawSphere(center.ToVector3xz(), 5f);
        }
#endif
        
        
    }

}