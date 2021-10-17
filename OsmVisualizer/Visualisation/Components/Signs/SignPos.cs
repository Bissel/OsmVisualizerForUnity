using System;
using OsmVisualizer.Data;

namespace OsmVisualizer.Visualisation.Components.Signs
{
    [Serializable]
    public class SignSimplePos
    {
        public const int L = -1; 
        public const int R = +1; 
        public const int S = -1; 
        public const int E = +1; 
        public const int Both = 0;
        
        public int PosX;
        public int PosY;

        public SignSimplePos(int posX, int posY)
        {
            PosX = posX;
            PosY = posY;
        }
        
        public override bool Equals(object obj)
        {
            return obj is SignSimplePos other && Equals(other);
        }

        protected bool Equals(SignSimplePos other)
        {
            return PosX == other.PosX && PosY == other.PosY;
        }

        public override int GetHashCode()
        {
            var hashCode = PosX;
            hashCode = (hashCode * 397) ^ PosY;
            return hashCode;
        }

        public override string ToString()
        {
            return $"{PosX},{PosY}";
        }
    }
    
    public class SignPos : SignSimplePos
    {

        public readonly LaneCollection Lc;
        public readonly MapData.LaneId LaneId;

        public SignPos(LaneCollection lc, int posX, int posY) : base(posX, posY)
        {
            Lc = lc;
            LaneId = Lc.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is SignPos other && Equals(other);
        }

        protected bool Equals(SignPos other)
        {
            return Equals(LaneId, other.LaneId) && PosX == other.PosX && PosY == other.PosY;
        }

        public override int GetHashCode()
        {
            var hashCode = (LaneId != null ? LaneId.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ base.GetHashCode();
            return hashCode;
        }
    }
}