// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using OsmVisualizer.Data;
// using OsmVisualizer.Data.Types;
// using OsmVisualizer.Data.Utils;
// using OsmVisualizer.Math;
// using UnityEngine;
// using Spline = OsmVisualizer.Data.Types.Spline;
//
// namespace OsmVisualizer.Mesh
// {
//     public class SideWalksMeshBuilder : MeshBuilder
//     {
//         protected readonly string[] types;
//         protected readonly bool combinedMesh;
//         
//         #region Materials
//         protected readonly Material matDefault;
//         protected readonly Material matCycleway;
//         protected readonly Material matCyclewayDivider;
//         protected readonly Material matGutters;
//         protected readonly Material matCurb;
//         #endregion Materials
//         
//         #region Heights and Widths
//         public const float SidewalkHeight = .1f;
//         public const float SidewalkWidth = 1.5f;
//         public const float CyclewayDividerWidth = .2f;
//         public const float CyclewayWidth = 1.5f;
//         public const float GuttersWidth = .3f;
//         public const float GuttersDepth = -.02f;
//         #endregion Heights and Widths
//         
//         #region Shapes
//
//         private readonly Shape _shapeGutter = new Shape(GuttersWidth, GuttersDepth);
//
//         private readonly Shape _shapeCurb = new Shape(
//             new List<Vector3>()
//             {
//                 new Vector3(0f, GuttersDepth, 0f),
//                 new Vector3(0f, SidewalkHeight, 0f),
//                 new Vector3(0f, SidewalkHeight, SidewalkHeight)
//             },
//             new List<Vector3>()
//             {
//                 new Vector3(0, 0, -1),
//                 new Vector3(0, 1, -1).normalized,
//                 Vector3.up
//             },
//             new List<float>()
//             {
//                 0f, 
//                 1f * SidewalkHeight,
//                 2f * SidewalkHeight
//             },
//             new List<bool>()
//             {
//                 true, true
//             }
//         );
//         
//         private readonly Shape _shapeCurbClose = new Shape(
//             new List<Vector3>()
//             {
//                 new Vector3(0f, SidewalkHeight, 0f),
//                 new Vector3(0f, SidewalkHeight, 0.05f),
//                 new Vector3(0f, 0, 0.05f)
//             },
//             new List<Vector3>()
//             {
//                 Vector3.up,
//                 new Vector3(0, 1, 1).normalized,
//                 new Vector3(0, 0, 1)
//             },
//             new List<float>()
//             {
//                 0f, 
//                 .1f,
//                 .1f + SidewalkHeight
//             },
//             new List<bool>()
//             {
//                 true, true
//             }
//         );
//             
//         private readonly Shape _shapeCycleDivider = new Shape(
//             new List<Vector3>()
//             {
//                 new Vector3(0f, 0f, 0f),
//                 new Vector3(0f, 0f, CyclewayDividerWidth)
//             },
//             new List<Vector3>()
//             {
//                 Vector3.up,
//                 Vector3.up
//             },
//             new List<float>()
//             {
//                 0f, 
//                 1f * CyclewayDividerWidth,
//             },
//             new List<bool>()
//             {
//                 true
//             }
//         );
//             
//         private readonly Shape  _shapeCycleway = new Shape(
//             new List<Vector3>()
//             {
//                 new Vector3(0f, 0f, 0f),
//                 new Vector3(0f, 0f, CyclewayWidth)
//             },
//             new List<Vector3>()
//             {
//                 Vector3.up,
//                 Vector3.up
//             },
//             new List<float>()
//             {
//                 0f, 
//                 1f * CyclewayWidth,
//             },
//             new List<bool>()
//             {
//                 true
//             }
//         );
//             
//         private readonly Shape _shapeSidewalk = new Shape(
//             new List<Vector3>()
//             {
//                 new Vector3(0f, 0f, 0f),
//                 new Vector3(0f, 0f, SidewalkWidth)
//             },
//             new List<Vector3>()
//             {
//                 Vector3.up,
//                 Vector3.up
//             },
//             new List<float>()
//             {
//                 0f, 
//                 1f * SidewalkWidth,
//             },
//             new List<bool>()
//             {
//                 true
//             }
//         );
//         #endregion Shapes
//
//         public SideWalksMeshBuilder(AbstractSettingsProvider settings, string[] types,
//             Material matDefault, 
//             Material matCycleway, 
//             Material matCyclewayDivider,
//             Material matGutters, 
//             Material matCurb, 
//             bool combinedMesh = false
//         ) : base(settings)
//         {
//             this.types = types;
//             this.matDefault = matDefault;
//             this.matCycleway = matCycleway;
//             this.matCyclewayDivider = matCyclewayDivider;
//             this.matGutters = matGutters;
//             this.matCurb = matCurb;
//             this.combinedMesh = combinedMesh;
//         }
//
//         public class Creator : MonoBehaviour
//         {
//             private GameObject _parent;
//             private MultiMesh _mesh; 
//
//             public void SetParent(MapTile tile, Material defaultMaterial, bool combinedMesh)
//             {
//                 _parent = new GameObject("SideWalks");
//                 _parent.transform.parent = tile.transform;
//                 _mesh = new MultiMesh(defaultMaterial, combinedMesh);
//             }
//
//             public void Destroy()
//             {
//                 Destroy(_parent);
//                 Destroy(this);
//             }
//
//             public void AddMesh(MeshHelper mesh, Material mat = null)
//             {
//                 _mesh.Add(mesh, mat);
//             }
//
//             public void CreateMesh()
//             {
//                 _mesh.AddToGameObject(_parent.transform, "SideWalks");
//             }
//         }
//
//         public override IEnumerator Destroy(MapData data, MapTile tile)
//         {
//             if (tile.gameObject.TryGetComponent<Creator>(out var creator))
//             {
//                 creator.Destroy();
//             }
//             yield return null;
//         }
//
//         public override IEnumerator Create(MapData data, MapTile tile)
//         {
//             var creator = tile.gameObject.AddComponent<Creator>();
//             creator.SetParent(tile, matDefault, combinedMesh);
//
//             // var done = new List<MapData.LaneId>();
//
//             var i = 1;
//             foreach (var lc in tile.LaneCollections.Values)
//             {
//                 // if(done.Contains(lc.Id))
//                 //     continue;
//                 
//                 // done.Add(lc.Id);
//                 // if(lc.OtherDirection != null)
//                 //     done.Add(lc.OtherDirection.Id);
//                 
//                 if(!types.Contains(lc.Characteristics.Type))
//                     continue;
//                 
//                 CreateForCollection(lc, creator);
//                 
//                 if (i++ % 500 == 0)
//                     yield return null;
//             }
//
//             foreach (var inter in tile.Intersections.Values)
//             {
//                 if(inter == null)
//                     continue;
//
//                 CreateForIntersection(inter, creator);
//             }
//             
//             creator.CreateMesh();
//             
//             yield return null;
//         }
//
//         private void CreateForIntersection(Intersection inter, Creator creator)
//         {
//             var meshGutter = new MeshHelper("Gutters");
//             var meshCurb = new MeshHelper("Curb");
//             var meshSidewalk = new MeshHelper("Sidewalk");
//             var meshCycleway = new MeshHelper("CycleWay");
//             var meshCycleDivider = new MeshHelper("CycleDivider");
//
//             if (inter.IsRoadRoadConnection)
//             {
//                 CreateForIntersectionRRC(inter, meshGutter, meshCurb, meshSidewalk, meshCycleway, meshCycleDivider);
//             }
//             else
//             {
//                 CreateForIntersectionFull(inter, meshGutter, meshCurb, meshSidewalk, meshCycleway, meshCycleDivider);
//             }
//             
//             creator.AddMesh(meshGutter, matGutters);
//             creator.AddMesh(meshCurb, matCurb);
//             creator.AddMesh(meshCycleDivider, matCyclewayDivider);
//             creator.AddMesh(meshCycleway, matCycleway);
//             creator.AddMesh(meshSidewalk);
//         }
//
//         private void CreateForIntersectionRRC(Intersection inter, MeshHelper meshGutter, MeshHelper meshCurb, MeshHelper meshSidewalk, MeshHelper meshCycleway, MeshHelper meshCycleDivider)
//         {
//             if (inter.LanesIn.Count < 1 || inter.LanesOut.Count < 1)
//                 return;
//
//             var lcIn = inter.LanesIn[0];
//             var inHasOtherDir = lcIn.OtherDirection != null;
//             
//             var lcOut = inHasOtherDir && inter.LanesOut[0] == lcIn.OtherDirection ? inter.LanesOut[1] : inter.LanesOut[0];
//             var outHasOtherDir = lcOut.OtherDirection != null;
//             
//             ChamferAngleStart(lcOut, inter, out var chamferAngleEndR, out var chamferAngleEndL);
//             ChamferAngleEnd(lcIn, inter, out var chamferAngleStartR, out var chamferAngleStartL);
//
//             if (inHasOtherDir)
//             {
//                 ChamferAngleStart(lcIn.OtherDirection, inter, out chamferAngleStartL, out _);
//             }
//             if (outHasOtherDir)
//             {
//                 ChamferAngleEnd(lcOut.OtherDirection, inter, out chamferAngleEndL, out _);
//             }
//             
//             var hasSidewalkR = lcIn.Characteristics.SidewalkRight != null && !lcIn.Characteristics.SidewalkIsSeparate
//                             && lcOut.Characteristics.SidewalkRight != null && !lcOut.Characteristics.SidewalkIsSeparate;
//             
//             var hasCyclewayR = lcIn.Characteristics.CyclewayRight != null && !lcIn.Characteristics.CyclewayIsShared
//                             && lcOut.Characteristics.CyclewayRight != null && !lcOut.Characteristics.CyclewayIsShared;
//             
//             var hasSidewalkL = lcIn.Characteristics.SidewalkLeft != null && !lcIn.Characteristics.SidewalkIsSeparate 
//                             && lcOut.Characteristics.SidewalkLeft != null && !lcOut.Characteristics.SidewalkIsSeparate;
//             
//             var hasCyclewayL = lcIn.Characteristics.CyclewayLeft != null && !lcIn.Characteristics.CyclewayIsShared
//                             && lcOut.Characteristics.CyclewayLeft != null && !lcOut.Characteristics.CyclewayIsShared;
//             
//             var splineR = new Spline(new[] {lcIn.GetPointRE0(), lcOut.GetPointRS0()}, inter.HeightOffset);
//             var splineL = new Spline(new[]
//                 {
//                     outHasOtherDir ? lcOut.OtherDirection.GetPointRE0() : lcOut.GetPointLS0(), 
//                     inHasOtherDir ? lcIn.OtherDirection.GetPointRS0() : lcIn.GetPointLE0()
//                 }, 
//                 inter.HeightOffset
//             );
//             
//             Generate(hasSidewalkR, hasCyclewayR, splineR, chamferAngleStartR, chamferAngleEndR, meshGutter, meshCurb, meshCycleDivider, meshCycleway, meshSidewalk);
//             Generate(hasSidewalkL, hasCyclewayL, splineL, chamferAngleEndL, chamferAngleStartL, meshGutter, meshCurb, meshCycleDivider, meshCycleway, meshSidewalk);
//         }
//
//         private void CreateForIntersectionFull(Intersection inter, MeshHelper meshGutter, MeshHelper meshCurb, MeshHelper meshSidewalk, MeshHelper meshCycleway, MeshHelper meshCycleDivider)
//         {
//             var last = inter.OutlinePoints[inter.OutlinePoints.Count - 1];
//             var lastLc = inter.CollectionByOrientation(inter.OutlinePoints.Count/2 - 1);
//
//             var isLastOut = inter.IsNthLaneCollectionDirIn(inter.OutlinePoints.Count/2 - 1);
//
//             for (var i = 0; i < inter.OutlinePoints.Count/2; i++)
//             {
//                 var lcId = inter.IndexByOrientation(i);
//                 var lc = inter.GetNthLaneCollection(lcId);
//                 var isOut = !inter.IsNthLaneCollectionDirIn(lcId);
//                 
//                 if (isOut || lc.OtherDirection == null)
//                 {
//                     var p = inter.OutlinePoints[i*2];
//
//                     var spline = new Spline(new[] {last, p}, inter.HeightOffset);
//
//                     float chamferAngleStart;
//                     float chamferAngleEnd;
//                     
//                     if (lc.OtherDirection == null && !isOut)
//                     {
//                         ChamferAngleEnd(lc, inter, out _, out chamferAngleEnd);
//                     }
//                     else
//                     {
//                         ChamferAngleStart(lc, inter, out chamferAngleEnd, out _);
//                     }
//
//                     if (lastLc.OtherDirection == null && isLastOut)
//                     {
//                         ChamferAngleStart(lastLc, inter, out _, out chamferAngleStart);
//                     }
//                     else
//                     {
//                         ChamferAngleEnd(lastLc, inter, out chamferAngleStart, out _);
//                     }
//
//                     var hasSidewalk = lc.Characteristics.SidewalkRight != null || lastLc.Characteristics.SidewalkRight != null;
//                     var hasCycleway = lc.Characteristics.CyclewayRight != null || lastLc.Characteristics.CyclewayRight != null;
//                     
//                     Generate(hasSidewalk, hasCycleway, spline, chamferAngleStart, chamferAngleEnd, meshGutter, meshCurb, meshCycleDivider, meshCycleway, meshSidewalk);
//                 }
//                 
//                 last = inter.OutlinePoints[i*2 + 1];
//                 isLastOut = isOut;
//                 lastLc = lc;
//             }
//         }
//
//         private void CreateForCollection(LaneCollection lc, Creator creator)
//         {
//             ChamferAngleStart(lc, lc.PrevIntersection, out var chamferAngleStartR, out var chamferAngleStartL);
//             ChamferAngleEnd(lc, lc.NextIntersection, out var chamferAngleEndR, out var chamferAngleEndL);
//
//             var splineR = new Spline(lc.OutlineRight);
//
//             splineR.SetForwardStartAndEnd(
//                 (lc.GetPointLS0() - lc.GetPointRS0()).rotate(-90f).normalized,
//                 (lc.GetPointLE0() - lc.GetPointRE0()).rotate(-90f).normalized
//             );
//             
//             var meshGutter = new MeshHelper("Gutters");
//             var meshCurb = new MeshHelper("Curb");
//             var meshSidewalk = new MeshHelper("Sidewalk");
//             var meshCycleway = new MeshHelper("CycleWay");
//             var meshCycleDivider = new MeshHelper("CycleDivider");
//             
//             var hasSidewalkR = lc.Characteristics.SidewalkRight != null && !lc.Characteristics.SidewalkIsSeparate;
//             var hasCyclewayR = lc.Characteristics.CyclewayRight != null && !lc.Characteristics.CyclewayIsShared;
//             
//             Generate(hasSidewalkR, hasCyclewayR, splineR, chamferAngleStartR, chamferAngleEndR, meshGutter, meshCurb, meshCycleDivider, meshCycleway, meshSidewalk);
//
//             if (lc.OtherDirection == null)
//             {
//                 var splineL = new Spline(lc.OutlineLeft.ToArray().Reverse().ToList());
//                 splineL.SetForwardStartAndEnd(
//                     (lc.GetPointLE0() - lc.GetPointRE0()).rotate(90f).normalized,
//                     (lc.GetPointLS0() - lc.GetPointRS0()).rotate(90f).normalized
//                 );
//                 
//                 var hasSidewalkL = lc.Characteristics.SidewalkLeft != null && !lc.Characteristics.SidewalkIsSeparate;
//                 var hasCyclewayL = lc.Characteristics.CyclewayLeft != null && !lc.Characteristics.CyclewayIsShared;
//                 
//                 Generate(hasSidewalkL, hasCyclewayL, splineL, chamferAngleEndL, chamferAngleStartL, meshGutter, meshCurb, meshCycleDivider, meshCycleway, meshSidewalk);
//             }
//             
//             creator.AddMesh(meshGutter, matGutters);
//             creator.AddMesh(meshCurb, matCurb);
//             creator.AddMesh(meshCycleDivider, matCyclewayDivider);
//             creator.AddMesh(meshCycleway, matCycleway);
//             creator.AddMesh(meshSidewalk);
//         }
//         
//         public static void ChamferAngleStart(
//             LaneCollection lc, 
//             Intersection inter, 
//             out float chamferAngleStartR, 
//             out float chamferAngleStartL
//         )
//         {
//             chamferAngleStartR = float.NaN;
//             chamferAngleStartL = float.NaN;
//
//             if (inter == null)
//                 return;
//             
//             var index = inter.GetLaneCollectionIndex(lc);
//             var oIndex = inter.OrientationNumberByCount(index);
//             var cnt = inter.OutlinePoints.Count;
//
//             {
//                 var a = inter.OutlinePoints[(oIndex * 2 - 0 + cnt) % cnt];
//                 var b = inter.OutlinePoints[(oIndex * 2 - 1 + cnt) % cnt];
//
//                 var angle = Vector2.SignedAngle(b - a, lc.GetPointRS1() - lc.GetPointRS0()) * .5f;
//                 chamferAngleStartR = angle < 0 ? -(angle + 90) : 90 - angle;
//             }
//
//             {
//                 var otherOffset = lc.OtherDirection == null ? 0 : 2;
//                 
//                 var a = inter.OutlinePoints[(oIndex * 2 + 1 + otherOffset + cnt) % cnt];
//                 var b = inter.OutlinePoints[(oIndex * 2 + 2 + otherOffset + cnt) % cnt];
//
//                 var angle = Vector2.SignedAngle(b - a, lc.GetPointLS1() - lc.GetPointLS0()) * -.5f;
//                 chamferAngleStartL = angle < 0 ? -(angle + 90) : 90 - angle;
//             }
//         }
//         
//         public static void ChamferAngleEnd(
//             LaneCollection lc, 
//             Intersection inter, 
//             out float chamferAngleEndR, 
//             out float chamferAngleEndL
//         )
//         {
//             chamferAngleEndR = float.NaN;
//             chamferAngleEndL = float.NaN;
//
//             if (inter == null)
//                 return;
//             
//             var index = inter.GetLaneCollectionIndex(lc);
//             var oIndex = inter.OrientationNumberByCount(index);
//             var cnt = inter.OutlinePoints.Count;
//
//             {
//                 var a = inter.OutlinePoints[oIndex * 2 + 1];
//                 var b = inter.OutlinePoints[(oIndex * 2 + 2) % cnt];
//
//                 var angle = Vector2.SignedAngle(b - a, lc.GetPointRE1() - lc.GetPointRE0()) * -.5f;
//                 chamferAngleEndR = angle < 0 ? -(angle + 90) : 90 - angle;
//             }
//             {
//                 var otherOffset = lc.OtherDirection == null ? 0 : -2;
//                 
//                 var a = inter.OutlinePoints[(oIndex * 2 - 0 + otherOffset + cnt) % cnt];
//                 var b = inter.OutlinePoints[(oIndex * 2 - 1 + otherOffset + cnt) % cnt];
//
//                 var angle = Vector2.SignedAngle(b - a, lc.GetPointLE1() - lc.GetPointLE0()) * .5f;
//                 chamferAngleEndL = angle < 0 ? -(angle + 90) : 90 - angle;
//             }
//         }
//
//         private void Generate(
//             bool hasSidewalk, bool hasCycleway, Spline spline, 
//             float chamferAngleStart, float chamferAngleEnd, 
//             MeshHelper meshGutter, MeshHelper meshCurb, MeshHelper meshCycleDivider, 
//             MeshHelper meshCycleway, MeshHelper meshSidewalk
//         ) {
//             spline.ExtrudeShape(
//                 meshGutter,
//                 _shapeGutter,
//                 chamferAngleStart,
//                 chamferAngleEnd
//             );
//
//             if (!hasSidewalk && !hasCycleway) return;
//             
//             var offset = new Vector3(0f, 0f, GuttersWidth);
//
//             spline.ExtrudeShape(
//                 meshCurb,
//                 _shapeCurb,
//                 chamferAngleStart,
//                 chamferAngleEnd,
//                 offset
//             );
//
//             offset.y += SidewalkHeight;
//             offset.z += SidewalkHeight;
//
//             if (hasCycleway)
//             {
//                 spline.ExtrudeShape(
//                     meshCycleDivider,
//                     _shapeCycleDivider,
//                     chamferAngleStart,
//                     chamferAngleEnd,
//                     offset
//                 );
//                 offset.z += CyclewayDividerWidth;
//
//                 spline.ExtrudeShape(
//                     meshCycleway,
//                     _shapeCycleway,
//                     chamferAngleStart,
//                     chamferAngleEnd,
//                     offset
//                 );
//                 offset.z += CyclewayWidth;
//             }
//
//             if (hasCycleway && hasSidewalk)
//             {
//                 spline.ExtrudeShape(
//                     meshCycleDivider,
//                     _shapeCycleDivider,
//                     chamferAngleStart,
//                     chamferAngleEnd,
//                     offset
//                 );
//                 offset.z += CyclewayDividerWidth;
//             }
//
//             if (hasSidewalk)
//             {
//                 spline.ExtrudeShape(
//                     meshSidewalk,
//                     _shapeSidewalk,
//                     chamferAngleStart,
//                     chamferAngleEnd,
//                     offset
//                 );
//                 offset.z += SidewalkWidth;
//             }
//             
//             offset.y = 0;
//             spline.ExtrudeShape(
//                 meshCurb,
//                 _shapeCurbClose,
//                 chamferAngleStart,
//                 chamferAngleEnd,
//                 offset
//             );
//
//         }
//     }
//
// }