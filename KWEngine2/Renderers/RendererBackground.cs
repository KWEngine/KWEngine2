using KWEngine2.GameObjects;
using KWEngine2.Helper;
using KWEngine2.Model;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2.Renderers
{
    internal class RendererBackground : Renderer
    {
        private int mUniformXYClip = -1;
        private int mUniformXYOffset = -1;


        public override void Initialize()
        {
            Name = "Background";

            mProgramId = GL.CreateProgram();

            string resourceNameFragmentShader = "KWEngine2.Shaders.shader_fragment_background.glsl";
            string resourceNameVertexShader = "KWEngine2.Shaders.shader_vertex_background.glsl";
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream s = assembly.GetManifestResourceStream(resourceNameVertexShader))
            {
                mShaderVertexId = LoadShader(s, ShaderType.VertexShader, mProgramId);
                //Console.WriteLine(GL.GetShaderInfoLog(mShaderVertexId));
            }

            using (Stream s = assembly.GetManifestResourceStream(resourceNameFragmentShader))
            {
                mShaderFragmentId = LoadShader(s, ShaderType.FragmentShader, mProgramId);
                //Console.WriteLine(GL.GetShaderInfoLog(mShaderFragmentId));
            }

            if (mShaderFragmentId >= 0 && mShaderVertexId >= 0)
            {
                GL.BindAttribLocation(mProgramId, 0, "aPosition");
                GL.BindAttribLocation(mProgramId, 2, "aTexture");

                GL.BindFragDataLocation(mProgramId, 0, "color");
                GL.BindFragDataLocation(mProgramId, 1, "bloom");

                GL.LinkProgram(mProgramId);
            }
            else
            {
                throw new Exception("Creating and linking shaders failed.");
            }

            mAttribute_vpos = GL.GetAttribLocation(mProgramId, "aPosition");
            mAttribute_vtexture = GL.GetAttribLocation(mProgramId, "aTexture");

            mUniform_MVP = GL.GetUniformLocation(mProgramId, "uMVP");
            mUniform_Texture = GL.GetUniformLocation(mProgramId, "uTextureDiffuse");
            mUniform_TintColor = GL.GetUniformLocation(mProgramId, "uTintColor");
            mUniform_TextureTransform = GL.GetUniformLocation(mProgramId, "uTextureTransform");

            mUniformXYOffset = GL.GetUniformLocation(mProgramId, "uTextureOffset");
            mUniformXYClip = GL.GetUniformLocation(mProgramId, "uTextureClip");
        }

        internal override void Draw(GameObject g, ref Matrix4 viewProjection, HelperFrustum frustum)
        {
            throw new NotImplementedException();
        }

        internal override void Draw(GameObject g, ref Matrix4 viewProjection)
        {
            GL.DepthFunc(DepthFunction.Lequal);
            GL.UseProgram(mProgramId);
            GL.UniformMatrix4(mUniform_MVP, false, ref viewProjection);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, KWEngine.CurrentWorld._textureBackground);
            GL.Uniform1(mUniform_Texture, 0);

            GL.Uniform2(mUniformXYOffset, ref KWEngine.CurrentWorld._textureBackgroundOffset);
            GL.Uniform2(mUniformXYClip, ref KWEngine.CurrentWorld._textureBackgroundClip);

            float ambient = KWEngine.CurrentWorld.GetSunColor().W + KWEngine.CurrentWorld.SunAmbientFactor;
            Vector4 skyColor = new Vector4(KWEngine.CurrentWorld.GetSunColor().X, KWEngine.CurrentWorld.GetSunColor().Y, KWEngine.CurrentWorld.GetSunColor().Z, ambient * KWEngine.CurrentWorld._textureBackgroundMultiplier);
            GL.Uniform4(mUniform_TintColor, ref skyColor);
            GL.Uniform2(mUniform_TextureTransform, ref KWEngine.CurrentWorld._textureBackgroundTransform);

            GeoMesh mesh = KWEngine.KWRect.Meshes.Values.ElementAt(0);
            GL.BindVertexArray(mesh.VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
            GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(0);
            GL.DepthFunc(DepthFunction.Less);
        }

        internal override void Draw(ParticleObject po, ref Matrix4 viewProjection)
        {
            throw new NotImplementedException();
        }

        internal override void Draw(HUDObject ho, ref Matrix4 viewProjection)
        {
            throw new NotImplementedException();
        }

        internal override void Draw(GameObject g, ref Matrix4 viewProjection, ref Matrix4 viewProjectionShadow, ref Matrix4 viewProjectionShadow2, HelperFrustum frustum, ref float[] lightColors, ref float[] lightTargets, ref float[] lightPositions, int lightCount, ref int lightShadow)
        {
            throw new NotImplementedException();
        }

        internal override void Draw(GameObject g, ref Matrix4 viewProjection, HelperFrustum frustum, bool isSun)
        {
            throw new NotImplementedException();
        }
    }
}
