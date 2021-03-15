using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2.Helper
{
    internal struct Sector
    {
        public float Left { get; private set; }
        public float Right { get; private set; }
        public float Back { get; private set; }
        public float Front { get; private set; }

        public int ID { get; set; }

        public Vector2 Center { get; private set; }
        public Sector(float l, float r, float b, float f)
        {
            Left = l;
            Right = r;
            Back = b;
            Front = f;

            Center = new Vector2((l + r) / 2f, (b + f) / 2f);
            ID = -1;
        }
    }
}
