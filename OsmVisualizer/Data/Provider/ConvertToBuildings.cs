using System.Collections;
using System.Collections.Generic;
using OsmVisualizer.Data.Characteristics;
using OsmVisualizer.Data.Request;


namespace OsmVisualizer.Data.Provider
{
    public class ConvertToBuildings : Provider
    {

        public const string ElementType = "way";
        
        public const string KeyBuilding = "building";
        public const string KeyBuildingPart = "building:part";
        
        public ConvertToBuildings(AbstractSettingsProvider settings) : base(settings, MapTile.InitStep.Building) {}


        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;

            foreach (var element in request.elements)
            {
                if (!element.insideTile || element.used || element.GeometryType != GeometryType.AREA 
                                        || element.type != ElementType || !element.HasProperty(KeyBuildingPart))
                    continue;
                
                ConvertElementToBuilding(element, tile.WayAreas, tile);
                element.used = true;

                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            foreach (var element in request.elements)
            {
                if (!element.insideTile || element.used || element.GeometryType != GeometryType.AREA 
                                        || element.type != ElementType || !element.HasProperty(KeyBuilding))
                    continue;
                
                ConvertElementToBuildingPart(element, tile.WayAreas, tile);
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

        private static void ConvertElementToBuilding(Element element, Dictionary<string, WayInterpretation> data, MapTile tile)
        {
            if (element.pointsV2.Count < 3)
                return;
            
            var characteristic = new BuildingCharacteristics(element);

            var ba = new BuildingArea(
                element.id,
                element.nodes,
                characteristic,
                element.pointsV2.ToArray()
            );
            
            data.Add(element.id, ba);
            
        }
        private static void ConvertElementToBuildingPart(Element element, Dictionary<string, WayInterpretation> data, MapTile tile)
        {
            if (element.pointsV2.Count < 3)
                return;
            
            var characteristic = new BuildingCharacteristics(element);

            var part = new BuildingPart(
                element.id,
                characteristic,
                element.GetProperty("building:part"),
                element.pointsV2.ToArray()
            );
            
            foreach (var way in data.Values)
            {
                if(way.WayType != WayInterpretation.Type.BUILDING && way.GetType() != typeof(BuildingArea))
                    continue;

                var building = (BuildingArea) way;
                
                // @todo checking if any point of element is inside the building area
                if (!building.ContainsPart(element.nodes)) 
                    continue;
                
                building.Parts.Add(part);
                return;
            }
            
            // no building to attache found
            var ba = new BuildingArea(
                element.id,
                element.nodes,
                characteristic,
                element.pointsV2.ToArray()
            );
            
            ba.Parts.Add(part);
            data.Add(element.id + "_part", ba);

        }
    }
}
