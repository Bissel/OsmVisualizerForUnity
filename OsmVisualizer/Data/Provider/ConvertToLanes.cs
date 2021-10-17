using System.Collections;
using System.Collections.Generic;
// using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Characteristics;
// using System.Runtime.Remoting.Messaging;
using OsmVisualizer.Data.Request;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using OsmVisualizer.Math;
using UnityEngine;


namespace OsmVisualizer.Data.Provider
{
    public class ConvertToLanes : Provider
    {
        private readonly float _streetLaneWidthForSpeedLimitLess50;
        private readonly float _streetLaneWidthForSpeedLimitLess70;
        private readonly float _streetLaneWidthForSpeedLimitLess100;
        private readonly float _streetLaneWidthForSpeedLimitOver100;
        
        private readonly float _streetLaneWidthScaler;
        private readonly float _streetLaneWidthForSpeedScaler;
        
        private readonly float _subdivideDistance;

        private readonly Dictionary<string, int> _defaultSpeedLimits;
        private readonly int _defaultSpeedLimit;
        
        public ConvertToLanes(
            AbstractSettingsProvider settings, float subdivideDistance,
            float streetLaneWidthForSpeedLimitLess50, float streetLaneWidthForSpeedLimitLess70,
            float streetLaneWidthForSpeedLimitLess100, float streetLaneWidthForSpeedLimitOver100,
            float streetLaneWidthScaler, float streetLaneWidthForSpeedScaler,
            Dictionary<string, int> defaultSpeedLimits, int defaultSpeedLimit
        ) : base(settings, MapTile.InitStep.Lanes)
        {
            _subdivideDistance = subdivideDistance;

            _streetLaneWidthForSpeedLimitLess50 = streetLaneWidthForSpeedLimitLess50;
            _streetLaneWidthForSpeedLimitLess70 = streetLaneWidthForSpeedLimitLess70;
            _streetLaneWidthForSpeedLimitLess100 = streetLaneWidthForSpeedLimitLess100;
            _streetLaneWidthForSpeedLimitOver100 = streetLaneWidthForSpeedLimitOver100;

            _streetLaneWidthScaler = streetLaneWidthScaler;
            _streetLaneWidthForSpeedScaler = streetLaneWidthForSpeedScaler;

            _defaultSpeedLimits = defaultSpeedLimits;
            _defaultSpeedLimit = defaultSpeedLimit;
        }

        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            // stopwatch.Stop();
            // while (!tile.HasAllNeighbours) yield return null;
            //
            // while (tile.Neighbours().Any(n => n.InitProgress < MapTile.InitStep.SplitOutOfBounds))
            //     yield return null;
            //
            // stopwatch.Start();
            
            var startTime = stopwatch.ElapsedMilliseconds;
            
            var useSubdivision = !float.IsNaN(_subdivideDistance);
            
            foreach (var element in request.elements)
            {
                if (element.split
                    || element.GeometryType != GeometryType.LINE 
                    || element.nodes.Length < 2
                    || element.type != "way" 
                    || !element.HasProperty("highway")
                )
                    continue;
                   
                if(useSubdivision)
                    Subdivide(element);
                
                ConvertElement(element, data, tile);

                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }

            var min = tile.Min;
            var max = tile.Max;
            
            foreach (var n in tile.Neighbours())
            {
                if(n == null) continue;
                
                var toDelete = new List<MapData.LaneId>();
                
                foreach (var dummy in n.DummyLaneCollections.Values)
                {
                    if (dummy == null) continue;
                    
                    if (dummy.Points.Any(p => p.IsInBounds(min, max)))
                    {
                        if(!tile.LaneCollections.ContainsKey(dummy.Id))
                        {
                            dummy.IsDummy = false;
                            tile.AddLaneCollection(dummy);
                        }
                        toDelete.Add(dummy.Id);
                    }

                    if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                        continue;
                    
                    stopwatch.Stop();
                    yield return null;
                    stopwatch.Start();
                    startTime = stopwatch.ElapsedMilliseconds;
                }

                foreach (var laneId in toDelete)
                {
                    n.DummyLaneCollections.Remove(laneId);
                }
            }
            
            stopwatch.Stop();
        }

        private void Subdivide(Element element)
        {
            // if (element.pointsV2.Count != element.nodes.Length)
            //     return;

            var points = new List<Vector2>();
            var lastPoint = element.pointsV2[0];
            foreach (var p in element.pointsV2)
            {
                var dist = Vector2.Distance(lastPoint, p); // - 1f;
                
                if (dist > _subdivideDistance)
                {
                    var dir = (p - lastPoint).normalized; // element.pointsV2.DirectionForPoint(i);

                    dist -= _subdivideDistance * .5f;
                    for (var len = _subdivideDistance; len < dist; len+=_subdivideDistance)
                        points.Add(lastPoint + dir * len );
                }

                points.Add(p);
                lastPoint = p;
            }
            
            element.pointsV2 = points;
        }

        private void ConvertElement(Element element, MapData data, MapTile tile)
        {
            var idFw = new MapData.LaneId(element.nodes);
            var idBw = idFw.GetReverseId();

            var neighbours = tile.NeighboursForPoints(element.pointsV2);
            
            var edgeFw = tile.NeighbourLaneCollection(idFw, neighbours);
            var edgeBw = tile.NeighbourLaneCollection(idBw, neighbours);

            var hasEdgeFw = edgeFw != null;
            var hasEdgeBw = edgeBw != null;
            
            if (hasEdgeFw && !edgeFw.IsDummy && hasEdgeBw && !edgeBw.IsDummy)
                return;

            var points = element.pointsV2.ToArray();

            var oneway = IsOneway(element);
            var onewayReverse = IsOnewayReverse(element);
            
            var characteristic = new WayCharacteristics(element, _defaultSpeedLimits, _defaultSpeedLimit);
            
            var laneCount = GetLanes(element, characteristic.Type, oneway);
            var laneCountForward = GetLanesForward(element, laneCount, oneway, onewayReverse);
            var laneCountBackward = GetLanesBackward(element, laneCount, oneway, onewayReverse);

            characteristic.WidthScaling = !float.IsNaN(characteristic.WidthAttr)
                ? _streetLaneWidthScaler
                : _streetLaneWidthForSpeedScaler;
            
            var width = characteristic.WidthScaling * (
                !float.IsNaN(characteristic.WidthAttr)
                    ? characteristic.WidthAttr
                    : StreetLaneWidth(characteristic.SpeedLimit) * laneCount
                );
            
            var laneWidth = width / laneCount;
            var wHalf = width * .5f;

            var hasFw = false;
            var hasBw = false;

            var hasFwOtherTile = false;
            var hasBwOtherTile = false;
            
            var fw = GetCollection(
                element, 
                laneCountForward, 
                hasEdgeFw, 
                edgeFw, 
                characteristic,
                points, 
                onewayReverse,
                wHalf, 
                laneWidth, 
                true,
                ref hasFw, 
                ref hasFwOtherTile
            );
            
            var bw = GetCollection(
                element, 
                laneCountBackward, 
                hasEdgeBw, 
                edgeBw, 
                characteristic,
                points, 
                onewayReverse,
                wHalf, 
                laneWidth, 
                false,
                ref hasBw, 
                ref hasBwOtherTile
            );

            if (hasFw && hasBw)
            {
                fw.OtherDirection = bw;
                bw.OtherDirection = fw;
            }
            
            AddCollection(tile, hasFw, hasFwOtherTile, fw, idFw);
            AddCollection(tile, hasBw, hasBwOtherTile, bw, idBw);
        }

        private static void AddCollection(MapTile tile, bool hasLc, bool hasLcOtherTile, LaneCollection lc, MapData.LaneId id)
        {
            if (!hasLc || hasLcOtherTile)
                return;
            
            var bwRail = id.GetOtherType(MapData.LaneType.RAILWAY);
            if (tile.LaneCollections.TryGetValue(bwRail, out var rail))
            {
                ((RailWayCharacteristics) rail.Characteristics).SetLaneCollection(lc);
                return;
            }

            if (tile.LaneCollections.ContainsKey(lc.Id))
            {
                // @todo check 
                // var otherLc = tile.LaneCollections[lc.Id];
                // if (otherLc.Equals(lc))
                // {
                    // Debug.Log($"tile ({tile.pos}) has a duplication: ${lc.Id}");
                    return;
                // }
                //
                // lc.Id.SetAsAlternative();
            }

            tile.AddLaneCollection(lc);
        }
        
        private static LaneCollection GetCollection(
            Element element,
            int laneCount, 
            bool hasEdge, 
            LaneCollection edge, 
            WayCharacteristics characteristic, 
            Vector2[] points,
            bool onewayReverse, 
            float wHalf, 
            float laneWidth, 
            bool isForward,
            ref bool hasCollection,
            ref bool hasOtherTile
        )
        {
            if (laneCount <= 0) return null;
            
            hasCollection = true;

            if (!hasEdge)
            {
                var reverse = onewayReverse ? !isForward : isForward;
                
                if (!isForward)
                    characteristic = characteristic.ReverseDirection();
                
                return GenerateForwardLaneCollection(
                    characteristic,
                    laneCount,
                    isForward ? points : points.Reverse().ToArray(),
                    GetDirection(element, isForward, onewayReverse),
                    wHalf,
                    laneWidth,
                    isForward ? element.nodes : element.nodes.Reverse().ToArray(),
                    false// !element.insideTile
                );
            }
            
            if(edge.IsDummy)
            {
                edge.IsDummy = false;
            }
            else
            {
                hasOtherTile = true;
            }
                
            return edge;
        }


        private static LaneCollection GenerateForwardLaneCollection(
            WayCharacteristics c, int laneCountDir, Vector2[] points, 
            Direction[][] directions, float wHalf, float laneWidth, long[] nodes,
            bool isDummy
        )
        {
            var laneWidthHalf = laneWidth * .5f;
            var pointsCount = points.Length;
            
            c.Width = laneWidth * laneCountDir;
            
            var offsetCenter = -wHalf + laneCountDir * laneWidth;
                
            var lanes = new Vector2[laneCountDir][];
            for (var i = 0; i < laneCountDir; i++)
            {
                lanes[i] = new Vector2[pointsCount];
            }
                
            var outlineL = new List<Vector2>(pointsCount);
            var outlineR = new List<Vector2>(pointsCount);

            for (var i = 0; i < pointsCount; i++)
            {

                var dir = points.DirectionForPoint(i);

                // rotate Left
                var rotatedUnit = dir.Rotate(90);
                var roadCenter = points[i];

                outlineL.Add(roadCenter + rotatedUnit * offsetCenter);
                outlineR.Add(roadCenter + rotatedUnit * -wHalf);

                for (var x = 0; x < laneCountDir; x++)
                {
                    lanes[x][i] = roadCenter + rotatedUnit * (offsetCenter - laneWidthHalf - x*laneWidth);
                }
                    
            }

            return new LaneCollection(
                c,
                lanes,
                directions,
                new Types.Spline(outlineL, 0f),
                new Types.Spline(outlineR, 0f),
                nodes,
                points,
                isDummy
            );
        }
        
        
        
        private static Direction[][] GetDirection(Element element, bool forward, bool isOnewayReverse)
        {
            return ( element.GetProperty($"turn:lanes:{(forward ^ !isOnewayReverse ? "backward" : "forward")}")
                       ?? element.GetProperty("turn:lanes")
                       ?? element.GetProperty("turn")
                )?.ToDirections();
        }

        private static int GetLanes(Element element, string type, bool isOneway)
        {
            var lanes = element.GetPropertyInt("lanes");

            if (lanes > 0)
                return isOneway || lanes > 1 ? lanes : 2;
            
            switch (type)
            {
                case "residential":
                case "unclassified":
                case "living_street":
                case "tertiary":
                case "secondary":
                case "primary":
                case "service":
                case "road":
                    return isOneway ? 1 : 2;

                // case "track":
                // case "path":
                //     return 1;

                case "motorway":
                case "trunk":
                    return 2;
                
                case "tertiary_link":
                case "secondary_link":
                case "primary_link":
                case "motorway_link":
                case "trunk_link":
                    return isOneway ? 1 : 2;

                default:
                    return -1;
            }
        
        }

        private static int GetLanesForward(Element element, int lanes, bool isOneway, bool isOnewayReverse)
        {
            var lanesDir = element.GetPropertyInt("lanes:forward");
            return lanesDir > 0
                ? lanesDir
                : lanes <= 0 || isOneway
                    ? (isOnewayReverse ? 0 : lanes)
                    : lanes / 2;
        }
        
        private static int GetLanesBackward(Element element, int lanes, bool isOneway, bool isOnewayReverse)
        {
            var lanesDir = element.GetPropertyInt("lanes:backward");
            return lanesDir > 0
                ? lanesDir
                : lanes <= 0 || isOneway
                    ? (!isOnewayReverse ? 0 : lanes)
                    : lanes / 2;
        }

        // https://de.wikipedia.org/wiki/Stra%C3%9Fenquerschnitt
        private float StreetLaneWidth(int allowedSpeed)
        {
            return allowedSpeed < 50 
                ? _streetLaneWidthForSpeedLimitLess50
                : allowedSpeed < 70
                    ? _streetLaneWidthForSpeedLimitLess70
                    : allowedSpeed < 100
                        ? _streetLaneWidthForSpeedLimitLess100
                        : _streetLaneWidthForSpeedLimitOver100;
        }
        
        private static bool IsOneway(Element element)
        {
            return element.GetProperty("oneway") switch
            {
                "yes" => true,
                "1" => true,
                "-1" => true,
                _ => false
            };
        }
        private static bool IsOnewayReverse(Element element)
        {
            return element.GetProperty("oneway") switch
            {
                "-1" => true,
                _ => false
            };
        }
    }
}
