using TMPro;
using UnityEngine;

namespace OsmVisualizer.Visualisation.Components.Signs
{
    public class SignPart : MonoBehaviour
    {
        public enum Orientation
        {
            Front, Back, Left, Right
        }

        public Orientation orientation;

        public float height = 0.60f;

        public Sign.SignType type;

        public TextMeshPro text;
        
        public void Init(Sign sign)
        {
            if(sign.Type != type)
            {
                Debug.LogError($"Set wrong sign (Sign.Type was '{sign.Type}' but SignPart is actually '{type}')");
                return;
            }
            
            switch (type)
            {
                case Sign.SignType.SpeedLimit: 
                    SetSpeed(sign.Data);
                    break;
                case Sign.SignType.SpeedLimitEnd: 
                    SetSpeed(sign.Data);
                    break;
            }
        }

        private void SetSpeed(string speed)
        {
            if (text)
                text.text = speed;
        }
    }
}