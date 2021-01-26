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
        public bool SpecularReflection;

        public bool TextureRoughnessIsSpecular;
        public bool TextureRoughnessInMetalness;

    
        public void SetTexture(string texture, TextureType type, int id)
        {
            if (type == TextureType.Albedo)
            {
                TextureAlbedo = new GeoTexture() { Filename = texture, OpenGLID = id, Type = type, UVMapIndex = 0, UVTransform = new Vector2(TextureAlbedo.UVTransform.X == 0 ? 1 : TextureAlbedo.UVTransform.X, TextureAlbedo.UVTransform.Y == 0 ? 1 : TextureAlbedo.UVTransform.Y) };
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
