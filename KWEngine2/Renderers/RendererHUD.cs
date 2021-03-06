﻿using KWEngine2.GameObjects;
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
    internal class RendererHUD : Renderer
    {
        public override void Initialize()
        {
            Name = "HUD";

            mProgramId = GL.CreateProgram();

            string resourceNameFragmentShader = "KWEngine2.Shaders.shader_fragment_hud.glsl";
            string resourceNameVertexShader = "KWEngine2.Shaders.shader_vertex_hud.glsl";
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
            mUniform_Texture = GL.GetUniformLocation(mProgramId, "uTextureDiffuse");
            mUniform_TintColor = GL.GetUniformLocation(mProgramId, "uTintColor");
            mUniform_Glow = GL.GetUniformLocation(mProgramId, "uGlow");
            mUniform_TextureHUDOffset = GL.GetUniformLocation(mProgramId, "uOffset");
            mUniform_TextureHUDIsText = GL.GetUniformLocation(mProgramId, "uIsText");
        }

        internal void Draw(HUDObject ho, ref Matrix4 viewProjection)
        {
            if (!ho.IsVisible)
                return;

            GeoMesh mesh = KWEngine.KWRect.Meshes.Values.ElementAt(0);
            GL.BindVertexArray(mesh.VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);

            lock (ho)
            {

                GL.Uniform4(mUniform_TintColor, ho._tint);
                GL.Uniform4(mUniform_Glow, ho._glow);

                lock (ho._modelMatrices)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    if(ho._type == HUDObjectType.Text)
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.FontTextureArray[(int)ho.Font]);
                    else
                        GL.BindTexture(TextureTarget.Texture2D, ho._textureId);
                    GL.Uniform1(mUniform_Texture, 0);

                    for (int i = 0; i < ho._positions.Count; i++)
                    {
                        Matrix4 mvp = ho._modelMatrices[i] * viewProjection;
                        GL.UniformMatrix4(mUniform_MVP, false, ref mvp);
                        GL.Uniform1(mUniform_TextureHUDOffset, ho._offsets[i]);
                        GL.Uniform1(mUniform_TextureHUDIsText, ho._type == HUDObjectType.Text ? 1 : 0);
                        GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                        
                    }
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }
            }

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
    }
}
