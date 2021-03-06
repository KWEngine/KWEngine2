﻿using KWEngine2.GameObjects;
using KWEngine2.Helper;
using KWEngine2.Model;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;
using System.Linq;
using System.Reflection;


namespace KWEngine2.Renderers
{
    internal class RendererSkybox : Renderer
    {
        private Matrix4 _viewMatrix = Matrix4.Identity;

        public override void Initialize()
        {
            Name = "Skybox";

            mProgramId = GL.CreateProgram();

            string resourceNameFragmentShader = "KWEngine2.Shaders.shader_fragment_skybox.glsl";
            string resourceNameVertexShader = "KWEngine2.Shaders.shader_vertex_skybox.glsl";
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

                GL.BindFragDataLocation(mProgramId, 0, "color");
                GL.BindFragDataLocation(mProgramId, 1, "bloom");

                GL.LinkProgram(mProgramId);
            }
            else
            {
                throw new Exception("Creating and linking shaders failed.");
            }

            mAttribute_vpos = GL.GetAttribLocation(mProgramId, "aPosition");

            mUniform_MVP = GL.GetUniformLocation(mProgramId, "uMVP");
            mUniform_Texture = GL.GetUniformLocation(mProgramId, "uTextureDiffuse");
            mUniform_TintColor = GL.GetUniformLocation(mProgramId, "uTintColor");
        }

        internal void Draw(Matrix4 projection, int environmentMapMatrix = -1)
        {
            if (environmentMapMatrix >= 0)
            {
                _viewMatrix = HelperSkybox._viewMatrixSkybox[environmentMapMatrix];
                projection = HelperSkybox._projectionMatrixSkybox;
            }
            else
            {
                if (KWEngine.CurrentWorld.IsFirstPersonMode)
                {
                    _viewMatrix = HelperCamera.GetViewMatrix(KWEngine.CurrentWorld.GetFirstPersonObject().Position);
                }
                else
                {
                    _viewMatrix = KWEngine.CurrentWindow._viewMatrix;
                }
            }

            // Clear translation part:
            _viewMatrix = _viewMatrix.ClearTranslation();
            _viewMatrix = KWEngine.CurrentWorld._skyboxRotation * _viewMatrix;
            Matrix4 MVP = _viewMatrix * projection;


            GL.FrontFace(FrontFaceDirection.Cw);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.UseProgram(mProgramId);
            GL.UniformMatrix4(mUniform_MVP, false, ref MVP);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.TextureCubeMap, KWEngine.CurrentWorld._textureSkybox);
            GL.Uniform1(mUniform_Texture, 0);

            Vector4 skyColor = new Vector4(KWEngine.CurrentWorld._ambientLight.Xyz, KWEngine.CurrentWorld._ambientLight.W * KWEngine.CurrentWorld._textureBackgroundMultiplier);
            GL.Uniform4(mUniform_TintColor, ref skyColor);

            GeoMesh mesh = KWEngine.Models["KWCube"].Meshes.Values.ElementAt(0);
            GL.BindVertexArray(mesh.VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
            GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
            GL.UseProgram(0);
            GL.DepthFunc(DepthFunction.Less);
            GL.FrontFace(FrontFaceDirection.Ccw);
        }
    }
}
