using System.Collections.Generic;
using OsmVisualizer.Mesh;
using UnityEngine;

namespace OsmVisualizer.Helper
{
    [RequireComponent(typeof(Map))]
    public class MeshBuilderList : MonoBehaviour
    {
        
        private AbstractSettingsProvider _settings;
        // ReSharper disable once MemberCanBePrivate.Global
        protected AbstractSettingsProvider Settings() => _settings ??= gameObject.GetComponent<Map>().settingsProvider;
        
        public List<MeshBuilder> MeshBuilders { get; protected set; }
        
        public void Start()
        {
            var matTar = Resources.Load("OSM/Materials/Tar", typeof(Material)) as Material;
            var matCobblestone = Resources.Load("OSM/Materials/Cobblestone", typeof(Material)) as Material;
            var matDirt = Resources.Load("OSM/Materials/SurfaceDirt", typeof(Material)) as Material;
            var matConcrete = Resources.Load("OSM/Materials/Concrete", typeof(Material)) as Material;
            var matPavingStones = Resources.Load("OSM/Materials/PavingStones", typeof(Material)) as Material;
            var matWood = Resources.Load("OSM/Materials/SurfaceWood", typeof(Material)) as Material;

            var matMarkings = Resources.Load("OSM/Materials/Markings", typeof(Material)) as Material;
            var matMarkingsDashed = Resources.Load("OSM/Materials/MarkingsDashed", typeof(Material)) as Material;

            MeshBuilders = new List<MeshBuilder>
            {
                new SimpleRoadMeshBuilder(
                    Settings(),
                    new []{            
                        "motorway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential",
                        "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link",
                        "living_street", 
                        "service", "emergency_bay",
                        "pedestrian", "track", "platform", "bus_stop", "crossing",
                        "road",
                        "cycleway"
                    },
                    new Dictionary<string, Material>
                    {
                        // {"paved", matTar},
                        // {"asphalt", matTar},
                        
                        {"concrete", matConcrete},
                        {"paving_stones", matPavingStones},
                        {"cobblestone", matCobblestone},
                        {"sett", matCobblestone},
                        {"unhewn_cobblestone", matCobblestone},
                        {"wood", matWood},
                        {"unpaved", matDirt},
                        {"compacted", matDirt},
                        {"ground", matDirt},
                        {"dirt", matDirt},
                        {"earth", matDirt},
                        {"mud", matDirt},
                        {"sand", matDirt},

                    },
                    matTar,
                    matMarkings,
                    matMarkingsDashed,
                    matConcrete
                )
            };
        }
    }
}
