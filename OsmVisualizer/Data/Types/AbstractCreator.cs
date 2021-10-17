using System.Collections.Generic;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Data.Types
{
    public abstract class AbstractCreator : MonoBehaviour
    {
        public LayerMask layer;
        public string goTag = null;

        public bool colliderIsTrigger = false;

        protected GameObject Parent;
        protected MultiMesh Mesh;

        protected abstract string Name();

        private class ColoredMat
        {
            private readonly string _color;
            private readonly Material _mat;

            public ColoredMat(string color, Material baseMat)
            {
                _color = color;
                _mat = baseMat;
            }

            public override int GetHashCode()
            {
                return $"{_color}{_mat.GetHashCode()}".GetHashCode();
            }
        }

        private readonly Dictionary<ColoredMat, Material> _coloredMaterials = new Dictionary<ColoredMat, Material>();

        public void SetParent(MapTile tile, Material defaultMaterial, bool combinedMesh)
        {
            Parent = new GameObject(Name());
            Parent.transform.parent = tile.transform;
            Mesh = new MultiMesh(defaultMaterial, combinedMesh);
        }

        public void Destroy()
        {
            Destroy(Parent);
            Destroy(this);
        }

        public void AddMesh(MeshHelper mesh, Material mat, string color = null)
        {
            if (color != null)
            {
                var colorKey = new ColoredMat(color, mat);
                if (!_coloredMaterials.ContainsKey(colorKey))
                    _coloredMaterials[colorKey] = new Material(mat) {color = color.ToColor()};

                mat = _coloredMaterials[colorKey];
            }

            Mesh.Add(mesh, mat);
        }

        public void CreateMesh(bool createCollider = true)
        {
            var layerIndex = 0;
            for(int i = 0; i < 32; i++)
            {
                if ((layer.value & (1 << i)) == 0)
                    continue;

                layerIndex = i;
                break;
            }
            
            Parent.layer = layerIndex;
            // if(layerName != null)
            //     Parent.layer = LayerMask.NameToLayer(layerName);
            
            Mesh.AddToGameObject(Parent.transform, Name(), createCollider: createCollider, goTag: goTag, colliderIsTrigger: colliderIsTrigger);
        }
    }
}