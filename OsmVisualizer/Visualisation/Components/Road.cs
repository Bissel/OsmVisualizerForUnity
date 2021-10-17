using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Request;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Visualisation.Components
{
    public class Road : VisualizerComponentMaterials
    {

        public Material markingsMat;
        public Material markingsDashedMat;
        
        public List<string> roadTypes = new List<string>
        {
            "motorway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential",
            "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link",
            "living_street",
            "road"
        };
        
        protected override void OnValidate()
        {
            base.OnValidate();
            if (materials.Length != 0)
                return;

            materials = new[]
            {
                new MaterialMapping{mat = null, name = "concrete"},
                new MaterialMapping{mat = null, name = "paving_stones"},
                new MaterialMapping{mat = null, name = "cobblestone"},
                new MaterialMapping{mat = null, name = "sett"},
                new MaterialMapping{mat = null, name = "unhewn_cobblestone"},
                new MaterialMapping{mat = null, name = "wood"},
                new MaterialMapping{mat = null, name = "unpaved"},
                new MaterialMapping{mat = null, name = "compacted"},
                new MaterialMapping{mat = null, name = "ground"},
                new MaterialMapping{mat = null, name = "dirt"},
                new MaterialMapping{mat = null, name = "earth"},
                new MaterialMapping{mat = null, name = "mud"},
                new MaterialMapping{mat = null, name = "sand"},
            };
        }
        
        private const float MarkingOffset = .08f;
        private const float MarkingWidth = .08f;
        private const float MarkingWidthHalf = MarkingWidth * .5f;
        
        #region Lane Marking Arrows
        private static readonly int[] LaneMarkingArrowThroughTriangles = {
            0, 1, 2,
            0, 2, 3,
            4, 5, 6
        };

        private static readonly Vector2[] LaneMarkingArrowThroughVertices = {
            new Vector2(-0.1f, 0.5f),
            new Vector2(-0.1f, 0.6f),
            new Vector2( 0.1f, 0.6f),
            new Vector2( 0.1f, 0.5f),
            
            new Vector2(-0.2f, 0.6f),
            new Vector2( 0.0f, 1.1f),
            new Vector2( 0.2f, 0.6f),
        };

        private static readonly int[] LaneMarkingArrowLeftTriangles = {
            0, 1, 2,
            0, 2, 3,
            4, 5, 6
        };

        private static readonly Vector2[] LaneMarkingArrowLeftVertices = {
            new Vector2(-0.1f, 0.5f),
            new Vector2(-0.1f, 0.1f),
            new Vector2(-0.2f, 0.1f),
            new Vector2(-0.2f, 0.5f),
            
            new Vector2(-0.2f,-0.3f),
            new Vector2(-0.4f, 0.3f),
            new Vector2(-0.2f, 0.9f),
        };
        
        private static readonly int[] LaneMarkingArrowRightTriangles = {
            0, 2, 1,
            0, 3, 2,
            4, 6, 5
        };

        private static readonly Vector2[] LaneMarkingArrowRightVertices = {
            new Vector2( 0.1f, 0.5f),
            new Vector2( 0.1f, 0.1f),
            new Vector2( 0.2f, 0.1f),
            new Vector2( 0.2f, 0.5f),
            
            new Vector2( 0.2f,-0.3f),
            new Vector2( 0.4f, 0.3f),
            new Vector2( 0.2f, 0.9f),
        };
        
        private static readonly int[] LaneMarkingArrowBaseTriangles = {
            0, 1, 2,
            0, 2, 3
        };

        private static readonly Vector2[] LaneMarkingArrowBaseVertices = {
            new Vector2(-0.1f, -0.5f),
            new Vector2(-0.1f, 0.5f),
            new Vector2(0.1f, 0.5f),
            new Vector2(0.1f, -0.5f)
        };
        #endregion
        
        #region Shapes
        protected readonly Shape WallLeft = Shape.WallLeft(.1f, -.1f);
        protected readonly Shape WallRight = Shape.WallRight(.1f, -.1f);

        protected readonly Shape MarkingLine = new Shape(
            new[]{new Vector3(0, 0.001f, -MarkingWidthHalf),new Vector3(0, 0.001f, MarkingWidthHalf)},
            new List<Vector3> {Vector3.up, Vector3.up},
            new List<float> {0, 1},
            new List<bool> {true}
        ); 
        protected readonly Shape MarkingStopLine = new Shape(
            new[]{new Vector3(0, 0.001f, 0),new Vector3(0, 0.001f, .5f)},
            new List<Vector3> {Vector3.up, Vector3.up},
            new List<float> {0, 1},
            new List<bool> {true}
        ); 
        #endregion
        
        
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
            
            // @todo block in Start
            var roadShape1 = Shape.WidthCentered(1f);
            var roadShape2 = Shape.WidthCentered(2f);
            var roadShape4 = Shape.WidthCentered(4f);
            
            var roadShapes = new Dictionary<string, Shape>
            {
                {"motorway", roadShape4},
                {"trunk", roadShape4}, 
                {"primary", roadShape4},
                {"secondary", roadShape4}, 
                {"tertiary", roadShape4},
                {"unclassified", roadShape2},
                {"motorway_link", roadShape2},
                {"trunk_link", roadShape2},
                {"primary_link", roadShape2},
                {"secondary_link", roadShape2},
                {"residential", roadShape1},
                {"tertiary_link", roadShape1},
                {"living_street", roadShape1},
                {"road", roadShape2}
            };

            var roadTypeHeightOffset = new Dictionary<string, float>
            {
                {"motorway", .9f},
                {"trunk", .8f}, 
                {"primary", .7f},
                {"secondary", .6f}, 
                {"tertiary", .5f},
                {"motorway_link", .85f},
                {"trunk_link", .75f},
                {"primary_link", .65f},
                {"secondary_link", .55f},
                {"tertiary_link", .45f},
                {"residential", .4f},
            };

            var arrowMat = Materials.ContainsKey("arrow") ? Materials["arrow"] : null;

            // end block

            foreach (var el in tile.RawData.elements)
            {
                if (el.GeometryType != GeometryType.LINE || el.split)
                    continue;

                var highway = el.GetProperty("highway");

                if (highway == null || !roadTypes.Contains(highway))
                    continue;
                
                var roadLayer = el.GetPropertyInt("layer");
                var heightOffset = 1f * roadLayer 
                                   + (roadTypeHeightOffset.ContainsKey(highway) ? roadTypeHeightOffset[highway] : 0f) 
                                   + Random.Range(-.01f, .01f);
                
                var shape = roadShapes.ContainsKey(highway) ? roadShapes[highway] : roadShape1;
                
                var mesh = new MeshHelper();
                new Data.Types.Spline(el.pointsV2, heightOffset).ExtrudeShape(mesh, shape);

                creator.AddColoredMesh(mesh, highway);

                var oneway = el.GetProperty("oneway");
                if (oneway != null && oneway != "no" && oneway != "0")
                {
                    var point = oneway == "-1" ? el.pointsV2.ToArray().Reverse().ToList() : el.pointsV2;
                    var onewayMesh = new MeshHelper();
                    var vertexIndex = 0;
                    for (var i = 3; i < point.Count - 5; i += 10)
                    {
                        var p0 = point[i];
                        var dir = (point[i + 1] - p0).normalized;
                        var p1 = p0 + dir * 10;
                        var rotatedUnit = dir.Rotate(90).ToVector3xz();
                        
                        onewayMesh.AddTwoPoints(p0.ToVector3xz(heightOffset + .02f), rotatedUnit, 2f, 0f);
                        onewayMesh.AddTwoPoints(p1.ToVector3xz(heightOffset + .02f), rotatedUnit, 2f, 1f);
                        
                        onewayMesh.AddQuad(vertexIndex);
                        vertexIndex+=4;
                    }
                    creator.AddMesh(onewayMesh, arrowMat);
                }

                // @todo street name
                
                
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
            
            foreach (var lc in tile.LaneCollections.Values)
            {
                if(lc.IsRemoved || lc.Id.Type != MapData.LaneType.HIGHWAY || !roadTypes.Contains(lc.Characteristics.Type))
                    continue;

                CreateLaneCollectionMesh(lc, creator);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }

            foreach (var inter in tile.Intersections.Values)
            {
                if(inter == null)
                    continue;

                CreateIntersectionMesh(inter, creator);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
        }
        
        
        private void CreateIntersectionMesh(Intersection intersection, Creator creator)
        {
            // @todo fix broken intersections
            if (intersection.Radius > 1000f)
                return;
            
            var mesh = new MeshHelper("Intersection " + intersection.Node);

            var area = intersection.Area;
            if (area == null)
            {
                Debug.LogWarning($"Intersection {intersection.Node} has no area");
                return;
            }
            
            area.Fill(mesh);
            area.ExtrudeShape(mesh, WallRight, cutCorners: true);
            
            var meshMarkings = new MeshHelper("Intersection " + intersection.Node + " markings");
            RenderIntersectionMarkings(intersection, meshMarkings);

            creator.AddColoredMesh(mesh, intersection.Surface /*, intersection.Elevation == null ? null : intersection.Elevation is Tunnel ? "yellow" : "red"*/);

            creator.AddMesh(meshMarkings, markingsMat);
        }

        private void RenderIntersectionMarkings(Intersection intersection, MeshHelper meshMarkings)
        {
            var roadMappings = intersection.GetRoadMapping();
            var lastElement = roadMappings[roadMappings.Count - 1];
            var last = lastElement.Point;
            var draw = lastElement.Lc == null;
            
            foreach (var m in roadMappings)
            {
                if (draw)
                {
                    draw = false;
                    new Data.Types.Spline(new[] {last, m.Point}, intersection.HeightOffset)
                        .ExtrudeShape(meshMarkings, MarkingLine, offset: Vector3.back * MarkingOffset);
                }

                if (m.Lc != null)
                {
                    if (!intersection.IsRoadRoadConnection && m.IsIn && !intersection.IsHigherPriority(m.Lc))
                        new Data.Types.Spline(new[] {m.Lc.GetPointLE0(), m.Lc.GetPointRE0()}, intersection.HeightOffset)
                            .ExtrudeShape(meshMarkings, MarkingStopLine);
                    
                    continue;
                }
                
                draw = true;
                last = m.Point;
            }
            
            // @todo center lane (if road-road-connection)
        }

        private void CreateLaneCollectionMesh(LaneCollection lc, Creator creator)
        {
            if (lc.OutlineLeft.Count < 2 && lc.OutlineRight.Count < 2)
                return;
            
            var mesh = new MeshHelper("Road " + lc.Characteristics.Name + lc.Id);
            var meshMarkings = new MeshHelper("Road " + lc.Characteristics.Name + lc.Id + " markings");
            var meshMarkingsDashed = new MeshHelper("Road " + lc.Characteristics.Name + lc.Id + " markings_d");

            var w = lc.Characteristics.Width * .5f;
            
            var heights = lc.OutlineRight.Select(p => p.y).ToList();
            
            lc.OutlineRight.ExtrudeShape(mesh, WallRight);
            lc.OutlineRight.ExtrudeShape(meshMarkings, MarkingLine, offset: Vector3.back * MarkingOffset);
            
            if (lc.OtherDirection == null)
            {
                lc.OutlineLeft.ExtrudeShape(mesh, WallLeft);
                lc.OutlineLeft.ExtrudeShape(meshMarkings, MarkingLine, offset: Vector3.forward * MarkingOffset);
            }
            else
            {
                lc.OutlineLeft.ExtrudeShape(meshMarkings, MarkingLine);
            }
            
            var laneOffset = Vector3.back * (lc.Characteristics.Width * .5f / lc.Lanes.Length);
            for (var i = 1; i < lc.Lanes.Length; i++)
            {
                var laneSpline = new Data.Types.Spline(lc.Lanes[i].Points, heights);
                laneSpline.ExtrudeShape(meshMarkingsDashed, MarkingLine, offset: laneOffset);
            }

            var triangleOffset = mesh.Vertices.Count;
            var totalLength = 0f;
            for (var i = 0; i < lc.OutlineRight.Count; i++)
            {
                totalLength += (lc.OutlineRight.Length[i] + lc.OutlineLeft.Length[i]) * .5f;
                
                mesh.Vertices.Add( lc.OutlineLeft[i] );
                mesh.Vertices.Add( lc.OutlineRight[i] );
            
                mesh.Normals.Add(Vector3.up);
                mesh.Normals.Add(Vector3.up);
            
                mesh.UV.Add(new Vector2(-w, totalLength));
                mesh.UV.Add(new Vector2(w, totalLength));
                
                if(i == 0) continue;
                
                mesh.AddQuad(i * 2 + triangleOffset - 2);
            }
            
            LaneMarkings(lc, meshMarkings, .001f * Vector3.up);

            creator.AddColoredMesh(mesh, lc.Characteristics.Surface/*, lc.Elevation == null ? null : lc.Elevation is Tunnel ? "yellow" : "red"*/);
            
            creator.AddMesh(meshMarkings, markingsMat);
            creator.AddMesh(meshMarkingsDashed, markingsDashedMat);
        }

        private static void LaneMarkings(LaneCollection lc, MeshHelper mhMarkings, Vector3 offset)
        {
            if (lc.Lanes == null || lc.Lanes.Length == 0)
                return;

            foreach (var lane in lc.Lanes)
            {
                if(lane.Directions == null || lane.Directions[0] == Direction.NONE)
                    continue;

                var points = lane.EvenlySpacedPoints();

                Vector3 pos;
                Vector3 last;

                if (points.Length < 3)
                {
                    pos = points[points.Length - 2].ToVector3xz() + offset;
                    last = points[points.Length - 1].ToVector3xz() + offset;
                }
                else
                {
                    pos = points[points.Length - 3].ToVector3xz() + offset;
                    last = points[points.Length - 2].ToVector3xz() + offset;
                }

                var rotation = Vector3.SignedAngle(Vector3.forward, last - pos, Vector3.up);
                

                var drawBase = false;
                foreach (var dir in lane.Directions)
                {
                    switch (dir)
                    {
                        case Direction.THROUGH:
                            drawBase = true;
                            LaneMarking(pos, rotation, LaneMarkingArrowThroughVertices, LaneMarkingArrowThroughTriangles, mhMarkings);
                            break;
                        
                        case Direction.LEFT:
                        case Direction.LEFT_SHARP:
                        case Direction.LEFT_SLIGHT:
                        case Direction.LEFT_MERGE:
                            drawBase = true;
                            LaneMarking(pos, rotation, LaneMarkingArrowLeftVertices, LaneMarkingArrowLeftTriangles, mhMarkings);
                            break;
                        
                        case Direction.RIGHT:
                        case Direction.RIGHT_SHARP:
                        case Direction.RIGHT_SLIGHT:
                        case Direction.RIGHT_MERGE:
                            drawBase = true;
                            LaneMarking(pos, rotation, LaneMarkingArrowRightVertices, LaneMarkingArrowRightTriangles, mhMarkings);
                            break;
                        
                        case Direction.REVERSE:
                            // @todo
                            break;
                        case Direction.NONE:
                        default:
                            break;
                    }
                    
                }
                
                if(drawBase)
                    LaneMarking(pos, rotation, LaneMarkingArrowBaseVertices, LaneMarkingArrowBaseTriangles, mhMarkings);
                
            }

                
        }
        

        private static void LaneMarking(Vector3 pos, float rotation, IEnumerable<Vector2> marking, IEnumerable<int> triangles, MeshHelper markingsMesh)
        {
            
            var triangleOffset = markingsMesh.Vertices.Count;
            
            foreach (var v in marking)
            {
                markingsMesh.Vertices.Add(pos + Quaternion.AngleAxis( rotation, Vector3.up) * v.ToVector3xz());
                markingsMesh.UV.Add(v);
                markingsMesh.Normals.Add(Vector3.up);
            }

            foreach (var t in triangles)
            {
                markingsMesh.Triangles.Add(t + triangleOffset);
            }
        }
        
    }
}