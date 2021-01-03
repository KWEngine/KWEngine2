using System;

namespace KWEngine2.Model
{
    internal struct GeoVertex
    {
        public override string ToString()
        {
            return Index + ": " + BoneIDs[0] + " - " + Math.Round(Weights[0], 3) + ", " + BoneIDs[1] + " - " + Math.Round(Weights[1], 3) + ", " + BoneIDs[2] + " - " + Math.Round(Weights[2], 3);
        }
        public int Index { get; internal set; }
        public float X { get; internal set; }
        public float Y { get; internal set; }
        public float Z { get; internal set; }
        public float[] Weights;
        public uint[] BoneIDs;
        internal int WeightSet;

        public GeoVertex(int i, float x, float y, float z)
        {
            Index = i;

            X = x;
            Y = y;
            Z = z;

            Weights = new float[KWEngine.MAX_BONE_WEIGHTS];
            BoneIDs = new uint[KWEngine.MAX_BONE_WEIGHTS];

            WeightSet = 0;
        }
        
    }
}
