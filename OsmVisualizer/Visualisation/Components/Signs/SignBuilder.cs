using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace OsmVisualizer.Visualisation.Components.Signs
{
    public class SignBuilder : MonoBehaviour
    {
        [Serializable]
        public class SignTypeToPart
        {
            public Sign.SignType type;
            public SignSimplePos pos;
            public GameObject part;
        }
        
        [Description("Sign parts, ordering is then placed bottom to top at the sign holder")]
        public List<SignTypeToPart> parts;

        public Transform polePart;
        
        public Transform signHolder;

        public float additionalOffsetForPole = -.05f;

        public void Init(List<Sign> signs, SignSimplePos pos)
        {
            var offsetFront = 0f;
            var offsetBack = 0f;
            foreach (var part in parts)
            {
                if (
                       part.pos.PosX != SignSimplePos.Both && pos.PosX != part.pos.PosX 
                    || part.pos.PosY != SignSimplePos.Both && pos.PosY != part.pos.PosY
                ) continue;
                
                var signsForType = signs.Where(s => s.Type == part.type).ToList();
                
                if (signsForType.Count == 0) continue;

                foreach(var s in signsForType)
                    AddSignPart(part.part, s, ref offsetFront, ref offsetBack);
            }
            
            SetPole(Mathf.Max(offsetFront, offsetBack) + additionalOffsetForPole);
        }

        private void SetPole(float offset)
        {
            var poleTransform = polePart.transform;
            poleTransform.localPosition = Vector3.up * (offset * .5f + signHolder.transform.localPosition.y);
            var poleScale = poleTransform.localScale;
            poleTransform.localScale = new Vector3(
                poleScale.x,
                offset * .5f,
                poleScale.z
            );
        }

        private void AddSignPart(GameObject part, Sign sign, ref float offsetFront, ref float offsetBack)
        {
            var inst = Instantiate(part, signHolder);
                    
            var sp = inst.GetComponent<SignPart>();

            var offset = sp.height * .5f;
            switch (sp.orientation)
            {
                case SignPart.Orientation.Front:
                    offset += offsetFront;
                    offsetFront += sp.height;
                    break;
                case SignPart.Orientation.Back:
                    offset += offsetBack;
                    offsetBack += sp.height;
                    break;
                case SignPart.Orientation.Left:
                case SignPart.Orientation.Right:
                default:
                    offset += Mathf.Max(offsetFront, offsetBack);
                    offsetFront = offsetBack = offset + sp.height * .5f;
                    break;
            }
            
            inst.transform.localPosition = Vector3.up * offset;

            sp.Init(sign);
        }
    }
}