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
    public class Sidewalks : VisualizerComponentMaterials
    {
        public Material cyclewayMat;
        public Material cyclewayDividerMat;
        public Material guttersMat;
        public Material curbMat;
        
        public List<string> roadTypes = new List<string>
        {
            "motorway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential",
            "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link",
            "living_street",
            "road"
        };
        
        #region Heights and Widths
        public const float SidewalkHeight = .1f;
        public const float SidewalkWidth = 1.5f;
        public const float CyclewayDividerWidth = .2f;
        public const float CyclewayWidth = 1.5f;
        public const float GuttersWidth = .3f;
        public const float GuttersDepth = -.02f;
        #endregion Heights and Widths
        
        #region Shapes

        private readonly Shape _shapeGutter = new Shape(GuttersWidth, GuttersDepth);

        private readonly Shape _shapeCurb = new Shape(
            new List<Vector3>()
            {
                new Vector3(0f, GuttersDepth, 0f),
                new Vector3(0f, SidewalkHeight, 0f),
                new Vector3(0f, SidewalkHeight, SidewalkHeight)
            },
            new List<Vector3>()
            {
                new Vector3(0, 0, -1),
                new Vector3(0, 1, -1).normalized,
                Vector3.up
            },
            new List<float>()
            {
                0f, 
                1f * SidewalkHeight,
                2f * SidewalkHeight
            },
            new List<bool>()
            {
                true, true
            }
        );
        
        private readonly Shape _shapeCurbClose = new Shape(
            new List<Vector3>()
            {
                new Vector3(0f, SidewalkHeight, 0f),
                new Vector3(0f, SidewalkHeight, SidewalkHeight * .5f),
                new Vector3(0f, 0, 0.05f)
            },
            new List<Vector3>()
            {
                Vector3.up,
                new Vector3(0, 1, 1).normalized,
                new Vector3(0, 0, 1)
            },
            new List<float>()
            {
                0f, 
                .1f,
                .1f + SidewalkHeight
            },
            new List<bool>()
            {
                true, true
            }
        );
            
        private readonly Shape _shapeCycleDivider = new Shape(
            new List<Vector3>()
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, CyclewayDividerWidth)
            },
            new List<Vector3>()
            {
                Vector3.up,
                Vector3.up
            },
            new List<float>()
            {
                0f, 
                1f * CyclewayDividerWidth,
            },
            new List<bool>()
            {
                true
            }
        );
            
        private readonly Shape  _shapeCycleway = new Shape(
            new List<Vector3>()
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, CyclewayWidth)
            },
            new List<Vector3>()
            {
                Vector3.up,
                Vector3.up
            },
            new List<float>()
            {
                0f, 
                1f * CyclewayWidth,
            },
            new List<bool>()
            {
                true
            }
        );
            
        private readonly Shape _shapeSidewalk = new Shape(
            new List<Vector3>()
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, SidewalkWidth)
            },
            new List<Vector3>()
            {
                Vector3.up,
                Vector3.up
            },
            new List<float>()
            {
                0f, 
                1f * SidewalkWidth,
            },
            new List<bool>()
            {
                true
            }
        );
        #endregion Shapes

        protected override IEnumerator Create(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch)
        {
            if(Visualizer.mode == Mode.MODE_2D)
                yield break;

            yield return Create3D(tile, creator, stopwatch);
        }
        
        private IEnumerator Create3D(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            
            foreach (var lc in tile.LaneCollections.Values)
            {
                if(lc.IsRemoved || !roadTypes.Contains(lc.Characteristics.Type))
                    continue;
                
                CreateForCollection(lc, creator);
                
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

                CreateForIntersection(inter, creator);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
        }
        
        private void CreateForIntersection(Intersection inter, Creator creator)
        {
            var meshGutter = new MeshHelper("Gutters");
            var meshCurb = new MeshHelper("Curb");
            var meshSidewalk = new MeshHelper("Sidewalk");
            var meshCycleway = new MeshHelper("CycleWay");
            var meshCycleDivider = new MeshHelper("CycleDivider");
            var meshUnderside = new MeshHelper("Underside");
            
            CreateForIntersectionFull(inter, meshGutter, meshCurb, meshSidewalk, meshCycleway, meshCycleDivider, meshUnderside);

            creator.AddMesh(meshGutter, guttersMat);
            creator.AddMesh(meshCurb, curbMat);
            creator.AddMesh(meshCycleDivider, cyclewayDividerMat);
            creator.AddMesh(meshCycleway, cyclewayMat);
            creator.AddMesh(meshSidewalk, defaultMaterial);
            creator.AddMesh(meshUnderside, curbMat);
        }

        private void CreateForIntersectionFull(Intersection inter, MeshHelper meshGutter, MeshHelper meshCurb, MeshHelper meshSidewalk, MeshHelper meshCycleway, MeshHelper meshCycleDivider, MeshHelper meshUnderside)
        {
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
                    
                    float chamferAngleStart;
                    float chamferAngleEnd;

                    if (last.IsIn)
                        ChamferAngleEnd(last.Lc, inter, out chamferAngleStart, out _);
                    else
                        ChamferAngleStart(last.Lc, inter, out _, out chamferAngleStart);

                    if (m.IsIn)
                        ChamferAngleEnd(m.Lc, inter, out _, out chamferAngleEnd);
                    else
                        ChamferAngleStart(m.Lc, inter, out chamferAngleEnd, out _);

                    var hasSidewalk = 
                        !m.Lc.Characteristics.SidewalkIsSeparate 
                        && (m.IsIn ? m.Lc.Characteristics.SidewalkLeft : m.Lc.Characteristics.SidewalkRight) != null
                        ||
                        !last.Lc.Characteristics.SidewalkIsSeparate 
                        && (last.IsIn ? last.Lc.Characteristics.SidewalkRight: last.Lc.Characteristics.SidewalkLeft) != null;
                    
                    var hasCycleway = 
                        !m.Lc.Characteristics.CyclewayIsShared 
                        && (m.IsIn ? m.Lc.Characteristics.CyclewayLeft : m.Lc.Characteristics.CyclewayRight) != null
                        ||
                        !last.Lc.Characteristics.CyclewayIsShared 
                        && (last.IsIn ? last.Lc.Characteristics.CyclewayRight: last.Lc.Characteristics.CyclewayLeft) != null;

                    for (var i = 0; i < lastPoints.Count - 1; i++)
                    {
                        var spline = new Spline(new[] {lastPoints[i], lastPoints[i + 1]}, inter.HeightOffset);
                        Generate(hasSidewalk, hasCycleway, spline, chamferAngleStart, chamferAngleEnd, meshGutter,
                            meshCurb, meshCycleDivider, meshCycleway, meshSidewalk, meshUnderside);
                    }

                    {
                        var spline = new Spline(new[] {lastPoints[lastPoints.Count - 1], m.Point}, inter.HeightOffset);
                        Generate(hasSidewalk, hasCycleway, spline, chamferAngleStart, chamferAngleEnd, meshGutter,
                            meshCurb, meshCycleDivider, meshCycleway, meshSidewalk, meshUnderside);
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
        }

        private void CreateForCollection(LaneCollection lc, Creator creator)
        {
            ChamferAngleStart(lc, lc.PrevIntersection, out var chamferAngleStartR, out var chamferAngleStartL);
            ChamferAngleEnd(lc, lc.NextIntersection, out var chamferAngleEndR, out var chamferAngleEndL);

            var splineR = new Spline(lc.OutlineRight);

            splineR.SetForwardStartAndEnd(
                (lc.GetPointLS0() - lc.GetPointRS0()).Rotate(-90f).normalized,
                (lc.GetPointLE0() - lc.GetPointRE0()).Rotate(-90f).normalized
            );
            
            var meshGutter = new MeshHelper("Gutters");
            var meshCurb = new MeshHelper("Curb");
            var meshSidewalk = new MeshHelper("Sidewalk");
            var meshCycleway = new MeshHelper("CycleWay");
            var meshCycleDivider = new MeshHelper("CycleDivider");
            var meshUnderside = new MeshHelper("Underside");
            
            var hasSidewalkR = lc.Characteristics.SidewalkRight != null && !lc.Characteristics.SidewalkIsSeparate;
            var hasCyclewayR = lc.Characteristics.CyclewayRight != null && !lc.Characteristics.CyclewayIsShared;
            
            Generate(hasSidewalkR, hasCyclewayR, splineR, chamferAngleStartR, chamferAngleEndR, meshGutter, meshCurb, meshCycleDivider, meshCycleway, meshSidewalk, meshUnderside);

            if (lc.OtherDirection == null)
            {
                var splineL = new Spline(lc.OutlineLeft.ToArray().Reverse().ToList());
                splineL.SetForwardStartAndEnd(
                    (lc.GetPointLE0() - lc.GetPointRE0()).Rotate(90f).normalized,
                    (lc.GetPointLS0() - lc.GetPointRS0()).Rotate(90f).normalized
                );
                
                var hasSidewalkL = lc.Characteristics.SidewalkLeft != null && !lc.Characteristics.SidewalkIsSeparate;
                var hasCyclewayL = lc.Characteristics.CyclewayLeft != null && !lc.Characteristics.CyclewayIsShared;
                
                Generate(hasSidewalkL, hasCyclewayL, splineL, chamferAngleEndL, chamferAngleStartL, meshGutter, meshCurb, meshCycleDivider, meshCycleway, meshSidewalk, meshUnderside);
            }
            
            creator.AddMesh(meshGutter, guttersMat);
            creator.AddMesh(meshCurb, curbMat);
            creator.AddMesh(meshCycleDivider, cyclewayDividerMat);
            creator.AddMesh(meshCycleway, cyclewayMat);
            creator.AddMesh(meshSidewalk, defaultMaterial);
            creator.AddMesh(meshUnderside, curbMat);
        }
        
        public static void ChamferAngleStart(
            LaneCollection lc, 
            Intersection inter, 
            out float chamferAngleStartR, 
            out float chamferAngleStartL
        )
        {
            chamferAngleStartR = float.NaN;
            chamferAngleStartL = float.NaN;
            
            if (inter == null)
                return;
            
            var rm = inter.GetRoadMapping();
            var cnt = rm.Count;
            if(cnt == 0)
                return;
            
            for (var i = 0; i < rm.Count; i++)
            {
                var r = rm[i];
                if (r.Lc != lc)
                    continue;
                
                {
                    var a = r.Point;
                    var b = rm[(i - 1 + cnt) % cnt].Point;
                
                    var angle = Vector2.SignedAngle(b - a, lc.GetPointRS1() - lc.GetPointRS0()) * .5f;
                    chamferAngleStartR = angle < 0 ? -(angle + 90) : 90 - angle;
                }
                {
                    var otherOffset = lc.OtherDirection == null ? 0 : 1;
                    
                    var a = rm[(i + 1 + otherOffset + cnt) % cnt].Point;
                    var b = rm[(i + 2 + otherOffset + cnt) % cnt].Point;
                    
                    var angle = Vector2.SignedAngle(b - a, lc.GetPointLS1() - lc.GetPointLS0()) * -.5f;
                    chamferAngleStartL = angle < 0 ? -(angle + 90) : 90 - angle;
                }
                break;
            }
        }
        
        public static void ChamferAngleEnd(
            LaneCollection lc, 
            Intersection inter, 
            out float chamferAngleEndR, 
            out float chamferAngleEndL
        )
        {
            chamferAngleEndR = float.NaN;
            chamferAngleEndL = float.NaN;

            if (inter == null)
                return;
            
            var rm = inter.GetRoadMapping();
            var cnt = rm.Count;
            if(cnt == 0)
                return;
            
            for (var i = 0; i < rm.Count; i++)
            {
                var r = rm[i];
                if (r.Lc != lc)
                    continue;
                
                {
                    var a = rm[(i + 1) % cnt].Point;
                    var b = rm[(i + 2) % cnt].Point;
                
                    var angle = Vector2.SignedAngle(b - a, lc.GetPointRE1() - lc.GetPointRE0()) * -.5f;
                    chamferAngleEndR = angle < 0 ? -(angle + 90) : 90 - angle;
                }
                {
                    var otherOffset = lc.OtherDirection == null ? 0 : -1;
                    
                    var a = rm[(i + 0 + otherOffset + cnt) % cnt].Point;
                    var b = rm[(i - 1 + otherOffset + cnt) % cnt].Point;
                    
                    var angle = Vector2.SignedAngle(b - a, lc.GetPointLE1() - lc.GetPointLE0()) * .5f;
                    chamferAngleEndL = angle < 0 ? -(angle + 90) : 90 - angle;
                }
                break;
            }
        }

        private void Generate(
            bool hasSidewalk, bool hasCycleway, Spline spline, 
            float chamferAngleStart, float chamferAngleEnd, 
            MeshHelper meshGutter, MeshHelper meshCurb, MeshHelper meshCycleDivider, 
            MeshHelper meshCycleway, MeshHelper meshSidewalk, MeshHelper underSide
        ) {
            spline.ExtrudeShape(
                meshGutter,
                _shapeGutter,
                chamferAngleStart,
                chamferAngleEnd
            );

            var offset = new Vector3(0f, 0f, GuttersWidth);

            if (hasSidewalk || hasCycleway)
            {
                spline.ExtrudeShape(
                    meshCurb,
                    _shapeCurb,
                    chamferAngleStart,
                    chamferAngleEnd,
                    offset
                );

                offset.y += SidewalkHeight;
                offset.z += SidewalkHeight;

                if (hasCycleway)
                {
                    spline.ExtrudeShape(
                        meshCycleDivider,
                        _shapeCycleDivider,
                        chamferAngleStart,
                        chamferAngleEnd,
                        offset
                    );
                    offset.z += CyclewayDividerWidth;

                    spline.ExtrudeShape(
                        meshCycleway,
                        _shapeCycleway,
                        chamferAngleStart,
                        chamferAngleEnd,
                        offset
                    );
                    offset.z += CyclewayWidth;
                }

                if (hasCycleway && hasSidewalk)
                {
                    spline.ExtrudeShape(
                        meshCycleDivider,
                        _shapeCycleDivider,
                        chamferAngleStart,
                        chamferAngleEnd,
                        offset
                    );
                    offset.z += CyclewayDividerWidth;
                }

                if (hasSidewalk)
                {
                    spline.ExtrudeShape(
                        meshSidewalk,
                        _shapeSidewalk,
                        chamferAngleStart,
                        chamferAngleEnd,
                        offset
                    );
                    offset.z += SidewalkWidth;
                }

                offset.y = 0;
                spline.ExtrudeShape(
                    meshCurb,
                    _shapeCurbClose,
                    chamferAngleStart,
                    chamferAngleEnd,
                    offset
                );
                offset.z += SidewalkHeight * .5f;
            }
            
            var underSideShape = new Shape(
                new []
                {
                    new Vector3(0, offset.y, offset.z),
                    new Vector3(0, -.05f, offset.z),
                    
                    new Vector3(0, -.05f, offset.z),
                    new Vector3(0, -.1f, offset.z + .5f),
                    
                    new Vector3(0, -.1f, offset.z + .5f),
                    new Vector3(0, -.5f, offset.z + .5f),
                    
                    new Vector3(0, -.5f, offset.z + .5f),
                    new Vector3(0, -.5f, 0),
                    
                    new Vector3(0, -.5f, 0),
                    new Vector3(0, 0, 0),
                },
                new List<Vector3>
                {
                    new Vector3(0,0,-1),
                    new Vector3(0,0,-1),
                    
                    new Vector3(0,1,0),
                    new Vector3(0,1,0),
                    
                    new Vector3(0,0,-1),
                    new Vector3(0,0,-1),
                    
                    new Vector3(0,-1,0),
                    new Vector3(0,-1,0),
                    
                    new Vector3(0,0,1),
                    new Vector3(0,0,1),
                },
                new List<float>
                {
                    offset.y + .05f, 0,
                    0, .5f,
                    0, .4f,
                    0, offset.z + .5f,
                    0, .5f
                },
                new List<bool>
                {
                    true, false,
                    true, false,
                    true, false,
                    true, false,
                    true,
                }
            );
            
            spline.ExtrudeShape(
                underSide,
                underSideShape,
                chamferAngleStart,
                chamferAngleEnd
            );
        }
        
    }
}