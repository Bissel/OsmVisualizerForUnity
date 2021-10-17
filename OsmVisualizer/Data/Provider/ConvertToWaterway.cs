using System.Collections;
using System.Collections.Generic;
using OsmVisualizer.Data.Request;

namespace OsmVisualizer.Data.Provider
{
    public class ConvertToWaterway : Provider
    {

        public const string ElementType = "way";
        
        public const string KeyWaterway = "waterway";

        public ConvertToWaterway(AbstractSettingsProvider settings) : base(settings, MapTile.InitStep.Waterway) {}


        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;

            foreach(var element in request.elements)
            {
                if (!element.insideTile || element.used || element.GeometryType != GeometryType.AREA 
                    || element.type != ElementType || !element.HasProperty(KeyWaterway) 
                    || element.HasProperty(ConvertToBuildings.KeyBuilding) || element.HasProperty(ConvertToBuildings.KeyBuildingPart) )
                    continue;
                
                ConvertElementToWaterway(element, tile.WayAreas);
                element.used = true;
            
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }      
            
            foreach(var element in request.elements)
            {
                if(!element.insideTile || element.used || element.GeometryType != GeometryType.AREA)
                    continue;
                
                var natural = element.GetProperty("natural");
                
                if(natural != "coastline")
                    continue;
                
                AddCoastlineToWaterway(element, tile.WayAreas);
                element.used = true;
            
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            foreach(var element in request.elements)
            {
                if (!element.insideTile || element.used || element.GeometryType != GeometryType.AREA 
                    || element.type != ElementType)
                    continue;

                var natural = element.GetProperty("natural");
                var waterway = element.GetProperty("waterway");
                var water = element.GetProperty("water");
                if( ! ( natural == "water" || natural == "riverbed" || waterway == "riverbank" || water != null) )
                    continue;
                
                ConvertElementToWaterwayArea(element, tile.WayAreas);
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

        private static void AddCoastlineToWaterway(Element element, Dictionary<string, WayInterpretation> data)
        {

            // @todo may need to be redone
            
            var coastline = new Coastline(element.id, element.pointsV2);
            
            foreach (var way in data.Values)
            {
                if(way.GetType() != typeof(Waterway))
                    continue;

                var waterway = (Waterway) way;
                
                if (!waterway.ContainsCoastline(element.nodes)) 
                    continue;
                
                waterway.Coastlines.Add(coastline);
                return;
            }
        }

        private static void ConvertElementToWaterway(Element element, Dictionary<string, WayInterpretation> data)
        {
            data.Add( element.id + "_Waterway", new Waterway(
                element.id, 
                element.GetProperty(ElementType),
                element.GetProperty("name"),
                element.GetPropertyMeasurement("width"),
                element.pointsV2,
                element.nodes
            ) );
        }

        private static void ConvertElementToWaterwayArea(Element element, Dictionary<string, WayInterpretation> data)
        {
            var naturalWater = new NaturalWater(
                element.id,
                element.pointsV2
            );
            
            data.Add( element.id, naturalWater );
            
            // foreach (var way in data.Values)
            // {
            //     if(way.GetType() != typeof(Waterway))
            //         continue;
            //
            //     var waterway = (Waterway) way;
            //     
            //     if (!waterway.ContainsWaterArea(element.nodes)) 
            //         continue;
            //     
            //     waterway.Areas.Add(naturalWater);
            //     break;
            // }
        }
        
    }
}
