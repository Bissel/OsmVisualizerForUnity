using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using UnityEngine;
using Spline = OsmVisualizer.Data.Types.Spline;

namespace OsmVisualizer.Visualisation.Components
{
    public class Tunnel : VisualizerComponentMaterials
    {
        public float wallThickness = .24f;

        public Material lightsMaterial;

        protected override IEnumerator Create(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            
            foreach (var wayArea in tile.WayAreas.Values)
            {
                if (!(wayArea is Data.Tunnel tunnel))
                    continue;
                
                CreateTunnelMesh(tunnel, creator);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }

            foreach (var inter in tile.Intersections.Values)
            {
                if(inter == null || !(inter.Elevation is Data.Tunnel tunnel))
                    continue;
                
                CreateRampWalls(inter, tunnel, creator);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
        }
        
        private void CreateTunnelMesh(Data.Tunnel tunnel, Creator creator)
        {
            var mesh = new MeshHelper(tunnel.Id);

            var tunnelWall = Shape.TunnelWallRight(tunnel.InnerHeight, wallThickness);
            
            var left = tunnel.OutlineLeftIsReversed ? tunnel.OutlineLeft : tunnel.OutlineLeft.Reverse();
            var right = tunnel.OutlineRightIsReversed ? tunnel.OutlineRight.Reverse() : tunnel.OutlineRight;
            
            left.ExtrudeShape(
                mesh,
                tunnelWall,
                offset: Vector3.forward * tunnel.OffsetLeft
            );
            
            right.ExtrudeShape(
                mesh,
                tunnelWall,
                offset: Vector3.forward * tunnel.OffsetRight
            );
            
            var width = Vector3.Distance(left.Get(-1), right[0]);
            var offsetLeft = tunnel.OffsetLeft + width;
            var offsetRight = tunnel.OffsetRight + 0;
            
            right.ExtrudeShape(
                mesh,
                Shape.TunnelCeiling(tunnel.InnerHeight, offsetLeft, offsetRight, wallThickness)
            );
            
            Shape.TunnelCap(mesh, width, tunnel.InnerHeight, wallThickness,
                tunnel.OffsetRight, tunnel.OffsetLeft,
                (left.Get(-1) + right[0]) * .5f,
                -right.Forward[0]
            );

            var meshTunnelCap = new MeshHelper();
            Shape.TunnelCap(meshTunnelCap, width, tunnel.InnerHeight, wallThickness,
                tunnel.OffsetLeft, tunnel.OffsetRight,
                Vector3.zero, 
                left.Forward[0]
            );

            var rotate = Quaternion.AngleAxis(-180, Vector3.up);
            var offset = (left[0] + right.Get(-1)) * .5f;
            for (var i = 0; i < meshTunnelCap.Vertices.Count; i++)
            {
                var p = meshTunnelCap.Vertices[i];
                meshTunnelCap.Vertices[i] = (rotate * p) + offset;
            }
            
            creator.AddMesh(mesh, defaultMaterial);
            creator.AddMesh(meshTunnelCap, defaultMaterial);
            
            CreateTunnelLights(right, tunnel, width, creator);
        }

        private void CreateRampWalls(Intersection inter, Data.Tunnel tunnel, Creator creator)
        {
            // if (!inter.ElevationRamp)
            // {
            //     // @todo add ceiling + ceiling lights
            //     return;
            // }
            
            const float maxHeight = 1f; 

            var mesh = new MeshHelper(tunnel.Id + " Ramp");

            // var isLastOut = inter.IsNthLaneCollectionDirIn(inter.OutlinePoints.Count/2 - 1);

            var wallR = Shape.WallRight(-tunnel.BaseHeight - yOffset + maxHeight);
            wallR.Offset(Vector3.forward * (wallThickness - .1f));
            var wall = Shape.WallLeft(-tunnel.BaseHeight - yOffset + maxHeight)
                    .Combine(wallR, Vector3.up, 0f, wallThickness)
                    .Combine(Shape.WidthCentered(1f));

            
            var ceilingWallR = Shape.WallRight(maxHeight);
            ceilingWallR.Offset(Vector3.forward * (wallThickness - .1f));
            var ceilingWall = Shape.WallLeft(maxHeight + .1f, -.1f)
                .Combine(ceilingWallR, Vector3.up, 0f, wallThickness)
                .Combine(new Shape(
                    new []{new Vector3(0, 0, wallThickness - .1f), new Vector3(0, -.1f, 0)}, 
                    new List<Vector3>{new Vector3(0, -1, .15f).normalized, new Vector3(0, -1, .15f).normalized},
                    new List<float>{0,(wallThickness - .1f) },
                    new List<bool>{true}
                    ));
            
            var roadMappings = inter.GetRoadMapping();
            
            if (roadMappings.Count == 0)
                return;

            var reverse = roadMappings.ToArray().Reverse().ToList();
            var last = reverse[0];
            var lastPoints = new List<Vector2>();
            var draw = false;

            foreach (var m in reverse)
            {
                if (m.Lc == null)
                {
                    lastPoints.Add(m.Point);
                    draw = true;
                    continue;
                }

                last = m;
                break;
            }

            lastPoints.Reverse();

            foreach (var m in roadMappings)
            {
                if (draw && m.Lc != null)
                {
                    draw = false;
                    
                    var hasSidewalk = m.Lc.Characteristics.SidewalkRight != null || last.Lc.Characteristics.SidewalkRight != null;
                    var hasCycleway = m.Lc.Characteristics.CyclewayRight != null || last.Lc.Characteristics.CyclewayRight != null;
                    
                    for (var i = 0; i < lastPoints.Count - 1; i++)
                    {
                        var spline = new Spline(new[] {lastPoints[i], lastPoints[i + 1]}, inter.HeightOffset);
                        GenerateWall(spline, mesh, wall, hasSidewalk, hasCycleway, maxHeight: maxHeight);
                    }

                    {
                        var spline = new Spline(new[] {lastPoints[lastPoints.Count - 1], m.Point}, inter.HeightOffset);
                        GenerateWall(spline, mesh, wall, hasSidewalk, hasCycleway, maxHeight: maxHeight);
                    }
                }

                if (m.Lc == null)
                {
                    draw = true;
                    lastPoints.Add(m.Point);
                }
                else
                {
                    if(lastPoints.Count > 0) lastPoints = new List<Vector2>();
                    last = m;
                }
            }

            foreach (var lc in inter.LanesIn)
            {
                if (lc.Elevation != null)
                {
                    if(lc.Elevation is Data.Tunnel lcTunnel)
                        GenerateCeilingWall(lc, lcTunnel, mesh, ceilingWall, false);
                    
                    continue;
                }

                {
                    var hasSidewalk = !lc.Characteristics.SidewalkIsSeparate && lc.Characteristics.SidewalkRight != null;
                    var hasCycleway = !lc.Characteristics.CyclewayIsShared && lc.Characteristics.CyclewayRight != null;

                    GenerateWall(lc.OutlineRight, mesh, wall, hasSidewalk, hasCycleway, maxHeight: maxHeight);
                }
                
                if(lc.OtherDirection != null)
                    continue;
                
                {
                    var hasSidewalk = !lc.Characteristics.SidewalkIsSeparate && lc.Characteristics.SidewalkLeft != null;
                    var hasCycleway = !lc.Characteristics.CyclewayIsShared && lc.Characteristics.CyclewayLeft != null;

                    GenerateWall(lc.OutlineLeft.Reverse(), mesh, wall, hasSidewalk, hasCycleway, maxHeight: maxHeight);
                }
            }
            
            foreach (var lc in inter.LanesOut)
            {
                if (lc.Elevation != null)
                {
                    if(lc.Elevation is Data.Tunnel lcTunnel)
                        GenerateCeilingWall(lc, lcTunnel, mesh, ceilingWall, true);
                    
                    continue;
                }
                
                {
                    var hasSidewalk = !lc.Characteristics.SidewalkIsSeparate && lc.Characteristics.SidewalkRight != null;
                    var hasCycleway = !lc.Characteristics.CyclewayIsShared && lc.Characteristics.CyclewayRight != null;

                    GenerateWall(lc.OutlineRight, mesh, wall, hasSidewalk, hasCycleway, maxHeight: maxHeight);
                }
                
                if(lc.OtherDirection != null)
                    continue;
                
                {
                    var hasSidewalk = !lc.Characteristics.SidewalkIsSeparate && lc.Characteristics.SidewalkLeft != null;
                    var hasCycleway = !lc.Characteristics.CyclewayIsShared && lc.Characteristics.CyclewayLeft != null;

                    GenerateWall(lc.OutlineLeft.Reverse(), mesh, wall, hasSidewalk, hasCycleway, maxHeight: maxHeight);
                }
            }

            creator.AddMesh(mesh, defaultMaterial);
        }

        private void GenerateCeilingWall(LaneCollection lc, Data.Tunnel tunnel, MeshHelper mesh, Shape ceilingWall, bool start)
        {
            // new Spline(new []{lc.GetPointRE0().ToVector3xz(), lc.GetPointLE0().ToVector3xz()}), 
            // mesh, wall, false, false, maxHeight: maxHeight

            Vector2 v0, v1;
            
            if (start)
            {
                v0 = lc.GetPointRS0();
                v1 = lc.GetPointLS0();
            }
            else
            {
                v0 = lc.GetPointRE0();
                v1 = lc.GetPointLE0();
            }
            

            var dir = (v1 - v0).normalized;
                
            v0 -= (tunnel.OffsetRight + .1f) * dir;
            if(lc.OtherDirection == null)
                v1 += (tunnel.OffsetLeft + .1f) * dir;

            new Spline(start ? new []{v1, v0} : new[] {v0, v1}).ExtrudeShape(mesh, ceilingWall);
        }

        private void GenerateWall(Spline spline, MeshHelper mesh, Shape wall, bool hasSidewalk, bool hasCycleway,  
            float chamferAngleStart = float.NaN, float chamferAngleEnd = float.NaN, float maxHeight = float.NaN)
        {
            var offset = Vector3.forward * 
             ( Sidewalks.GuttersWidth + Sidewalks.SidewalkHeight
                 + (hasCycleway ? Sidewalks.CyclewayDividerWidth + Sidewalks.CyclewayWidth : 0f)
                 + (hasCycleway && hasSidewalk ? Sidewalks.CyclewayDividerWidth : 0f)
                 + (hasSidewalk ? Sidewalks.SidewalkWidth : 0f));
            
            spline.ExtrudeShape(mesh, wall, chamferAngleStart, chamferAngleEnd, offset, maxHeight: maxHeight);
            
            var height = Mathf.Abs(wall[0].y - wall[1].y);
            var normal = -spline.Forward[0];
            var rotation = Quaternion.FromToRotation(Vector3.right, normal);

            IntersectionWallCap(mesh, wall, spline[0] + rotation * offset, rotation, height, normal, maxHeight);
            
            normal = -spline.Forward[spline.Forward.Count - 1];
            rotation = Quaternion.FromToRotation(Vector3.right, normal);
            IntersectionWallCap(mesh, wall, spline[spline.Count - 1] + rotation * offset, rotation, height, normal, maxHeight, true);
        }

        private void IntersectionWallCap(MeshHelper mesh, Shape wall, Vector3 v0, Quaternion rotation, float height,
            Vector3 normal, float maxHeight = float.NaN, bool invert = false)
        {
            Vector3 p0, p1, p2, p3;
            if (invert)
            {
                p0 = wall[5];
                p1 = wall[0];
                p2 = wall[4];
                p3 = wall[1];
                normal = -normal;
            }
            else
            {
                p0 = wall[0];
                p1 = wall[5];
                p2 = wall[1];
                p3 = wall[4];
            }
            
            int vertOffset = mesh.Vertices.Count;;
            mesh.Vertices.Add((v0 + rotation * p0).ClipYGreater(maxHeight));
            mesh.Vertices.Add((v0 + rotation * p1).ClipYGreater(maxHeight));
            mesh.Vertices.Add((v0 + rotation * p2).ClipYGreater(maxHeight));
            mesh.Vertices.Add((v0 + rotation * p3).ClipYGreater(maxHeight));

            mesh.UV.Add(new Vector2(0, 0));
            mesh.UV.Add(new Vector2(0, wallThickness - .1f));
            mesh.UV.Add(new Vector2(height, 0));
            mesh.UV.Add(new Vector2(height, wallThickness - .1f));

            mesh.AddTriangle(0, 2, 3, vertOffset);
            mesh.AddTriangle(0, 3, 1, vertOffset);

            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
        }

        private void CreateTunnelLights(Spline right, Data.Tunnel tunnel, float width, Creator creator)
        {
            var meshLights = new MeshHelper(tunnel.Id + " Lights");
            
            Spline rightLights;
            {
                var list = right.ToList();
                list[0] = right[0] + right.Forward[0] * .5f;
                list[list.Count - 1] = right.Get(-1) - right.Forward[right.Count - 2] * .5f;
                rightLights = new Spline(list);
            }
            
            rightLights.ExtrudeShape(
                meshLights,
                Shape.TunnelCeilingLights(tunnel.InnerHeight, tunnel.OffsetLeft + width, tunnel.OffsetRight)
            );
            
            creator.AddMesh(meshLights, lightsMaterial);
        }
    }
}