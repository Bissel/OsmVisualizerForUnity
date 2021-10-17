using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace OsmVisualizer.Data
{

    public class MultiMesh
    {
        private readonly List<MeshHelper> _meshes = new List<MeshHelper>();
        private readonly Dictionary<Material, List<int>> _materials = new Dictionary<Material, List<int>>();

        public Material DefaultMaterial;
        private readonly bool _combinedMesh;
        
        public MultiMesh(Material defaultMat, bool combinedMesh = true)
        {
            DefaultMaterial = defaultMat;
            _combinedMesh = combinedMesh;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mesh">Gets added if mesh has size is greater then 0</param>
        /// <param name="mat">Uses defaultMat if mat is null</param>
        public void Add(MeshHelper mesh, Material mat = null)
        {
            if (mesh.Vertices.Count == 0)
                return;
            
            if (mat == null)
                mat = DefaultMaterial;
            
            _meshes.Add(mesh);
            
            if (!_materials.ContainsKey(mat))
                _materials.Add(mat, new List<int>());
            
            _materials[mat].Add(_meshes.Count - 1);
        }

        public void AddToGameObject(Transform parent, string name, float upOffset = 0f, bool createCollider = true, bool castShadow = false, string goTag = null, bool colliderIsTrigger = false)
        {
            if (_combinedMesh)
            {
                AddToGameObjectSingleMesh(parent, name, upOffset, createCollider, castShadow, goTag, colliderIsTrigger);
            }
            else
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.layer = parent.gameObject.layer;
                AddToGameObjectMultiMesh(go.transform, upOffset, createCollider, castShadow, goTag, colliderIsTrigger);
            }
        }

        private void AddToGameObjectSingleMesh(Transform parent, string name, float upOffset, bool createCollider, bool castShadow, string goTag, bool colliderIsTrigger)
        {
            var chunk = 0;
            _materials.Keys.ChunkBy(8).ForEach(matChunk =>
            {
                var go = new GameObject(name + (chunk > 0 ? "_" + chunk : ""));
                chunk++;

                go.transform.SetParent(parent, false);
                go.transform.Translate(Vector3.up * upOffset);
                go.layer = parent.gameObject.layer;
                if(goTag != null) go.tag = goTag;

                var meshIndex = 0;
                var triangleOffset = 0;

                var vertices = new List<Vector3>();
                var normals = new List<Vector3>();
                var uvs = new List<Vector2>();
                var materials = new Material[matChunk.Count];
                var triangles = new List<int>[matChunk.Count];

                foreach (var material in matChunk)
                {
                    materials[meshIndex] = material;
                    triangles[meshIndex] = new List<int>();

                    foreach (var mIndex in _materials[material])
                    {
                        var m = _meshes[mIndex];
                        triangles[meshIndex].AddRange(m.Triangles.Select(i => i + triangleOffset));
                        triangleOffset += m.Vertices.Count;

                        vertices.AddRange(m.Vertices);
                        normals.AddRange(m.Normals);
                        uvs.AddRange(m.UV);
                    }

                    meshIndex++;
                }

                var mesh = new UnityEngine.Mesh
                {
                    vertices = vertices.ToArray(),
                    normals  = normals.ToArray(),
                    uv       = uvs.ToArray(),
                    subMeshCount = triangles.Length
                };

                for (var i = 0; i < triangles.Length; i++)
                {
                    mesh.SetTriangles(triangles[i], i);
                }

                var meshFilter = go.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                var renderer = go.AddComponent<MeshRenderer>();
                renderer.materials = materials;
                renderer.shadowCastingMode = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;

                if (!createCollider) 
                    return;
                
                var collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                
                if (!colliderIsTrigger) return;
                collider.convex = true;
                collider.isTrigger = true;
            });
        }

        private void AddToGameObjectMultiMesh(Transform parent, float upOffset, bool createCollider, bool castShadow, string goTag, bool colliderIsTrigger)
        {
            for (var i = 0; i < _meshes.Count; i++)
            {
                var mesh = _meshes[i];
                var mat = DefaultMaterial;
                foreach (var kv in _materials)
                {
                    if(!kv.Value.Contains(i))
                        continue;

                    mat = kv.Key;
                    break;
                }

                var go = mesh.CreateMeshGameObject(parent, mesh.Name ?? "", mat, upOffset, createCollider, castShadow, colliderIsTrigger);
                if(goTag != null) go.tag = goTag;
            }
        }
        
    }
    
    public class MeshHelper
    {

        public readonly string Name;

        public MeshHelper(string name = null)
        {
            Name = name;
        }
        
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<int> Triangles = new List<int>();
        public readonly List<Vector2> UV = new List<Vector2>();


        public UnityEngine.Mesh GenerateMesh()
        {
            return new UnityEngine.Mesh
            {
                vertices = Vertices.ToArray(),
                triangles = Triangles.ToArray(),
                normals = Normals.ToArray(),
                uv = UV.ToArray()
            };
        }

        public GameObject CreateMeshGameObject(Transform parent, string name, Material mat, float upOffset = 0f, bool createCollider = true, bool castShadow = false, bool colliderIsTrigger = false)
        {
            var go = new GameObject(name);

            go.transform.SetParent(parent, false);
            go.transform.Translate(Vector3.up * upOffset);
            go.layer = parent.gameObject.layer;

            AddMeshToGameObject(go, mat, createCollider, castShadow, colliderIsTrigger);
            
            return go;
        }

        public void AddMeshToGameObject(GameObject go, Material mat, bool createCollider = true, bool castShadow = false, bool colliderIsTrigger = false)
        {
            var mesh = GenerateMesh();

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.material = mat;
            renderer.shadowCastingMode = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;

            if (!createCollider) return;

            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;

            if (!colliderIsTrigger) return;

            if (mesh.triangles.Length < 3 || mesh.triangles.Length > 255)
            {
                Debug.LogWarning("Convex Collider are only allowed up to 255 Triangles");
                return;
            }

            collider.convex = true;
            collider.isTrigger = true;
        }

        public void AddTriangle(int a, int b, int c, int vertexOffset = 0 )
        {
            Triangles.Add(vertexOffset + a);
            Triangles.Add(vertexOffset + b);
            Triangles.Add(vertexOffset + c);
        }

        public void AddQuad(int firstIndex)
        {
            Triangles.Add(firstIndex);
            Triangles.Add(firstIndex + 3);
            Triangles.Add(firstIndex + 1);
            
            Triangles.Add(firstIndex);
            Triangles.Add(firstIndex + 2);
            Triangles.Add(firstIndex + 3);
        }

        public void AddTwoPoints(Vector3 a, Vector3 b, float v)
        {
            Vertices.Add( a );
            Vertices.Add( b );
            
            Normals.Add(Vector3.up);
            Normals.Add(Vector3.up);
            
            UV.Add(new Vector2(0, v));
            UV.Add(new Vector2(1, v));
        }
        
        public void AddTwoPoints(Vector3 center, Vector3 rotatedUnit, float offset, float v)
        {
            Vertices.Add( center + rotatedUnit * offset );
            Vertices.Add( center - rotatedUnit * offset );
            
            Normals.Add(Vector3.up);
            Normals.Add(Vector3.up);
            
            UV.Add(new Vector2(-offset, v));
            UV.Add(new Vector2(offset, v));
        }
    }
}
