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
    internal class RendererTerrain : Renderer
    {
        private Matrix4 _identityMatrix = Matrix4.Identity;

        private int mUniform_TextureBlend = -1;
        private int mUniform_TextureRed = -1;
        private int mUniform_TextureGreen = -1;
        private int mUniform_TextureBlue = -1;
        private int mUniform_UseBlend = -1;

        public override void Initialize()
        {
            Name = "Terrain";

            mProgramId = GL.CreateProgram();

            string resourceNameFragmentShader = "KWEngine2.Shaders.shader_fragment_terrain_pbr.glsl";
            string resourceNameVertexShader = "KWEngine2.Shaders.shader_vertex_terrain_pbr.glsl";
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
                GL.BindAttribLocation(mProgramId, 4, "aTangent");
                GL.BindAttribLocation(mProgramId, 5, "aBiTangent");

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
            mAttribute_vnormal = GL.GetAttribLocation(mProgramId, "aNormal");
            mAttribute_vnormaltangent = GL.GetAttribLocation(mProgramId, "aTangent");
            mAttribute_vnormalbitangent = GL.GetAttribLocation(mProgramId, "aBiTangent");

            mUniform_MVP = GL.GetUniformLocation(mProgramId, "uMVP");
            mUniform_VPShadowMap = GL.GetUniformLocation(mProgramId, "uMVPShadowMap");
            mUniform_NormalMatrix = GL.GetUniformLocation(mProgramId, "uNormalMatrix");
            mUniform_ModelMatrix = GL.GetUniformLocation(mProgramId, "uModelMatrix");

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

            mUniform_TextureRed = GL.GetUniformLocation(mProgramId, "uTextureDiffuseR");
            mUniform_TextureGreen = GL.GetUniformLocation(mProgramId, "uTextureDiffuseG");
            mUniform_TextureBlue = GL.GetUniformLocation(mProgramId, "uTextureDiffuseB");
            mUniform_TextureBlend = GL.GetUniformLocation(mProgramId, "uTextureDiffuseBlend");
            mUniform_UseBlend = GL.GetUniformLocation(mProgramId, "uTextureUseBlend");

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
            mUniform_LightsMeta = GL.GetUniformLocation(mProgramId, "uLightsMeta");

            mUniform_TextureTransform = GL.GetUniformLocation(mProgramId, "uTextureTransform");

            mUniform_TextureShadowMap = GL.GetUniformLocation(mProgramId, "uTextureShadowMap");
            mUniform_TextureShadowMapCubeMap = GL.GetUniformLocation(mProgramId, "uTextureShadowMapCubeMap");

            mUniform_TextureUseEmissiveMap = GL.GetUniformLocation(mProgramId, "uUseTextureEmissive");
            mUniform_TextureEmissiveMap = GL.GetUniformLocation(mProgramId, "uTextureEmissive");

            mUniform_Opacity = GL.GetUniformLocation(mProgramId, "uOpacity");

            mUniform_TextureSkybox = GL.GetUniformLocation(mProgramId, "uTextureSkybox");
            mUniform_TextureIsSkybox = GL.GetUniformLocation(mProgramId, "uUseTextureSkybox");
            mUniform_TextureSky2D = GL.GetUniformLocation(mProgramId, "uTextureSky2D");
            mUniform_TextureSkyBoost = GL.GetUniformLocation(mProgramId, "uTextureSkyBoost");

            mUniform_SpecularReflectionFactor = GL.GetUniformLocation(mProgramId, "uSpecularReflectionFactor");

            GL.ValidateProgram(mProgramId);
            HelperGL.CheckGLErrors();
        }

        internal void Draw(GameObject g, ref Matrix4 viewProjection, HelperFrustum frustum, int textureIndex)
        {
            if (g == null || !g.HasModel || g.CurrentWorld == null || g.Opacity <= 0)
                return;

            g.IsInsideScreenSpace = frustum.SphereVsFrustum(g.GetCenterPointForAllHitboxes(), g.GetMaxDiameter() / 2);
            if (!g.IsInsideScreenSpace)
                return;
            
            GL.Disable(EnableCap.Blend);
            lock (g)
            {
                if (g.Opacity < 1)
                {
                    GL.Enable(EnableCap.Blend);
                }

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

                Matrix4.Mult(ref g.ModelMatrixForRenderPass[0], ref viewProjection, out _modelViewProjection);
                Matrix4.Transpose(ref g.ModelMatrixForRenderPass[0], out _normalMatrix);
                Matrix4.Invert(ref _normalMatrix, out _normalMatrix);

                GL.UniformMatrix4(mUniform_ModelMatrix, false, ref g.ModelMatrixForRenderPass[0]);
                GL.UniformMatrix4(mUniform_NormalMatrix, false, ref _normalMatrix);
                GL.UniformMatrix4(mUniform_MVP, false, ref _modelViewProjection);

                if (g.ColorEmissive.W > 0)
                {
                    GL.Uniform4(mUniform_EmissiveColor, g.ColorEmissive);
                }
                else
                {
                    GL.Uniform4(mUniform_EmissiveColor, Vector4.Zero);
                }

                string meshName = g.Model.Meshes.Keys.ElementAt(0);
                GeoMesh mesh = g.Model.Meshes[meshName];
                GeoMaterial meshMaterial = mesh.Material;

                GL.Uniform1(mUniform_Roughness, meshMaterial.Roughness);
                GL.Uniform1(mUniform_Metalness, meshMaterial.Metalness);
                GL.Uniform1(mUniform_SpecularReflectionFactor, meshMaterial.SpecularReflection ? 1 : 0);

                // TODO: Check if overrides exist
                GL.Uniform2(mUniform_TextureTransform, mesh.Terrain.mTexX, mesh.Terrain.mTexY);

                // albedo map:
                int texId = -1;
                texId = mesh.Material.TextureAlbedo.OpenGLID;
                GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                GL.BindTexture(TextureTarget.Texture2D, texId > 0 ? texId : KWEngine.TextureWhite);
                GL.Uniform1(mUniform_Texture, textureIndex);
                GL.Uniform1(mUniform_TextureUse, texId > 0 ? 1 : 0);
                GL.Uniform3(mUniform_BaseColor, texId > 0 ? new Vector3(1f, 1f, 1f) : new Vector3(meshMaterial.ColorAlbedo.X, meshMaterial.ColorAlbedo.Y, meshMaterial.ColorAlbedo.Z));
                textureIndex++;

                // normal map:
                texId = meshMaterial.TextureNormal.OpenGLID;
                GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                GL.BindTexture(TextureTarget.Texture2D, texId > 0 ? texId : KWEngine.TextureWhite);
                GL.Uniform1(mUniform_TextureNormalMap, textureIndex);
                GL.Uniform1(mUniform_TextureUseNormalMap, texId > 0 ? 1 : 0);
                textureIndex++;

                // roughness map:
                GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                if (meshMaterial.TextureRoughness.OpenGLID > 0)
                {
                    GL.BindTexture(TextureTarget.Texture2D, meshMaterial.TextureRoughness.OpenGLID);
                    GL.Uniform1(mUniform_TextureRoughnessMap, textureIndex);
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 1);
                    GL.Uniform1(mUniform_TextureRoughnessIsSpecular, meshMaterial.TextureRoughnessIsSpecular ? 1 : 0);
                }
                else
                {
                    if (meshMaterial.TextureRoughnessInMetalness && meshMaterial.TextureMetalness.OpenGLID > 0)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                        GL.Uniform1(mUniform_TextureRoughnessMap, textureIndex);
                        GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                        GL.Uniform1(mUniform_TextureRoughnessIsSpecular, 1);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                        GL.Uniform1(mUniform_TextureRoughnessMap, textureIndex);
                        GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                        GL.Uniform1(mUniform_TextureRoughnessIsSpecular, 0);
                    }

                }
                textureIndex++;

                // metalness map:
                GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                if (meshMaterial.TextureMetalness.OpenGLID > 0)
                {
                    GL.BindTexture(TextureTarget.Texture2D, meshMaterial.TextureMetalness.OpenGLID);
                    GL.Uniform1(mUniform_TextureMetalnessMap, textureIndex);
                    GL.Uniform1(mUniform_TextureUseMetalnessMap, 1);
                }
                else
                {
                    GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureBlack);
                    GL.Uniform1(mUniform_TextureMetalnessMap, textureIndex);
                    GL.Uniform1(mUniform_TextureUseMetalnessMap, 0);
                }
                textureIndex++;

                // emissive map:
                GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                if (meshMaterial.TextureEmissive.OpenGLID > 0)
                {
                    GL.BindTexture(TextureTarget.Texture2D, meshMaterial.TextureEmissive.OpenGLID);
                    GL.Uniform1(mUniform_TextureEmissiveMap, textureIndex);
                    GL.Uniform1(mUniform_TextureUseEmissiveMap, 1);
                }
                else
                {
                    GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureBlack);
                    GL.Uniform1(mUniform_TextureEmissiveMap, textureIndex);
                    GL.Uniform1(mUniform_TextureUseEmissiveMap, 0);
                }
                textureIndex++;

                // Blendmapping:
                if (mesh.Terrain._texBlend > 0 && mesh.Terrain._texBlend != KWEngine.TextureBlack)
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    GL.BindTexture(TextureTarget.Texture2D, mesh.Terrain._texBlend);
                    GL.Uniform1(mUniform_TextureBlend, textureIndex);
                    textureIndex++;

                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    GL.BindTexture(TextureTarget.Texture2D, mesh.Terrain._texR);
                    GL.Uniform1(mUniform_TextureRed, textureIndex);
                    textureIndex++;

                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    if (mesh.Terrain._texG > 0 && mesh.Terrain._texG != KWEngine.TextureAlpha)
                        GL.BindTexture(TextureTarget.Texture2D, mesh.Terrain._texG);
                    else
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureAlpha);
                    GL.Uniform1(mUniform_TextureGreen, textureIndex);
                    textureIndex++;

                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    if (mesh.Terrain._texB > 0 && mesh.Terrain._texB != KWEngine.TextureAlpha)
                        GL.BindTexture(TextureTarget.Texture2D, mesh.Terrain._texB);
                    else
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureAlpha);
                    GL.Uniform1(mUniform_TextureBlue, textureIndex);
                    textureIndex++;


                    GL.Uniform1(mUniform_UseBlend, 1);
                }
                else
                {
                        
                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureBlack);
                    GL.Uniform1(mUniform_TextureBlend, textureIndex);
                    textureIndex++;

                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureBlack);
                    GL.Uniform1(mUniform_TextureRed, textureIndex);
                    textureIndex++;

                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureBlack);
                    GL.Uniform1(mUniform_TextureGreen, textureIndex);
                    textureIndex++;

                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureBlack);
                    GL.Uniform1(mUniform_TextureBlue, textureIndex);
                    textureIndex++;
                        
                    GL.Uniform1(mUniform_UseBlend, 0);
                }

                GL.BindVertexArray(mesh.VAO);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
                GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
                GL.BindVertexArray(0);
                HelperGL.CheckGLErrors();
            }

            if (g.Opacity < 1)
            {
                GL.Disable(EnableCap.Blend);
            }
            
        }
    }
}