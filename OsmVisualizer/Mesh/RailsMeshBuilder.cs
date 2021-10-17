using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Characteristics;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using OsmVisualizer.Math;
using UnityEngine;
using Spline = OsmVisualizer.Data.Types.Spline;

namespace OsmVisualizer.Mesh
{
    public class RailsMeshBuilder : MeshBuilder
    {
        protected readonly string[] types;
        protected readonly bool combinedMesh;
        
        #region Materials
        protected readonly Material matDefault;
        protected readonly Material matRail;
        protected readonly Material matRubber;
        protected readonly Material matCrosstie;
        protected readonly Material matDrivableSurface;
        #endregion Materials
        
        #region Heights and Widths

        // https://de.wikipedia.org/wiki/Schiene_(Schienenverkehr)#Standardprofile
        private const float RailHeight = .154f;
        private const float RailHeadWidth = .067f;
        private const float RailHeadHeight = .035f;
        private const float RailWebWidth = .016f;
        private const float RailFootWidth = .125f;
        private const float RailFootHeight = .01f;
        private const float BaseDepth = -RailHeight + .015f;
        private const float BaseOffset = .4f;
        #endregion Heights and Widths
        
        #region Shapes

        private readonly Shape _rail = new Shape(
            new []
            {
                new Vector3(0f, 0f, -RailFootWidth * .5f),
                new Vector3(0f, RailFootHeight, -RailFootWidth * .5f),
                
                new Vector3(0f, RailFootHeight, -RailFootWidth * .5f),
                new Vector3(0f, RailFootHeight * 2f, -RailWebWidth * .65f),
                new Vector3(0f, RailHeight * .5f, -RailWebWidth * .5f),
                new Vector3(0f, RailHeight - RailHeadHeight - .01f, -RailWebWidth * .65f),
                new Vector3(0f, RailHeight - RailHeadHeight, -RailHeadWidth * .5f),
                new Vector3(0f, RailHeight, -RailHeadWidth * .5f),
                
                new Vector3(0f, RailHeight, RailHeadWidth * .5f),
                new Vector3(0f, RailHeight - RailHeadHeight, RailHeadWidth * .5f),
                new Vector3(0f, RailHeight - RailHeadHeight - .01f, RailWebWidth * .65f),
                new Vector3(0f, RailHeight * .5f, RailWebWidth * .5f),        
                new Vector3(0f, RailFootHeight * 2f, RailWebWidth * .65f),
                new Vector3(0f, RailFootHeight, RailFootWidth * .5f),
                
                new Vector3(0f, RailFootHeight, RailFootWidth * .5f),
                new Vector3(0f, 0f, RailFootWidth * .5f)
            },
            new List<Vector3>
            {
                new Vector3(0, 0, -1),
                new Vector3(0, 0, -1),
                
                new Vector3(0, 1, 0),
                new Vector3(0, 1, -1).normalized,
                new Vector3(0, 0, -1),
                new Vector3(0, -1, -1).normalized,
                new Vector3(0, -1, -1).normalized,
                new Vector3(0, 1, -1).normalized,
                
                new Vector3(0, 1, 1).normalized,
                new Vector3(0, -1, 1).normalized,
                new Vector3(0, -1, 1).normalized,
                new Vector3(0, 0, 1),
                new Vector3(0, 1, 1).normalized,
                new Vector3(0, 1, 0),
                
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 1),
            },
            new List<float>
            {
                0f, 
                RailFootHeight,
                
                RailFootHeight + RailFootWidth * .5f, 
                RailFootHeight + RailFootWidth * .5f + RailHeight * .5f, 
                RailFootHeight + RailFootWidth * .5f + RailHeight - RailHeadHeight - .01f, 
                RailFootHeight + RailFootWidth * .5f + RailHeight - RailHeadHeight + (RailHeadWidth - RailWebWidth) * .5f,
                RailFootHeight + RailFootWidth * .5f + RailHeight + (RailHeadWidth - RailWebWidth) * .5f,
                RailFootHeight + RailFootWidth * .5f + RailHeight + (RailHeadWidth - RailWebWidth) * .5f + RailHeadHeight,
                
                RailFootHeight + RailFootWidth * .5f + RailHeight + (RailHeadWidth - RailWebWidth) * .5f + RailHeadHeight + RailHeadWidth,
                RailFootHeight + RailFootWidth * .5f + RailHeight + (RailHeadWidth - RailWebWidth) * .5f + RailHeadHeight + RailHeadWidth + RailHeadHeight,
                RailFootHeight + RailFootWidth * .5f + RailHeight + (RailHeadWidth - RailWebWidth) + RailHeadHeight + RailHeadWidth + RailHeadHeight,
                RailFootHeight + RailFootWidth * .5f + RailHeight + (RailHeadWidth - RailWebWidth) + RailHeadHeight + RailHeadWidth + RailHeight * .5f,
                RailFootHeight + RailFootWidth * .5f + RailHeight + (RailHeadWidth - RailWebWidth) + RailHeadHeight + RailHeadWidth + RailHeight,
                RailFootHeight + RailFootWidth + RailHeight + (RailHeadWidth - RailWebWidth) + RailHeadHeight + RailHeadWidth + RailHeight,
                
                RailFootHeight + RailFootWidth + RailHeight + (RailHeadWidth - RailWebWidth) + RailHeadHeight + RailHeadWidth + RailHeight,
                RailFootHeight + RailFootWidth + RailHeight + (RailHeadWidth - RailWebWidth) + RailHeadHeight + RailHeadWidth + RailHeight + RailFootHeight,
            },
            new List<bool>
            {
                true, false, 
                true, true, true, true, true,
                true, // head top surface
                true, true, true, true, true,
                false, true 
            }
        );
        #endregion Shapes

        public RailsMeshBuilder(AbstractSettingsProvider settings, string[] types,
            Material matDefault, 
            Material matRail, 
            Material matRubber,
            Material matCrosstie,
            Material matDrivableSurface,
            bool combinedMesh = false
        ) : base(settings)
        {
            this.types = types;
            this.matDefault = matDefault;
            this.matRail = matRail;
            this.matRubber = matRubber;
            this.matCrosstie = matCrosstie;
            this.matDrivableSurface = matDrivableSurface;
            this.combinedMesh = combinedMesh;
        }

        public class Creator : AbstractCreator
        {
            protected override string Name() => "Rail";
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
            creator.SetParent(tile, matDefault, combinedMesh);

            var done = new List<MapData.LaneId>();

            var i = 1;
            foreach (var lc in tile.LaneCollections.Values)
            {
                if( lc.Lanes.Length == 0 || !(lc.Characteristics is RailWayCharacteristics) )
                    continue;
                
                if(done.Contains(lc.Id))
                    continue;
                
                done.Add(lc.Id);
                if(lc.OtherDirection != null)
                    done.Add(lc.OtherDirection.Id);
                
                CreateForCollection(lc, creator);
                
                if (i++ % 500 == 0)
                    yield return null;
            }
            
            creator.CreateMesh();
            
            yield return null;
        }


        private void CreateForCollection(LaneCollection lc, Creator creator)
        {
            var characteristics = (RailWayCharacteristics) lc.Characteristics;

            if (characteristics.RailType == "platform" || characteristics.RailType.Contains("stop"))
            {
                // @todo platform
                return;
            }

            var heightOffsets = lc.OutlineRight.Select(p => p.y).ToList();
            
            var meshBase = new MeshHelper("Base");
            var meshRail = new MeshHelper("Rail");
            // var meshRubber = new MeshHelper("Rubber");
            // var meshCrosstie = new MeshHelper("Crosstie");
            
            foreach (var lane in lc.Lanes)
            {
                var spline = new Spline(lane.Points, heightOffsets);
                Generate(spline, characteristics.Gauge, meshBase, meshRail);
            }
            
            creator.AddMesh(meshBase, matDefault, lc.Elevation != null ? "red" : null);
            creator.AddMesh(meshRail, matRail);
            // creator.AddMesh(meshRubber, matRubber);
            // creator.AddMesh(meshCrosstie, matCrosstie);

            if (characteristics.IsForBus || characteristics.IsForPublicServiceVehicles)
            {
                var meshDrivableSurface = new MeshHelper("DrivableSurface");
                
                
                
                creator.AddMesh(meshDrivableSurface, matDrivableSurface);
            }
        }

        private void Generate(Spline spline, float gauge, MeshHelper meshBase, MeshHelper meshRail)
        {
            var baseWidth = gauge + (BaseOffset + RailFootWidth) * 2f;
            var shapeBase = new Shape(baseWidth, BaseDepth, .1f);
            
            spline.ExtrudeShape(meshBase, shapeBase, offset: new Vector3(0f, 0f, baseWidth * -.5f));

            spline.ExtrudeShape(meshRail, _rail, offset: new Vector3(0f, BaseDepth, (gauge + RailHeadWidth) * -.5f));
            spline.ExtrudeShape(meshRail, _rail, offset: new Vector3(0f, BaseDepth, (gauge + RailHeadWidth) * .5f));
            
        }
    }

}