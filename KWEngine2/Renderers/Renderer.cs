using KWEngine2.GameObjects;
using KWEngine2.Helper;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.IO;

namespace KWEngine2.Renderers
{
    internal abstract class Renderer
    {
        protected Matrix4 _tmpMatrix = Matrix4.Identity;
        protected Matrix4 _modelViewProjection = Matrix4.Identity;
        protected Matrix4 _normalMatrix = Matrix4.Identity;

        public string Name { get; protected set; } = "";

        protected int mProgramId = -1;
        protected int mShaderFragmentId = -1;
        protected int mShaderVertexId = -1;

        protected int mAttribute_vpos = -1;
        protected int mAttribute_vnormal = -1;
        protected int mAttribute_vnormaltangent = -1;
        protected int mAttribute_vnormalbitangent = -1;
        protected int mAttribute_vtexture = -1;
        protected int mAttribute_vtexture2 = -1;
        protected int mAttribute_vjoints = -1;
        protected int mAttribute_vweights = -1;

        protected int mUniform_MVP = -1;
        protected int mUniform_MVPShadowMap = -1;
        protected int mUniform_MVPShadowMap2 = -1;
        protected int mUniform_NormalMatrix = -1;
        protected int mUniform_ModelMatrix = -1;
        protected int mUniform_Texture = -1;
        protected int mUniform_TextureSkybox = -1;
        protected int mUniform_UseAnimations = -1;
        protected int mUniform_BoneTransforms = -1;

        protected int mUniform_ShadowLightPosition = -1;

        protected int mUniform_Opacity = -1;

        protected int mUniform_SunPosition = -1;
        protected int mUniform_SunDirection = -1;
        protected int mUniform_SunIntensity = -1;
        protected int mUniform_SunAmbient = -1;
        protected int mUniform_SunAffection = -1;
        protected int mUniform_LightAffection = -1;

        protected int mUniform_BloomStep = -1;
        protected int mUniform_BloomTextureScene = -1;
        protected int mUniform_Resolution = -1;
        protected int mUniform_TextureNormalMap = -1;
        protected int mUniform_TextureRoughnessMap = -1;
        protected int mUniform_TextureMetalnessMap = -1;
        protected int mUniform_TextureRoughnessIsSpecular = -1;
        protected int mUniform_Roughness = -1;
        protected int mUniform_Metalness = -1;
        protected int mUniform_TextureEmissiveMap = -1;
        protected int mUniform_TextureUse = -1;
        protected int mUniform_TextureUseNormalMap = -1;
        protected int mUniform_TextureUseRoughnessMap = -1;
        protected int mUniform_TextureUseMetalnessMap = -1;
        protected int mUniform_TextureUseEmissiveMap = -1;
        protected int mUniform_TextureIsSkybox = -1;
        protected int mUniform_TextureShadowMap = -1;
        protected int mUniform_TextureShadowMap2 = -1;
        protected int mUniform_TextureUseShadowMap2 = -1;
        protected int mUniform_BaseColor = -1;
        protected int mUniform_TintColor = -1;
        protected int mUniform_Glow = -1;
        protected int mUniform_Outline = -1;

        protected int mUniform_BiasCoefficient = -1;
        protected int mUniform_BiasCoefficient2 = -1;

        protected int mUniform_uCameraPos = -1;
        protected int mUniform_uCameraDirection = -1;

        protected int mUniform_EmissiveColor = -1;

        protected int mUniform_LightsColors = -1;
        protected int mUniform_LightsPositions = -1;
        protected int mUniform_LightsTargets = -1;
        protected int mUniform_LightCount = -1;

        protected int mUniform_TextureTransform = -1;

        protected int mUniform_TextureLightMap = -1;
        protected int mUniform_TextureUseLightMap = -1;
        protected int mUniform_TextureHUDOffset = -1;
        protected int mUniform_TextureHUDIsText = -1;

        protected int mUniform_SpecularReflectionFactor = -1;

        public Renderer()
        {
            Initialize();
        }

        public void Dispose()
        {
            if (mProgramId >= 0)
            {
                GL.DeleteProgram(mProgramId);
                GL.DeleteShader(mShaderVertexId);
                GL.DeleteShader(mShaderFragmentId);
                mProgramId = -1;
            }
        }

        protected int LoadShader(Stream pFileStream, ShaderType pType, int pProgram)
        {
            int address = GL.CreateShader(pType);
            using (StreamReader sr = new StreamReader(pFileStream))
            {
                GL.ShaderSource(address, sr.ReadToEnd());
            }
            GL.CompileShader(address);
            GL.AttachShader(pProgram, address);
            return address;
        }
        
        public abstract void Initialize();

        public int GetProgramId()
        {
            return mProgramId;
        }

        public int GetAttributeHandlePosition()
        {
            return mAttribute_vpos;
        }

        public int GetAttributeHandleTexture2()
        {
            return mAttribute_vtexture2;
        }

        public int GetUniformHandleColorTint()
        {
            return mUniform_TintColor;
        }

        public int GetAttributeHandleNormals()
        {
            return mAttribute_vnormal;
        }

        public int GetAttributeHandleNormalTangents()
        {
            return mAttribute_vnormaltangent;
        }

        public int GetUniformHandleResolution()
        {
            return mUniform_Resolution;
        }

        public int GetAttributeHandleNormalBiTangents()
        {
            return mAttribute_vnormalbitangent;
        }

        public int GetAttributeHandleTexture()
        {
            return mAttribute_vtexture;
        }

        public int GetUniformHandleMVP()
        {
            return mUniform_MVP;
        }

        public int GetUniformHandleGlow()
        {
            return mUniform_Glow;
        }
        public int GetUniformHandleM()
        {
            return mUniform_ModelMatrix;
        }

        public int GetUniformHandleN()
        {
            return mUniform_NormalMatrix;
        }

        public int GetUniformHandleTexture()
        {
            return mUniform_Texture;
        }

        public int GetUniformHandleHasNormalMap()
        {
            return mUniform_TextureUseNormalMap;
        }

        public int GetUniformHandleBloomStep()
        {
            return mUniform_BloomStep;
        }

        public int GetUniformHandleHasTexture()
        {
            return mUniform_TextureUse;
        }

        public int GetUniformHandleTextureShadowMap()
        {
            return mUniform_TextureShadowMap;
        }

        public int GetUniformHandleTextureScene()
        {

            return mUniform_BloomTextureScene;
        }

        public int GetUniformHandleSunAmbient()
        {
            return mUniform_SunAmbient;
        }
        public int GetUniformBoneTransforms()
        {
            return mUniform_BoneTransforms;
        }

        public int GetUniformHasBones()
        {
            return mUniform_UseAnimations;
        }

        public int GetUniformSunPosition()
        {
            return mUniform_SunPosition;
        }

        public int GetUniformSunDirection()
        {
            return mUniform_SunDirection;
        }

        public int GetUniformSunIntensity()
        {
            return mUniform_SunIntensity;
        }

        public int GetUniformSunAffection()
        {
            return mUniform_SunAffection;
        }

        public int GetUniformBaseColor()
        {
            return mUniform_BaseColor;
        }
     
        public int GetUniformEmissive()
        {
            return mUniform_EmissiveColor;
        }

        public int GetUniformHandleMVPShadowMap()
        {
            return mUniform_MVPShadowMap;
        }

        public int GetUniformHandleTextureSkybox()
        {
            return mUniform_TextureSkybox;
        }

        public int GetUniformHandleTextureIsSkybox()
        {
            return mUniform_TextureIsSkybox;
        }

        public int GetUniformHandleLightsColors()
        {
            return mUniform_LightsColors;
        }

        public int GetUniformHandleLightsPositions()
        {
            return mUniform_LightsPositions;
        }

        public int GetUniformHandleLightsTargets()
        {
            return mUniform_LightsTargets;
        }

        public int GetUniformHandleLightCount()
        {
            return mUniform_LightCount;
        }

        public int GetUniformHandleUseSpecularMap()
        {
            return mUniform_TextureUseRoughnessMap;
        }

        public int GetUniformHandleSpecularMap()
        {
            return mUniform_TextureRoughnessMap;
        }

        public int GetUniformHandleUseNormalMap()
        {
            return mUniform_TextureUseNormalMap;
        }

        public int GetUniformHandleNormalMap()
        {
            return mUniform_TextureNormalMap;
        }

        public int GetUniformHandleCameraPosition()
        {
            return mUniform_uCameraPos;
        }

        public int GetUniformHandleTextureTransform()
        {
            return mUniform_TextureTransform;
        }

        public int GetUniformHandleTextureLightmap()
        {
            return mUniform_TextureLightMap;
        }

        public int GetUniformHandleTextureUseLightmap()
        {
            return mUniform_TextureUseLightMap;
        }

        internal abstract void Draw(GameObject g, ref Matrix4 viewProjection);
        internal abstract void Draw(GameObject g, ref Matrix4 viewProjection, HelperFrustum frustum, bool isSun);
        internal abstract void Draw(GameObject g, ref Matrix4 viewProjection, HelperFrustum frustum);
        internal abstract void Draw(GameObject g, ref Matrix4 viewProjection, ref Matrix4 viewProjectionShadow, ref Matrix4 viewProjectionShadow2, HelperFrustum frustum, ref float[] lightColors, ref float[] lightTargets, ref float[] lightPositions, int lightCount, ref int lightShadow );
        internal abstract void Draw(ParticleObject po, ref Matrix4 viewProjection);
        internal abstract void Draw(HUDObject ho, ref Matrix4 viewProjection);
    }
}
