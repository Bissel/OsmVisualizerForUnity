using System;
using System.Collections;
using System.Collections.Generic;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Mesh;
using UnityEngine;

namespace OsmVisualizer.Visualisation
{
    
    [RequireComponent(typeof(Visualizer))]
    public abstract class VisualizerComponent : MonoBehaviour
    {
        public LayerMask layer;

        public bool combineMeshes = true;
        public bool createCollider = false;

        public float yOffset = 0.0f;
        
        [Tooltip("If empty it uses the classname")]
        public string componentName;

        [HideInInspector]
        public string componentFullName;

        protected Visualizer Visualizer;

        protected class Creator : AbstractCreator
        {
            protected override string Name() => gameObject.name;

            private Dictionary<string, Material> _materials = null;

            public void Init(bool combineMesh)
            {
                Parent = gameObject;
                Mesh = new MultiMesh(null, combineMesh);
            }

            public void SetMaterials(Material defaultMaterial, Dictionary<string, Material> materials = null)
            {
                Mesh.DefaultMaterial = defaultMaterial;
                _materials = materials;
            }
            
            public void AddColoredMesh(MeshHelper mesh, MaterialCharacteristics c)
            {
                AddColoredMesh(mesh, c.Material, c.Color);
            }
            
            public void AddColoredMesh(MeshHelper mesh, string material, string color = null)
            {
                if (material == null || _materials == null || !_materials.TryGetValue(material, out var mat))
                    mat = Mesh.DefaultMaterial;
            
                AddMesh(mesh, mat, color);
            }

            public Transform GetTransform()
            {
                return Parent.transform;
            }
        }

        protected virtual void Start()
        {
            Visualizer = GetComponent<Visualizer>();
            componentName ??= GetType().Name;
            componentFullName = $"{Visualizer.mode} {componentName}";
        }

        protected virtual void OnValidate()
        {
            componentName ??= GetType().Name;
            if(layer.value == 0)
                layer = LayerMask.NameToLayer("Default");
        }

        protected virtual Creator GetNewCreator(MapTile tile)
        {
            var go = new GameObject(componentFullName);
            go.transform.parent = tile.transform;
            go.transform.position = new Vector3(0, yOffset + Visualizer.yOffset,0);
            go.layer = Visualizer.gameObject.layer;
            
            var creator = go.AddComponent<Creator>();
            creator.layer = layer;
            creator.Init(combineMeshes);
            return creator;
        }

        protected abstract IEnumerator Create(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch);

        public IEnumerator CreateComponent(MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var creator = GetNewCreator(tile);
            
            yield return Create(tile, creator, stopwatch);

            creator.CreateMesh(createCollider);
        }

        public IEnumerator Destroy(MapTile tile)
        {
            if (!tile.transform.Find(componentFullName).TryGetComponent<Creator>(out var creator)) yield break;
            
            creator.Destroy();
        }

    }
}