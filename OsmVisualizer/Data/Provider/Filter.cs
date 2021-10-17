using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OsmVisualizer.Data.Request;
using UnityEngine;

namespace OsmVisualizer.Data.Provider
{
    public class Filter : Provider
    {

        private readonly string[] _types;
        private readonly Dictionary<string, Regex> _tags;

        public Filter(string[] types, Dictionary<string, Regex> tags, AbstractSettingsProvider settings) : base(settings, MapTile.InitStep.Filter)
        {
            _types = types;
            _tags = tags;
        }

        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;

            var filteredElements = new List<Element>();
            var tagKeys = _tags.Keys;

            foreach (var e in request.elements)
            {
                if (!_types.Contains(e.type))
                {
                    filteredElements.Add(e);
                    continue;
                }

                foreach (var elementTag in e.tags.Keys)
                {
                    if (!tagKeys.Contains(elementTag))
                        continue;

                    _tags.TryGetValue(elementTag, out var matcher);

                    if(!matcher.IsMatch(e.GetProperty(elementTag)))
                        continue;
                    
                    filteredElements.Add(e);
                    break;
                }

            }

            request.elements = filteredElements.ToArray();

            stopwatch.Stop();
            if (stopwatch.ElapsedMilliseconds - startTime > tile.sp.maxFrameTime)
                yield return null;
        }
    }
}