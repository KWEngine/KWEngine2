namespace KWEngine2.Helper
{
    internal struct TextureSet
    {
        public int meshCount;
        public int[] albedo;
        public int[] normal;
        public int[] metalness;
        public int[] roughness;
        public int[] emissive;
        public float[,] uvTransform;

        public TextureSet(int meshCount = 0)
        {
            this.meshCount = meshCount;
            if (meshCount > 0)
            {
                this.albedo = new int[meshCount];
                this.normal = new int[meshCount];
                this.metalness = new int[meshCount];
                this.roughness = new int[meshCount];
                this.emissive = new int[meshCount];
                this.uvTransform = new float[meshCount, 2];
                this.uvTransform[0, 0] = 1;
                this.uvTransform[0, 1] = 1;
            }
            else
            {
                this.albedo = null;
                this.normal = null;
                this.metalness = null;
                this.roughness = null;
                this.emissive = null;
                this.uvTransform = null;
            }
        }

        public void SetUVTransform(int cubeSide, float u, float v)
        {
            if (cubeSide == 10)
            {
                for (int i = 0; i < meshCount; i++)
                {
                    uvTransform[i,0] = u;
                    uvTransform[i, 1] = v;
                }
            }
            else
            {
                uvTransform[cubeSide, 0] = u;
                uvTransform[cubeSide, 1] = v;
            }
        }

        public void SetTexture(int textureId, int cubeSide, TextureType type)
        {
            if(cubeSide == 10)
            {
                for(int i = 0; i < meshCount; i++)
                {
                    if (type == TextureType.Albedo)
                    {
                        albedo[i] = textureId;
                    }
                    else if (type == TextureType.Normal)
                    {
                        normal[i] = textureId;
                    }
                    else if (type == TextureType.Roughness)
                    {
                        roughness[i] = textureId;
                    }
                    else if (type == TextureType.Metalness)
                    {
                        metalness[i] = textureId;
                    }
                    else if (type == TextureType.Emissive)
                    {
                        emissive[i] = textureId;
                    }
                }
            }
            else
            {
                if (type == TextureType.Albedo)
                {
                    albedo[cubeSide] = textureId;
                }
                else if (type == TextureType.Normal)
                {
                    normal[cubeSide] = textureId;
                }
                else if (type == TextureType.Roughness)
                {
                    roughness[cubeSide] = textureId;
                }
                else if (type == TextureType.Metalness)
                {
                    metalness[cubeSide] = textureId;
                }
                else if (type == TextureType.Emissive)
                {
                    emissive[cubeSide] = textureId;
                }
            }
            
        }
    }
}
