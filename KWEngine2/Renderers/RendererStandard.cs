﻿using KWEngine2.GameObjects;
using KWEngine2.Helper;
using KWEngine2.Model;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;
using System.Reflection;
using static KWEngine2.KWEngine;

namespace KWEngine2.Renderers
{
    internal class RendererStandard : Renderer
    {
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
                Console.WriteLine(GL.GetShaderInfoLog(mShaderVertexId));
            }

            using (Stream s = assembly.GetManifestResourceStream(resourceNameFragmentShader))
            {
                mShaderFragmentId = LoadShader(s, ShaderType.FragmentShader, mProgramId);
                Console.WriteLine(GL.GetShaderInfoLog(mShaderFragmentId));
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
            
            mUniform_TextureSpecularMap = GL.GetUniformLocation(mProgramId, "uTextureSpecular");
            mUniform_TextureUseSpecularMap = GL.GetUniformLocation(mProgramId, "uUseTextureSpecular");
            mUniform_TextureSpecularIsRoughness = GL.GetUniformLocation(mProgramId, "uRoughness");

            mUniform_TextureLightMap = GL.GetUniformLocation(mProgramId, "uTextureLightmap");
            mUniform_TextureUseLightMap = GL.GetUniformLocation(mProgramId, "uUseTextureLightMap");
            
            mUniform_TextureShadowMap = GL.GetUniformLocation(mProgramId, "uTextureShadowMap");
            
            mUniform_TextureUseEmissiveMap = GL.GetUniformLocation(mProgramId, "uUseTextureEmissive");
            mUniform_TextureEmissiveMap = GL.GetUniformLocation(mProgramId, "uTextureEmissive");


            mUniform_Glow = GL.GetUniformLocation(mProgramId, "uGlow");
            mUniform_BaseColor = GL.GetUniformLocation(mProgramId, "uBaseColor");
            mUniform_TintColor = GL.GetUniformLocation(mProgramId, "uTintColor");
            mUniform_EmissiveColor = GL.GetUniformLocation(mProgramId, "uEmissiveColor");            

            mUniform_SpecularArea = GL.GetUniformLocation(mProgramId, "uSpecularArea");
            mUniform_SpecularPower = GL.GetUniformLocation(mProgramId, "uSpecularPower");
            
            mUniform_uCameraPos = GL.GetUniformLocation(mProgramId, "uCameraPos");
            mUniform_BiasCoefficient = GL.GetUniformLocation(mProgramId, "uBiasCoefficient");
           

            mUniform_SunPosition = GL.GetUniformLocation(mProgramId, "uSunPosition");
            mUniform_SunDirection = GL.GetUniformLocation(mProgramId, "uSunDirection");
            mUniform_SunIntensity = GL.GetUniformLocation(mProgramId, "uSunIntensity");
            mUniform_SunAffection = GL.GetUniformLocation(mProgramId, "uSunAffection");
            mUniform_SunAmbient = GL.GetUniformLocation(mProgramId, "uSunAmbient");

            mUniform_LightsColors = GL.GetUniformLocation(mProgramId, "uLightsColors");
            mUniform_LightsPositions = GL.GetUniformLocation(mProgramId, "uLightsPositions");
            mUniform_LightsTargets = GL.GetUniformLocation(mProgramId, "uLightsTargets");
            mUniform_LightCount = GL.GetUniformLocation(mProgramId, "uLightCount");

            mUniform_TextureTransform = GL.GetUniformLocation(mProgramId, "uTextureTransform");
        }

        internal override void Draw(GameObject g, ref Matrix4 viewProjection, ref Matrix4 viewProjectionShadowBiased, HelperFrustum frustum, ref float[] lightColors, ref float[] lightTargets, ref float[] lightPositions, int lightCount)
        {
            if (g == null || !g.HasModel || g.CurrentWorld == null || !frustum.SphereVsFrustum(g.GetGameObjectCenterPoint(), g.GetGameObjectMaxDiameter() / 2))
                return;

            GL.UseProgram(mProgramId);

            lock (g)
            {
                if(mUniform_BiasCoefficient >= 0)
                {
                    GL.Uniform1(mUniform_BiasCoefficient, KWEngine.ShadowMapCoefficient);
                }

                if (mUniform_Glow >= 0)
                {
                    GL.Uniform4(mUniform_Glow, g.Glow.X, g.Glow.Y, g.Glow.Z, g.Glow.W);
                }

                if(mUniform_TintColor >= 0)
                {
                    GL.Uniform3(mUniform_TintColor, g.Color.X, g.Color.Y, g.Color.Z);
                }

                if (mUniform_LightCount >= 0)
                {
                    // How many lights are there?
                    GL.Uniform1(mUniform_LightCount, lightCount);
                    GL.Uniform4(mUniform_LightsColors, KWEngine.MAX_LIGHTS, lightColors);
                    GL.Uniform4(mUniform_LightsTargets, KWEngine.MAX_LIGHTS, lightTargets);
                    GL.Uniform4(mUniform_LightsPositions, KWEngine.MAX_LIGHTS, lightPositions);
                }

                // Sun
                if(mUniform_SunIntensity >= 0)
                {
                    GL.Uniform4(mUniform_SunIntensity, g.CurrentWorld.GetSunColor());
                }

                if (mUniform_SunPosition >= 0)
                {
                    GL.Uniform3(mUniform_SunPosition, g.CurrentWorld.GetSunPosition().X, g.CurrentWorld.GetSunPosition().Z, g.CurrentWorld.GetSunPosition().Z);
                    Vector3 sunDirection = g.CurrentWorld.GetSunPosition() - g.CurrentWorld.GetSunTarget();
                    sunDirection.NormalizeFast();
                    GL.Uniform3(mUniform_SunDirection, ref sunDirection);
                    GL.Uniform1(mUniform_SunAmbient, g.CurrentWorld.SunAmbientFactor);
                }

                if (mUniform_SunAffection >= 0)
                {
                    GL.Uniform1(mUniform_SunAffection, g.IsAffectedBySun ? 1 : 0);
                }

                // Camera
                if (mUniform_uCameraPos >= 0)
                {
                    GL.Uniform3(mUniform_uCameraPos, g.CurrentWorld.GetCameraPosition().X, g.CurrentWorld.GetCameraPosition().Z, g.CurrentWorld.GetCameraPosition().Z);
                }

                // Upload depth texture (shadow mapping)
                if (mUniform_TextureShadowMap >= 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture3);
                    GL.BindTexture(TextureTarget.Texture2D, GLWindow.CurrentWindow.TextureShadowMap);
                    GL.Uniform1(mUniform_TextureShadowMap, 3);
                }

                foreach (string meshName in g.Model.Meshes.Keys)
                {
                    GeoMesh mesh = g.Model.Meshes[meshName];

                    bool useMeshTransform = true;

                    if (g.AnimationID >= 0 && g.Model.Animations != null && g.Model.Animations.Count > 0)
                    {
                        if (mUniform_UseAnimations >= 0)
                        {
                            GL.Uniform1(mUniform_UseAnimations, 1);
                        }
                        if (mUniform_BoneTransforms >= 0)
                        {

                            lock (g.BoneTranslationMatrices)
                            {
                                for (int i = 0; i < g.BoneTranslationMatrices[meshName].Length; i++)
                                {
                                    Matrix4 tmp = g.BoneTranslationMatrices[meshName][i];
                                    GL.UniformMatrix4(mUniform_BoneTransforms + i, false, ref tmp);
                                }
                            }
                        }

                        useMeshTransform = false;
                    }
                    else
                    {
                        if (mUniform_UseAnimations >= 0)
                        {
                            GL.Uniform1(mUniform_UseAnimations, 0);
                        }
                    }


                    if (mUniform_SpecularPower >= 0)
                    {

                        GL.Uniform1(mUniform_SpecularPower, mesh.Material.SpecularPower);
                        
                    }
                    if (mUniform_SpecularArea >= 0)
                    {

                        GL.Uniform1(mUniform_SpecularArea, mesh.Material.SpecularArea);

                    }

                    // Might not be needed because shadow map pass already calculated it:
                    /*
                    if (useMeshTransform)
                        Matrix4.Mult(ref mesh.Transform, ref g._modelMatrix, out _tmpMatrix);
                    else
                        _tmpMatrix = g._modelMatrix;
                    */
                    Matrix4.Mult(ref g.ModelMatrixForRenderPass, ref viewProjection, out _modelViewProjection);
                    Matrix4.Transpose(ref g.ModelMatrixForRenderPass, out _normalMatrix);
                    Matrix4.Invert(ref _normalMatrix, out _normalMatrix);

                    GL.UniformMatrix4(mUniform_ModelMatrix, false, ref g.ModelMatrixForRenderPass);
                    GL.UniformMatrix4(mUniform_NormalMatrix, false, ref _normalMatrix);
                    GL.UniformMatrix4(mUniform_MVP, false, ref _modelViewProjection);

                    

                    // Shadow mapping
                    if (mUniform_MVPShadowMap >= 0)
                    {
                        Matrix4 modelViewProjectionMatrixBiased = g.ModelMatrixForRenderPass * viewProjectionShadowBiased;
                        GL.UniformMatrix4(mUniform_MVPShadowMap, false, ref modelViewProjectionMatrixBiased);
                    }

                    if (g._cubeModel != null)
                    {
                        UploadMaterialForKWCube(g._cubeModel, mesh);
                    }
                    else
                    {
                        // TODO: Add lightmap feature
                        if(mUniform_TextureUseLightMap >= 0)
                        {
                            GL.Uniform1(mUniform_TextureUseLightMap, 0);
                        }

                        GL.Uniform2(mUniform_TextureTransform, mesh.Material.TextureDiffuse.UVTransform.X, mesh.Material.TextureDiffuse.UVTransform.Y);
                        if (mUniform_Texture >= 0 && mesh.Material.TextureDiffuse.OpenGLID > 0)
                        {
                            GL.ActiveTexture(TextureUnit.Texture0);
                            GL.BindTexture(TextureTarget.Texture2D, mesh.Material.TextureDiffuse.OpenGLID);
                            GL.Uniform1(mUniform_Texture, 0);
                            GL.Uniform1(mUniform_TextureUse, 1);

                            
                            GL.Uniform3(mUniform_BaseColor, 1f, 1f, 1f);
                        }
                        else
                        {
                            GL.Uniform1(mUniform_TextureUse, 0);
                            GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorDiffuse.X, mesh.Material.ColorDiffuse.Y, mesh.Material.ColorDiffuse.Z);
                        }

                        if (mUniform_TextureNormalMap >= 0 && mesh.Material.TextureNormal.OpenGLID > 0)
                        {
                            GL.ActiveTexture(TextureUnit.Texture1);
                            GL.BindTexture(TextureTarget.Texture2D, mesh.Material.TextureNormal.OpenGLID);
                            GL.Uniform1(mUniform_TextureNormalMap, 1);
                            GL.Uniform1(mUniform_TextureUseNormalMap, 1);
                        }
                        else
                        {
                            GL.Uniform1(mUniform_TextureUseNormalMap, 0);
                        }

                        if (mUniform_TextureSpecularMap >= 0 && mesh.Material.TextureSpecular.OpenGLID > 0)
                        {
                            GL.ActiveTexture(TextureUnit.Texture2);
                            GL.BindTexture(TextureTarget.Texture2D, mesh.Material.TextureSpecular.OpenGLID);
                            GL.Uniform1(mUniform_TextureSpecularMap, 2);
                            GL.Uniform1(mUniform_TextureUseSpecularMap, 1);
                            if (mesh.Material.TextureSpecularIsRoughness)
                            {
                                GL.Uniform1(mUniform_TextureSpecularIsRoughness, mesh.Material.TextureSpecularIsRoughness ? 1 : 0);
                            }
                        }
                        else
                        {
                            GL.Uniform1(mUniform_TextureUseSpecularMap, 0);
                        }


                        if(mUniform_TextureSpecularMap >= 0 && mesh.Material.TextureEmissive.OpenGLID > 0)
                        {
                            GL.ActiveTexture(TextureUnit.Texture4);
                            GL.BindTexture(TextureTarget.Texture2D, mesh.Material.TextureEmissive.OpenGLID);
                            GL.Uniform1(mUniform_TextureSpecularMap, 4);
                            GL.Uniform1(mUniform_TextureUseEmissiveMap, 1);
                        }
                        else
                        {
                            GL.Uniform1(mUniform_TextureUseEmissiveMap, 0);
                        }
                        
                        GL.Uniform4(mUniform_EmissiveColor, mesh.Material.ColorEmissive);
                    }

                    GL.BindVertexArray(mesh.VAO);

                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.VBOIndex);
                    GL.DrawElements(mesh.Primitive, mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

                    GL.BindVertexArray(0);
                }
            }

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

        private void UploadMaterialForKWCube(GeoModelCube cubeModel, GeoMesh mesh)
        {
            
            if (mesh.Material.Name == "KWCube")
            {
                UploadMaterialForSide(CubeSide.Front, cubeModel, mesh);
            }
            else
            {
                if(mesh.Material.Name == "Front")
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
                if (mUniform_Texture >= 0 && cubeModel.GeoTextureFront.OpenGLID > 0)
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
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorDiffuse.X, mesh.Material.ColorDiffuse.Y, mesh.Material.ColorDiffuse.Z);
                }

                if (mUniform_TextureNormalMap >= 0 && cubeModel.GeoTextureFrontNormal.OpenGLID > 0)
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

                if (mUniform_TextureSpecularMap >= 0 && cubeModel.GeoTextureFrontSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureFrontSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureSpecularMap, 2);
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 0);
                }
            }
            else if(side == CubeSide.Back)
            {
                GL.Uniform2(mUniform_TextureTransform, cubeModel.GeoTextureBack.UVTransform.X, cubeModel.GeoTextureBack.UVTransform.Y);
                if (mUniform_Texture >= 0 && cubeModel.GeoTextureBack.OpenGLID > 0)
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
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorDiffuse.X, mesh.Material.ColorDiffuse.Y, mesh.Material.ColorDiffuse.Z);
                }

                if (mUniform_TextureNormalMap >= 0 && cubeModel.GeoTextureBackNormal.OpenGLID > 0)
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

                if (mUniform_TextureSpecularMap >= 0 && cubeModel.GeoTextureBackSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureBackSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureSpecularMap, 2);
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 0);
                }
            }
            else if (side == CubeSide.Left)
            {
                GL.Uniform2(mUniform_TextureTransform, cubeModel.GeoTextureLeft.UVTransform.X, cubeModel.GeoTextureLeft.UVTransform.Y);

                if (mUniform_Texture >= 0 && cubeModel.GeoTextureLeft.OpenGLID > 0)
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
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorDiffuse.X, mesh.Material.ColorDiffuse.Y, mesh.Material.ColorDiffuse.Z);
                }

                if (mUniform_TextureNormalMap >= 0 && cubeModel.GeoTextureLeftNormal.OpenGLID > 0)
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

                if (mUniform_TextureSpecularMap >= 0 && cubeModel.GeoTextureLeftSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureLeftSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureSpecularMap, 2);
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 0);
                }
            }
            else if (side == CubeSide.Right)
            {
                GL.Uniform2(mUniform_TextureTransform, cubeModel.GeoTextureRight.UVTransform.X, cubeModel.GeoTextureRight.UVTransform.Y);


                if (mUniform_Texture >= 0 && cubeModel.GeoTextureRight.OpenGLID > 0)
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
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorDiffuse.X, mesh.Material.ColorDiffuse.Y, mesh.Material.ColorDiffuse.Z);
                }

                if (mUniform_TextureNormalMap >= 0 && cubeModel.GeoTextureRightNormal.OpenGLID > 0)
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

                if (mUniform_TextureSpecularMap >= 0 && cubeModel.GeoTextureRightSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureRightSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureSpecularMap, 2);
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 0);
                }
            }
            else if (side == CubeSide.Top)
            {
                GL.Uniform2(mUniform_TextureTransform, cubeModel.GeoTextureTop.UVTransform.X, cubeModel.GeoTextureTop.UVTransform.Y);


                if (mUniform_Texture >= 0 && cubeModel.GeoTextureTop.OpenGLID > 0)
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
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorDiffuse.X, mesh.Material.ColorDiffuse.Y, mesh.Material.ColorDiffuse.Z);
                }

                if (mUniform_TextureNormalMap >= 0 && cubeModel.GeoTextureTopNormal.OpenGLID > 0)
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

                if (mUniform_TextureSpecularMap >= 0 && cubeModel.GeoTextureTopSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureTopSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureSpecularMap, 2);
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 0);
                }
            }
            else if (side == CubeSide.Bottom)
            {
                GL.Uniform2(mUniform_TextureTransform, cubeModel.GeoTextureBottom.UVTransform.X, cubeModel.GeoTextureBottom.UVTransform.Y);


                if (mUniform_Texture >= 0 && cubeModel.GeoTextureBottom.OpenGLID > 0)
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
                    GL.Uniform3(mUniform_BaseColor, mesh.Material.ColorDiffuse.X, mesh.Material.ColorDiffuse.Y, mesh.Material.ColorDiffuse.Z);
                }

                if (mUniform_TextureNormalMap >= 0 && cubeModel.GeoTextureBottomNormal.OpenGLID > 0)
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

                if (mUniform_TextureSpecularMap >= 0 && cubeModel.GeoTextureBottomSpecular.OpenGLID > 0)
                {
                    GL.ActiveTexture(TextureUnit.Texture2);
                    GL.BindTexture(TextureTarget.Texture2D, cubeModel.GeoTextureBottomSpecular.OpenGLID);
                    GL.Uniform1(mUniform_TextureSpecularMap, 2);
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 1);
                }
                else
                {
                    GL.Uniform1(mUniform_TextureUseSpecularMap, 0);
                }
            }
        }
    }
}