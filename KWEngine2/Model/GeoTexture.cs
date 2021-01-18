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
        public string Filename { get; internal set; }
        public int OpenGLID { get; internal set; }
        public int UVMapIndex { get; internal set; }
        public Vector2 UVTransform { get; internal set; }
        public TextureType Type { get; internal set; }

        public GeoTexture(string name = null)
        {
            Type = TextureType.Albedo;
            Filename = "undefined.";
            OpenGLID = -1;
            UVMapIndex = 0;
            UVTransform = new Vector2(1, 1);
        }
    }
}
