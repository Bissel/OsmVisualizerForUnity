using OsmVisualizer.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace OsmVisualizer
{
    public abstract class AbstractSettingsProvider : MonoBehaviour
    {
        // in Assets/Resources/
        public string assetsDirTrees = "OSM/Models/Trees";

        public string assetsDirFacades = "OSM/Models/Facades";

        [Tooltip("Size of a Tile x-z in Meters")]
        [Min(50f)]
        public int tileSize = 500;

        [Tooltip("Size of the border around a tile, helps with interlocking tiles / intersections")]
        [Range(0f, 100f)]
        public int tileSizeBorderWidth = 50;

        [Tooltip("A movable object, acts as the center for the visibility radius")]
        public Transform mapCenter;
        
        [Tooltip("The radius around the MapCenter that is visible")]
        [Range(0f,50f)]
        public int visibleRadiusInTileCount = 2;
        
        [Tooltip("The buffer for visible tiles outside of the radius around the MapCenter")]
        [Range(0f,50f)]
        public int visibleRadiusBuffer = 1;

        public int targetFPS = 90;

        [HideInInspector]
        public long maxFrameTime;

        public void Start()
        {
            maxFrameTime = 1000 / targetFPS;
        }
    }
}
