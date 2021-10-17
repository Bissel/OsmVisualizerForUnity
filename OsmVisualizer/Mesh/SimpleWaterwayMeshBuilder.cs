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
    public class SimpleWaterwayMeshBuilder : MeshBuilder
    {
        protected readonly string[] _types;
        protected readonly Material _surfaceMat;
        protected readonly bool _combinedMesh;
        protected readonly bool _addGizmos;

        private const float BaseOffset = -.1f;

        public SimpleWaterwayMeshBuilder(AbstractSettingsProvider settings, string[] types, Material surfaceMat, bool combinedMesh = false, bool addGizmos = true) : base(settings)
        {
            _types = types;
            _surfaceMat = surfaceMat;
            _combinedMesh = combinedMesh;
            _addGizmos = addGizmos;
        }
        
        public class Creator : AbstractCreator
        {
            protected override string Name() => "Water";
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
            creator.SetParent(tile, _surfaceMat, _combinedMesh);

            var i = 1;
            var mesh = new MeshHelper();
            
            foreach (var wayArea in tile.WayAreas.Values)
            {
                switch (wayArea)
                {
                    case NaturalWater water:
                        water.Area.Fill(mesh, Vector3.up * (BaseOffset + Random.Range(-.005f, .005f)));
                        break;
                    case Waterway waterway:
                        waterway.Flow.Fill(mesh, Vector3.up * (BaseOffset + Random.Range(-.005f, .005f)));
                        break;
                    case Coastline coastline:
                        coastline.Area.Fill(mesh, Vector3.up * (BaseOffset + Random.Range(-.005f, .005f)));
                        break;
                    default:
                        continue;
                }

                if (i++ % 500 == 0)
                    yield return null;
            }
            
            creator.AddMesh(mesh, _surfaceMat);
            creator.CreateMesh(false);
        }
    }

}