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
    public class SimpleBuildingMeshBuilder : MeshBuilder
    {
        protected readonly string[] Types;
        protected readonly Dictionary<string, Material> SurfaceMats;
        protected readonly Material DefaultSurfaceMat;
        protected readonly bool CombinedMesh;

        protected readonly bool Roof;
        protected readonly bool Floor;

        public SimpleBuildingMeshBuilder(
            AbstractSettingsProvider settings, 
            string[] types, 
            Dictionary<string, Material> surfaceMats, 
            Material defaultSurfaceMat, 
            bool roof, 
            bool floor, 
            bool combinedMesh = false
        ) : base(settings)
        {
            Types = types;
            SurfaceMats = surfaceMats;
            DefaultSurfaceMat = defaultSurfaceMat;
            CombinedMesh = false; // combinedMesh;
            Roof = roof;
            Floor = floor;
        }

        public class Creator : AbstractCreator
        {
            protected override string Name() => "Buildings";
        }

        private void CreateColoredMesh(Creator creator, MeshHelper mesh, MaterialCharacteristics c)
        {
            if (c.Material == null || !SurfaceMats.TryGetValue(c.Material, out var mat))
                mat = DefaultSurfaceMat;
            
            creator.AddMesh(mesh, mat, c.Color);
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
            creator.SetParent(tile, DefaultSurfaceMat, CombinedMesh);

            var i = 1;
            
            foreach (var wayArea in tile.WayAreas.Values)
            {
                if(wayArea.WayType != WayInterpretation.Type.BUILDING 
                   || !(wayArea is BuildingArea buildingArea))
                    continue;

                if(Types != null && !Types.Contains(buildingArea.Characteristics.Type))
                    continue;

                CreateBuildingAreaMesh(buildingArea, creator);

                if ( i++ % 500 == 0)
                    yield return null;
            }
            
            creator.CreateMesh(false);
        }

        private void CreateBuildingAreaMesh(BuildingArea buildingArea, Creator creator)
        {
            var height = buildingArea.Characteristics.GetHeight();
            var heightMin = buildingArea.Characteristics.GetHeightMin();

            var isRoof = buildingArea.Characteristics.Type == "roof";
            if (isRoof && heightMin < .1f)
            {
                heightMin = height;
                height += .5f;
            }
            
            if (buildingArea.Parts.Count == 0)
            {
                CreateBuildingMesh(buildingArea.Characteristics, buildingArea.Area, height, heightMin, creator, buildingArea.Id);
                return;
            }
            
            foreach (var part in buildingArea.Parts)
            {
                var partHeight = part.Characteristics.GetHeight(height);
                var partHeightMin = part.Characteristics.GetHeightMin();
                
                if (isRoof || part.Characteristics.Type == "roof")
                {
                    partHeightMin = partHeight;
                    partHeight += .5f;
                }

                // use part as roof
                if (part.PartType == "yes" && part.Characteristics.Roof is {Lines: true})
                {
                    partHeightMin = part.Characteristics.GetHeightMin(height);
                    if (!float.IsNaN(part.Characteristics.Roof.Height))
                    {
                        partHeight = part.Characteristics.Roof.Height;
                        partHeightMin = part.Characteristics.GetHeight(partHeight);
                    }
                    else
                    {
                        partHeight = part.Characteristics.GetHeight(.5f);
                    }
                }
                
                CreateBuildingMesh(part.Characteristics, part.Area, partHeight, partHeightMin, creator);
            }

            if (buildingArea.Characteristics.IsFootprint || buildingArea.Characteristics.HeightMin > 1f)
            {
                CreateBuildingMesh(buildingArea.Characteristics, buildingArea.Area, height, heightMin, creator);
            }
                

        }

        private void CreateBuildingMesh(BuildingCharacteristics characteristics, Area area, float height, float heightMin, Creator creator, string test = null)
        {
            // Reduce z-fighting issues
            var randomOffset = new Vector3(
                (Random.value -.500f) * .0025f,    
                 Random.value * -.0025f,    
                (Random.value -.500f) * .0025f    
            );
            
            if (test == "219167608")
            {
                Debug.Log($"building test {height} {heightMin}");
            }
            
            if (height < .001f && heightMin < .001f)
                height = 5f;


            var topOffset = height > heightMin ? height : height + heightMin;
            
            CreateBuildingMeshWalls(characteristics, creator, area, topOffset, heightMin, randomOffset);
            
            if(Roof) CreateBuildingMeshRoof(characteristics, creator, area, topOffset, randomOffset);
            if(Floor && heightMin > 0f) CreateBuildingMeshFloor(characteristics, creator, area, heightMin, randomOffset);
            
        }

        private void CreateBuildingMeshWalls(BuildingCharacteristics characteristics, Creator creator, Area area, float topOffset, float bottomOffset, Vector3 offset)
        {
            var wallShape = Shape.WallLeft(topOffset - bottomOffset, bottomOffset);
            
            var mesh = new MeshHelper("Walls");
            area.ExtrudeShape(mesh, wallShape, offset: offset, cutCorners: true);
            CreateColoredMesh(creator, mesh, characteristics);
        }

        private void CreateBuildingMeshRoof(BuildingCharacteristics characteristics, Creator creator, Area area, float topOffset, Vector3 offset)
        {
            var mesh = new MeshHelper("Roof");
            area.Fill(mesh, topOffset * Vector3.up + offset);
            CreateColoredMesh(creator, mesh, characteristics.Roof);
        }

        private void CreateBuildingMeshFloor(BuildingCharacteristics characteristics, Creator creator, Area area, float bottomOffset, Vector3 offset)
        {
            var mesh = new MeshHelper("Floor");
            area.Fill(mesh, bottomOffset * Vector3.up + offset, true);
            CreateColoredMesh(creator, mesh, characteristics);
        }
        
    }

}