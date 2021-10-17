using System;
using System.Collections;
// using System.Collections.Concurrent;
using System.Collections.Generic;
// using System.Diagnostics.Eventing.Reader;
using System.Linq;
using OsmVisualizer.Data.Request;
using UnityEngine;

// using UnityEngine;

namespace OsmVisualizer.Data.Provider
{
    public class SplitRoadsOnIntersection : Provider
    {

        // private readonly ConcurrentDictionary<Element, List<long>> _splitElementList = new ConcurrentDictionary<Element, List<long>>();
        
        public SplitRoadsOnIntersection(AbstractSettingsProvider settings) : base(settings, MapTile.InitStep.SplitOnIntersection) {}

        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            
            var useElement = new bool[request.elements.Length];

            for (var index = 0; index < request.elements.Length; index++)
            {
                var el = request.elements[index];

                bool useEl;
                {
                    var highway = el.GetProperty("highway");
                    useEl = el.nodes.Length > 0 
                            && el.GeometryType == GeometryType.LINE 
                            && el.type == "way"
                            && highway != null 
                            && highway != "pedestrian" 
                            && highway != "footway"
                            && highway != "path"
                            && highway != "cycleway"
                            // && !(el.HasProperty("bridge") && el.GetPropertyBool("bridge"))
                            // && !(el.HasProperty("tunnel") && el.GetPropertyBool("tunnel"))
                        ;
                    
                    useElement[index] = useEl;
                }
                
                if (!useEl)
                    continue;

                var neighbours = tile.NeighboursForPoints(el.pointsV2);
                var fwId = new MapData.LaneId(el.nodes);
                var fwSplit = tile.NeighbourSplitElement(fwId, neighbours);
                var bwSplit = tile.NeighbourSplitElement(fwId.GetReverseId(), neighbours);
                
                if (fwSplit == null && bwSplit == null)
                    continue;
                
                var nodes = el.nodes.ToList();
                
                if (fwSplit != null)
                {
                    var start = -1;
                    var end = -1;
                    
                    LaneCollection lastEdge = null;
                    foreach (var laneId in fwSplit.SplitElements)
                    {
                        var edgeFwTmp = tile.NeighbourLaneCollection(laneId, neighbours);
                
                        if (start < 0 && edgeFwTmp != null)
                        {
                            start = System.Math.Min(nodes.IndexOf(edgeFwTmp.Id.StartNode), nodes.IndexOf(edgeFwTmp.Id.EndNode));
                        }
                        
                        if (start >= 0 && edgeFwTmp == null)
                        {
                            if (lastEdge != null)
                                end = System.Math.Max(nodes.IndexOf(lastEdge.Id.StartNode), nodes.IndexOf(lastEdge.Id.EndNode));
                            
                            break;
                        }
                
                        lastEdge = edgeFwTmp;
                    }
                
                    request.elements[index] = start == -1 || end == -1 
                        ? null 
                        : start == 0 
                            ? el.ElementFromRange(end, nodes.Count - 1, tile.Min, tile.Max)
                            : el.ElementFromRange(0, start, tile.Min, tile.Max);
                }
                if (bwSplit != null)
                {
                    var start = -1;
                    var end = -1;
                    
                    LaneCollection lastEdge = null;
                    foreach (var laneId in bwSplit.SplitElements)
                    {
                        var edgeFwTmp = tile.NeighbourLaneCollection(laneId, neighbours);
                
                        if (start < 0 && edgeFwTmp != null)
                        {
                            start = System.Math.Min(nodes.IndexOf(edgeFwTmp.Id.StartNode), nodes.IndexOf(edgeFwTmp.Id.EndNode));
                        }
                        
                        if (start >= 0 && edgeFwTmp == null)
                        {
                            if (lastEdge != null)
                                end = System.Math.Max(nodes.IndexOf(lastEdge.Id.StartNode), nodes.IndexOf(lastEdge.Id.EndNode));
                            
                            break;
                        }
                
                        lastEdge = edgeFwTmp;
                    }
                
                    request.elements[index] = start == -1 || end == -1 
                        ? null 
                        : start == 0 
                            ? el.ElementFromRange(end, nodes.Count - 1, tile.Min, tile.Max)
                            : el.ElementFromRange(0, start, tile.Min, tile.Max);
                }
            }


            var nodeCounter = new Dictionary<long, int>();
            var nodePosition = new Dictionary<long, Vector2>();

            var i = 0;
            foreach (var el in request.elements)
            {
                if(!useElement[i++] || el == null)
                    continue;

                for (var j = 0; j < el.nodes.Length; j++)
                {
                    var node = el.nodes[j];
                    if (!nodeCounter.ContainsKey(node))
                        nodeCounter.Add(node, 0);
                    
                    nodeCounter[node]++;

                    if(nodeCounter[node] > 1 && !nodePosition.ContainsKey(node) && (el.geometry == null || el.geometry[j] != null))
                        nodePosition.Add(node, el.pointsV2[j]);
                }
            }

            if (stopwatch.ElapsedMilliseconds - startTime > tile.sp.maxFrameTime)
            {
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }

            var elements = request.elements.ToList();

            i = 0;
            foreach (var el in request.elements)
            {
                if (!useElement[i++] || el == null || el.nodes.Length < 3)
                    continue;

                var splitNodesIndices = new List<int>();
                
                for (var index = 1; index < el.nodes.Length - 1; index++)
                {
                    var node = el.nodes[index];
                    if (nodeCounter[node] == 1) continue;
                    
                    splitNodesIndices.Add(index);
                }

                if (splitNodesIndices.Count == 0)
                    continue;

                splitNodesIndices.Add(el.nodes.Length - 1);
                
                var lastIndex = 0;
                var splitElements = new List<MapData.LaneId>();
                foreach (var nodeIndex in splitNodesIndices)
                {
                    // Debug.Log($"{el.nodes.Length} {lastIndex} {nodeIndex}");
                    splitElements.Add(new MapData.LaneId(el.nodes[lastIndex], el.nodes[nodeIndex]));
                    elements.Add(el.ElementFromRange(lastIndex, nodeIndex, tile.Min, tile.Max));
                    lastIndex = nodeIndex;
                }

                el.split = true;
                el.SplitElements = splitElements;
                
                if (stopwatch.ElapsedMilliseconds - startTime > tile.sp.maxFrameTime)
                {
                    stopwatch.Stop();
                    yield return null;
                    stopwatch.Start();
                    startTime = stopwatch.ElapsedMilliseconds;
                }
            }
            
            request.elements = elements.ToArray();

            foreach (var kv in nodeCounter)
            {
                if (kv.Value <= 1 || tile.IntersectionPoints.ContainsKey(kv.Key)) continue;
                tile.IntersectionPoints.Add(kv.Key, nodePosition[kv.Key]);
            }
            
            stopwatch.Stop();
        }
    }
}
