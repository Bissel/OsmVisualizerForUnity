using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Request;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Math;
using UnityEngine;

namespace OsmVisualizer.Data.Provider
{

    public class BasicConverter : Provider
    {

        private readonly List<CustomTags.CustomTag> _customTags;
        private readonly List<CustomRemove.Lane> _customRemove;
        private readonly Dictionary<long, Vector2> _customNodePositions;
        
        public BasicConverter(AbstractSettingsProvider settings, 
            List<CustomTags.CustomTag> customTags,
            List<CustomRemove.Lane> customRemove,
            Dictionary<long, Vector2> customNodePositions
        ) 
            : base(settings, MapTile.InitStep.Base)
        {
            _customTags = customTags;
            _customRemove = customRemove;
            _customNodePositions = customNodePositions;
        }
        
        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            
            var setInsideTile = true;
            SettingsProvider sp = null;
            if (tile.sp is SettingsProvider provider)
            {
                setInsideTile = false;
                sp = provider;
            }

            var elements = new List<Element>(request.elements.Length);

            foreach (var element in request.elements)
            {
                var s = element.nodes[0];
                var e = element.nodes[element.nodes.Length - 1];
                
                if(_customRemove.Count(r => r.start == s || r.start == e && r.end == s || r.end == e) > 0)
                    continue;
                
                var cTags = _customTags.Where(c => c.wayId + "" == element.id).ToList();
                foreach (var cTag in cTags)
                {
                    cTag.tags.ForEach(t =>
                    {
                        if (element.tags.ContainsKey(t.tag))
                            element.tags[t.tag] = t.value;
                        else
                            element.tags.Add(t.tag, t.value);
                    });
                }
                
                // FilterNullGeometry(element);
                CalculatePoints(element, tile, sp);
                CalculatePointsOob(element, tile);
                SetGeometryType(element);
                
                element.insideTile = element.insideTile || setInsideTile;
                
                elements.Add(element);

                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }

            request.elements = elements.ToArray();
            
            stopwatch.Stop();
        }

        private static void FilterNullGeometry(Element element)
        {
            var nodes = new List<long>();
            var geom = new List<Position2>();

            for (var i = 0; i < element.geometry.Length; i++)
            {
                var pos = element.geometry[i];
                if (pos == null)
                    continue;

                nodes.Add(element.nodes[i]);
                geom.Add(pos);
            }

            if (nodes.Count == element.nodes.Length)
                return;

            element.nodes = nodes.ToArray();
            element.geometry = geom.ToArray();
        }

        private static void SetGeometryType(Element element)
        {
            if (element.nodes.Length <= 1)
                return;
            
            if (element.nodes[0] == element.nodes[element.nodes.Length - 1])
            {
                element.GeometryType = GeometryType.AREA;
                element.geometry = element.geometry?.Take(element.nodes.Length - 1).ToArray();
                element.nodes = element.nodes.Take(element.nodes.Length - 1).ToArray();
            }
            else if(element.HasProperty("area") && element.GetPropertyBool("area"))
            {
                element.GeometryType = GeometryType.AREA;
            }
            else
            {
                element.GeometryType = GeometryType.LINE;
            }
        }

        private void CalculatePoints(Element element, MapTile tile, SettingsProvider sp)
        {
            if (element.pointsV2 != null)
                return;
            
            element.pointsV2 = new List<Vector2>();

            if (element.geometry == null || element.geometry.Length == 0)
                return;
            
            var min = tile.Min;
            var max = tile.Max;

            for (var i = 0; i < element.geometry.Length; i++)
            {
                var node = element.nodes[i];

                if (!_customNodePositions.TryGetValue(node, out var p))
                {
                    var pos = element.geometry[i];
                    if (pos == null)
                        continue;
                
                    p = pos.InWorldCoords() - sp.startPosition.InWorldCoords();
                }
                
                element.pointsV2.Add(p);
            }
        }

        private static void CalculatePointsOob(Element element, MapTile tile)
        {
            element.pointsOutOfBounds = element.pointsV2.Select(p => p.IsOutOfBounds(tile.Min, tile.Max)).ToList();
            element.insideTile = element.pointsOutOfBounds.All(p => !p);
        }

    }
}