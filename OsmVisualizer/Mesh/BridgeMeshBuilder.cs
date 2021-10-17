using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using OsmVisualizer.Math;
using UnityEngine;

namespace OsmVisualizer.Mesh
{
    public class BridgeMeshBuilder : MeshBuilder
    {
        protected readonly Material DefaultSurfaceMat;
        protected readonly bool CombinedMesh;
        protected readonly float Thickness;
        
        private readonly Shape _wallShape;

        public BridgeMeshBuilder(
            AbstractSettingsProvider settings, Material defaultSurfaceMat, bool combinedMesh = false,
            float thickness = .5f
        ) : base(settings)
        {
            DefaultSurfaceMat = defaultSurfaceMat;
            CombinedMesh = combinedMesh;
            Thickness = thickness;
            _wallShape = Shape.WallLeft(Thickness, -Thickness);
        }

        public override IEnumerator Destroy(MapData data, MapTile tile)
        {
            yield return null;
        }

        public override IEnumerator Create(MapData data, MapTile tile)
        {
            var i = 0;
            var creator = new MultiMesh(DefaultSurfaceMat, CombinedMesh);
            
            foreach (var wayArea in tile.WayAreas.Values)
            {
                if(wayArea == null || wayArea.GetType() != typeof(Bridge))
                    continue;

                var bridge = (Bridge)wayArea;
                
                CreateBridgeMesh(bridge, creator);
                
                bridge.Ramps.ForEach( ramp => CreateBridgeRamp(bridge.Id, ramp, creator));
                
                if ( (i++) % 500 == 0)
                    yield return null;
            }

            var go = new GameObject();
            go.transform.parent = tile.transform;
            creator.AddToGameObject(go.transform, "Bridge", castShadow: true);
        }

        private void CreateBridgeMesh(Bridge bridge, MultiMesh creator)
        {
            var meshT = new MeshHelper(bridge.Id + " Top");
            var meshW = new MeshHelper(bridge.Id + " Walls");
            var meshB = new MeshHelper(bridge.Id + " Bottom");

            bridge.Area.ExtrudeShape(meshW, _wallShape, cutCorners: true);
            
            creator.Add(meshW);

            bridge.Area.Fill(meshT);
            bridge.Area.Fill(meshB, new Vector3(0f, -Thickness, 0f), true);
            
            creator.Add(meshT);
            creator.Add(meshB);
        }

        private void CreateBridgeRamp(string bridgeId, Area area, MultiMesh creator)
        {
            var meshT = new MeshHelper(bridgeId + " Ramp Top");
            var meshW = new MeshHelper(bridgeId + " Ramp Walls");
            var meshB = new MeshHelper(bridgeId + " Ramp Bottom");

            area.ExtrudeShape(meshW, _wallShape, cutCorners: true);
            
            creator.Add(meshW);

            area.Fill(meshT);
            area.Fill(meshB, new Vector3(0f, -Thickness, 0f), true);
            
            creator.Add(meshT);
            creator.Add(meshB);
        }

    }

}