using System.Collections;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Characteristics;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Visualisation.Components
{
    public class Buildings : VisualizerComponentMaterials
    {
        public bool showRoofs = true;
        public bool showFloors = true;
            
        
        protected override IEnumerator Create(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch)
        {
            yield return Visualizer.mode switch
            {
                Mode.MODE_2D => Create2D(tile, creator, stopwatch),
                Mode.MODE_3D => Create3D(tile, creator, stopwatch),
                _ => null
            };
        }

        private IEnumerator Create2D(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            
            foreach (var w in tile.WayAreas.Values)
            {
                if (w.WayType != WayInterpretation.Type.BUILDING) continue;
                
                var mh = new MeshHelper("Buildings");
                ((BuildingArea) w).Area.Fill(mh);
                mh.Vertices.ForEach(v => v.Set(v.x, 0, v.y));
                creator.AddMesh(mh, defaultMaterial);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
        }
        
        private IEnumerator Create3D(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            
            foreach (var w in tile.WayAreas.Values)
            {
                if (w.WayType != WayInterpretation.Type.BUILDING) continue;

                CreateBuildingAreaMesh((BuildingArea)w, creator);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
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
            
            if (height < .001f && heightMin < .001f)
                height = 5f;


            var topOffset = height > heightMin ? height : height + heightMin;
            
            CreateBuildingMeshWalls(characteristics, creator, area, topOffset, heightMin, randomOffset);
            
            if(showRoofs) CreateBuildingMeshRoof(characteristics, creator, area, topOffset, randomOffset);
            if(showFloors && heightMin > 0f) CreateBuildingMeshFloor(characteristics, creator, area, heightMin, randomOffset);
            
        }

        private static void CreateBuildingMeshWalls(BuildingCharacteristics characteristics, Creator creator, Area area, float topOffset, float bottomOffset, Vector3 offset)
        {
            var wallShape = Shape.WallLeft(topOffset - bottomOffset, bottomOffset);
            
            var mesh = new MeshHelper("Walls");
            area.ExtrudeShape(mesh, wallShape, offset: offset, cutCorners: true);
            
            creator.AddColoredMesh(mesh, characteristics);
        }

        private static void CreateBuildingMeshRoof(BuildingCharacteristics characteristics, Creator creator, Area area, float topOffset, Vector3 offset)
        {
            var mesh = new MeshHelper("Roof");
            area.Fill(mesh, topOffset * Vector3.up + offset);
            creator.AddColoredMesh(mesh, characteristics.Roof);
        }

        private static void CreateBuildingMeshFloor(BuildingCharacteristics characteristics, Creator creator, Area area, float bottomOffset, Vector3 offset)
        {
            var mesh = new MeshHelper("Floor");
            area.Fill(mesh, bottomOffset * Vector3.up + offset, true);
            creator.AddColoredMesh(mesh, characteristics);
        }

    }
}