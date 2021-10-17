using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Characteristics;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using OsmVisualizer.Math;
using UnityEngine;

namespace OsmVisualizer.Mesh
{
    public class SimpleLanduseMeshBuilder : MeshBuilder
    {
        protected readonly string[] _types;
        protected readonly Dictionary<string, Material> _surfaceMats;
        protected readonly Material _defaultSurfaceMat;
        protected readonly bool _combinedMesh;
        protected readonly bool _addGizmos;

        private float BaseOffset = -.01f;

        public SimpleLanduseMeshBuilder(AbstractSettingsProvider settings, string[] types, Dictionary<string, Material> surfaceMats, Material defaultSurfaceMat, bool combinedMesh = false, bool addGizmos = true) : base(settings)
        {
            _types = types;
            _surfaceMats = surfaceMats;
            _defaultSurfaceMat = defaultSurfaceMat;
            _combinedMesh = combinedMesh;
            _addGizmos = addGizmos;
        }
        
        public class Creator : AbstractCreator
        {
            protected override string Name() => "Landuse";
        }

        private void CreateColoredMesh(Creator creator, MeshHelper mesh, LandCharacteristics c)
        {
            if (c.Material == null || !_surfaceMats.TryGetValue(c.Material, out var mat))
                mat = _defaultSurfaceMat;
            
            creator.AddMesh(mesh, mat);
        }
        
        public override IEnumerator Destroy(MapData data, MapTile tile)
        {
            if (tile.gameObject.TryGetComponent<Creator>(out var creator))
            {
                creator.Destroy();
            }
            yield return null;
        }
        
        public override IEnumerator Create(MapData data, MapTile tile)
        {
            var creator = tile.gameObject.AddComponent<Creator>();
            creator.SetParent(tile, _defaultSurfaceMat, _combinedMesh);

            var i = 1;
            foreach (var wayArea in tile.WayAreas.Values)
            {
                if(!(wayArea is Landuse landuse))
                    continue;
                
                if(_types != null && !_types.Contains(landuse.Characteristics.Material))
                    continue;

                var mesh = new MeshHelper();
                landuse.Area.Fill(mesh, Vector3.up * (BaseOffset + Random.Range(-.005f, .005f)));

                CreateColoredMesh(creator, mesh, landuse.Characteristics);

                if (i++ % 500 == 0)
                    yield return null;
            }
            
            creator.CreateMesh(false);
        }
        
    }

}