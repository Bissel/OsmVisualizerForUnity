using System.Collections;
using OsmVisualizer.Data.Request;
using UnityEngine;

namespace OsmVisualizer.Data.Provider
{
    public abstract class Provider
    {
        protected AbstractSettingsProvider Settings;

        public readonly MapTile.InitStep Step;
        
        public Provider(AbstractSettingsProvider settings, MapTile.InitStep step)
        {
            Settings = settings;
            Step = step;
        }
        
        public abstract IEnumerator Convert(Result request, MapData data, MapTile tile, System.Diagnostics.Stopwatch stopwatch);
    }
}