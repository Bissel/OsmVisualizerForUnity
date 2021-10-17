using System.Collections;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Visualisation.Components
{
    public class Waterway : VisualizerComponentMaterials
    {
        protected override IEnumerator Create(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            MeshHelper mesh;
            foreach (var w in tile.WayAreas.Values)
            {
                switch (w)
                {
                    case NaturalWater water:
                        mesh = new MeshHelper();
                        water.Area.Fill(mesh, Vector3.up * Random.Range(-.005f, .005f));
                        creator.AddMesh(mesh, defaultMaterial);
                        break;
                    case Data.Waterway waterway:
                        mesh = new MeshHelper();
                        waterway.Flow.Fill(mesh, Vector3.up * Random.Range(-.005f, .005f));
                        creator.AddMesh(mesh, defaultMaterial);
                        break;
                    case Coastline coastline:
                        mesh = new MeshHelper();
                        coastline.Area.Fill(mesh, Vector3.up * Random.Range(-.005f, .005f));
                        creator.AddMesh(mesh, defaultMaterial);
                        break;
                    default:
                        continue;
                }
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
        }
    }
}