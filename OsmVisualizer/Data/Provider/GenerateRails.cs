using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using OsmVisualizer.Data.Characteristics;
using OsmVisualizer.Data.Request;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using OsmVisualizer.Math;
using UnityEngine;


namespace OsmVisualizer.Data.Provider
{
    public class GenerateRails : Provider
    {

        public GenerateRails(AbstractSettingsProvider settings) : base(settings, MapTile.InitStep.Rails) {}


        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            
            foreach(var element in request.elements)
            {
                if ( 
                       /*!element.insideTile // @todo split railway ways on tile bounds
                    || */ element.type != "way"
                    || element.GeometryType != GeometryType.LINE
                    || !element.HasProperty("railway"))
                    continue;
                
                ConvertElement(element, data, tile);
                element.used = true;

                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            stopwatch.Stop();
        }

        private static void ConvertElement(Element element, MapData data, MapTile tile)
        {
            if (element.nodes.Length < 2)
                return;
            
            var id = new MapData.LaneId(element.nodes, MapData.LaneType.RAILWAY);
            
            if (tile.LaneCollections.ContainsKey(id))
                return;
            
            var characteristic = new RailWayCharacteristics(element);

            var laneCount = characteristic.Tracks;
            var width = laneCount * (characteristic.RailType == "Tram" ? 2f : 3f) * characteristic.Gauge;
            
            var lc = GenerateLaneCollection(
                id,
                characteristic,
                laneCount,
                element.pointsV2.ToArray(),
                null,
                width * .5f,
                width / laneCount,
                element.nodes
            );
            
            tile.AddLaneCollection(lc);
        }


        private static LaneCollection GenerateLaneCollection(MapData.LaneId id, RailWayCharacteristics c, int laneCountDir, Vector2[] points, Direction[][] directions, float wHalf, float laneWidth, long[] nodes)
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
                false,
                id
            );
        }
        
    }
}
