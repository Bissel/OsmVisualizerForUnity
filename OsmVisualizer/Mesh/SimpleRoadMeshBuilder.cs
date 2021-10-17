using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Mesh
{
    public class SimpleRoadMeshBuilder : MeshBuilder
    {
        protected readonly string[] Types;
        protected readonly Dictionary<string, Material> SurfaceMats;
        protected readonly Material DefaultSurfaceMat;
        protected readonly Material MarkingsMat;
        protected readonly Material MarkingsDashedMat;
        protected readonly Material BuildingMat;
        protected readonly bool CombinedMesh;
        

        private const float BridgeBuildingOffset = .5f;
        private const float TunnelBuildingOffset = -.5f;
        
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
        #endregion
        
        
        public SimpleRoadMeshBuilder(AbstractSettingsProvider settings, string[] types, Dictionary<string, Material> surfaceMats, Material defaultSurfaceMat, Material markingsMat, Material markingsDashedMat, Material buildingMat, bool combinedMesh = false) : base(settings)
        {
            Types = types;
            SurfaceMats = surfaceMats;
            DefaultSurfaceMat = defaultSurfaceMat;
            MarkingsMat = markingsMat;
            MarkingsDashedMat = markingsDashedMat;
            BuildingMat = markingsMat;
            CombinedMesh = combinedMesh;
        }
        
        public class Creator : AbstractCreator
        {
            protected override string Name() => "Roads";
        }
        
        private void CreateColoredMesh(AbstractCreator creator, MeshHelper mesh, string surface, string color = null)
        {
            if (surface == null || !SurfaceMats.TryGetValue(surface, out var mat))
                mat = DefaultSurfaceMat;
            
            creator.AddMesh(mesh, mat, color);
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
            
            var rendered = new List<LaneCollection>();
            var renderedIntersections = new List<Intersection>();
            
            foreach (var laneCollection in tile.LaneCollections.Values)
            {
                if(laneCollection.Id.Type != MapData.LaneType.HIGHWAY
                    || !Types.Contains(laneCollection.Characteristics.Type)
                    || rendered.Contains(laneCollection))
                    continue;
                
                rendered.Add(laneCollection);
            
                RenderLaneCollection(laneCollection, creator, rendered);
                
                if ( i++ % 500 == 0)
                    yield return null;
            }

            foreach (var intersection in tile.Intersections.Values)
            {
                if(intersection == null
                    || renderedIntersections.Contains(intersection))
                    continue;
                
                renderedIntersections.Add(intersection);

                RenderIntersection(intersection, creator);

                if ( i++ % 500 == 0)
                    yield return null;
            }
            
            creator.CreateMesh();
        }

        private void RenderIntersection(Intersection intersection, Creator creator)
        {
            // @todo fix broken intersections
            if (intersection.Radius > 1000f)
                return;
            
            var mesh = new MeshHelper("Intersection " + intersection.Node);

            var area = intersection.Area; // ?? new Area(intersection.Points);
            
            area.Fill(mesh);
            area.ExtrudeShape(mesh, WallRight, cutCorners: true);
            
            var meshMarkings = new MeshHelper("Intersection " + intersection.Node + " markings");
            RenderIntersectionMarkings(intersection, meshMarkings);

            CreateColoredMesh(creator, mesh, intersection.Surface, intersection.Elevation == null ? null : intersection.Elevation is Tunnel ? "yellow" : "red");
            creator.AddMesh(meshMarkings, MarkingsMat);
        }

        private void RenderIntersectionMarkings(Intersection intersection, MeshHelper meshMarkings)
        {
            var last = intersection.OutlinePoints[intersection.OutlinePoints.Count - 1];
            for (var i = 0; i < intersection.OutlinePoints.Count/2; i++)
            {
                var lcId = intersection.IndexByOrientation(i);
                var lc = intersection.GetNthLaneCollection(lcId);
                var isOut = !intersection.IsNthLaneCollectionDirIn(lcId);
                if (isOut || lc.OtherDirection == null)
                {
                    var p = intersection.OutlinePoints[i*2];

                    new Data.Types.Spline(new[] {last, p}, intersection.HeightOffset)
                        .ExtrudeShape(meshMarkings, MarkingLine, offset: Vector3.back * MarkingOffset);
                }
                last = intersection.OutlinePoints[i*2 + 1];
            }
            
            // @todo center lane (if road-road-connection)
        }

        private void RenderLaneCollection(LaneCollection lc, Creator creator, List<LaneCollection> rendered)
        {
            if (lc.OutlineLeft.Count < 2 && lc.OutlineRight.Count < 2)
                return;
            
            var mesh = new MeshHelper("Road " + lc.Characteristics.Name + lc.Id);
            var meshBuilding = new MeshHelper("Road " + lc.Characteristics.Name + lc.Id + " building");
            var meshMarkings = new MeshHelper("Road " + lc.Characteristics.Name + lc.Id + " markings");
            var meshMarkingsDashed = new MeshHelper("Road " + lc.Characteristics.Name + lc.Id + " markings_d");

            // var left = lc.OutlineLeft;
            // var right = lc.OutlineRight;
            //
            var w = lc.Characteristics.Width * .5f;
            // var other = lc.OtherDirection;

            // if (other != null)
            // {
            //     other = lc.OtherDirection;
            //     rendered.Add(other);
            //     left = other.OutlineRight.Points.ToArray().Reverse().ToList();
            //     w+= other.Characteristics.Width * .5f;
            // }

            // while (left.Count < right.Count) left.Add(left[left.Count - 1]);
            // while (left.Count > right.Count) right.Add(right[right.Count - 1]);



            var heights = lc.OutlineRight.Select(p => p.y).ToList();
            // {
            //     ;
            //     // var length = 0f;
            //     // var last = lc.OutlineLeft[0];
            //     // lc.OutlineRight.ForEach(p =>
            //     // {
            //     //     length += Vector2.Distance(last, p);
            //     //     last = p;
            //     //     heights.Add(lc.GetHeightOffset(length, lc.Id.StartNode));
            //     // });
            // }
            
            // var splineRight = new Data.Types.Spline(right, heights);
            // var splineLeft = new Data.Types.Spline(left, /*other != null ? heights :*/ heights.ToArray().Reverse().ToList());
            
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


            // const int start = 0;
            // var end = left.Count;
            // var lineIndex = 0;
            // var triangleOffset = mesh.Vertices.Count;
            //
            // var lengthL = 0f;
            // var lengthR = 0f;
            // var currL = Vector2.zero;
            // var currR = Vector2.zero;
            //
            // var totalLength = lc.ApproxLength;
            // var hasHeightOffset = lc.HasHeightOffset || lc.HasAdjacentHeightOffset;
            // var lastLengthAvg = 0f;
            //
            // var onRamp = lc.HeightOffsetRampLength * .5f;
            // var offRamp = totalLength - lc.HeightOffsetRampLength * .5f;
            //
            // var buildingLineOffset = 0;
            // var buildingLineAdditionalOffset = 0;
            // var lastOffset = Vector3.zero;
            //
            // for (var i = start; i < end; i++)
            // {
            //     var lastR = currR;
            //     var lastL = currL;
            //
            //     currL = left[i].ToVector2xz();
            //     currR = right[i].ToVector2xz();
            //
            //
            //     var distL = 0f;
            //     var distR = 0f;
            //     if (i > 0)
            //     {
            //         distL = Vector2.Distance(currL, lastL);
            //         distR = Vector2.Distance(currR, lastR);
            //     }
            //
            //     lengthL += distL;
            //     lengthR += distR;
            //     
            //     var lengthAvg = (lengthL + lengthR) * .5f;
            //     
            //     if(hasHeightOffset && lastLengthAvg < onRamp && lengthAvg > onRamp)
            //     {
            //         
            //         var subL = onRamp - (lengthL - distL);
            //         var subR = onRamp - (lengthR - distR);
            //         
            //         currL = lastL + (currL - lastL).normalized * subL;
            //         currR = lastR + (currR - lastR).normalized * subR;
            //     
            //         lengthL = onRamp;
            //         lengthR = onRamp;
            //         lengthAvg = onRamp;
            //     
            //         i--;
            //     } 
            //     else if(hasHeightOffset && lastLengthAvg < offRamp && lengthAvg > offRamp)
            //     {
            //         
            //         var subL = offRamp - (lengthL - distL);
            //         var subR = offRamp - (lengthR - distR);
            //         
            //         currL = lastL + (currL - lastL).normalized * subL;
            //         currR = lastR + (currR - lastR).normalized * subR;
            //     
            //         lengthL = offRamp;
            //         lengthR = offRamp;
            //         lengthAvg = offRamp;
            //     
            //         i--;
            //     }
            //
            //     lastLengthAvg = lengthAvg;
            //     
            //     if (i == end - 1)
            //         lengthAvg = totalLength;
            //     
            //     var offset = lc.GetHeightOffset(lengthAvg, lc.Id.StartNode) * Vector3.up;
            //     
            //     mesh.Vertices.Add(currL.ToVector3xz() + offset);
            //     mesh.Vertices.Add(currR.ToVector3xz() + offset);
            //     
            //     mesh.Normals.Add(Vector3.up);
            //     mesh.Normals.Add(Vector3.up);
            //     
            //     mesh.UV.Add(new Vector2(-w, lengthR));
            //     mesh.UV.Add(new Vector2(+w, lengthL));
            //     
            //     if (lineIndex > 0)
            //     {
            //         mesh.AddQuad(lineIndex * 2 + triangleOffset - 2);
            //     }
            //
            //     if (lineIndex > 0 && offset.magnitude > .001f)
            //     {
            //         OffsetBuilding(
            //             lastL.ToVector3xz(),currL.ToVector3xz(),
            //             lastR.ToVector3xz(), currR.ToVector3xz(),
            //             lastOffset, offset, 
            //             lengthL, lengthR,
            //             meshBuilding, ref buildingLineOffset,
            //             buildingLineAdditionalOffset
            //         );
            //
            //         if (offset.y > 0f)
            //         {
            //             OffsetBuildingBridge(
            //                 lastL.ToVector3xz(),currL.ToVector3xz(),
            //                 lastR.ToVector3xz(), currR.ToVector3xz(),
            //                 lastOffset, offset, 
            //                 lengthL, lengthR,
            //                 meshBuilding, ref buildingLineOffset,
            //                 ref buildingLineAdditionalOffset,
            //                 w
            //             );
            //         }
            //         else
            //         {
            //             OffsetBuildingTunnel(
            //                 lastL.ToVector3xz(),currL.ToVector3xz(),
            //                 lastR.ToVector3xz(), currR.ToVector3xz(),
            //                 lastOffset, offset, 
            //                 lengthL, lengthR,
            //                 meshBuilding, ref buildingLineOffset,
            //                 ref buildingLineAdditionalOffset,
            //                 w
            //             );
            //         }
            //     }
            //
            //     
            //     lineIndex++;
            //
            //     lastOffset = offset;
            //
            //     // laneSeparationMarkings(i, );
            //
            //     // @todo
            //     // markings.Vertices.Add(currL);
            //     // markings.Vertices.Add(currL);
            //     // markings.Normals.Add(Vector3.up);
            //     // markings.Normals.Add(Vector3.up);
            //     // markings.UV.Add(new Vector2(-w, lengthR));
            //     // markings.UV.Add(new Vector2(+w, lengthL));
            //     //
            //     // markings.Vertices.Add(currR);
            //     // markings.Vertices.Add(currR);
            //     // markings.Normals.Add(Vector3.up);
            //     // markings.Normals.Add(Vector3.up);
            //     // markings.UV.Add(new Vector2(-w, lengthR));
            //     // markings.UV.Add(new Vector2(+w, lengthL));
            //     //
            //     // if (center != null)
            //     // {
            //     //     markings.Vertices.Add(center[i]);
            //     //     markings.Vertices.Add(center[i]);
            //     //     markings.Normals.Add(Vector3.up);
            //     //     markings.Normals.Add(Vector3.up);
            //     //     markings.UV.Add(new Vector2(-w, lengthR));
            //     //     markings.UV.Add(new Vector2(+w, lengthL));
            //     // }
            // }

            LaneMarkings(lc, meshMarkings, .001f * Vector3.up);

            // foreach (var lane in collection.Lanes)
            // {
            //     streetMarkingLine(lane.Points, markings, false);
            // }
            
            CreateColoredMesh(creator, mesh, lc.Characteristics.Surface, lc.Elevation == null ? null : lc.Elevation is Tunnel ? "yellow" : "red");
            creator.AddMesh(meshBuilding, BuildingMat);
            creator.AddMesh(meshMarkings, MarkingsMat);
            creator.AddMesh(meshMarkingsDashed, MarkingsDashedMat);

        }

        // private static void OffsetBuilding(
        //     Vector3 lastL, Vector3 currL, 
        //     Vector3 lastR, Vector3 currR, 
        //     Vector3 lastOffset, Vector3 offset, 
        //     float lengthL, float lengthR,
        //     MeshHelper building, ref int buildingLineOffset, 
        //     int additionalOffset
        // )
        // {
        //     var offsetZero = Vector3.up * Mathf.Max(offset.y - BridgeBuildingOffset, 0f);
        //     var lastOffsetZero = Vector3.up * Mathf.Max(lastOffset.y - BridgeBuildingOffset, 0f);
        //     
        //     var p0L = lastL + lastOffsetZero;
        //     var p1L = currL + offset;
        //     var p2L = currL + offsetZero;
        //     
        //     var p0R = lastR + lastOffsetZero;
        //     var p1R = currR + offset;
        //     var p2R = currR + offsetZero;
        //     
        //     var normalL = Vector3.Cross(p2L - p0L, p1L - p0L).normalized;
        //     var normalR = Vector3.Cross(p2R - p0R, p1R - p0R).normalized;
        //
        //     if (buildingLineOffset == 0)
        //     {
        //         building.Vertices.Add(lastL + lastOffset);
        //         building.Vertices.Add(p0L);
        //         building.Vertices.Add(lastR + lastOffset);
        //         building.Vertices.Add(p0R);
        //
        //         building.Normals.Add(normalL);
        //         building.Normals.Add(normalL);
        //         building.Normals.Add(normalR);
        //         building.Normals.Add(normalR);
        //
        //         building.UV.Add(new Vector2(0, 0));
        //         building.UV.Add(new Vector2(lastOffset.y, 0));
        //         building.UV.Add(new Vector2(0, 0));
        //         building.UV.Add(new Vector2(lastOffset.y, 0));
        //
        //         buildingLineOffset = 4;
        //     }
        //     
        //     building.Vertices.Add(p1L);
        //     building.Vertices.Add(p2L);
        //     building.Vertices.Add(p1R);
        //     building.Vertices.Add(p2R);
        //     
        //     building.Normals.Add(normalL);
        //     building.Normals.Add(normalL);
        //     building.Normals.Add(normalR);
        //     building.Normals.Add(normalR);
        //
        //     building.UV.Add(new Vector2(0, lengthL));
        //     building.UV.Add(new Vector2(offset.y, lengthL));
        //     building.UV.Add(new Vector2(0, lengthR));
        //     building.UV.Add(new Vector2(offset.y, lengthR));
        //
        //     building.Triangles.Add(buildingLineOffset + 1);
        //     building.Triangles.Add(buildingLineOffset + 0);
        //     building.Triangles.Add(buildingLineOffset - additionalOffset - 4);
        //
        //     building.Triangles.Add(buildingLineOffset + 1);
        //     building.Triangles.Add(buildingLineOffset - additionalOffset - 4);
        //     building.Triangles.Add(buildingLineOffset - additionalOffset - 3);
        //
        //     building.Triangles.Add(buildingLineOffset + 3);
        //     building.Triangles.Add(buildingLineOffset - additionalOffset - 2);
        //     building.Triangles.Add(buildingLineOffset + 2);
        //
        //     building.Triangles.Add(buildingLineOffset + 3);
        //     building.Triangles.Add(buildingLineOffset - additionalOffset - 1);
        //     building.Triangles.Add(buildingLineOffset - additionalOffset - 2);
        //
        //     buildingLineOffset += 4;
        // }
        //
        // private static void OffsetBuildingBridge(
        //     Vector3 lastL, Vector3 currL, 
        //     Vector3 lastR, Vector3 currR, 
        //     Vector3 lastOffset, Vector3 offset, 
        //     float lengthL, float lengthR,
        //     MeshHelper building, ref int buildingLineOffset, 
        //     ref int additionalOffset, float w
        // )
        // {
        //     var offsetZero = Vector3.up * (offset.y - BridgeBuildingOffset);
        //     var lastOffsetZero = Vector3.up * (lastOffset.y - BridgeBuildingOffset);
        //
        //     var normal = Vector3.down;
        //
        //     var indexOffset = 4;
        //     
        //     if (additionalOffset == 0)
        //     {
        //         additionalOffset = 4;
        //         building.Vertices.Add(lastL + lastOffsetZero);
        //         building.Vertices.Add(lastR + lastOffsetZero);
        //         building.Normals.Add(normal);
        //         building.Normals.Add(normal);
        //         building.UV.Add(new Vector2(-w, 0));
        //         building.UV.Add(new Vector2(+w, 0));
        //
        //         buildingLineOffset += 2;
        //         indexOffset = 0;
        //     }
        //     else
        //     {
        //         additionalOffset = 2;
        //     }
        //     
        //     building.Vertices.Add(currL + offsetZero);
        //     building.Vertices.Add(currR + offsetZero);
        //     building.Normals.Add(normal);
        //     building.Normals.Add(normal);
        //     building.UV.Add(new Vector2(-w, lengthL));
        //     building.UV.Add(new Vector2(+w, lengthR));
        //     
        //     building.Triangles.Add(buildingLineOffset + 0);
        //     building.Triangles.Add(buildingLineOffset - indexOffset - 2);
        //     building.Triangles.Add(buildingLineOffset - indexOffset - 1);
        //     
        //     building.Triangles.Add(buildingLineOffset + 0);
        //     building.Triangles.Add(buildingLineOffset - indexOffset - 1);
        //     building.Triangles.Add(buildingLineOffset + 1);
        //
        //     buildingLineOffset += 2;
        // }
        //
        // private static void OffsetBuildingTunnel(
        //     Vector3 lastL, Vector3 currL, 
        //     Vector3 lastR, Vector3 currR, 
        //     Vector3 lastOffset, Vector3 offset, 
        //     float lengthL, float lengthR,
        //     MeshHelper building, ref int buildingLineOffset, 
        //     ref int additionalOffset, float w
        // )
        // {
        //     additionalOffset = 4;
        //     var init = false;
        //     var indexOffset = 4;
        //     
        //     var lidOffsetTop = -.001f * Vector3.up;
        //     var offsetZero = Vector3.up * TunnelBuildingOffset;
        //     var lastOffsetZero = Vector3.up * TunnelBuildingOffset;
        //     
        //     if (offset.magnitude < 5f)
        //     {
        //         if (lastOffset.magnitude > 5f)
        //         {
        //             // lid-cap
        //             var p0 = lastL + lastOffsetZero;
        //             var p1 = lastL + lidOffsetTop;
        //             var p2 = lastR + lastOffsetZero;
        //     
        //             var normalCap = Vector3.Cross(p2 - p0, p1 - p0).normalized;
        //         
        //             building.Vertices.Add(lastL + lastOffsetZero);
        //             building.Vertices.Add(lastR + lastOffsetZero);
        //             building.Vertices.Add(lastL + lidOffsetTop);
        //             building.Vertices.Add(lastR + lidOffsetTop);
        //         
        //             building.Normals.Add(normalCap);
        //             building.Normals.Add(normalCap);
        //             building.Normals.Add(normalCap);
        //             building.Normals.Add(normalCap);
        //         
        //             building.UV.Add(new Vector2(-w, 0));
        //             building.UV.Add(new Vector2(+w, 0));
        //             building.UV.Add(new Vector2(-w, TunnelBuildingOffset));
        //             building.UV.Add(new Vector2(+w, TunnelBuildingOffset));
        //         
        //             building.Triangles.Add(buildingLineOffset + 0);
        //             building.Triangles.Add(buildingLineOffset + 1);
        //             building.Triangles.Add(buildingLineOffset + 2);
        //
        //             building.Triangles.Add(buildingLineOffset + 1);
        //             building.Triangles.Add(buildingLineOffset + 3);
        //             building.Triangles.Add(buildingLineOffset + 2);
        //
        //             additionalOffset += 4;
        //             buildingLineOffset += 4;
        //         }
        //         else
        //         {
        //             additionalOffset = 0;
        //         }
        //
        //         return;
        //     }
        //     
        //     if (lastOffset.magnitude < 5f)
        //     {
        //         init = true;
        //         indexOffset = 0;
        //         
        //         // lid-cap
        //         var p0 = lastL + lastOffsetZero;
        //         var p1 = lastL + lidOffsetTop;
        //         var p2 = lastR + lastOffsetZero;
        //     
        //     
        //         var normalCap = Vector3.Cross(p2 - p0, p1 - p0).normalized;
        //         
        //         building.Vertices.Add(lastL + lastOffsetZero);
        //         building.Vertices.Add(lastR + lastOffsetZero);
        //         building.Vertices.Add(lastL + lidOffsetTop);
        //         building.Vertices.Add(lastR + lidOffsetTop);
        //         
        //         building.Normals.Add(normalCap);
        //         building.Normals.Add(normalCap);
        //         building.Normals.Add(normalCap);
        //         building.Normals.Add(normalCap);
        //         
        //         building.UV.Add(new Vector2(-w, 0));
        //         building.UV.Add(new Vector2(+w, 0));
        //         building.UV.Add(new Vector2(-w, TunnelBuildingOffset));
        //         building.UV.Add(new Vector2(+w, TunnelBuildingOffset));
        //         
        //         building.Triangles.Add(buildingLineOffset + 0);
        //         building.Triangles.Add(buildingLineOffset + 2);
        //         building.Triangles.Add(buildingLineOffset + 1);
        //
        //         building.Triangles.Add(buildingLineOffset + 1);
        //         building.Triangles.Add(buildingLineOffset + 2);
        //         building.Triangles.Add(buildingLineOffset + 3);
        //
        //         additionalOffset += 4;
        //         buildingLineOffset += 4;
        //     }
        //     
        //
        //     var normal = Vector3.down;
        //     
        //     if (init)
        //     {
        //         additionalOffset += 4;
        //         building.Vertices.Add(lastL + lastOffsetZero);
        //         building.Vertices.Add(lastR + lastOffsetZero);
        //         building.Vertices.Add(lastL + lidOffsetTop);
        //         building.Vertices.Add(lastR + lidOffsetTop);
        //         building.Normals.Add(normal);
        //         building.Normals.Add(normal);
        //         building.Normals.Add(-normal);
        //         building.Normals.Add(-normal);
        //         building.UV.Add(new Vector2(-w, 0));
        //         building.UV.Add(new Vector2(+w, 0));
        //         building.UV.Add(new Vector2(-w, 0));
        //         building.UV.Add(new Vector2(+w, 0));
        //
        //         buildingLineOffset += 4;
        //     }
        //     
        //     building.Vertices.Add(currL + offsetZero);
        //     building.Vertices.Add(currR + offsetZero);
        //     building.Vertices.Add(currL + lidOffsetTop);
        //     building.Vertices.Add(currR + lidOffsetTop);
        //     
        //     building.Normals.Add(normal);
        //     building.Normals.Add(normal);
        //     building.Normals.Add(-normal);
        //     building.Normals.Add(-normal);
        //     
        //     building.UV.Add(new Vector2(-w, lengthL));
        //     building.UV.Add(new Vector2(+w, lengthR));
        //     building.UV.Add(new Vector2(-w, lengthL));
        //     building.UV.Add(new Vector2(+w, lengthR));
        //     
        //     building.Triangles.Add(buildingLineOffset + 0);
        //     building.Triangles.Add(buildingLineOffset - indexOffset - 4);
        //     building.Triangles.Add(buildingLineOffset - indexOffset - 3);
        //     
        //     building.Triangles.Add(buildingLineOffset + 0);
        //     building.Triangles.Add(buildingLineOffset - indexOffset - 3);
        //     building.Triangles.Add(buildingLineOffset + 1);
        //
        //     building.Triangles.Add(buildingLineOffset - indexOffset - 2);
        //     building.Triangles.Add(buildingLineOffset + 2);
        //     building.Triangles.Add(buildingLineOffset - indexOffset - 1);
        //
        //     building.Triangles.Add(buildingLineOffset - indexOffset - 1);
        //     building.Triangles.Add(buildingLineOffset + 2);
        //     building.Triangles.Add(buildingLineOffset + 3);
        //
        //     buildingLineOffset += 4;
        // }

        private static void LaneMarkings(LaneCollection lc, MeshHelper mhMarkings, Vector3 offset)
        {
            if (lc.Lanes == null || lc.Lanes.Length == 0)
                return;

            foreach (var lane in lc.Lanes)
            {
                if(lane.Directions == null || lane.Directions[0] == Direction.NONE)
                    continue;

                var points = lane.EvenlySpacedPoints();

                var pos = points[points.Length - 2].ToVector3xz() + offset;
                var last = points[points.Length - 1].ToVector3xz() + offset;
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
        
        
        // private void streetMarkingLine(Spline line, MeshHelper markingsMesh, bool isDashed, int length)
        // {
        //     var points = line.Points; //.GenerateEvenlySpacedPoints(0,0, 32f/(length-1) * 0.5f);
        //     streetMarkingLine(points, markingsMesh, isDashed);
        // }

        // private void streetMarkingLine(IReadOnlyList<Vector2> points, MeshHelper markingsMesh, bool isDashed = false)
        // {
        //     if (points.Count < 2)
        //         return;
        //     
        //     var triangleOffsetMarkings = markingsMesh.Vertices.Count;
        //
        //     Vector3 prev;
        //     Vector3 curr = Vector3.zero;
        //     Vector3 next = Vector3.zero;
        //     
        //     for (var i = 0; i < points.Count; i++)
        //     {
        //         var dir = Vector3.zero;
        //         
        //         prev = curr;
        //         curr = i == 0 ? points[i].ToVector3xz() : next;
        //         
        //         if (i - 1 >= 0)
        //         {
        //             var offset = prev - curr;
        //             dir += offset.normalized;
        //         }
        //         if (i + 1 < points.Count)
        //         {
        //             next = points[i + 1];
        //             var offset = next - curr;
        //             dir -= offset.normalized;
        //         }
        //         
        //         dir.Normalize();
        //         
        //         var rotatedUnit = Quaternion.AngleAxis(-90, Vector3.up) * dir;
        //         
        //         markingsMesh.Vertices.Add(curr - rotatedUnit * MarkingWidthHalf);
        //         markingsMesh.Vertices.Add(curr + rotatedUnit * MarkingWidthHalf);
        //
        //         markingsMesh.Normals.Add(Vector3.up);
        //         markingsMesh.Normals.Add(Vector3.up);
        //         
        //         var v = i % 2;
        //         markingsMesh.UV.Add(new Vector2(0, v));
        //         markingsMesh.UV.Add(new Vector2(1, v));
        //
        //         if (i > 0 && (!isDashed || (i % 8 < 4)))
        //         {
        //             var lineIndex = i * 2 + triangleOffsetMarkings;
        //             
        //             markingsMesh.Triangles.Add(lineIndex - 2);
        //             markingsMesh.Triangles.Add(lineIndex + 1);
        //             markingsMesh.Triangles.Add(lineIndex - 1);
        //             
        //             markingsMesh.Triangles.Add(lineIndex - 2);
        //             markingsMesh.Triangles.Add(lineIndex + 0);
        //             markingsMesh.Triangles.Add(lineIndex + 1);
        //         }
        //
        //     }
        // }
    }

}