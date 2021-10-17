using System.Collections;
using System.Linq;
using OsmVisualizer.Data.Request;

namespace OsmVisualizer.Data.Provider
{
    public class RemoveBorderLaneCollections : Provider
    {
        public RemoveBorderLaneCollections(AbstractSettingsProvider settings) : base(settings, MapTile.InitStep.RemoveBorderLc) { }

        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;
            var toDelete = tile.LaneCollections.Values.Where(lc => lc.IsDummy).ToList();

            foreach (var lc in toDelete)
            {
                tile.LaneCollections.Remove(lc.Id);
                tile.DummyLaneCollections.Add(lc.Id, lc);

                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            stopwatch.Stop();
        }
    }
}