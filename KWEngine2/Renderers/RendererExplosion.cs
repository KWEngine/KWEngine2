using KWEngine2.GameObjects;
using KWEngine2.Helper;
using KWEngine2.Model;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using static KWEngine2.KWEngine;

namespace KWEngine2.Renderers
{
    internal class RendererExplosion : Renderer
    {
        private int mUniform_Time = -1;
        private int mUniform_VP = -1;
        private int mUniform_Number = -1;
        private int mUniform_Spread = -1;
        private int mUniform_Position = -1;
        private int mUniform_Size = -1;
        private int mUniform_Axes = -1;
        private int mUniform_Algorithm = -1;
        private int mUniform_Towards = -1;

        public override void Initialize()
        {
            Name = "Explosion";

            mProgramId = GL.CreateProgram();

            string resourceNameFragmentShader = "KWEngine2.Shaders.shader_fragment_explosion.glsl";
            string resourceNameVertexShader = "KWEngine2.Shaders.shader_vertex_explosion.glsl";
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream s = assembly.GetManifestResourceStream(resourceNameVertexShader))
            {
                mShaderVertexId = LoadShader(s, ShaderType.VertexShader, mProgramId);
            }

            using (Stream s = assembly.GetManifestResourceStream(resourceNameFragmentShader))
            {
                mShaderFragmentId = LoadShader(s, ShaderType.FragmentShader, mProgramId);
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
            
            mUniform_VP = GL.GetUniformLocation(mProgramId, "uVP");

            // Textures:
            mUniform_Texture = GL.GetUniformLocation(mProgramId, "uTextureDiffuse");
            mUniform_TextureUse = GL.GetUniformLocation(mProgramId, "uUseTextureDiffuse");
            mUniform_TextureTransform = GL.GetUniformLocation(mProgramId, "uTextureTransform");

            mUniform_Glow = GL.GetUniformLocation(mProgramId, "uGlow");
            mUniform_SunAmbient = GL.GetUniformLocation(mProgramId, "uSunAmbient");
            mUniform_Time = GL.GetUniformLocation(mProgramId, "uTime");
            mUniform_Number = GL.GetUniformLocation(mProgramId, "uNumber");
            mUniform_Spread= GL.GetUniformLocation(mProgramId, "uSpread");
            mUniform_Size = GL.GetUniformLocation(mProgramId, "uSize");
            mUniform_TintColor = GL.GetUniformLocation(mProgramId, "uTintColor");
            mUniform_Position = GL.GetUniformLocation(mProgramId, "uPosition");
            mUniform_Axes = GL.GetUniformLocation(mProgramId, "uAxes");
            mUniform_Algorithm = GL.GetUniformLocation(mProgramId, "uAlgorithm");
            mUniform_Towards = GL.GetUniformLocation(mProgramId, "uTowardsIndex");

        }

        internal void Draw(Explosion e, ref Matrix4 viewProjection)
        {
            if (e == null || e._model == null || e._currentWorld == null)
                return;

            GL.UseProgram(mProgramId);

            lock (e)
            {
                int type = (int)e._type;

                GL.Uniform4(mUniform_Glow, e.Glow.X, e.Glow.Y, e.Glow.Z, e.Glow.W);
                GL.Uniform1(mUniform_SunAmbient, HelperGL.Clamp(e._currentWorld._ambientLight.W * 2f, 0, 1));
                GL.Uniform1(mUniform_Number, (float)e._amount);
                GL.Uniform1(mUniform_Spread, e._spread);
                GL.Uniform3(mUniform_Position, e.Position);
                GL.Uniform1(mUniform_Time, e._secondsAlive / e._duration);
                GL.Uniform1(mUniform_Size, e._particleSize);
                GL.Uniform1(mUniform_Algorithm, e._algorithm);
                GL.Uniform3(mUniform_TintColor, e.Color);
                if (type < 100)
                    GL.Uniform1(mUniform_Towards, 0);
                else if(type >= 100 && type < 1000)
                    GL.Uniform1(mUniform_Towards, 1);
                else
                    GL.Uniform1(mUniform_Towards, 2);
                GL.Uniform4(mUniform_Axes, e._amount, e._directions);
                GL.UniformMatrix4(mUniform_VP, false, ref viewProjection);
                GL.Uniform2(mUniform_TextureTransform, e._textureTransform.X, e._textureTransform.Y);

                if (e._textureId > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, e._textureId);
                    GL.Uniform1(mUniform_Texture, 0);
                    GL.Uniform1(mUniform_TextureUse, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUse, 0);
                }

                GeoMesh mesh = e._model.Meshes.ElementAt(0).Value;
                GL.BindVertexArray(mesh.VAO);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
                GL.DrawElementsInstanced(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, e._amount);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

                GL.BindVertexArray(0);
                
            }

            GL.UseProgram(0);
            
        }
    }
}