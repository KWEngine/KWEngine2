using KWEngine2.Collision;
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
using static KWEngine2.KWEngine;

namespace KWEngine2.Renderers
{
    internal class RendererSimple : Renderer
    {
        public override void Initialize()
        {
            Name = "Simple";

            mProgramId = GL.CreateProgram();

            string resourceNameFragmentShader = "KWEngine2.Shaders.shader_fragment_simple.glsl";
            string resourceNameVertexShader = "KWEngine2.Shaders.shader_vertex_simple.glsl";
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
            mUniform_BaseColor = GL.GetUniformLocation(mProgramId, "uBaseColor");
        }

        internal Matrix4 rotMat = Matrix4.CreateRotationX((float)(Math.PI / 2.0));

        internal void DrawGrid(GridType type, ref Matrix4 viewProjection)
        {
            GL.UseProgram(mProgramId);

            Matrix4 mvp = viewProjection;
            if (type == GridType.GridXY)
            {
                mvp = rotMat * viewProjection;
            }
            GL.UniformMatrix4(mUniform_MVP, false, ref mvp);
            
            GL.Uniform4(mUniform_BaseColor, 1.0f, 1.0f, 1.0f, 1.0f);

            GeoMesh mesh = KWEngine.KWGrid.Meshes.Values.ElementAt(0);
            GL.BindVertexArray(mesh.VAO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
            GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);

            GL.UseProgram(0);
        }

        internal void DrawHitbox(GameObject g, ref Matrix4 viewProjection)
        {
            if (!g.IsInsideScreenSpace || g.Opacity <= 0 || !g.IsCollisionObject || g.IsSpherePerfect())
                return;

            
            GL.Disable(EnableCap.Blend);

            for (int i = 0; i < g.Hitboxes.Count; i++)
            {
                if (g.Hitboxes[i].IsActive)
                {
                    float[] v = g.Hitboxes[i].GetVertices();
                    GL.UniformMatrix4(mUniform_MVP, false, ref viewProjection);
                    GL.Uniform4(mUniform_BaseColor, 1.0f, 1.0f, 1.0f, 1.0f);

                    int tmpVAO = GL.GenVertexArray();
                    GL.BindVertexArray(tmpVAO);
                    int tmp = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ArrayBuffer, tmp);
                    GL.BufferData(BufferTarget.ArrayBuffer, v.Length * 4, v, BufferUsageHint.StaticDraw);
                    GL.VertexAttribPointer(mAttribute_vpos, 3, VertexAttribPointerType.Float, false, 0, 0);
                    GL.EnableVertexAttribArray(mAttribute_vpos);
                    GL.DrawArrays(PrimitiveType.Points, 0, v.Length / 3);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                    GL.DisableVertexAttribArray(0);

                    GL.DeleteBuffer(tmp);
                    GL.DeleteVertexArray(tmpVAO);

                }
            }
        }
        /*
        internal void Draw(GameObject g, ref Matrix4 viewProjection, ref Matrix4 viewProjectionShadowBiased, ref Matrix4 viewProjectionShadowBiased2, HelperFrustum frustum, ref float[] lightColors, ref float[] lightTargets, ref float[] lightPositions, int lightCount, ref int lightShadow)
        {
            if (g == null || !g.HasModel || g.CurrentWorld == null || g.Opacity <= 0)
                return;

            g.IsInsideScreenSpace = frustum.SphereVsFrustum(g.GetCenterPointForAllHitboxes(), g.GetMaxDiameter() / 2);
            if (!g.IsInsideScreenSpace)
                return;
            
            GL.UseProgram(mProgramId);

            lock (g)
            {
                int index = 0;
                foreach (string meshName in g.Model.Meshes.Keys)
                {
                    if (g.Model.IsKWCube6)
                    {
                        index = 0;
                    }
                    Matrix4.Mult(ref g.ModelMatrixForRenderPass[index], ref viewProjection, out _modelViewProjection);
                    GL.UniformMatrix4(mUniform_MVP, false, ref _modelViewProjection);
                    index++;

                    GL.Disable(EnableCap.Blend);
                    GeoMesh mesh = g.Model.Meshes[meshName];
                    if (mesh.Material.Opacity <= 0)
                    {
                        continue;
                    }

                    GL.Uniform4(mUniform_BaseColor, mesh.Material.ColorAlbedo.X, mesh.Material.ColorAlbedo.Y, mesh.Material.ColorAlbedo.Z, 1.0f);

                    HelperGL.CheckGLErrors();
                    GL.BindVertexArray(mesh.VAO);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
                    GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                    HelperGL.CheckGLErrors();
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
                    GL.BindVertexArray(0);
                }
            }

            GL.UseProgram(0);
        }
        */
    }
}