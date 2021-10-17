using System;
using System.Collections.Generic;
using OsmVisualizer.Data;
using UnityEngine;

namespace OsmVisualizer.Visualisation
{

    [Serializable]
    public class MaterialMapping
    {
        public string name;
        public Material mat;
    }
    
    public abstract class VisualizerComponentMaterials : VisualizerComponent
    {
        public Material defaultMaterial;
        
        public MaterialMapping[] materials = new MaterialMapping[0];

        protected readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        protected override void Start()
        {
            base.Start();
            foreach (var m in materials) 
                Materials.Add(m.name, m.mat);
        }

        protected override Creator GetNewCreator(MapTile tile)
        {
            var creator = base.GetNewCreator(tile);
            creator.SetMaterials(defaultMaterial, Materials);
            return creator;
        }
    }
}