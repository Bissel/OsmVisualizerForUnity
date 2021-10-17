using it.bissel;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Types;
using UnityEngine;
using UnityEngine.Serialization;

namespace OsmVisualizer
{
    public class SettingsProvider : AbstractSettingsProvider
    {
        [Tooltip("Uses cache around Starting Position")]
        public bool useCache = true;

        // [Tooltip("Uses cache around Starting Position in the Build.")]
        // public bool BakeCache = true;

        public Position2 startPosition = new Position2(53.0778f,8.810003f);

        /**
         * For bigger things, please create your own OSM-Overpass-Api server
         * https://wiki.openstreetmap.org/wiki/Overpass_API
         */
        [Tooltip("For bigger things, please create your own OSM-Overpass-Api server (https://wiki.openstreetmap.org/wiki/Overpass_API)")]
        public string overpassUri = "";

        [Tooltip("Please set this on public servers")]
        public bool useRequestQueue = true;

        [Tooltip("Please set this on public servers >= 10")]
        [Range(0f,60f)]
        public int requestTimeout = 10;

        public string GetOverpassUri()
        {
            return GlobalSettings.GetInstance().SettingsReplacer(overpassUri);
        }
    }
}
