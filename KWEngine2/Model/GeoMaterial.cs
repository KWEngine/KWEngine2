using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace KWEngine2.Model
{
    internal struct GeoMaterial
    {
        public string Name;
        public BlendingFactor BlendMode;
        public Vector4 ColorEmissive;
        public Vector4 ColorAlbedo;

        public float Opacity;

        public GeoTexture TextureAlbedo;
        public GeoTexture TextureNormal;
        public GeoTexture TextureEmissive;
        public GeoTexture TextureLight;
        public GeoTexture TextureMetalness;
        public GeoTexture TextureRoughness;

        public float Metalness;
        public float Roughness;

        public bool TextureRoughnessIsSpecular;
        public bool TextureRoughnessInMetalness;

        /*
        public GeoMaterial(string name)
        {
            BlendMode = BlendingFactor.OneMinusSrcAlpha;
            ColorEmissive = new Vector4(0, 0, 0, 0);
            ColorAlbedo = new Vector4(1, 1, 1, 1);
            Opacity = 1;
            TextureAlbedo = new GeoTexture();
            TextureNormal = new GeoTexture();
            TextureMetalness = new GeoTexture();
            TextureRoughness = new GeoTexture();
            Metalness = 0;
            Roughness = 1;
            TextureEmissive = new GeoTexture();
            TextureLight = new GeoTexture();
            TextureRoughnessInMetalness = false;
            TextureRoughnessIsSpecular = false;
            Name = name;
        }
        */
     
        public void SetTexture(string texture, TextureType type, int id)
        {
            if (type == TextureType.Albedo)
            {
                TextureAlbedo = new GeoTexture() { Filename = texture, OpenGLID = id, Type = type, UVMapIndex = 0, UVTransform = new Vector2(TextureAlbedo.UVTransform.X, TextureAlbedo.UVTransform.Y) };
            }
            else if (type == TextureType.Emissive)
            {
                TextureEmissive = new GeoTexture() { Filename = texture, OpenGLID = id, Type = type, UVMapIndex = 0, UVTransform = new Vector2(1, 1) };
            }
            else if (type == TextureType.Light)
            {
                TextureLight = new GeoTexture() { Filename = texture, OpenGLID = id, Type = type, UVMapIndex = 1, UVTransform = new Vector2(1, 1) };
            }
            else if (type == TextureType.Metalness)
            {
                TextureMetalness = new GeoTexture() { Filename = texture, OpenGLID = id, Type = type, UVMapIndex = 0, UVTransform = new Vector2(1, 1) };
            }
            else if (type == TextureType.Roughness)
            {
                TextureRoughness = new GeoTexture() { Filename = texture, OpenGLID = id, Type = type, UVMapIndex = 0, UVTransform = new Vector2(1, 1) };
            }
            else if (type == TextureType.Normal)
            {
                TextureNormal = new GeoTexture() { Filename = texture, OpenGLID = id, Type = type, UVMapIndex = 0, UVTransform = new Vector2(1, 1) };
            }
            else
                throw new System.Exception("Unknown texture type for current material " + Name);
        }
    }
}
