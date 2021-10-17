using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Request;
using OsmVisualizer.Data.Utils;
using UnityEngine;


namespace OsmVisualizer.Data.Provider
{
    public class GenerateIntersections : Provider
    {

        private bool _useMerging;
        
        private readonly Dictionary<long, List<long>> _customMerge;
        
        public GenerateIntersections(AbstractSettingsProvider settings, Dictionary<long, List<long>> customMerge, bool useMerging = true) : base(settings, MapTile.InitStep.Intersection)
        {
            _useMerging = useMerging;
            _customMerge = customMerge;
        }


        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            stopwatch.Stop();
            while (!tile.HasAllNeighbours) yield return null;

            if(!(tile.sp is SettingsPlainLevelProvider))
                while (tile.Neighbours().Any(n => n.InitProgress <= MapTile.InitStep.Lanes))
                    yield return null;
            
            stopwatch.Start();
            
            var startTime = stopwatch.ElapsedMilliseconds;

            var ignoreNodes = new List<long>();
            var trafficLights = new List<long>();
            
            foreach (var el in request.elements)
            {
                if(el.GeometryType != GeometryType.POINT || el.type != "way" || el.GetProperty("highway") != "traffic_signals")
                    continue;
                
                trafficLights.Add(el.nodes[0]);
            }
            
            foreach (var kv in tile.IntersectionPoints)
            {
                var node = kv.Key;
                var pos = kv.Value;
                
                if (tile.NeighbourHasIntersection(node, tile.NeighboursForPoint(pos)))
                {
                    ignoreNodes.Add(node);
                    continue;
                }
            
                CombineLanes(node, tile, ignoreNodes);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            foreach (var node in tile.IntersectionPoints.Keys)
            {
                if(ignoreNodes.Contains(node))
                    continue;
                
                MakeIntersection(node, tile, trafficLights.Contains(node));
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            if(_useMerging)
                foreach (var kv in tile.Intersections)
                {
                    var inter = kv.Value;
                    
                    if(inter == null) continue;
                
                    CombineIntersections(inter, tile);
                
                    if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                        continue;
                    
                    stopwatch.Stop();
                    yield return null;
                    stopwatch.Start();
                    startTime = stopwatch.ElapsedMilliseconds;
                }
            
            if(_customMerge.Count > 0)
                foreach (var kv in tile.Intersections)
                {
                    var inter = kv.Value;
                    
                    if(inter == null) continue;

                    if (!_customMerge.ContainsKey(kv.Key))
                        continue;

                    CombineIntersectionCustom(inter, _customMerge[kv.Key], tile);
                    
                    if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                        continue;
                    
                    stopwatch.Stop();
                    yield return null;
                    stopwatch.Start();
                    startTime = stopwatch.ElapsedMilliseconds;
                }
            
            
            
            foreach (var inter in tile.Intersections.Values)
            {
                if(inter == null) continue;
            
                inter.SetLaneIntersectionBounds();
            
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            stopwatch.Stop();
        }

        private static void CombineIntersectionCustom(Intersection inter, List<long> others, MapTile tile)
        {
            if (others.Count == 0) return;
            
            foreach (var lc in inter.LanesOut)
            {
                var o = lc.NextIntersection;
                if (o == null || !others.Contains(o.Node)) continue;
                        
                MergeIntersections(inter, o, lc, tile);
            }
            
            CleanupMergedIntersection(inter, tile);
        }

        private static void CombineIntersections(Intersection inter, MapTile tile, int iterations = 0)
        {
            if (inter.IsRoadRoadConnection)
                return;
            
            if(iterations == 20)
            {
                Debug.LogWarning("merge: force stop (after 20 iterations)");
                return;
            }
            
            var onewayCount = inter.LanesIn.Count(lc => lc.OtherDirection == null)
                              + inter.LanesOut.Count(lc => lc.OtherDirection == null);
            
            // if (onewayCount == 0 || inter.LanesIn.Count + inter.LanesOut.Count < 4)
            //     return;

            var interRadius = inter.Radius + inter.RoadWidthNoScale() * inter.MaxAngleMultiplier();
            
            foreach (var lc in inter.LanesOut)
            {
                var nextI = lc?.NextIntersection;
                
                if(nextI == null 
                   || nextI == inter 
                   || nextI.IsRoadRoadConnection
                   || onewayCount == 1 && lc.OtherDirection == null)
                    continue;
                    
                var dist = Vector2.Distance(nextI.Center, inter.Center);
                var nextRadius = nextI.Radius + nextI.RoadWidthNoScale() * nextI.MaxAngleMultiplier();
                
                if(dist > 25f
                   || inter.IsRoadRoadConnection && nextI.IsRoadRoadConnection 
                   || inter.hasSingleLaneType && nextI.hasSingleLaneType && inter.mainLaneType != nextI.mainLaneType
                   || dist > interRadius + nextRadius
                   || nextI == inter 
                   || !tile.Intersections.ContainsKey(nextI.Node))
                    continue;

                MergeIntersections(inter, nextI, lc, tile);
                    
                CombineIntersections(inter, tile, iterations + 1);
                break;
            }
            
            CleanupMergedIntersection(inter, tile);
        }

        private static void MergeIntersections(Intersection inter, Intersection other, LaneCollection via, MapTile tile)
        {
            inter.Merge(other, via);
                
            via.IsRemoved = true;
            if(via.OtherDirection is {} otherDir)
                otherDir.IsRemoved = true;
                    
            tile.Intersections[other.Node] = null;
        }

        private static void CleanupMergedIntersection(Intersection inter, MapTile tile)
        {
            var internalLanes = inter.RemoveInternal();
            
            foreach (var lc in internalLanes)
            {
                lc.IsRemoved = true;
                if (lc.OtherDirection is { } other)
                    other.IsRemoved = true;
            }
        }

        private static void CombineLanes(long node, MapTile tile, List<long> ignoreNodes)
        {
            var inC = new List<LaneCollection>();
            var outC = new List<LaneCollection>();

            foreach (var kv in tile.LaneCollections)
            {
                if (kv.Value == null || kv.Value.Id.Type != MapData.LaneType.HIGHWAY)
                    continue;

                if(kv.Key.EndNode == node)
                    inC.Add(kv.Value);
                
                if(kv.Key.StartNode == node)
                    outC.Add(kv.Value);
            }
            
            var maxIn = inC.Count;
            var maxOut = outC.Count;

            if (!Intersection.IsNodeRoadRoadConnection(inC, outC))
                return;
            
            if (maxIn == 1 && maxOut == 1)
            {
                var i = inC[0];
                var o = outC[0];
                
                if (i.IsDummy == o.IsDummy
                    && i.Lanes.Length == o.Lanes.Length
                    && i.Characteristics.GetHashCode() == o.Characteristics.GetHashCode() 
                    && i.OtherDirection != o 
                    && Mathf.Abs(Vector3.SignedAngle(
                        i.OutlineRight.Forward[i.OutlineRight.Forward.Count - 1],
                        o.OutlineRight.Forward[0],
                        Vector3.up
                    )) < 45f
                )
                {
                    var outlineLeft = i.OutlineLeft.ToList();
                    outlineLeft.AddRange(o.OutlineLeft.Skip(1));
                    var outlineRight = i.OutlineRight.ToList();
                    outlineRight.AddRange(o.OutlineRight.Skip(1));
                    var nodes = i.Nodes.ToList();
                    nodes.AddRange(o.Nodes.Skip(1));
                    var points = i.Points.ToList();
                    points.AddRange(o.Points);
                    
                    var lanes = new List<Vector2[]>(i.Lanes.Length);
                    for (var idx = 0; idx < i.Lanes.Length; idx++)
                    {
                        var ps = i.Lanes[idx].Points;
                        ps.AddRange(o.Lanes[idx].Points.Skip(1));
                        lanes.Add(ps.ToArray());
                    }

                    var id = new MapData.LaneId(i.Id.StartNode, o.Id.EndNode);

                    if (tile.LaneCollections.ContainsKey(id))
                        return;

                    var n = new LaneCollection(
                        i.Characteristics,
                        lanes,
                        o.Lanes.Select(l => l.Directions).ToList(),
                        new Types.Spline(outlineLeft),
                        new Types.Spline(outlineRight),
                        nodes,
                        points,
                        false, // i.IsDummy,
                        id
                    );
                    
                    tile.LaneCollections.Remove(i.Id);
                    tile.LaneCollections.Remove(o.Id);
                    
                    tile.LaneCollections.Add(id, n);
                    
                    ignoreNodes.Add(node);
                    return;
                }
                // else if(maxIn == 2 && maxOut == 2)
                // {
                //     inC[0].Characteristics
                //
                //
                //
                //
                //
                // }

            }
        }

        private static void MakeIntersection(long node, MapTile tile, bool hasTrafficLights)
        {
            var inC = new List<LaneCollection>();
            var outC = new List<LaneCollection>();

            foreach (var kv in tile.LaneCollections)
            {
                if (kv.Value == null || kv.Value.Id.Type != MapData.LaneType.HIGHWAY)
                    continue;

                if(kv.Key.EndNode == node)
                    inC.Add(kv.Value);
                
                if(kv.Key.StartNode == node)
                    outC.Add(kv.Value);
            }
            
                        
            if(node == 99812578L)
                Debug.Log($"Intersection 99812578 {inC.Count} {outC.Count}");


            // @todo can maybe removed
            if (inC.Count > 0 || outC.Count > 0)
            {
                foreach (var otherTile in tile.Neighbours())
                {
                    foreach(var kv in otherTile.LaneCollections)
                    {
                        if (kv.Value == null || kv.Value.Id.Type != MapData.LaneType.HIGHWAY)
                            continue;

                        if (kv.Key.EndNode == node && (
                                inC.Count == 0 
                                || inC.All(lane => lane.Nodes[lane.Nodes.Count - 2] != kv.Value.Nodes[kv.Value.Nodes.Count - 2])
                            )
                        )
                        {
                            inC.Add(kv.Value);
                        }

                        if (kv.Key.StartNode == node && (
                                outC.Count == 0 
                                || outC.All(lane => lane.Nodes[1] != kv.Value.Nodes[1] )
                            )
                        )
                        {
                            outC.Add(kv.Value);
                        }
                    }
                }
            }

            var maxIn = inC.Count;
            var maxOut = outC.Count;
            var total = maxIn + maxOut;
            
            if(node == 99812578L)
                Debug.Log($"Intersection 99812578 {maxIn} {maxOut} {total}");
            
            if (
                inC.Count == 0 
                || total < 2 
                || inC.Count == 1 && outC.Count == 1 && inC[0].OtherDirection == outC[0]
            )
            {
                return;
            }

            var lc = inC[0];
            var lcOtherDirIndex = lc.OtherDirection == null 
                ? - 1 
                : outC.IndexOf(lc.OtherDirection) + maxIn;

            Vector2 center; //  = lc.Points.Last();
            {
                var a = lc.GetPointRE0();
                var b = lc.OtherDirection?.GetPointRS0() ?? lc.GetPointLE0();
            
                center = a - (a - b) / 2;
            }
            
            var points = new List<Vector2>();
            var orientation = new List<int>();
            var angles = new List<float>();

            var roadA = (lc.GetPointLE0() - lc.GetPointRE0()).ToVector3xz();
            angles.Add(0);
            orientation.Add(0);

            for (var i = 1; i < total; i++)
            {
                var roadB = (GetB0(i, inC, outC) - GetA0(i, inC, outC)).ToVector3xz();
                var signedAngle = Vector3.SignedAngle(roadA, roadB, Vector3.up);
                
                angles.Add(( 360f - signedAngle ) % 360f);
                
                if(i != lcOtherDirIndex)
                    orientation.Add(i);
            }
            
            orientation.Sort((a, b) =>
            {
                var aA = angles[a];
                var aB = angles[b];

                return Mathf.Abs(aA - aB) < 0.5f
                    ? 0 - a.CompareTo(b) // out dir before in dir 
                    : aA.CompareTo(aB);
            });
            
            if(lcOtherDirIndex >= 0)
                orientation.Add(lcOtherDirIndex);

            var index = 0;
            var lastSkipped = false;
            for (var i = 0; i < total; i++)
            {
                var nextIndex = orientation[(i + 1) % orientation.Count];
                
                if (index >= maxIn && outC[index - maxIn].OtherDirection != null)
                {
                    index = nextIndex;
                    lastSkipped = true;
                    continue;
                }
                
                var angle = (360 + angles[nextIndex] - angles[index]) % 360;
                Vector3 intersectionPoint;
                if (angle > 135f)
                {
                    var a0 = GetA0(index, inC, outC);
                    var b0 = GetB0(nextIndex, inC, outC);
                
                    intersectionPoint = ( (a0 - center).magnitude > (b0 - center).magnitude ? a0 : b0 ).ToVector3xz() ;
                }
                else
                {
                    var a0 = GetA0(index, inC, outC).ToVector3xz();
                    var a1 = GetA1(index, inC, outC).ToVector3xz();
                    var av = a1 - a0;
                
                    var b0 = GetB0(nextIndex, inC, outC).ToVector3xz();
                    var b1 = GetB1(nextIndex, inC, outC).ToVector3xz();
                    var bv = b1 - b0;
                
                    if (!Math.Math.LineLineIntersection(out intersectionPoint, a0, av, b0, bv))
                    {
                        Debug.LogWarning($"There should be an intersection point (Node: {node})");
                        return;
                    }
                }
                if(lastSkipped)
                {
                    lastSkipped = false;
                    
                    var a0 = points.Last().ToVector3xz(); // Right from Out
                    var a1 = intersectionPoint; // Right from In
                    var av = a1 - a0;
                
                    // Line between Out and In
                    var b0 = GetB0(index, inC, outC).ToVector3xz();
                    var b1 = GetB1(index, inC, outC).ToVector3xz();
                    var bv = b1 - b0;
                
                    // should always be found
                    if (Math.Math.LineLineIntersection(out var intersectionMiddleLine, a0, av, b0, bv))
                    {
                        // In between point (Between Out and In)
                        points.Add(intersectionMiddleLine.ToVector2xz());
                        angles.Add(0f);
                    }
                    else
                    {
                        Debug.Log("Intersection not found");
                    }
                }
                
                points.Add(intersectionPoint.ToVector2xz());
                
                index = nextIndex;
            }


            if (lcOtherDirIndex >= 0)
            {
                var a0 = points.Last().ToVector3xz(); // Right from Out
                var a1 = points[0].ToVector3xz(); // Right from In
                var av = a1 - a0;
                
                // Line between Out and In
                var b0 = GetB0(0, inC, outC).ToVector3xz();
                var b1 = GetB1(0, inC, outC).ToVector3xz();
                var bv = b1 - b0;
                
                // should always be found
                if (Math.Math.LineLineIntersection(out var intersectionMiddleLine, a0, av, b0, bv))
                {
                    // In between point (Between Out and In)
                    points.Add(intersectionMiddleLine.ToVector2xz());
                }
            }

            var intersection = new Intersection(node, center, inC, outC, points, orientation, angles, tile, hasTrafficLights);
            // tile.Intersections[node] = intersection;
            tile.Intersections.TryAdd(node, intersection);
            // data.nodes[node] = intersection;

        }

        private static Vector2 GetA0(int index, List<LaneCollection> inC, List<LaneCollection> outC)
        {
            return index < inC.Count
                ? inC[index].GetPointRE0()
                : outC[index - inC.Count].GetPointLS0();
        }
        private static Vector2 GetA1(int index, List<LaneCollection> inC, List<LaneCollection> outC)
        {
            return index < inC.Count
                ? inC[index].GetPointRE1()
                : outC[index - inC.Count].GetPointLS1();
        }
        
        private static Vector2 GetB0(int index, List<LaneCollection> inC, List<LaneCollection> outC)
        {
            return index < inC.Count
                ? inC[index].GetPointLE0()
                : outC[index - inC.Count].GetPointRS0();
        }
        private static Vector2 GetB1(int index, List<LaneCollection> inC, List<LaneCollection> outC)
        {
            return index < inC.Count
                ? inC[index].GetPointLE1()
                : outC[index - inC.Count].GetPointRS1();
        }

    }
}
