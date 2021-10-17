
using System.Collections;
using OsmVisualizer.Data;
using UnityEngine;

namespace OsmVisualizer.Mesh {

    public abstract class MeshBuilder
    {
        protected AbstractSettingsProvider settings;
        public bool IsEnabled = true;
        
        public MeshBuilder(AbstractSettingsProvider settings)
        {
            this.settings = settings;
        }
        
        public abstract IEnumerator Create(MapData data, MapTile tile);
        public abstract IEnumerator Destroy(MapData data, MapTile tile);

    }

}