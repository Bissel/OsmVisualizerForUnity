using System.Collections;
using System.Collections.Generic;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Visualisation.Components
{
    public class Bridge : VisualizerComponentMaterials
    {
        private Shape _wallShape;

        public float thickness = .5f;

        protected override void Start()
        {
            base.Start();
            _wallShape = Shape.WallLeft(thickness, -thickness);
        }
        
        protected override IEnumerator Create(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;

            var intersections = new List<long>();
            
            foreach (var w in tile.WayAreas.Values)
            {
                if(!(w is Data.Bridge bridge))
                    continue;
                
                CreateBridgeMesh(bridge, creator);
                
                // bridge.Ramps.ForEach( ramp => CreateBridgeRamp(bridge.Id, ramp, creator));
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }

            foreach (var lc in tile.LaneCollections.Values)
            {
                if(lc.Elevation != null)
                    continue;
                        
                var prevRamp = lc.PrevIntersection?.ElevationRamp ?? false;
                var nextRamp = lc.NextIntersection?.ElevationRamp ?? false;
                
                if(!prevRamp && !nextRamp)
                    continue;
                
                CreateBridgeRampLane(lc, creator);
                
                if(nextRamp && !intersections.Contains(lc.NextIntersection.Node))
                {
                    CreateBridgeMeshIntersection(lc.NextIntersection, creator);
                    intersections.Add(lc.NextIntersection.Node);
                }
                if(prevRamp && !intersections.Contains(lc.PrevIntersection.Node))
                {
                    CreateBridgeMeshIntersection(lc.PrevIntersection, creator);
                    intersections.Add(lc.PrevIntersection.Node);
                }
            }
        }

        private void CreateBridgeMeshIntersection(Intersection inter, Creator creator)
        {
            var mesh = new MeshHelper(inter.Node + " BridgeIntersection");
            
            inter.Area.Reverse().ExtrudeShape(mesh, _wallShape, cutCorners: true);
            inter.Area.Fill(mesh, new Vector3(0f, -thickness, 0f), true);
            
            creator.AddMesh(mesh, defaultMaterial);
        }
        
        private void CreateBridgeMesh(Data.Bridge bridge, Creator creator)
        {
            var meshT = new MeshHelper(bridge.Id + " Top");
            var meshW = new MeshHelper(bridge.Id + " Walls");
            var meshB = new MeshHelper(bridge.Id + " Bottom");

            bridge.Area.ExtrudeShape(meshW, _wallShape, cutCorners: true);
            
            creator.AddMesh(meshW, defaultMaterial);

            bridge.Area.Fill(meshT);
            bridge.Area.Fill(meshB, new Vector3(0f, -thickness, 0f), true);
            
            creator.AddMesh(meshT, defaultMaterial);
            creator.AddMesh(meshB, defaultMaterial);
        }

        private void CreateBridgeRampLane(LaneCollection lc, Creator creator)
        {
            var w = lc.Characteristics.Width;

            // var offsetR = lc.GetOffsetRight();
            // var offsetL = lc.GetOffsetLeft() + w;
            //
            // var lAdd = lc.OtherDirection == null ? -thickness : 0f;
            //
            // var r = offsetR + thickness;
            // var l = -offsetL + lAdd;
            //
            // var halfThickness = thickness * .5f;


            // var shape = new Shape(
            //     new[]
            //     {
            //         new Vector3(0f, -thickness, offsetR),
            //         new Vector3(0f, 0f, offsetR),
            //         
            //         new Vector3(0f, 0f, offsetR),
            //         new Vector3(0f, 0f, r - halfThickness),
            //         
            //         new Vector3(0f, 0f, r - halfThickness),
            //         new Vector3(0f, 1f, r - halfThickness),
            //         
            //         new Vector3(0f, 1f, r - halfThickness),
            //         new Vector3(0f, 1f, r),
            //         
            //         new Vector3(0f, 1f, r),
            //         new Vector3(0f, -thickness, r),
            //         
            //         new Vector3(0f, -thickness, r),
            //         new Vector3(0f, -thickness, l),
            //         
            //         new Vector3(0f, -thickness, l),
            //         new Vector3(0f, 0f, l),
            //         
            //         new Vector3(0f, 0f, l),
            //         new Vector3(0f, 0f, -offsetL),
            //     },
            //     
            //     new List<Vector3>
            //     {
            //         new Vector3(0, 0, -1),
            //         new Vector3(0, 0, -1),
            //         
            //         new Vector3(0, 1, 0),
            //         new Vector3(0, 1, 0),
            //         
            //         new Vector3(0, 0, -1),
            //         new Vector3(0, 0, -1),
            //         
            //         new Vector3(0, 1, 0),
            //         new Vector3(0, 1, 0),
            //
            //         new Vector3(0, 0, 1),
            //         new Vector3(0, 0, 1),
            //         
            //         new Vector3(0, -1, 0),
            //         new Vector3(0, -1, 0),
            //         
            //         new Vector3(0, 0, -1),
            //         new Vector3(0, 0, -1),
            //         
            //         new Vector3(0, 1, 0),
            //         new Vector3(0, 1, 0),
            //     },
            //     new List<float>
            //     {
            //         0, thickness,
            //         0, halfThickness,
            //         0, 1f,
            //         0, halfThickness,
            //         0, thickness + 1f,
            //         0, offsetR + offsetL,
            //         0, thickness,
            //         thickness, 0,
            //     },
            //     new List<bool>
            //     {
            //         true, false,
            //         true, false,
            //         true, false,
            //         true, false,
            //         true, false,
            //         true, false,
            //         true, false,
            //         true
            //     }
            // );

            var shape = new Shape(
                new[]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(0f, -thickness, 0f),

                    new Vector3(0f, -thickness, 0f),
                    new Vector3(0f, -thickness, -w),

                    new Vector3(0f, -thickness, -w),
                    new Vector3(0f, -thickness, -w),
                },
                new List<Vector3>
                {
                    new Vector3(0, 0, -1),
                    new Vector3(0, 0, -1),
                    
                    new Vector3(0, -1, 0),
                    new Vector3(0, -1, 0),
                    
                    new Vector3(0, 0, 1),
                    new Vector3(0, 0, 1),
                },
                new List<float>
                {
                    0, thickness,
                    0, w,
                    0, thickness
                },
                new List<bool>
                {
                    true, false, true, false, true
                }
            );

            var mesh = new MeshHelper(lc.Id + " BridgeRamp");
            var outline = lc.OutlineRight;
            // var forward = outline.Forward;
            
            outline.ExtrudeShape(mesh, shape);
            //
            // var angleStart = Vector3.SignedAngle(Vector3.forward, forward[0], Vector3.up);
            // var normalStart = Vector3.back.RotateAroundY(angleStart);
            //
            // var angleEnd = Vector3.SignedAngle(Vector3.forward, forward[forward.Count - 1], Vector3.up);
            // var normalEnd = Vector3.forward.RotateAroundY(angleEnd);
            //
            // var vertexOffset = mesh.Vertices.Count;
            // var vertexOffsetEnd = vertexOffset + 4;
            //
            // mesh.Vertices.Add(mesh.Vertices[4]);
            // mesh.Vertices.Add(mesh.Vertices[5]);
            // mesh.Vertices.Add(mesh.Vertices[7]);
            // mesh.Vertices.Add(mesh.Vertices[9]);
            //
            // mesh.Normals.Add(normalStart);
            // mesh.Normals.Add(normalStart);
            // mesh.Normals.Add(normalStart);
            // mesh.Normals.Add(normalStart);
            //
            // mesh.UV.Add(new Vector2(0,0) );
            // mesh.UV.Add(new Vector2(0,1) );
            // mesh.UV.Add(new Vector2(halfThickness,1) );
            // mesh.UV.Add(new Vector2(halfThickness,0) );
            //
            // mesh.AddTriangle(0, 1, 2, vertexOffset);
            // mesh.AddTriangle(0, 2, 3, vertexOffset);
            //
            // mesh.Vertices.Add(mesh.Vertices[vertexOffset - shape.Count + 4]);
            // mesh.Vertices.Add(mesh.Vertices[vertexOffset - shape.Count + 5]);
            // mesh.Vertices.Add(mesh.Vertices[vertexOffset - shape.Count + 7]);
            // mesh.Vertices.Add(mesh.Vertices[vertexOffset - shape.Count + 9]);
            //
            // mesh.Normals.Add(normalEnd);
            // mesh.Normals.Add(normalEnd);
            // mesh.Normals.Add(normalEnd);
            // mesh.Normals.Add(normalEnd);
            //
            // mesh.UV.Add(new Vector2(0,0) );
            // mesh.UV.Add(new Vector2(0,1) );
            // mesh.UV.Add(new Vector2(halfThickness,1) );
            // mesh.UV.Add(new Vector2(halfThickness,0) );
            //
            // mesh.AddTriangle(0, 2, 1, vertexOffsetEnd);
            // mesh.AddTriangle(0, 3, 2, vertexOffsetEnd);

            
            creator.AddMesh(mesh, defaultMaterial);

        }
        
        
        // private void CreateBridgeRamp(string bridgeId, Area area, Creator creator)
        // {
        //     // @todo replace with the actual roads as ramps
        //     var meshT = new MeshHelper(bridgeId + " Ramp Top");
        //     var meshW = new MeshHelper(bridgeId + " Ramp Walls");
        //     var meshB = new MeshHelper(bridgeId + " Ramp Bottom");
        //
        //     area.ExtrudeShape(meshW, _wallShape, cutCorners: true);
        //     
        //     creator.AddMesh(meshW, defaultMaterial);
        //
        //     area.Fill(meshT);
        //     area.Fill(meshB, new Vector3(0f, -thickness, 0f), true);
        //     
        //     creator.AddMesh(meshT, defaultMaterial);
        //     creator.AddMesh(meshB, defaultMaterial);
        // }
    }
}