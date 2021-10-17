using System.Collections;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Visualisation.Components
{
    public class Landuse : VisualizerComponentMaterials
    {
        protected override IEnumerator Create(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            
            foreach (var w in tile.WayAreas.Values)
            {
                if(!(w is Data.Landuse landuse)) continue;
                
                var mesh = new MeshHelper();
                landuse.Area.Fill(mesh, Vector3.up * Random.Range(-.005f, .005f));

                creator.AddColoredMesh(mesh, landuse.Characteristics);
                
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