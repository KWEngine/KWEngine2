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
    internal class RendererOld : Renderer
    {
        private Matrix4 _identityMatrix = Matrix4.Identity;

        public override void Initialize()
        {
            Name = "Standard";

            mProgramId = GL.CreateProgram();

            string resourceNameFragmentShader = "KWEngine2.Shaders.shader_fragment.glsl";
            string resourceNameVertexShader = "KWEngine2.Shaders.shader_vertex.glsl";
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

            // Textures:
            mUniform_Texture = GL.GetUniformLocation(mProgramId, "uTextureDiffuse");
            mUniform_TextureUse = GL.GetUniformLocation(mProgramId, "uUseTextureDiffuse");

            mUniform_TextureNormalMap = GL.GetUniformLocation(mProgramId, "uTextureNormal");
            mUniform_TextureUseNormalMap = GL.GetUniformLocation(mProgramId, "uUseTextureNormal");

            mUniform_TextureRoughnessMap = GL.GetUniformLocation(mProgramId, "uTextureSpecular");
            mUniform_TextureUseRoughnessMap = GL.GetUniformLocation(mProgramId, "uUseTextureSpecular");
            mUniform_TextureRoughnessIsSpecular = GL.GetUniformLocation(mProgramId, "uRoughness");

            mUniform_TextureLightMap = GL.GetUniformLocation(mProgramId, "uTextureLightmap");
            mUniform_TextureUseLightMap = GL.GetUniformLocation(mProgramId, "uUseTextureLightmap");

            mUniform_TextureShadowMap = GL.GetUniformLocation(mProgramId, "uTextureShadowMap");
            
            mUniform_TextureUseEmissiveMap = GL.GetUniformLocation(mProgramId, "uUseTextureEmissive");
            mUniform_TextureEmissiveMap = GL.GetUniformLocation(mProgramId, "uTextureEmissive");

            // 2nd shadow map:
            mUniform_MVPShadowMap2 = GL.GetUniformLocation(mProgramId, "uMVPShadowMap2");
            mUniform_TextureShadowMap2 = GL.GetUniformLocation(mProgramId, "uTextureShadowMap2");
            mUniform_ShadowLightPosition = GL.GetUniformLocation(mProgramId, "uShadowLightPosition");
            mUniform_BiasCoefficient2 = GL.GetUniformLocation(mProgramId, "uBiasCoefficient2");

            mUniform_Glow = GL.GetUniformLocation(mProgramId, "uGlow");
            mUniform_Outline = GL.GetUniformLocation(mProgramId, "uOutline");
            mUniform_BaseColor = GL.GetUniformLocation(mProgramId, "uBaseColor");
            mUniform_TintColor = GL.GetUniformLocation(mProgramId, "uTintColor");
            mUniform_EmissiveColor = GL.GetUniformLocation(mProgramId, "uEmissiveColor");

            mUniform_SpecularArea = GL.GetUniformLocation(mProgramId, "uSpecularArea");
            mUniform_SpecularPower = GL.GetUniformLocation(mProgramId, "uSpecularPower");

            mUniform_uCameraPos = GL.GetUniformLocation(mProgramId, "uCameraPos");
            mUniform_uCameraDirection = GL.GetUniformLocation(mProgramId, "uCameraDirection");
            mUniform_BiasCoefficient = GL.GetUniformLocation(mProgramId, "uBiasCoefficient");
            


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
               
                GL.Uniform1(mUniform_BiasCoefficient, KWEngine.ShadowMapCoefficient);

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
                Vector3 sunDirection = g.CurrentWorld.GetSunPosition() - g.CurrentWorld.GetSunTarget();
                sunDirection.NormalizeFast();
                GL.Uniform3(mUniform_SunDirection, ref sunDirection);
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

                

                // Upload depth texture (shadow mapping)
                GL.ActiveTexture(TextureUnit.Texture3);
                GL.BindTexture(TextureTarget.Texture2D, GLWindow.CurrentWindow.TextureShadowMap);
                GL.Uniform1(mUniform_TextureShadowMap, 3);

                if(lightShadow >= 0)
                {
                   

                    GL.ActiveTexture(TextureUnit.Texture5);
                    GL.BindTexture(TextureTarget.Texture2D, GLWindow.CurrentWindow.TextureShadowMap2);
                    GL.Uniform1(mUniform_TextureShadowMap2, 5);

                    GL.Uniform1(mUniform_ShadowLightPosition, lightShadow);
                    GL.Uniform1(mUniform_BiasCoefficient2, CurrentWorld.GetLightObjects().ElementAt(lightShadow).ShadowMapBiasCoefficient);
                }
                else
                {
                    GL.ActiveTexture(TextureUnit.Texture5);
                    GL.BindTexture(TextureTarget.Texture2D, GLWindow.CurrentWindow.TextureShadowMap2);
                    GL.Uniform1(mUniform_TextureShadowMap2, 5);
                    GL.Uniform1(mUniform_ShadowLightPosition, -1);
                }

                int index = 0;
                foreach (string meshName in g.Model.Meshes.Keys)
                {
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

                    if (lightShadow >= 0)
                    {
                        Matrix4 modelViewProjectionMatrixBiased2 = g.ModelMatrixForRenderPass[index] * viewProjectionShadowBiased2;
                        GL.UniformMatrix4(mUniform_MVPShadowMap2, false, ref modelViewProjectionMatrixBiased2);
                    }
                    index++;


                    GL.Disable(EnableCap.Blend);
                    GeoMesh mesh = g.Model.Meshes[meshName];
                    if (mesh.Material.Opacity <= 0)
                    {
                        continue;
                    }

                    if (mesh.Material.Opacity < 1 || g.Opacity < 1)
                    {
                        GL.Enable(EnableCap.Blend);
                    }
                    if (g.Opacity < mesh.Material.Opacity)
                        GL.Uniform1(mUniform_Opacity, g.Opacity);
                    else
                        GL.Uniform1(mUniform_Opacity, mesh.Material.Opacity);

                    if (mesh.BoneNames.Count > 0 && g.AnimationID >= 0 && g.Model.Animations != null && g.Model.Animations.Count > 0)
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


                    //GL.Uniform1(mUniform_SpecularPower, mesh.Material.SpecularPower);
                    //GL.Uniform1(mUniform_SpecularArea, mesh.Material.SpecularArea);

                    if (mesh.Material.TextureLight.OpenGLID > 0)
                    {
                        GL.ActiveTexture(TextureUnit.Texture8);
                        GL.BindTexture(TextureTarget.Texture2D, mesh.Material.TextureLight.OpenGLID);
                        GL.Uniform1(mUniform_TextureLightMap, 8);
                        GL.Uniform1(mUniform_TextureUseLightMap, 1);
                    }
                    else
                    {
                        GL.Uniform1(mUniform_TextureUseLightMap, 0);
                    }

                    GL.Uniform2(mUniform_TextureTransform, mesh.Material.TextureAlbedo.UVTransform.X, mesh.Material.TextureAlbedo.UVTransform.Y);


                    int texId = mesh.Material.TextureAlbedo.OpenGLID;

                    if (texId > 0)
                    {
                        GL.ActiveTexture(TextureUnit.Texture0);
                        GL.BindTexture(TextureTarget.Texture2D, texId);
                        GL.Uniform1(mUniform_Texture, 0);
                        GL.Uniform1(mUniform_TextureUse, 1);
                        GL.Uniform3(mUniform_BaseColor, 1f, 1f, 1f);
                    }
                    else
                    {
                        GL.Uniform1(mUniform_TextureUse, 0);
                        GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorAlbedo.X, mesh.Material.ColorAlbedo.Y, mesh.Material.ColorAlbedo.Z);
                    }


                    texId = mesh.Material.TextureNormal.OpenGLID;
                    if (texId > 0)
                    {
                        GL.ActiveTexture(TextureUnit.Texture1);
                        GL.BindTexture(TextureTarget.Texture2D, texId);
                        GL.Uniform1(mUniform_TextureNormalMap, 1);
                        GL.Uniform1(mUniform_TextureUseNormalMap, 1);
                    }
                    else
                    {
                        GL.Uniform1(mUniform_TextureUseNormalMap, 0);
                    }


                    texId = mesh.Material.TextureRoughness.OpenGLID;
                    if (texId > 0)
                    {
                        GL.ActiveTexture(TextureUnit.Texture2);
                        GL.BindTexture(TextureTarget.Texture2D, texId);
                        GL.Uniform1(mUniform_TextureRoughnessMap, 2);
                        GL.Uniform1(mUniform_TextureUseRoughnessMap, 1);
                        GL.Uniform1(mUniform_TextureRoughnessIsSpecular, mesh.Material.TextureRoughnessIsSpecular ? 1 : 0);
                    }
                    else
                    {
                        GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                        GL.Uniform1(mUniform_TextureRoughnessIsSpecular, 0);
                    }


                    if (mesh.Material.TextureEmissive.OpenGLID > 0)
                    {
                        GL.ActiveTexture(TextureUnit.Texture4);
                        GL.BindTexture(TextureTarget.Texture2D, mesh.Material.TextureEmissive.OpenGLID);
                        GL.Uniform1(mUniform_TextureEmissiveMap, 4);
                        GL.Uniform1(mUniform_TextureUseEmissiveMap, 1);
                    }
                    else
                    {
                        GL.Uniform1(mUniform_TextureUseEmissiveMap, 0);
                    }

                    if (g.ColorEmissive.W > 0)
                    {
                        GL.Uniform4(mUniform_EmissiveColor, g.ColorEmissive);
                    }
                    else
                    {
                        GL.Uniform4(mUniform_EmissiveColor, mesh.Material.ColorEmissive);
                    }


                    GL.BindVertexArray(mesh.VAO);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
                    GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                    //HelperGL.CheckGLErrors();
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
                    GL.BindVertexArray(0);
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }
            }
            GL.BindTexture(TextureTarget.Texture2D,0);
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

        /*
        private void UploadMaterialForKWCube(GeoModelCube cubeModel, GeoMesh mesh, GameObject g)
        {

            if (g.ColorEmissive.W > 0)
            {
                GL.Uniform4(mUniform_EmissiveColor, g.ColorEmissive);
            }
            else
            {
                GL.Uniform4(mUniform_EmissiveColor, Vector4.Zero);
            }

            GL.Uniform1(mUniform_Opacity, g.Opacity);
            if (g.Opacity < 1)
            {
                GL.Enable(EnableCap.Blend);
            }

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

            if (side == CubeSide.Front)
            {
                GL.Uniform2(mUniform_TextureTransform, cubeModel.GeoTextureFront.UVTransform.X, cubeModel.GeoTextureFront.UVTransform.Y);
                if (cubeModel.GeoTextureFront.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureFront.OpenGLID);
                    GL.Uniform1(mUniform_Texture, 0);
                    GL.Uniform1(mUniform_TextureUse, 1);
                    GL.Uniform3(mUniform_BaseColor, 1f, 1f, 1f);

                }
                else
                {
                    GL.Uniform1(mUniform_TextureUse, 0);
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorAlbedo.X, mesh.Material.ColorAlbedo.Y, mesh.Material.ColorAlbedo.Z);
                }

                if (cubeModel.GeoTextureFrontNormal.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureFrontNormal.OpenGLID);
                    GL.Uniform1(mUniform_TextureNormalMap, 1);
                    GL.Uniform1(mUniform_TextureUseNormalMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseNormalMap, 0);
                }

                if (cubeModel.GeoTextureFrontSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureFrontSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureRoughnessMap, 2);
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                }
            }
            else if (side == CubeSide.Back)
            {
                GL.Uniform2(mUniform_TextureTransform, cubeModel.GeoTextureBack.UVTransform.X, cubeModel.GeoTextureBack.UVTransform.Y);
                if (cubeModel.GeoTextureBack.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureBack.OpenGLID);
                    GL.Uniform1(mUniform_Texture, 0);
                    GL.Uniform1(mUniform_TextureUse, 1);
                    GL.Uniform3(mUniform_BaseColor, 1f, 1f, 1f);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUse, 0);
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorAlbedo.X, mesh.Material.ColorAlbedo.Y, mesh.Material.ColorAlbedo.Z);
                }

                if (cubeModel.GeoTextureBackNormal.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureBackNormal.OpenGLID);
                    GL.Uniform1(mUniform_TextureNormalMap, 1);
                    GL.Uniform1(mUniform_TextureUseNormalMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseNormalMap, 0);
                }

                if (cubeModel.GeoTextureBackSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureBackSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureRoughnessMap, 2);
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                }
            }
            else if (side == CubeSide.Left)
            {
                GL.Uniform2(mUniform_TextureTransform, cubeModel.GeoTextureLeft.UVTransform.X, cubeModel.GeoTextureLeft.UVTransform.Y);

                if (cubeModel.GeoTextureLeft.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureLeft.OpenGLID);
                    GL.Uniform1(mUniform_Texture, 0);
                    GL.Uniform1(mUniform_TextureUse, 1);
                    GL.Uniform3(mUniform_BaseColor, 1f, 1f, 1f);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUse, 0);
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorAlbedo.X, mesh.Material.ColorAlbedo.Y, mesh.Material.ColorAlbedo.Z);
                }

                if (cubeModel.GeoTextureLeftNormal.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureLeftNormal.OpenGLID);
                    GL.Uniform1(mUniform_TextureNormalMap, 1);
                    GL.Uniform1(mUniform_TextureUseNormalMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseNormalMap, 0);
                }

                if (cubeModel.GeoTextureLeftSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureLeftSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureRoughnessMap, 2);
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                }
            }
            else if (side == CubeSide.Right)
            {
                GL.Uniform2(mUniform_TextureTransform, cubeModel.GeoTextureRight.UVTransform.X, cubeModel.GeoTextureRight.UVTransform.Y);


                if (cubeModel.GeoTextureRight.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureRight.OpenGLID);
                    GL.Uniform1(mUniform_Texture, 0);
                    GL.Uniform1(mUniform_TextureUse, 1);
                    GL.Uniform3(mUniform_BaseColor, 1f, 1f, 1f);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUse, 0);
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorAlbedo.X, mesh.Material.ColorAlbedo.Y, mesh.Material.ColorAlbedo.Z);
                }

                if (cubeModel.GeoTextureRightNormal.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureRightNormal.OpenGLID);
                    GL.Uniform1(mUniform_TextureNormalMap, 1);
                    GL.Uniform1(mUniform_TextureUseNormalMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseNormalMap, 0);
                }

                if (cubeModel.GeoTextureRightSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureRightSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureRoughnessMap, 2);
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                }
            }
            else if (side == CubeSide.Top)
            {
                GL.Uniform2(mUniform_TextureTransform, cubeModel.GeoTextureTop.UVTransform.X, cubeModel.GeoTextureTop.UVTransform.Y);


                if (cubeModel.GeoTextureTop.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureTop.OpenGLID);
                    GL.Uniform1(mUniform_Texture, 0);
                    GL.Uniform1(mUniform_TextureUse, 1);
                    GL.Uniform3(mUniform_BaseColor, 1f, 1f, 1f);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUse, 0);
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorAlbedo.X, mesh.Material.ColorAlbedo.Y, mesh.Material.ColorAlbedo.Z);
                }

                if (cubeModel.GeoTextureTopNormal.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureTopNormal.OpenGLID);
                    GL.Uniform1(mUniform_TextureNormalMap, 1);
                    GL.Uniform1(mUniform_TextureUseNormalMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseNormalMap, 0);
                }

                if (cubeModel.GeoTextureTopSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureTopSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureRoughnessMap, 2);
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                }
            }
            else if (side == CubeSide.Bottom)
            {
                GL.Uniform2(mUniform_TextureTransform, cubeModel.GeoTextureBottom.UVTransform.X, cubeModel.GeoTextureBottom.UVTransform.Y);


                if (cubeModel.GeoTextureBottom.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureBottom.OpenGLID);
                    GL.Uniform1(mUniform_Texture, 0);
                    GL.Uniform1(mUniform_TextureUse, 1);
                    GL.Uniform3(mUniform_BaseColor, 1f, 1f, 1f);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUse, 0);
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorAlbedo.X, mesh.Material.ColorAlbedo.Y, mesh.Material.ColorAlbedo.Z);
                }

                if (cubeModel.GeoTextureBottomNormal.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureBottomNormal.OpenGLID);
                    GL.Uniform1(mUniform_TextureNormalMap, 1);
                    GL.Uniform1(mUniform_TextureUseNormalMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseNormalMap, 0);
                }

                if (cubeModel.GeoTextureBottomSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureBottomSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureRoughnessMap, 2);
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseRoughnessMap, 0);
                }
            }
        }
        */
    }
}