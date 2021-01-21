using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KWEngine2.KWEngine;

namespace KWEngine2.Model
{
    internal struct GeoTexture
    {
        public string Filename;
        public int OpenGLID;
        public int UVMapIndex;
        public Vector2 UVTransform;
        public TextureType Type;
    }
}
