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
            mUniform_MVPShadowMap = GL.GetUniformLocation(mProgramId, "uMVPShadowMap");
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
            mUniform_BiasCoefficient = GL.GetUniformLocation(mProgramId, "uBiasCoefficient");

            mUniform_TextureUseEmissiveMap = GL.GetUniformLocation(mProgramId, "uUseTextureEmissive");
            mUniform_TextureEmissiveMap = GL.GetUniformLocation(mProgramId, "uTextureEmissive");

            // 2nd shadow map:
            mUniform_MVPShadowMap2 = GL.GetUniformLocation(mProgramId, "uMVPShadowMap2");
            mUniform_TextureShadowMap2 = GL.GetUniformLocation(mProgramId, "uTextureShadowMap2");
            mUniform_ShadowLightPosition = GL.GetUniformLocation(mProgramId, "uShadowLightPosition");
            mUniform_BiasCoefficient2 = GL.GetUniformLocation(mProgramId, "uBiasCoefficient2");

            mUniform_Glow = GL.GetUniformLocation(mProgramId, "uGlow");
            mUniform_Outline = GL.GetUniformLocation(mProgramId, "uOutline");
            mUniform_BaseColor = GL.GetUniformLocation(mProgramId, "uAlbedoColor");
            mUniform_TintColor = GL.GetUniformLocation(mProgramId, "uTintColor");
            mUniform_EmissiveColor = GL.GetUniformLocation(mProgramId, "uEmissiveColor");

            mUniform_SpecularArea = GL.GetUniformLocation(mProgramId, "uSpecularArea");
            mUniform_SpecularPower = GL.GetUniformLocation(mProgramId, "uSpecularPower");

            mUniform_uCameraPos = GL.GetUniformLocation(mProgramId, "uCameraPos");
            mUniform_uCameraDirection = GL.GetUniformLocation(mProgramId, "uCameraDirection");

            mUniform_SunPosition = GL.GetUniformLocation(mProgramId, "uSunPosition");
            mUniform_SunDirection = GL.GetUniformLocation(mProgramId, "uSunDirection");
            mUniform_SunIntensity = GL.GetUniformLocation(mProgramId, "uSunIntensity");
            mUniform_SunAffection = GL.GetUniformLocation(mProgramId, "uSunAffection");
            mUniform_SunAmbient = GL.GetUniformLocation(mProgramId, "uSunAmbient");
            mUniform_LightAffection = GL.GetUniformLocation(mProgramId, "uLightAffection");

            mUniform_LightsColors = GL.GetUniformLocation(mProgramId, "uLightsColors");
            mUniform_LightsPositions = GL.GetUniformLocation(mProgramId, "uLightsPositions");
            mUniform_LightsTargets = GL.GetUniformLocation(mProgramId, "uLightsTargets");
            mUniform_LightCount = GL.GetUniformLocation(mProgramId, "uLightCount");

            mUniform_TextureTransform = GL.GetUniformLocation(mProgramId, "uTextureTransform");

            mUniform_TextureSkybox = GL.GetUniformLocation(mProgramId, "uTextureSkybox");
            mUniform_TextureIsSkybox = GL.GetUniformLocation(mProgramId, "uUseTextureSkybox");
        }

        internal override void Draw(GameObject g, ref Matrix4 viewProjection, ref Matrix4 viewProjectionShadowBiased, ref Matrix4 viewProjectionShadowBiased2, HelperFrustum frustum, ref float[] lightColors, ref float[] lightTargets, ref float[] lightPositions, int lightCount, ref int lightShadow)
        {
            if (g == null || !g.HasModel || g.CurrentWorld == null || g.Opacity <= 0)
                return;

            g.IsInsideScreenSpace = frustum.VolumeVsFrustum(g.GetCenterPointForAllHitboxes(), g.GetMaxDimensions().X, g.GetMaxDimensions().Y, g.GetMaxDimensions().Z);
            if (!g.IsInsideScreenSpace)
                return;

            GL.UseProgram(mProgramId);

            lock (g)
            {
                GL.Uniform4(mUniform_Glow, g.Glow);
                GL.Uniform4(mUniform_Outline, g.ColorOutline);
                GL.Uniform3(mUniform_TintColor, g.Color);

                // How many lights are there?
                GL.Uniform1(mUniform_LightCount, lightCount);
                GL.Uniform4(mUniform_LightsColors, KWEngine.MAX_LIGHTS, lightColors);
                GL.Uniform4(mUniform_LightsTargets, KWEngine.MAX_LIGHTS, lightTargets);
                GL.Uniform4(mUniform_LightsPositions, KWEngine.MAX_LIGHTS, lightPositions);

                // Sun
                GL.Uniform4(mUniform_SunIntensity, g.CurrentWorld.GetSunColor());
                GL.Uniform3(mUniform_SunPosition, g.CurrentWorld.GetSunPosition().X, g.CurrentWorld.GetSunPosition().Y, g.CurrentWorld.GetSunPosition().Z);
                GL.Uniform3(mUniform_SunDirection, ref g.CurrentWorld._sunDirectionInverted);
                GL.Uniform1(mUniform_SunAmbient, g.CurrentWorld.SunAmbientFactor);
                GL.Uniform1(mUniform_SunAffection, g.IsAffectedBySun ? 1 : 0);
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

                // Upload depth texture from sun (shadow mapping)
                GL.ActiveTexture(TextureUnit.Texture10);
                GL.BindTexture(TextureTarget.Texture2D, GLWindow.CurrentWindow.TextureShadowMap);
                GL.Uniform1(mUniform_TextureShadowMap, 10);
                GL.Uniform1(mUniform_BiasCoefficient, KWEngine.ShadowMapCoefficient);

                // optionally upload depth texture for second light:
                GL.ActiveTexture(TextureUnit.Texture11);
                GL.BindTexture(TextureTarget.Texture2D, lightShadow >= 0 ? GLWindow.CurrentWindow.TextureShadowMap2 : KWEngine.TextureDepthEmpty);
                GL.Uniform1(mUniform_TextureShadowMap2, 11);
                GL.Uniform1(mUniform_ShadowLightPosition, lightShadow);
                GL.Uniform1(mUniform_BiasCoefficient2, lightShadow >= 0 ? CurrentWorld.GetLightObjects().ElementAt(lightShadow).ShadowMapBiasCoefficient : 0f);

                // Upload skybox for metal reflections:
                GL.ActiveTexture(TextureUnit.Texture12);
                GL.BindTexture(TextureTarget.TextureCubeMap, g.CurrentWorld._textureSkybox >= 0 ? g.CurrentWorld._textureSkybox : KWEngine.TextureCubemapEmpty);
                GL.Uniform1(mUniform_TextureSkybox, 12);
                GL.Uniform1(mUniform_TextureIsSkybox, g.CurrentWorld._textureSkybox >= 0 ? 1 : 0);

                if (g.ColorEmissive.W > 0)
                {
                    GL.Uniform4(mUniform_EmissiveColor, g.ColorEmissive);
                }
                else
                {
                    GL.Uniform4(mUniform_EmissiveColor, Vector4.Zero);
                }

                int index = 0;
                for (int i = 0; i < g.Model.Meshes.Keys.Count; i++)// (string meshName in g.Model.Meshes.Keys)
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

                    // Shadow mapping
                    Matrix4 modelViewProjectionMatrixBiased = g.ModelMatrixForRenderPass[index] * viewProjectionShadowBiased;
                    GL.UniformMatrix4(mUniform_MVPShadowMap, false, ref modelViewProjectionMatrixBiased);

                    Matrix4 modelViewProjectionMatrixBiased2 = lightShadow >= 0 ? g.ModelMatrixForRenderPass[index] * viewProjectionShadowBiased2 : Matrix4.Identity;
                    GL.UniformMatrix4(mUniform_MVPShadowMap2, false, ref modelViewProjectionMatrixBiased2);

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

                    

                    GL.Uniform1(mUniform_Roughness, meshMaterial.Roughness);
                    GL.Uniform1(mUniform_Metalness, meshMaterial.Metalness);
                    GL.Uniform2(mUniform_TextureTransform, meshMaterial.TextureAlbedo.UVTransform.X == 0 ? 1 : meshMaterial.TextureAlbedo.UVTransform.X, meshMaterial.TextureAlbedo.UVTransform.Y == 0 ? 1 : meshMaterial.TextureAlbedo.UVTransform.Y);

                    // albedo map:
                    int texId = meshMaterial.TextureAlbedo.OpenGLID;
                    GL.ActiveTexture(TextureUnit.Texture0);
                    if (texId > 0)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, texId);
                        GL.Uniform1(mUniform_Texture, 0);
                        GL.Uniform1(mUniform_TextureUse, 1);
                        GL.Uniform3(mUniform_BaseColor, 1f, 1f, 1f);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                        GL.Uniform1(mUniform_Texture, 0);
                        GL.Uniform1(mUniform_TextureUse, 0);
                        GL.Uniform3(mUniform_BaseColor, meshMaterial.ColorAlbedo.X, meshMaterial.ColorAlbedo.Y, meshMaterial.ColorAlbedo.Z);
                    }

                    // normal map:
                    texId = meshMaterial.TextureNormal.OpenGLID;
                    GL.ActiveTexture(TextureUnit.Texture1);
                    if (texId > 0)
                    {

                        GL.BindTexture(TextureTarget.Texture2D, texId);
                        GL.Uniform1(mUniform_TextureNormalMap, 1);
                        GL.Uniform1(mUniform_TextureUseNormalMap, 1);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                        GL.Uniform1(mUniform_TextureNormalMap, 1);
                        GL.Uniform1(mUniform_TextureUseNormalMap, 0);
                    }

                    // roughness map:
                    GL.ActiveTexture(TextureUnit.Texture2);
                    if (meshMaterial.TextureRoughness.OpenGLID > 0)
                    {

                        GL.BindTexture(TextureTarget.Texture2D, meshMaterial.TextureRoughness.OpenGLID);
                        GL.Uniform1(mUniform_TextureRoughnessMap, 2);
                        GL.Uniform1(mUniform_TextureUseRoughnessMap, 1);
                        GL.Uniform1(mUniform_TextureRoughnessIsSpecular, meshMaterial.TextureRoughnessIsSpecular ? 1 : 0);
                    }
                    else
                    {
                        if (meshMaterial.TextureRoughnessInMetalness && meshMaterial.TextureMetalness.OpenGLID > 0)
                        {
                            GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                            GL.Uniform1(mUniform_TextureRoughnessMap, 2);
                            GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                            GL.Uniform1(mUniform_TextureRoughnessIsSpecular, 1);
                        }
                        else
                        {
                            GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                            GL.Uniform1(mUniform_TextureRoughnessMap, 2);
                            GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                            GL.Uniform1(mUniform_TextureRoughnessIsSpecular, 0);
                        }

                    }

                    // metalness map:
                    GL.ActiveTexture(TextureUnit.Texture3);
                    if (meshMaterial.TextureMetalness.OpenGLID > 0)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, meshMaterial.TextureMetalness.OpenGLID);
                        GL.Uniform1(mUniform_TextureMetalnessMap, 3);
                        GL.Uniform1(mUniform_TextureUseMetalnessMap, 0);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureBlack);
                        GL.Uniform1(mUniform_TextureMetalnessMap, 3);
                        GL.Uniform1(mUniform_TextureUseMetalnessMap, 0);
                    }

                    // emissive map:
                    GL.ActiveTexture(TextureUnit.Texture4);
                    if (meshMaterial.TextureEmissive.OpenGLID > 0)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, meshMaterial.TextureEmissive.OpenGLID);
                        GL.Uniform1(mUniform_TextureEmissiveMap, 4);
                        GL.Uniform1(mUniform_TextureUseEmissiveMap, 1);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureBlack);
                        GL.Uniform1(mUniform_TextureEmissiveMap, 4);
                        GL.Uniform1(mUniform_TextureUseEmissiveMap, 0);
                    }



                    GL.ActiveTexture(TextureUnit.Texture8);
                    if (meshMaterial.TextureLight.OpenGLID > 0)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, meshMaterial.TextureLight.OpenGLID);
                        GL.Uniform1(mUniform_TextureLightMap, 8);
                        GL.Uniform1(mUniform_TextureUseLightMap, 1);
                    }
                    else
                    {
                        GL.BindTexture(TextureTarget.Texture2D, KWEngine.TextureWhite);
                        GL.Uniform1(mUniform_TextureLightMap, 8);
                        GL.Uniform1(mUniform_TextureUseLightMap, 0);
                    }


                    GL.BindVertexArray(mesh.VAO);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
                    GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                    HelperGL.CheckGLErrors();
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
                    GL.BindVertexArray(0);
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(0);
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

        internal override void Draw(GameObject g, ref Matrix4 viewProjection, HelperFrustum frustum, bool isSun)
        {
            throw new NotImplementedException();
        }
    }
}