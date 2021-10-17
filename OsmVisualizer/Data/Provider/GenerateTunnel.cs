using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Request;
using UnityEngine;

namespace OsmVisualizer.Data.Provider
{
    public class GenerateTunnel : Provider
    {
        
        public GenerateTunnel(AbstractSettingsProvider settings) : base(settings, MapTile.InitStep.Tunnel) {}


        public override IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            var startTime = stopwatch.ElapsedMilliseconds;

            var tunnel = new List<Tunnel>();
            var lcUsed = new List<MapData.LaneId>();
            
            foreach (var lc in tile.LaneCollections.Values)
            {
                // @todo make rail work
                if(lc == null || lc.Id.Type != MapData.LaneType.HIGHWAY)
                    continue;
                
                if (!lc.Characteristics.IsTunnel || lcUsed.Contains(lc.Id))
                    continue;
                
                lcUsed.Add(lc.Id);
                if(lc.OtherDirection is {} other)
                    lcUsed.Add(other.Id);

                var id = "Tunnel " + lc.Id;
                var t = new Tunnel(id, lc);
                tile.WayAreas.Add(id, t);
                tunnel.Add(t);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }

            
            // @todo merge tunnel if outline + 1m overlaps
            // var removeTunnel = new List<Tunnel>();
            // foreach (var t in tunnel)
            // {
            //     if(removeTunnel.Contains(t))
            //         continue;
            //     
            //     foreach(var other in tunnel)
            //     {
            //         // if(true)
            //         //     continue;
            //
            //         var left = true;
            //         var otherLeft = false;
            //         
            //         removeTunnel.Add(other);
            //         t.MergeTunnel(other, left, otherLeft);
            //     }
            // }
            
            stopwatch.Stop();
        }
        
        
    }
}