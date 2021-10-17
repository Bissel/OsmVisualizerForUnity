using System.Collections;
using System.Linq;
using OsmVisualizer.Data.Request;

namespace OsmVisualizer.Data.Provider
{
    public class SetHeight : Provider
    {

        public SetHeight(AbstractSettingsProvider settings) : base(settings, MapTile.InitStep.Height) {}


        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;

            var bridges = tile.WayAreas.Values
                .Where(a => a.WayType == WayInterpretation.Type.BRIDGE)
                .Select(b => (Bridge)b)
                .ToList();
            
            foreach (var bridge in bridges)
            {
                bridge.SetHeight();
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }

            foreach (var inter in tile.Intersections.Values)
            {
                if(inter == null) continue;
                
                inter.SetHeightOffset();
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }

            foreach (var lc in tile.LaneCollections.Values)
            {
                lc.SetHeightOffset();
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            // foreach (var lc in tile.LaneCollections.Values)
            // {
            //     lc.SetHeightOffsetRamps();
            //     
            //     if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
            //         continue;
            //     
            //     stopwatch.Stop();
            //     yield return null;
            //     stopwatch.Start();
            //     startTime = stopwatch.ElapsedMilliseconds;
            // }
            //
            // foreach (var inter in tile.Intersections.Values)
            // {
            //     if (inter == null) continue;
            //     
            //     inter.LockHeightOffsetRamps();
            //     
            //     if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
            //         continue;
            //     
            //     stopwatch.Stop();
            //     yield return null;
            //     stopwatch.Start();
            //     startTime = stopwatch.ElapsedMilliseconds;
            // }
            //
            // foreach (var lc in tile.LaneCollections.Values)
            // {
            //     if (lc == null) continue;
            //     lc.LockHeightOffsetRamps();
            //     
            //     if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
            //         continue;
            //     
            //     stopwatch.Stop();
            //     yield return null;
            //     stopwatch.Start();
            //     startTime = stopwatch.ElapsedMilliseconds;
            // }
            
            // var i = 0;
            // foreach (var lc in tile.laneCollections.Values)
            // {
            //     if(!lc.HasHeightOffset) continue;
            //
            //     lc.SetHeightOffsetRamps(true);
            //     
            //     i++;
            //     if (i % 100 == 0)
            //         yield return null;
            // }
            // foreach (var lc in tile.laneCollections.Values)
            // {
            //     if(lc.HasHeightOffset) continue;
            //     
            //     lc.SetHeightOffsetRamps();
            //     
            //     i++;
            //     if (i % 100 == 0)
            //         yield return null;
            // }
            
            stopwatch.Stop();
        }
    }
}
