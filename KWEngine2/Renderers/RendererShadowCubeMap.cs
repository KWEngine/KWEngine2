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
    internal class RendererShadowCubeMap : Renderer
    {
        private Matrix4 _identityMatrix = Matrix4.Identity;

        private int mUniform_ViewProjectionMatrix = -1;

        public override void Initialize()
        {
            Name = "ShadowCubeMap";

            mProgramId = GL.CreateProgram();

            string resourceNameFragmentShader = "KWEngine2.Shaders.shader_fragment_shadow_cube.glsl";
            string resourceNameGeometryShader = "KWEngine2.Shaders.shader_geometry_shadow_cube.glsl";
            string resourceNameVertexShader = "KWEngine2.Shaders.shader_vertex_shadow_cube.glsl";
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream s = assembly.GetManifestResourceStream(resourceNameVertexShader))
            {
                mShaderVertexId = LoadShader(s, ShaderType.VertexShader, mProgramId);
            }

            using (Stream s = assembly.GetManifestResourceStream(resourceNameGeometryShader))
            {
                mShaderGeometryId = LoadShader(s, ShaderType.GeometryShader, mProgramId);
            }

            using (Stream s = assembly.GetManifestResourceStream(resourceNameFragmentShader))
            {
                mShaderFragmentId = LoadShader(s, ShaderType.FragmentShader, mProgramId);
            }

            if (mShaderFragmentId >= 0 && mShaderVertexId >= 0 && mShaderGeometryId >= 0)
            {
                GL.BindAttribLocation(mProgramId, 0, "aPosition");
                GL.BindAttribLocation(mProgramId, 6, "aBoneIds");
                GL.BindAttribLocation(mProgramId, 7, "aBoneWeights");
                GL.LinkProgram(mProgramId);
            }
            else
            {
                throw new Exception("Creating and linking shaders failed.");
            }

            mAttribute_vpos = GL.GetAttribLocation(mProgramId, "aPosition");
            mAttribute_vjoints = GL.GetAttribLocation(mProgramId, "aBoneIds");
            mAttribute_vweights = GL.GetAttribLocation(mProgramId, "aBoneWeights");

            mUniform_ModelMatrix = GL.GetUniformLocation(mProgramId, "uModelMatrix");
            mUniform_ViewProjectionMatrices = GL.GetUniformLocation(mProgramId, "uShadowMatrices");
            mUniform_UseAnimations = GL.GetUniformLocation(mProgramId, "uUseAnimations");
            mUniform_BoneTransforms = GL.GetUniformLocation(mProgramId, "uBoneTransforms");
        }

        internal void Draw(GameObject g, ref Matrix4[] viewProjection, HelperFrustum frustum)
        {
            if (g == null || !g.HasModel)
                return;
            
            lock (g)
            {
                int index = -1;
                foreach (string meshName in g.Model.Meshes.Keys)
                {
                    index++;
                    if(g.Model.IsKWCube6)
                    {
                        index = 0;
                    }
                    GeoMesh mesh = g.Model.Meshes[meshName];
                    bool useMeshTransform = mesh.BoneNames.Count == 0 || !(g.AnimationID >= 0 && g.Model.Animations != null && g.Model.Animations.Count > 0);
                    

                    if (useMeshTransform)
                    {
                        Matrix4.Mult(ref mesh.Transform, ref g._modelMatrix, out g.ModelMatrixForRenderPass[index]);
                    }
                    else
                    {
                        g.ModelMatrixForRenderPass[index] = g._modelMatrix;
                    }

                    if (mesh.Material.Opacity <= 0 || !isInsideFrustum)
                    {
                        continue;
                    }

                    

                    if (g.IsShadowCaster && g.Opacity > 0f)
                    {
                        if (useMeshTransform == false)
                        {
                            GL.Uniform1(mUniform_UseAnimations, 1);
                            for (int i = 0; i < g.BoneTranslationMatrices[meshName].Length; i++)
                            {
                                Matrix4 tmp = g.BoneTranslationMatrices[meshName][i];
                                GL.UniformMatrix4(mUniform_BoneTransforms + i, false, ref tmp);
                            }
                        }
                        else
                        {
                            GL.Uniform1(mUniform_UseAnimations, 0);
                        }

                        //TODO Matrix4.Mult(ref g.ModelMatrixForRenderPass[index], ref viewProjection, out _modelViewProjection);
                        GL.UniformMatrix4(mUniform_ModelMatrix, false, ref g.ModelMatrixForRenderPass[index]);
                        for(int i = 0; i < 6; i++)
                        {
                            GL.UniformMatrix4(mUniform_ViewProjectionMatrices + i, false, ref viewProjection[i]);
                        }
                        GL.BindVertexArray(mesh.VAO);
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
                        GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
                        GL.BindVertexArray(0);
                    }
                }
            }
        }
    }
}
