using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Request;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Data.Provider
{
    public class GenerateBridges : Provider
    {
        
        public GenerateBridges(AbstractSettingsProvider settings) : base(settings, MapTile.InitStep.Bridges) {}


        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;

            var bridges = new List<Bridge>();
            
            foreach (var element in request.elements)
            {
                if (element.GeometryType != GeometryType.AREA || element.type != "way" || element.pointsV2.Count < 3)
                    continue;
                
                // if(element.id != "261318659")
                //     continue;

                GenerateBridge(data, tile, bridges, element);

                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            foreach (var element in request.elements)
            {
                if (element.GeometryType != GeometryType.LINE || element.type != "way" || element.pointsV2.Count < 2 
                    || !element.HasProperty("bridge"))
                    continue;

                if (data.Bridges.ContainsKey(element.id))
                    continue;
                
                // is there a bridge that already uses any nodes of this element 
                // (is this bridge already defined by bridge:structure)
                if(bridges.Any(b => b.Nodes.Contains(element.nodes[0]) && b.Nodes.Contains(element.nodes[element.nodes.Length - 1])))
                   continue; 
                
                GenerateSimpleBridge(data, tile, bridges, element);

                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            

            foreach (var bridge in data.Bridges.Values)
            {
                bridge.SetIntersectionNodes(tile);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            foreach (var bridge in bridges)
            {
                bridge.SetAdjacentBridges(data.Bridges.Values);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            stopwatch.Stop();
        }

        private static void GenerateSimpleBridge(MapData data, MapTile tile, List<Bridge> bridges, Element element)
        {
            var points = new Types.Spline(element.pointsV2);
            
            var widthHalf = 3f;

            var laneId = new MapData.LaneId(element.nodes, MapData.LaneType.HIGHWAY);
            if (tile.LaneCollections.ContainsKey(laneId))
            {
                var lc = tile.LaneCollections[laneId];
                widthHalf = (lc.Characteristics.Width + (lc.OtherDirection?.Characteristics.Width ?? 0)) * .5f 
                            + (lc.Characteristics.HasSidewalk() ? 3f : 0f)
                            + (lc.Characteristics.CyclewayLeft != null || lc.Characteristics.CyclewayRight != null ? 3f : 0f)                                             
                            + 1f;
            }

            var bridgePointsR = new List<Vector2>();
            var bridgePointsL = new List<Vector2>();

            for (var i = 0; i < points.Count; i++)
            {
                var c = points[i].ToVector2xz();
                var fw = points.Forward[i].ToVector2xz().Rotate(-90) * widthHalf;
                
                bridgePointsR.Add(c + fw);
                bridgePointsL.Add(c - fw);
            }
            
            bridgePointsR.Reverse();
            bridgePointsL.AddRange(bridgePointsR);

            // var bridgePoints = points;
            
            var bridge = new Bridge(
                element.id,
                "beam",
                bridgePointsL.ToArray(),
                element.nodes,
                element.GetPropertyInt("layer", 1),
                element.GetPropertyMeasurement("maxheight")
            );

            if (!data.Bridges.TryAdd(bridge.Id, bridge)) 
                return;
            
            tile.BridgeNodes.AddRange(element.nodes);
            tile.WayAreas.Add(element.id, bridge);
            bridges.Add(bridge);
        }

        private static void GenerateBridge(MapData data, MapTile tile, List<Bridge> bridges, Element element)
        {
            var structure = element.GetProperty("bridge:structure");
            var support = element.GetProperty("bridge:support");
                
            if(structure == null && support == null)
                return;

            if (structure == null)
            {
                tile.WayAreas.Add(element.id, new BridgeSupport(
                    element.id,
                    support,
                    element.pointsV2.ToArray(),
                    element.nodes
                ));
                return;
            }

            if (data.Bridges.ContainsKey(element.id))
                return;
            
            var bridge = new Bridge(
                element.id,
                structure,
                element.pointsV2.ToArray(),
                element.nodes,
                element.GetPropertyInt("layer", 1),
                element.GetPropertyMeasurement("maxheight")
            );

            if (!data.Bridges.TryAdd(bridge.Id, bridge)) 
                return;
            
            tile.BridgeNodes.AddRange(element.nodes);
            tile.WayAreas.Add(element.id, bridge);
            bridges.Add(bridge);
        }
    }
}