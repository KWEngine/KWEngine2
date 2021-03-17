using KWEngine2.GameObjects;
using KWEngine2.Helper;
using KWEngine2.Model;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using static KWEngine2.KWEngine;

namespace KWEngine2.Renderers
{
    internal class RendererStandard : Renderer
    {
        private Matrix4 _identityMatrix = Matrix4.Identity;

        public override void Initialize()
        {
            Name = "StandardPBR";

            mProgramId = GL.CreateProgram();

            string resourceNameFragmentShader = "KWEngine2.Shaders.shader_fragment_pbr.glsl";
            string resourceNameVertexShader = "KWEngine2.Shaders.shader_vertex_pbr.glsl";
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
                GL.BindAttribLocation(mProgramId, 1, "aNormal");
                GL.BindAttribLocation(mProgramId, 2, "aTexture");
                GL.BindAttribLocation(mProgramId, 3, "aTexture2");
                GL.BindAttribLocation(mProgramId, 4, "aTangent");
                GL.BindAttribLocation(mProgramId, 5, "aBiTangent");
                GL.BindAttribLocation(mProgramId, 6, "aBoneIds");
                GL.BindAttribLocation(mProgramId, 7, "aBoneWeights");

                GL.BindFragDataLocation(mProgramId, 0, "color");
                GL.BindFragDataLocation(mProgramId, 1, "bloom");
                GL.LinkProgram(mProgramId);
                GL.GetProgram(mProgramId, GetProgramParameterName.LinkStatus, out int log);
            }
            else
            {
                throw new Exception("Creating and linking shaders failed.");
            }

            mAttribute_vpos = GL.GetAttribLocation(mProgramId, "aPosition");
            mAttribute_vtexture = GL.GetAttribLocation(mProgramId, "aTexture");
            mAttribute_vnormal = GL.GetAttribLocation(mProgramId, "aNormal");
            mAttribute_vnormaltangent = GL.GetAttribLocation(mProgramId, "aTangent");
            mAttribute_vnormalbitangent = GL.GetAttribLocation(mProgramId, "aBiTangent");
            mAttribute_vjoints = GL.GetAttribLocation(mProgramId, "aBoneIds");
            mAttribute_vweights = GL.GetAttribLocation(mProgramId, "aBoneWeights");
            mAttribute_vtexture2 = GL.GetAttribLocation(mProgramId, "aTexture2");

            mUniform_Opacity = GL.GetUniformLocation(mProgramId, "uOpacity");

            mUniform_MVP = GL.GetUniformLocation(mProgramId, "uMVP");
            mUniform_VPShadowMap = GL.GetUniformLocation(mProgramId, "uMVPShadowMap");
            mUniform_NormalMatrix = GL.GetUniformLocation(mProgramId, "uNormalMatrix");
            mUniform_ModelMatrix = GL.GetUniformLocation(mProgramId, "uModelMatrix");
            mUniform_UseAnimations = GL.GetUniformLocation(mProgramId, "uUseAnimations");
            mUniform_BoneTransforms = GL.GetUniformLocation(mProgramId, "uBoneTransforms");

            // Global metalness/roughness:
            mUniform_Roughness = GL.GetUniformLocation(mProgramId, "uRoughness");
            mUniform_Metalness = GL.GetUniformLocation(mProgramId, "uMetalness");

            // Textures:
            mUniform_Texture = GL.GetUniformLocation(mProgramId, "uTextureAlbedo");
            mUniform_TextureUse = GL.GetUniformLocation(mProgramId, "uUseTextureAlbedo");

            mUniform_TextureNormalMap = GL.GetUniformLocation(mProgramId, "uTextureNormal");
            mUniform_TextureUseNormalMap = GL.GetUniformLocation(mProgramId, "uUseTextureNormal");

            mUniform_TextureRoughnessMap = GL.GetUniformLocation(mProgramId, "uTextureRoughness");
            mUniform_TextureUseRoughnessMap = GL.GetUniformLocation(mProgramId, "uUseTextureRoughness");
            mUniform_TextureRoughnessIsSpecular = GL.GetUniformLocation(mProgramId, "uUseTextureRoughnessIsSpecular");

            mUniform_TextureMetalnessMap = GL.GetUniformLocation(mProgramId, "uTextureMetalness");
            mUniform_TextureUseMetalnessMap = GL.GetUniformLocation(mProgramId, "uUseTextureMetalness");

            mUniform_TextureLightMap = GL.GetUniformLocation(mProgramId, "uTextureLightmap");
            mUniform_TextureUseLightMap = GL.GetUniformLocation(mProgramId, "uUseTextureLightmap");

            mUniform_TextureShadowMap = GL.GetUniformLocation(mProgramId, "uTextureShadowMap");
            mUniform_TextureShadowMapCubeMap = GL.GetUniformLocation(mProgramId, "uTextureShadowMapCubeMap");
            mUniform_LightsMeta = GL.GetUniformLocation(mProgramId, "uLightsMeta");

            mUniform_TextureUseEmissiveMap = GL.GetUniformLocation(mProgramId, "uUseTextureEmissive");
            mUniform_TextureEmissiveMap = GL.GetUniformLocation(mProgramId, "uTextureEmissive");

            mUniform_Glow = GL.GetUniformLocation(mProgramId, "uGlow");
            mUniform_Outline = GL.GetUniformLocation(mProgramId, "uOutline");
            mUniform_BaseColor = GL.GetUniformLocation(mProgramId, "uAlbedoColor");
            mUniform_TintColor = GL.GetUniformLocation(mProgramId, "uTintColor");
            mUniform_EmissiveColor = GL.GetUniformLocation(mProgramId, "uEmissiveColor");

            mUniform_uCameraPos = GL.GetUniformLocation(mProgramId, "uCameraPos");
            mUniform_uCameraDirection = GL.GetUniformLocation(mProgramId, "uCameraDirection");

            mUniform_SunAmbient = GL.GetUniformLocation(mProgramId, "uSunAmbient");
            mUniform_LightAffection = GL.GetUniformLocation(mProgramId, "uLightAffection");

            mUniform_LightsColors = GL.GetUniformLocation(mProgramId, "uLightsColors");
            mUniform_LightsPositions = GL.GetUniformLocation(mProgramId, "uLightsPositions");
            mUniform_LightsTargets = GL.GetUniformLocation(mProgramId, "uLightsTargets");
            mUniform_LightCount = GL.GetUniformLocation(mProgramId, "uLightCount");

            mUniform_TextureTransform = GL.GetUniformLocation(mProgramId, "uTextureTransform");

            mUniform_TextureSkybox = GL.GetUniformLocation(mProgramId, "uTextureSkybox");
            mUniform_TextureIsSkybox = GL.GetUniformLocation(mProgramId, "uUseTextureSkybox");
            mUniform_TextureSky2D = GL.GetUniformLocation(mProgramId, "uTextureSky2D");
            mUniform_TextureSkyBoost = GL.GetUniformLocation(mProgramId, "uTextureSkyBoost");

            mUniform_SpecularReflectionFactor = GL.GetUniformLocation(mProgramId, "uSpecularReflectionFactor");
        }

        internal void Draw(GameObject g, ref Matrix4 viewProjection, HelperFrustum frustum, int textureIndex)
        {
            lock (g)
            {
                GL.Uniform4(mUniform_Glow, g.Glow);
                GL.Uniform4(mUniform_Outline, g.ColorOutline);
                GL.Uniform3(mUniform_TintColor, g.Color);
               
                GL.Uniform1(mUniform_LightAffection, g.IsAffectedByLight ? 1 : 0);

                // Camera
                if (!CurrentWorld.IsFirstPersonMode)
                {
                    GL.Uniform3(mUniform_uCameraPos, g.CurrentWorld.GetCameraPosition().X, g.CurrentWorld.GetCameraPosition().Y, g.CurrentWorld.GetCameraPosition().Z);
                    GL.Uniform3(mUniform_uCameraDirection, g.CurrentWorld.GetCameraLookAtVector());
                }
                else
                {
                    GL.Uniform3(mUniform_uCameraPos, g.CurrentWorld.GetFirstPersonObject().Position.X, g.CurrentWorld.GetFirstPersonObject().Position.Y + g.CurrentWorld.GetFirstPersonObject().FPSEyeOffset, g.CurrentWorld.GetFirstPersonObject().Position.Z);
                    GL.Uniform3(mUniform_uCameraDirection, HelperCamera.GetLookAtVector());
                }

                int index = 0;
                for (int i = 0; i < g.Model.Meshes.Keys.Count; i++)
                {
                    string meshName = g.Model.Meshes.Keys.ElementAt(i);
                    if (g.Model.IsKWCube6)
                    {
                        index = 0;
                    }

                    // Matrices:
                    try
                    {
                        Matrix4.Mult(ref g.ModelMatrixForRenderPass[index], ref viewProjection, out _modelViewProjection);
                        Matrix4.Transpose(ref g.ModelMatrixForRenderPass[index], out _normalMatrix);
                        Matrix4.Invert(ref _normalMatrix, out _normalMatrix);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    GL.UniformMatrix4(mUniform_ModelMatrix, false, ref g.ModelMatrixForRenderPass[index]);
                    GL.UniformMatrix4(mUniform_NormalMatrix, false, ref _normalMatrix);
                    GL.UniformMatrix4(mUniform_MVP, false, ref _modelViewProjection);

                    index++;


                    GL.Disable(EnableCap.Blend);
                    GeoMesh mesh = g.Model.Meshes.Values.ElementAt(i);

                    GeoMaterial meshMaterial = g._materials[i];

                    if (meshMaterial.Opacity <= 0)
                    {
                        continue;
                    }

                    if (meshMaterial.Opacity < 1 || g.Opacity < 1)
                    {
                        GL.Enable(EnableCap.Blend);
                    }

                    if (g.ColorEmissive.W > 0)
                    {
                        GL.Uniform4(mUniform_EmissiveColor, g.ColorEmissive);
                    }
                    else
                    {
                        GL.Uniform4(mUniform_EmissiveColor, meshMaterial.ColorEmissive.X, meshMaterial.ColorEmissive.Y, meshMaterial.ColorEmissive.Z, meshMaterial.ColorEmissive.W);
                    }
                    GL.Uniform1(mUniform_Opacity, meshMaterial.Opacity * g.Opacity);

                    if (mesh.BoneNames.Count > 0 && g.AnimationID >= 0 && g.Model.Animations != null && g.Model.Animations.Count > 0)
                    {
                        GL.Uniform1(mUniform_UseAnimations, 1);
                        for (int j = 0; j < g.BoneTranslationMatrices[meshName].Length; j++)
                        {
                            Matrix4 tmp = g.BoneTranslationMatrices[meshName][j];
                            GL.UniformMatrix4(mUniform_BoneTransforms + j, false, ref tmp);
                        }
                    }
                    else
                    {
                        GL.Uniform1(mUniform_UseAnimations, 0);
                    }
                    if(meshMaterial.Name.Contains("wire"))
                    {
                        Console.WriteLine("!");
                    }
                    GL.Uniform1(mUniform_Roughness, meshMaterial.Roughness);
                    GL.Uniform1(mUniform_Metalness, meshMaterial.Metalness);
                    GL.Uniform1(mUniform_SpecularReflectionFactor, meshMaterial.SpecularReflection ? 1 : 0);
                    GL.Uniform2(mUniform_TextureTransform, meshMaterial.TextureAlbedo.UVTransform.X == 0 ? 1 : meshMaterial.TextureAlbedo.UVTransform.X, meshMaterial.TextureAlbedo.UVTransform.Y == 0 ? 1 : meshMaterial.TextureAlbedo.UVTransform.Y);

                    // albedo map:
                    int texId = meshMaterial.TextureAlbedo.OpenGLID;
                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    if (texId > 0)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, texId);
                        GL.Uniform1(mUniform_Texture, textureIndex);
                        GL.Uniform1(mUniform_TextureUse, 1);
                        GL.Uniform3(mUniform_BaseColor, 1f, 1f, 1f);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                        GL.Uniform1(mUniform_Texture, textureIndex);
                        GL.Uniform1(mUniform_TextureUse, 0);
                        GL.Uniform3(mUniform_BaseColor, meshMaterial.ColorAlbedo.X, meshMaterial.ColorAlbedo.Y, meshMaterial.ColorAlbedo.Z);
                    }

                    // normal map:
                    texId = meshMaterial.TextureNormal.OpenGLID;
                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex + 1);
                    if (texId > 0)
                    {

                        GL.BindTexture(TextureTarget.Texture2D, texId);
                        GL.Uniform1(mUniform_TextureNormalMap, textureIndex + 1);
                        GL.Uniform1(mUniform_TextureUseNormalMap, 1);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                        GL.Uniform1(mUniform_TextureNormalMap, textureIndex + 1);
                        GL.Uniform1(mUniform_TextureUseNormalMap, 0);
                    }

                    // roughness map:
                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex + 2);
                    if (meshMaterial.TextureRoughness.OpenGLID > 0)
                    {

                        GL.BindTexture(TextureTarget.Texture2D, meshMaterial.TextureRoughness.OpenGLID);
                        GL.Uniform1(mUniform_TextureRoughnessMap, textureIndex + 2);
                        GL.Uniform1(mUniform_TextureUseRoughnessMap, 1);
                        GL.Uniform1(mUniform_TextureRoughnessIsSpecular, meshMaterial.TextureRoughnessIsSpecular ? 1 : 0);
                    }
                    else
                    {
                        if (meshMaterial.TextureRoughnessInMetalness && meshMaterial.TextureMetalness.OpenGLID > 0)
                        {
                            GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                            GL.Uniform1(mUniform_TextureRoughnessMap, textureIndex + 2);
                            GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                            GL.Uniform1(mUniform_TextureRoughnessIsSpecular, 1);
                        }
                        else
                        {
                            GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                            GL.Uniform1(mUniform_TextureRoughnessMap, textureIndex + 2);
                            GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                            GL.Uniform1(mUniform_TextureRoughnessIsSpecular, 0);
                        }

                    }

                    // metalness map:
                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex + 3);
                    if (meshMaterial.TextureMetalness.OpenGLID > 0)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, meshMaterial.TextureMetalness.OpenGLID);
                        GL.Uniform1(mUniform_TextureMetalnessMap, textureIndex + 3);
                        GL.Uniform1(mUniform_TextureUseMetalnessMap, 1);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureBlack);
                        GL.Uniform1(mUniform_TextureMetalnessMap, textureIndex + 3);
                        GL.Uniform1(mUniform_TextureUseMetalnessMap, 0);
                    }

                    // emissive map:
                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex + 4);
                    if (meshMaterial.TextureEmissive.OpenGLID > 0)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, meshMaterial.TextureEmissive.OpenGLID);
                        GL.Uniform1(mUniform_TextureEmissiveMap, textureIndex + 4);
                        GL.Uniform1(mUniform_TextureUseEmissiveMap, 1);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureBlack);
                        GL.Uniform1(mUniform_TextureEmissiveMap, textureIndex + 4);
                        GL.Uniform1(mUniform_TextureUseEmissiveMap, 0);
                    }



                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex + 5);
                    if (meshMaterial.TextureLight.OpenGLID > 0)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, meshMaterial.TextureLight.OpenGLID);
                        GL.Uniform1(mUniform_TextureLightMap, textureIndex + 5);
                        GL.Uniform1(mUniform_TextureUseLightMap, 1);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                        GL.Uniform1(mUniform_TextureLightMap, textureIndex + 5);
                        GL.Uniform1(mUniform_TextureUseLightMap, 0);
                    }

                    GL.BindVertexArray(mesh.VAO);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
                    GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
                    GL.BindVertexArray(0);
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                    HelperGL.CheckGLErrors();
                }
            }
           
       
        }
    }
}