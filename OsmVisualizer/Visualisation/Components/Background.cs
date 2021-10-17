using System.Collections;
using System.Collections.Generic;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Utils;
using Unity3d.PlaneTriangulator;
using UnityEngine;

namespace OsmVisualizer.Visualisation.Components
{
    public class Background : VisualizerComponentMaterials
    {
        private DelaunayTriangulationBuilder builder;

        [Range(2, 100)]
        public int stepping = 10;

        protected override void Start()
        {
            base.Start();
            builder = new DelaunayTriangulationBuilder();
        }
        
        protected override IEnumerator Create(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            var points = new List<Vector3>();
            
            var step = 1f / stepping;
            
            // fixes: first line of points get not connected
            points.Add(tile.Min.ToVector3xz() - new Vector3(.0005f,0,.0005f));
            for(var x = 0; x <= stepping; x++)
            {
                var xVal = Mathf.Lerp(tile.Min.x, tile.Max.x, step* x);
                for (var y = 0; y <= stepping; y++)
                {
                    var yVal = Mathf.Lerp(tile.Min.y, tile.Max.y, step * y);
                    points.Add(new Vector3(xVal, 0f, yVal));
                }
            }

            var pointsInner = new List<Vector3>();

            foreach (var inter in tile.Intersections.Values)
            {
                if(!(inter?.Elevation is Data.Tunnel))
                    continue;

                var tunnelInnerPoints = RemoveTunnelRamp(inter);
                
                points.RemoveAll(p => p.InsidePolygon(tunnelInnerPoints));
                
                pointsInner.AddRange(tunnelInnerPoints);
            }

            var outerPointsCount = points.Count;
            points.AddRange(pointsInner);

            var mesh = new MeshHelper();
            
            var points2d = new List<Vector2>();
            var innerPointsIndices = new List<int>();
            
            for (var i = 0; i < points.Count; i++)
            {
                var p3 = points[i];
                var p2 = p3.ToVector2xz();
                points[i] = p2.ToVector3xz();
                points2d.Add(p2);
                mesh.Normals.Add(Vector3.up);
                
                if(i < outerPointsCount)
                    continue;
                
                if(pointsInner.Contains(p3))
                    innerPointsIndices.Add(i);
            }

            mesh.Vertices.AddRange(points);
            mesh.UV.AddRange(points2d);
            mesh.Triangles.AddRange(builder.Build(points2d, new[] {innerPointsIndices}));
            
            creator.AddMesh(mesh, defaultMaterial);

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime)
                yield break;
                
            yield return null;
        }

        private static List<Vector3> RemoveTunnelRamp(Intersection inter)
        {
            var points = new List<Vector3>();
            var roadMappings = inter.GetRoadMapping();
            foreach (var rm in roadMappings)
            {
                if(rm.Lc == null)
                    continue;
                
                if(rm.Lc.Elevation != null)
                {
                    if (rm.IsIn)
                    {
                        var r = rm.Lc.GetOutlineRightPoints(true);
                        points.Add(r[r.Count - 1]);
                        if (rm.Lc.OtherDirection == null)
                        {
                            var l = rm.Lc.GetOutlineLeftPoints(true);
                            points.Add(l[l.Count - 1]);
                        }
                    }
                    else
                    {
                        points.Add(rm.Lc.GetOutlineRightPoints(true)[0]);
                        if (rm.Lc.OtherDirection == null)
                        {
                            points.Add(rm.Lc.GetOutlineLeftPoints(true)[0]);
                        }
                    }

                    continue;
                }
                
                if(rm.IsIn)
                    points.Add(rm.Lc.GetOutlineLeftPoints(true)[0]);
                
                points.AddRange(rm.Lc.GetOutlineRightPoints(true));
                
                if(rm.Lc.OtherDirection != null)
                    continue;
                
                points.AddRange(rm.Lc.GetOutlineLeftPoints(true));
            }

            return points;
        }
    }
}