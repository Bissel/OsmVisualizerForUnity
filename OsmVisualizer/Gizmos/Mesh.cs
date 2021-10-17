using UnityEditor;
using UnityEngine;

namespace OsmVisualizer.Gizmos
{
    
    [RequireComponent(typeof(MeshFilter))]
    public class Mesh : MonoBehaviour
    {
        
        public bool OnSelect = true;
        
        public bool ApplyParentTransform = false;

        public Color[] Colors = {Color.red, Color.green, Color.blue, Color.magenta, Color.yellow, Color.cyan};

#if (UNITY_EDITOR)
        public void OnDrawGizmosSelected()
        {
            if (Selection.activeGameObject != transform.gameObject)
                return;
            
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
            var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            var offset = ApplyParentTransform ? gameObject.transform.parent.position : Vector3.zero;

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var normals = mesh.normals;

            for (var i = 0; i < vertices.Length; i++)
            {
                UnityEngine.Gizmos.color = Colors[i % Colors.Length];
                Handles.Label(vertices[i] + offset,  "" + i);

                var v = vertices[i] + offset;
                
                UnityEngine.Gizmos.DrawLine(v, v + normals[i]);
            }

            for (var i = 0; i < triangles.Length / 3; i ++)
            {
                UnityEngine.Gizmos.color = Colors[i % Colors.Length];
                var index = i * 3;
                
                Helper.DrawTriangle(
                    vertices[triangles[index]] + offset, 
                    vertices[triangles[index + 1]] + offset, 
                    vertices[triangles[index + 2]] + offset
                );
            }
        }
#endif
        
    }

}