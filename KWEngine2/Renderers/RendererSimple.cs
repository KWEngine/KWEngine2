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

        internal override void Draw(GameObject g, ref Matrix4 viewProjection)
        {
            throw new NotImplementedException();
        }

        internal override void Draw(GameObject g, ref Matrix4 viewProjection, HelperFrustum frustum)
        {
            throw new NotImplementedException();
        }

        internal override void Draw(ParticleObject po, ref Matrix4 viewProjection)
        {
            throw new NotImplementedException();
        }

        internal override void Draw(HUDObject ho, ref Matrix4 viewProjection)
        {
            throw new NotImplementedException();
        }

        internal void DrawHitbox(GameObject g, ref Matrix4 viewProjection)
        {
            if (!g.IsInsideScreenSpace || g.Opacity <= 0 || !g.IsCollisionObject)
                return;

            
            GL.Disable(EnableCap.Blend);

            for (int i = 0; i < g.Hitboxes.Count; i++)
            {
                if (g.Hitboxes[i].IsActive)
                {
                    GL.UseProgram(mProgramId);

                    float[] v = g.Hitboxes[i].GetVertices();
                    GL.UniformMatrix4(mUniform_MVP, false, ref viewProjection);
                    GL.Uniform3(mUniform_BaseColor, 1.0f, 1.0f, 1.0f);

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

                    GL.UseProgram(0);
                }
            }
        }

        internal override void Draw(GameObject g, ref Matrix4 viewProjection, ref Matrix4 viewProjectionShadowBiased, ref Matrix4 viewProjectionShadowBiased2, HelperFrustum frustum, ref float[] lightColors, ref float[] lightTargets, ref float[] lightPositions, int lightCount, ref int lightShadow)
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
                    if (g._cubeModel is GeoModelCube6)
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


                    if (g._cubeModel != null)
                    {
                        UploadMaterialForKWCube(g._cubeModel, mesh, g);
                    }
                    else
                    {
                        GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorDiffuse.X, mesh.Material.ColorDiffuse.Y, mesh.Material.ColorDiffuse.Z);
                    }
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

        private void UploadMaterialForKWCube(GeoModelCube cubeModel, GeoMesh mesh, GameObject g)
        {
            if (mesh.Material.Name == "KWCube")
            {
                UploadMaterialForSide(CubeSide.Front, cubeModel, mesh);
            }
            else
            {
                if (mesh.Material.Name == "Front")
                {
                    UploadMaterialForSide(CubeSide.Front, cubeModel, mesh);
                }
                else if (mesh.Material.Name == "Back")
                {
                    UploadMaterialForSide(CubeSide.Back, cubeModel, mesh);
                }
                else if (mesh.Material.Name == "Left")
                {
                    UploadMaterialForSide(CubeSide.Left, cubeModel, mesh);
                }
                else if (mesh.Material.Name == "Right")
                {
                    UploadMaterialForSide(CubeSide.Right, cubeModel, mesh);
                }
                else if (mesh.Material.Name == "Top")
                {
                    UploadMaterialForSide(CubeSide.Top, cubeModel, mesh);
                }
                else if (mesh.Material.Name == "Bottom")
                {
                    UploadMaterialForSide(CubeSide.Bottom, cubeModel, mesh);
                }
            }
        }

        private void UploadMaterialForSide(CubeSide side, GeoModelCube cubeModel, GeoMesh mesh)
        {
            GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorDiffuse.X, mesh.Material.ColorDiffuse.Y, mesh.Material.ColorDiffuse.Z);
        }

        internal override void Draw(GameObject g, ref Matrix4 viewProjection, HelperFrustum frustum, bool isSun)
        {
            throw new NotImplementedException();
        }
    }
}