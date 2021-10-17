using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using it.bissel.Utils;
using OsmVisualizer.Data.Request;
using UnityEngine;


namespace OsmVisualizer.Data.Provider
{

    public class SplitOutOfTileBounds : Provider
    {

        private readonly Dictionary<MapData.LaneId, MapData.TilePos> _outOfBoundsClaims =
            new Dictionary<MapData.LaneId, MapData.TilePos>();

        private readonly Dictionary<MapData.TilePos, List<long>> _tilesDone =
            new Dictionary<MapData.TilePos, List<long>>();
        
        private readonly Dictionary<MapData.TilePos, List<Element>> _elementsToAdd =
            new Dictionary<MapData.TilePos, List<Element>>();

        private readonly List<long> _createdIntersections = new List<long>(); 

        public SplitOutOfTileBounds(AbstractSettingsProvider settings) : base(settings, MapTile.InitStep.SplitOutOfBounds) {}

        // private bool _converting = false;

        // private ConcurrentBag<bool> _converting = new ConcurrentBag<bool>();

        private readonly Semaphore _converting = new Semaphore( 1, 1);
        
        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            if(tile.sp is SettingsPlainLevelProvider)
                yield break;
            
            stopwatch.Stop();

            _converting.WaitOne();

            var startTime = stopwatch.ElapsedMilliseconds;
            var min = tile.Min;
            var max = tile.Max;
            var pos = tile.pos;
            var elements = new List<Element>();
            
            CreateNeighbourLists(pos);
            
            // adding all elements from other Tiles reaching into this tile
            foreach (var element in request.elements)
            {
                var nodes = element.nodes.Skip(1).Take(element.nodes.Length - 3);
                _elementsToAdd[pos].RemoveAll(e => e.Equals(element) || e.nodes.Skip(1).Take(element.nodes.Length - 3).Any(n => nodes.Contains(n)));
            }
            
            elements.AddRange(_elementsToAdd[pos]);
            
            foreach (var element in request.elements)
            {
                if (!ConvertElement(element, min, max, tile, elements))
                    continue;

                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            _tilesDone.Add(
                pos, 
                elements
                    .Where(e => e.GeometryType == GeometryType.LINE && e.HasProperty("highway"))
                    .SelectMany(e => e.nodes)
                    .ToList()
            );

            _converting.Release();
            
            request.elements = elements.ToArray();
            
            stopwatch.Stop();
        }

        private bool ConvertElement(Element element, Vector2 min, Vector2 max, MapTile tile, ICollection<Element> elements)
        {
            if (
                element.GeometryType != GeometryType.LINE
                || element.geometry == null
                || element.geometry.Length == 0
                || element.pointsV2.Count < 2
                || element.type != "way"
                || !element.HasProperty("highway"))
            {
                elements.Add(element);
                return false;
            }
            
            var outOfBounds = element.pointsOutOfBounds;
            
            if (element.insideTile)
            {
                elements.Add(element);
                return false;
            }
            if (outOfBounds.All(t => t))
            {
                AddElement(0, element.pointsV2.Count - 1, min, max, tile.pos, true, element, elements);
                return false;
            }

            var lastOob = outOfBounds[0];
            var startIndex = 0;
            var end = element.pointsV2.Count - 1;
            for (var i = 1; i < end; i++)
            {
                var oob = outOfBounds[i];
                if (oob == lastOob) continue;

                AddElement(startIndex, i, min, max, tile.pos, lastOob, element, elements);

                var node = element.nodes[i];
                
                if(!_createdIntersections.Contains(node) && !tile.IntersectionPoints.ContainsKey(node))
                {
                    tile.IntersectionPoints.Add(node, element.pointsV2[i]);
                    _createdIntersections.Add(node);
                }

                lastOob = oob;
                startIndex = i;
            }

            AddElement(startIndex, end, min, max, tile.pos, lastOob, element, elements);

            return true;
        }
        
        private void AddElement(int startIndex, int endIndex, Vector2 min, Vector2 max, MapData.TilePos pos, bool lastOob, Element element, ICollection<Element> elements) {
            var newElement = element.ElementFromRange(startIndex, endIndex, min, max);

            if (!lastOob)
            {
                elements.Add(newElement);
                return;
            }
            
            var p = element.pointsV2[startIndex + (endIndex - startIndex) / 2];

            var otherTile = pos.FromOffset(
                p.x < min.x ? -1 : p.x > max.x ? 1 : 0,
                p.y < min.y ? -1 : p.y > max.y ? 1 : 0
            );
            
            if (!_tilesDone.ContainsKey(otherTile))
            {
                _elementsToAdd[otherTile].Add(newElement);
                return;
            }
            
            var nodes = newElement.nodes.Skip(1).Take(newElement.nodes.Length - 3).ToList();
            if(!_tilesDone[otherTile].Any(n => nodes.Contains(n)))
            {
                elements.Add(newElement);
            }
            // else
            // {
            //     Debug.Log($"{newElement.id} not added (other tile ({otherTile}) contains part of it)");
            // }
        
        }
        


        // var lastOutSide = outOfBounds[0];
        // var addElement = false;
        // for (var i = 1; i < element.pointsV2.Count - 1; i++)
        // {
        //     var point = element.pointsV2[i];
        //     var currOutSide = outOfBounds[i];
        //     
        //     if (lastOutSide == currOutSide)
        //         continue;
        //
        //     addElement = true;
        //     var distLast = element.pointsV2[i - 1].DistanceToBounds(min, max);
        //     var distCurr = point.DistanceToBounds(min, max);
        //
        //     var index = distLast < distCurr ? i - 1 : i;
        //     
        //     var startIndex = 0;
        //     var endIndex = element.pointsV2.Count - 1;
        //     
        //     // if(element.nodes[0] == 2777348052L || element.nodes[element.nodes.Length - 1] == 2777348052L)
        //     //     Debug.Log(element.id + $"AA {startIndex} {endIndex} {element.nodes.Length} {tile.key}");
        //
        //     if (index - 1 == startIndex || index + 1 == endIndex)
        //         break;
        //
        //     if (lastOutSide)
        //         startIndex = index;
        //     else
        //         endIndex = index;
        //     
        //     // if(element.nodes[0] == 2777348052L || element.nodes[element.nodes.Length - 1] == 2777348052L)
        //     //     Debug.Log(element.id + $"BB {startIndex} {endIndex} {element.nodes.Length} {tile.key}");
        //
        //     if (startIndex > 0)
        //         AddPrev(element, startIndex, tile.pos, min, max, elements);
        //     
        //     if (endIndex < element.nodes.Length - 1)
        //         AddNext(element, endIndex, tile.pos, min, max, elements);
        //     
        //     element = element.ElementFromRange(startIndex, endIndex, min, max);
        //     break;
        //
        // }
        //
        // if ( (addElement || lastOutSide != outOfBounds[outOfBounds.Count-1]) && _outOfBoundsClaims.TryAdd(new MapData.LaneId(element.nodes), tile.pos) )
        // {
        //     elements.Add(element);
        // }

    //     return true;
    // }

        // private void AddPrev(Element element, int endIndex, MapData.TilePos pos, Vector2 min, Vector2 max, List<Element> elements)
        // {
        //     var otherPos = new MapData.TilePos(
        //         pos.X + element.pointsV2[0].x < min.x ? -1 : 1,
        //         pos.Y + element.pointsV2[0].y < min.y ? -1 : 1
        //     );
        //
        //     var otherElement = element.ElementFromRange(0, endIndex, min, max);
        //
        //     // if the other tile is already finished, the part out of bounds should be 
        //     // added to this tile
        //     if (_tilesDone.ContainsKey(otherPos) && !_outOfBoundsClaims.TryAdd(new MapData.LaneId(otherElement.nodes), otherPos))
        //     {
        //         elements.Add(otherElement);
        //         return;
        //     }
        //     
        //     _outOfBoundsClaims.TryAdd(new MapData.LaneId(otherElement.nodes), otherPos);
        //     _elementsToAdd.TryAdd(otherPos, new List<Element>());
        //     _elementsToAdd[otherPos].Add(otherElement);
        // }
        //
        // private void AddNext(Element element, int startIndex, MapData.TilePos pos, Vector2 min, Vector2 max, List<Element> elements)
        // {
        //     var lastIndex = element.pointsV2.Count - 1;
        //     var otherPos = new MapData.TilePos(
        //         pos.X + element.pointsV2[lastIndex].x < min.x ? -1 : 1,
        //         pos.Y + element.pointsV2[lastIndex].y < min.y ? -1 : 1
        //     );
        //
        //     var otherElement = element.ElementFromRange(startIndex, element.nodes.Length - 1, min, max);
        //
        //     // if the other tile is already finished, the part out of bounds should be 
        //     // added to this tile
        //     if (_tilesDone.ContainsKey(otherPos) && !_outOfBoundsClaims.TryAdd(new MapData.LaneId(otherElement.nodes), otherPos))
        //     {
        //         elements.Add(otherElement);
        //         return;
        //     }
        //     
        //     _outOfBoundsClaims.TryAdd(new MapData.LaneId(otherElement.nodes), otherPos);
        //     _elementsToAdd.TryAdd(otherPos, new List<Element>());
        //     _elementsToAdd[otherPos].Add(otherElement);
        // }
        
        
        private void CreateNeighbourLists(MapData.TilePos pos)
        {
            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    var key = pos.FromOffset(x, y);
                    if (!_elementsToAdd.ContainsKey(key))
                        _elementsToAdd.Add(key, new List<Element>());
                }
            }
        }
    }
}
