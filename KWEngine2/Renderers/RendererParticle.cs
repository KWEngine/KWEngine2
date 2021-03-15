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
    internal class RendererParticle : Renderer
    {
        private int mUniform_AnimationState = -1;
        private int mUniform_AnimationStates = -1;

        public override void Initialize()
        {
            Name = "Particle";

            mProgramId = GL.CreateProgram();

            string resourceNameFragmentShader = "KWEngine2.Shaders.shader_fragment_particle.glsl";
            string resourceNameVertexShader = "KWEngine2.Shaders.shader_vertex_particle.glsl";
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
            
            mUniform_MVP = GL.GetUniformLocation(mProgramId, "uMVP");

            // Textures:
            mUniform_Texture = GL.GetUniformLocation(mProgramId, "uTextureDiffuse");

            mUniform_AnimationState = GL.GetUniformLocation(mProgramId, "uAnimationState");
            mUniform_AnimationStates = GL.GetUniformLocation(mProgramId, "uAnimationStates");
            mUniform_TintColor = GL.GetUniformLocation(mProgramId, "uTintColor");
        }

        internal void Draw(ParticleObject po, ref Matrix4 viewProjection)
        {
            lock (po)
            {
                GL.Uniform4(mUniform_TintColor, ref po._tint);
                Matrix4 mvp = po._modelMatrix * viewProjection;
                GL.UniformMatrix4(mUniform_MVP, false, ref mvp);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, po._info.Texture);
                GL.Uniform1(mUniform_Texture, 0);

                GL.Uniform1(mUniform_AnimationState, po._frame);
                GL.Uniform1(mUniform_AnimationStates, po._info.Images);

                GeoMesh mesh = po._model.Meshes.Values.ElementAt(0);
                GL.BindVertexArray(mesh.VAO);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
                GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.BindVertexArray(0);

            }
        }
    }
}