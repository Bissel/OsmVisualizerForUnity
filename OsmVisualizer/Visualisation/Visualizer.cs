using System.Collections;
using System.Linq;
using OsmVisualizer.Data;
using UnityEngine;

namespace OsmVisualizer.Visualisation
{
    public enum Mode
    {
        MODE_2D, MODE_3D
    }
    public class Visualizer : MonoBehaviour
    {
        public Mode mode = Mode.MODE_3D;

        public float yOffset = 0f;

        private VisualizerComponent[] _visualizerComponents;
        private bool[] _activeVisualizerComponents;

        private void Start()
        {
            _visualizerComponents = GetComponents<VisualizerComponent>();
            _activeVisualizerComponents = _visualizerComponents.Select(c => c.enabled).ToArray();
        }

        public IEnumerator UpdateTile(MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            for (var i = 0; i < _visualizerComponents.Length; i++)
            {
                var comp = _visualizerComponents[i];
                var lastActive = _activeVisualizerComponents[i];
                var currActive = comp.enabled;
                
                if(lastActive == currActive)
                    continue;

                _activeVisualizerComponents[i] = currActive;

                yield return currActive 
                    ? comp.CreateComponent(tile, stopwatch) 
                    : comp.Destroy(tile);
            }
        }

        public IEnumerator Create(MapTile tile, System.Diagnostics.Stopwatch stopwatch)
        {
            foreach (var comp in _visualizerComponents)
            {
                if(!comp.enabled)
                    continue;
                
                yield return comp.CreateComponent(tile, stopwatch);
            }

            yield return null;
        }

    }
}