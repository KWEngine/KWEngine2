using KWEngine2.Audio;
using KWEngine2.GameObjects;
using KWEngine2.Helper;
using KWEngine2.Model;
using KWEngine2.Renderers;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading;
using System.Windows.Forms;

namespace KWEngine2
{
    /// <summary>
    /// Fensterklasse
    /// </summary>
    public abstract class GLWindow : GameWindow
    {
        /// <summary>
        /// Aktuelle Welt
        /// </summary>
        public World CurrentWorld { get; private set; }
        internal GameObject _dummy = null;
        internal double frameCounter = 0;
        internal double frameData = 0;

        internal float bloomWidth = 1;
        internal float bloomHeight = 1;
        internal int _fsaa = 0;
        internal float _anisotropy = 1.0f;
        internal bool _multithreaded = false;
        internal bool _vSync = true;

        /// <summary>
        /// Aktuelles Fenster
        /// </summary>
        public static GLWindow CurrentWindow { get; internal set; }
        internal Matrix4 _viewMatrix = Matrix4.Identity;
        internal Matrix4 _modelViewProjectionMatrixBackground = Matrix4.Identity;
        internal Matrix4 _modelViewProjectionMatrixBloom = Matrix4.Identity;
        internal Matrix4 _modelViewProjectionMatrixBloomMerge = Matrix4.Identity;
        internal Matrix4 _projectionMatrix = Matrix4.Identity;
        internal Matrix4 _viewProjectionMatrixHUD = Matrix4.Identity;

        internal static float[] LightColors = new float[KWEngine.MAX_LIGHTS * 4];
        internal static float[] LightTargets = new float[KWEngine.MAX_LIGHTS * 4];
        internal static float[] LightPositions = new float[KWEngine.MAX_LIGHTS * 4];
        internal static float[] LightMeta = new float[KWEngine.MAX_LIGHTS * 3];

        internal HelperFrustum Frustum = new HelperFrustum();

        internal System.Drawing.Rectangle _windowRect;
        internal System.Drawing.Point _mousePoint = new System.Drawing.Point(0, 0);
        internal System.Drawing.Point _mousePointFPS = new System.Drawing.Point(0, 0);

        internal GeoModel _bloomQuad;

        /// <summary>
        /// Gibt den Grad der anisotropischen Texturfilterung zurück (Standard: 1)
        /// </summary>
        public float AnisotropicFiltering
        {
            get
            {
                return _anisotropy;
            }
        }

        /// <summary>
        /// Standardkonstruktormethode
        /// </summary>
        protected GLWindow()
           : this(1280, 720, GameWindowFlags.FixedWindow, 0, true, false)
        {

        }

        /// <summary>
        /// Standardkonstruktormethode
        /// </summary>
        /// <param name="width">Breite des Fensters</param>
        /// <param name="height">Höhe des Fensters</param>
        protected GLWindow(int width, int height)
           : this(width, height, GameWindowFlags.FixedWindow, 0, true, false)
        {

        }

        internal int _bloomwidth = 1024;
        internal int _bloomheight = 512;

        /// <summary>
        /// Konstruktormethode
        /// </summary>
        /// <param name="width">Breite des Fensters</param>
        /// <param name="height">Höhe des Fensters</param>
        /// <param name="flag">FixedWindow oder FullScreen</param>
        /// <param name="antialiasing">FSAA-Wert (Anti-Aliasing)</param>
        /// <param name="vSync">VSync aktivieren</param>
        /// <param name="multithreading">Multithreading aktivieren (Standard: false)</param>
        /// <param name="textureAnisotropy">Level der anisotropischen Texturfilterung [1 bis 16, Standard: 1 (aus)]</param>
        /// <param name="shadowMapSize">Größe der Shadow Map (Standard: 1024)</param>
        protected GLWindow(int width, int height, GameWindowFlags flag, int antialiasing = 0, bool vSync = true, bool multithreading = false, int textureAnisotropy = 1, int shadowMapSize = 1024)
            : base(width, height, GraphicsMode.Default, "KWEngine2 - C# 3D Gaming", flag == GameWindowFlags.Default ? GameWindowFlags.FixedWindow : flag, DisplayDevice.Default, 4, 5, GraphicsContextFlags.Default, null, !multithreading)
        {
            _multithreaded = multithreading;
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Width = width;
            Height = height;
            CurrentWindow = this;
            KWEngine.ShadowMapSize = shadowMapSize;

            _bloomwidth = HelperTexture.RoundDownToPowerOf2(width);
            if (_bloomwidth > 1024)
                _bloomwidth = 1024;
            _bloomheight = _bloomwidth / 2;

            if (textureAnisotropy >= 2 && textureAnisotropy <= 16)
                _anisotropy = HelperTexture.RoundDownToPowerOf2(textureAnisotropy);
            else
                _anisotropy = 1;

            GLAudioEngine.InitAudioEngine();

            if (antialiasing >= 0 && antialiasing <= 8)
            {
                antialiasing = HelperTexture.RoundDownToPowerOf2(antialiasing);
            }
            else
            {
                antialiasing = 0;
            }
            _fsaa = antialiasing;

            if (flag != GameWindowFlags.Fullscreen)
            {
                X = Screen.PrimaryScreen.Bounds.Width / 2 - Width / 2;
                Y = Screen.PrimaryScreen.Bounds.Height / 2 - Height / 2;
            }
            else
            {
                X = 0;
                Y = 0;
            }
            if (_multithreaded)
            {
                TargetUpdateFrequency = TargetUpdateFrequency < 1 ? 59.94 : TargetUpdateFrequency;
                DeltaTime.movAveragePeriod = 5f; // #frames involved in average calc (suggested values 5-100)
                DeltaTime.smoothFactor = 0.02f; // adjusting ratio (suggested values 0.01-0.5)
            }
            
            _vSync = vSync;
            VSync = vSync ? VSyncMode.On : VSyncMode.Off;
            BasicInit();
        }


        /// <summary>
        /// Konstruktormethode
        /// </summary>
        /// <param name="width">Breite des Fensters</param>
        /// <param name="height">Höhe des Fensters</param>
        /// <param name="flag">FixedWindow oder FullScreen</param>
        /// <param name="antialiasing">FSAA-Wert (Anti-Aliasing)</param>
        /// <param name="vSync">VSync aktivieren</param>
        protected GLWindow(int width, int height, GameWindowFlags flag, int antialiasing = 0, bool vSync = true)
            : this(width, height, flag, antialiasing, vSync, false)
        {

        }

        private void BasicInit()
        {
            string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName + " " + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductMajorPart +
                "." + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductMinorPart + "." + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductBuildPart;
            Console.Write("\n\n\n============================================================================\n" + "Running " + productVersion + " ");
            Console.WriteLine("on OpenGL 4.5 Core Profile.");
            if (_multithreaded)
                Console.WriteLine("                [ experimental multithreading mode ]");
            Console.WriteLine("============================================================================\n");

            KWEngine.TextureDefault = HelperTexture.LoadTextureInternal("checkerboard.png");
            KWEngine.TextureBlack = HelperTexture.LoadTextureInternal("black.png");
            KWEngine.TextureWhite = HelperTexture.LoadTextureInternal("white.png");
            KWEngine.TextureAlpha = HelperTexture.LoadTextureInternal("alpha.png");

            KWEngine.TextureDepthEmpty = HelperTexture.CreateEmptyDepthTexture();
            KWEngine.TextureDepthCubeMapEmpty = HelperTexture.CreateEmptyCubemapDepthTexture();
            KWEngine.TextureCubemapEmpty = HelperTexture.CreateEmptyCubemapTexture();

            KWEngine.InitializeShaders();
            KWEngine.InitializeModels();
            KWEngine.InitializeParticles();
            KWEngine.InitializeFont("anonymous.dds", 0);
            KWEngine.InitializeFont("anonymous2.dds", 1);
            KWEngine.InitializeFont("anonymous3.dds", 2);
            KWEngine.InitializeFont("anonymous4.dds", 3);
            _bloomQuad = KWEngine.KWRect;
        }


        /// <summary>
        /// EventHandler für das erste Laden des Fensters
        /// </summary>
        /// <param name="e">Parameter</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.ProgramPointSize);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Only needed for tesselation... maybe later?
            //GL.PatchParameter(PatchParameterInt.PatchVertices, 4);
        }

        /// <summary>
        /// Schließt das Fenster
        /// </summary>
        /// <param name="manual">true, wenn manuell herbeigeführt</param>
        protected override void Dispose(bool manual)
        {
            if (CurrentWorld != null)
                CurrentWorld.Dispose();
            GLAudioEngine.SoundStopAll();
            GLAudioEngine.Dispose();

            base.Dispose(manual);
        }

        internal void ForceClose()
        {
            if (CurrentWorld != null)
            {
                CurrentWorld.Dispose();
            }
            Close();
        }

        private List<GameObject> mInstancesRenderLast = new List<GameObject>();
        private List<GameObject> mInstancesTerrain = new List<GameObject>();

        /// <summary>
        /// EventHandler für den Render-Thread
        /// </summary>
        /// <param name="e">Parameter</param>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            if (KWEngine.GlobalError)
                return;

            lock (HelperGLLoader.LoadList)
            {
                if (HelperGLLoader.LoadList.Count > 0)
                {

                    LoadPackage lp = HelperGLLoader.LoadList[0];
                    HelperGLLoader.LoadList.RemoveAt(0);
                    Type type = lp.ReceiverType.BaseType;
                    if (type.IsEquivalentTo(typeof(GLWindow)))
                    {
                        if (CurrentWorld != null)
                        {
                            CurrentWorld._prepared = false;
                        }
                        lp.Action.Invoke();
                        return;
                    }
                    else
                    {
                        lp.Action.Invoke();
                    }

                }
            }

            if (CurrentWorld != null && CurrentWorld._prepared)
            {
                mInstancesRenderLast.Clear();
                mInstancesTerrain.Clear();

                // ==================================================
                // Generate VIEW and PROJECTION for main render pass!
                // ==================================================
                Matrix4 viewProjection;
                if (CurrentWorld.DebugShadowLight != null && CurrentWorld.DebugShadowLight.Type != LightType.Point && CurrentWorld.DebugShadowLight.CurrentWorld != null)
                {
                    _viewMatrix = Matrix4.LookAt(CurrentWorld.DebugShadowLight.Position, CurrentWorld.DebugShadowLight.Target, KWEngine.WorldUp);
                    viewProjection = CurrentWorld.DebugShadowLight._viewProjectionMatrixShadow[0];
                    Frustum.CalculateFrustum(CurrentWorld.DebugShadowLight._projectionMatrixShadow, _viewMatrix);
                }
                else
                {
                    if (CurrentWorld.IsFirstPersonMode)
                        _viewMatrix = HelperCamera.GetViewMatrix(CurrentWorld.GetFirstPersonObject().Position);
                    else
                        _viewMatrix = Matrix4.LookAt(CurrentWorld.GetCameraPosition(), CurrentWorld.GetCameraTarget(), KWEngine.WorldUp);
                    viewProjection = _viewMatrix * _projectionMatrix;
                    Frustum.CalculateFrustum(_projectionMatrix, _viewMatrix);
                }

                

                // ==================================================
                // Generate light arrays for main render pass!
                // ==================================================
                lock (CurrentWorld._lightObjects)
                {
                    //TODO: add light type and ids for arrays!
                    CurrentWorld._lightObjects.Sort();
                    LightObject.PrepareLightsForRenderPass(CurrentWorld._lightObjects, ref LightColors, ref LightTargets, ref LightPositions, ref LightMeta, ref CurrentWorld._lightcount);
                }

                // ==================================================
                // Prepare model matrices for render pass:
                // ==================================================
                foreach (GameObject g in CurrentWorld._gameObjects)
                {
                    for (int index = 0; index < g.Model.Meshes.Count; index++)
                    {
                        GeoMesh mesh = g.Model.Meshes.Values.ElementAt(index);
                        bool useMeshTransform = mesh.BoneNames.Count == 0 || !(g.AnimationID >= 0 && g.Model.Animations != null && g.Model.Animations.Count > 0);
                        if (useMeshTransform)
                        {
                            Matrix4.Mult(ref mesh.Transform, ref g._modelMatrix, out g.ModelMatrixForRenderPass[g.Model.IsKWCube6 ? 0 : index]);
                        }
                        else
                        {
                            g.ModelMatrixForRenderPass[g.Model.IsKWCube6 ? 0 : index] = g._modelMatrix;
                        }
                    }
                }
                // ==================================================
                // Do the shadow render pass for all lights:
                // ==================================================
                for (int i = 0; i < CurrentWorld._lightObjects.Count; i++)
                {
                    LightObject currentLight = CurrentWorld._lightObjects[i];
                    if(!currentLight.IsShadowCaster)
                    {
                        continue;
                    }

                    int fbId = -1;
                    if(currentLight.Type == LightType.Point)
                    {
                        fbId = FramebuffersShadowCubeMap[currentLight._framebufferIndex];
                    }
                    else
                    {
                        fbId = FramebuffersShadow[currentLight._framebufferIndex];
                    }
                    if(fbId < 0)
                    {
                        HelperGL.ShowErrorAndQuit("GLWindow::OnRenderFrame()", "Internal error: frame buffer index for light is -1");
                        return;
                    }
                    SwitchToBufferAndClear(fbId);
                    
                    if(currentLight.Type == LightType.Point)
                    {
                        GL.Viewport(0, 0, HelperTexture.RoundDownToPowerOf2(KWEngine.ShadowMapSize / 4), HelperTexture.RoundDownToPowerOf2(KWEngine.ShadowMapSize / 4));
                        GL.UseProgram(KWEngine.RendererShadowCubeMap.GetProgramId());
                        foreach (GameObject g in CurrentWorld._gameObjects)
                        {
                            KWEngine.RendererShadowCubeMap.Draw(g, currentLight.Position, currentLight.GetDistanceMultiplier(), currentLight.GetDistanceMultiplier() * currentLight.GetFrustumMultiplier(), ref currentLight._viewProjectionMatrixShadow);
                        }
                        GL.UseProgram(0);
                    }
                    else
                    {
                        GL.Viewport(0, 0, KWEngine.ShadowMapSize, KWEngine.ShadowMapSize);
                        GL.UseProgram(KWEngine.RendererShadow.GetProgramId());
                        foreach (GameObject g in CurrentWorld._gameObjects)
                        {
                            KWEngine.RendererShadow.Draw(g, ref currentLight._viewProjectionMatrixShadow[0], currentLight._frustumShadowMap);
                        }
                        GL.UseProgram(0);
                    }
                }

                // ==================================================
                // Do the MAIN RENDER PASS:
                // ==================================================
                SwitchToBufferAndClear(FramebufferMainMultisample);
                GL.Viewport(ClientRectangle);

                if(CurrentWorld.DebugShowCoordinateSystemGrid != GridType.None)
                {
                    KWEngine.RendererSimple.DrawGrid(CurrentWorld.DebugShowCoordinateSystemGrid, ref viewProjection);
                }

                lock (CurrentWorld._gameObjects)
                {
                    GL.UseProgram(KWEngine.RendererStandard.GetProgramId());

                    // How many lights are there?
                    GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleLightCount(), CurrentWorld._lightcount);
                    GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsColors(), KWEngine.MAX_LIGHTS, LightColors);
                    GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsTargets(), KWEngine.MAX_LIGHTS, LightTargets);
                    GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsPositions(), KWEngine.MAX_LIGHTS, LightPositions);
                    GL.Uniform3(KWEngine.RendererStandard.GetUniformHandleLightsMeta(), KWEngine.MAX_LIGHTS, LightMeta);

                    // Ambient color and intensity:
                    GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleSunAmbient(), CurrentWorld._ambientLight);

                    List<LightObject> lights = CurrentWorld._lightObjects;
                    int numLights = lights.Count;
                    int textureIndex = 0;
                    for (int lightIndex = 0; lightIndex < KWEngine.MAX_SHADOWMAPS; textureIndex += 2, lightIndex++)
                    {
                        LightObject currentLight = null;
                        if (lightIndex < numLights)
                            currentLight = lights[lightIndex];

                        // POINT LIGHT:
                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                        GL.BindTexture(TextureTarget.TextureCubeMap, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTexturesCubeMap[currentLight._framebufferIndex] : KWEngine.TextureDepthCubeMapEmpty);
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureShadowMapCubeMap() + lightIndex, textureIndex);

                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex + 1);
                        GL.BindTexture(TextureTarget.Texture2D, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTextures[currentLight._framebufferIndex] : KWEngine.TextureDepthEmpty);
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureShadowMap() + lightIndex, textureIndex + 1);
                        if (currentLight != null)
                            GL.UniformMatrix4(KWEngine.RendererStandard.GetUniformHandleVPShadowMap() + lightIndex, false, ref currentLight._viewProjectionMatrixShadow[0]);
                        else
                            GL.UniformMatrix4(KWEngine.RendererStandard.GetUniformHandleVPShadowMap() + lightIndex, false, ref KWEngine.Identity);
                    }

                    // Upload skybox for metal reflections:
                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    GL.BindTexture(TextureTarget.TextureCubeMap, CurrentWorld._textureSkybox > 0 ? CurrentWorld._textureSkybox : KWEngine.TextureCubemapEmpty);
                    GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSkybox(), textureIndex);
                    textureIndex++;

                    // Upload 2d sky for metal reflections:
                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    GL.BindTexture(TextureTarget.Texture2D, CurrentWorld._textureBackground > 0 ? CurrentWorld._textureBackground : KWEngine.TextureBlack);
                    GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSky2D(), textureIndex);
                    textureIndex++;

                    GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureIsSkybox(), CurrentWorld._textureSkybox > 0 ? 1 : CurrentWorld._textureBackground > 0 ? -1 : 0);
                    GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSkyBoost(), CurrentWorld._textureBackgroundMultiplier);


                    foreach (GameObject g in CurrentWorld._gameObjects)
                    {
                        g._collisionCandidates.Clear(); // clear collision list for this objects

                        if (g == null || !g.HasModel || g.CurrentWorld == null || g.Opacity <= 0)
                            continue;

                        if (g.Model.IsTerrain)
                        {
                            mInstancesTerrain.Add(g);
                            continue;
                        }

                        if (g.CurrentWorld.IsFirstPersonMode && g.CurrentWorld.GetFirstPersonObject().Equals(g))
                            continue;
                        if (KWEngine.Projection == ProjectionType.Perspective)
                        {
                            g.IsInsideScreenSpace = Frustum.VolumeVsFrustum(g.GetCenterPointForAllHitboxes(), g.GetMaxDimensions().X, g.GetMaxDimensions().Y, g.GetMaxDimensions().Z);
                        }
                        else
                        {
                            Vector3 lookAt = CurrentWorld.GetCameraLookAtVectorEitherWay();
                            Vector3 camPos = CurrentWorld.GetCameraPositionEitherWay();
                            Vector3 objectCenter = g.GetCenterPointForAllHitboxes();
                            float dot = Vector3.Dot(objectCenter - camPos, lookAt);
                            g.IsInsideScreenSpace = dot > 0;
                        }
                        if (!g.IsInsideScreenSpace)
                            continue;

                        bool opacityFound = false;
                        foreach (GeoMesh mesh in g.Model.Meshes.Values)
                        {
                            if (mesh.Material.Opacity < 1f)
                            {
                                opacityFound = true;
                                break;
                            }
                        }
                        if (opacityFound || g.Opacity < 1f)
                        {
                            mInstancesRenderLast.Add(g);
                            continue;
                        }
                        else
                        {
                            KWEngine.RendererStandard.Draw(g, ref viewProjection, textureIndex);
                        }
                    }
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                    GL.UseProgram(0);

                    if (CurrentWorld.DebugShowHitboxes)
                    {
                        GL.UseProgram(KWEngine.RendererSimple.GetProgramId());
                        foreach (GameObject g in CurrentWorld._gameObjects)
                        {
                            KWEngine.RendererSimple.DrawHitbox(g, ref viewProjection);
                        }
                        GL.UseProgram(0);
                    }

                    if (mInstancesTerrain.Count > 0)
                    {
                        GL.UseProgram(KWEngine.RendererTerrain.GetProgramId());

                        // How many lights are there?
                        GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleLightCount(), CurrentWorld._lightcount);
                        GL.Uniform4(KWEngine.RendererTerrain.GetUniformHandleLightsColors(), KWEngine.MAX_LIGHTS, LightColors);
                        GL.Uniform4(KWEngine.RendererTerrain.GetUniformHandleLightsTargets(), KWEngine.MAX_LIGHTS, LightTargets);
                        GL.Uniform4(KWEngine.RendererTerrain.GetUniformHandleLightsPositions(), KWEngine.MAX_LIGHTS, LightPositions);
                        GL.Uniform3(KWEngine.RendererTerrain.GetUniformHandleLightsMeta(), KWEngine.MAX_LIGHTS, LightMeta);

                        // Ambient color and intensity:
                        GL.Uniform4(KWEngine.RendererTerrain.GetUniformHandleSunAmbient(), CurrentWorld._ambientLight);

                        textureIndex = 0;
                        for (int lightIndex = 0; lightIndex < KWEngine.MAX_SHADOWMAPS; textureIndex += 2, lightIndex++)
                        {
                            LightObject currentLight = null;
                            if (lightIndex < numLights)
                                currentLight = lights[lightIndex];

                            // POINT LIGHT:
                            GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                            GL.BindTexture(TextureTarget.TextureCubeMap, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTexturesCubeMap[currentLight._framebufferIndex] : KWEngine.TextureDepthCubeMapEmpty);
                            GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureShadowMapCubeMap() + lightIndex, textureIndex);

                            GL.ActiveTexture(TextureUnit.Texture0 + textureIndex + 1);
                            GL.BindTexture(TextureTarget.Texture2D, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTextures[currentLight._framebufferIndex] : KWEngine.TextureDepthEmpty);
                            GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureShadowMap() + lightIndex, textureIndex + 1);
                            if (currentLight != null)
                                GL.UniformMatrix4(KWEngine.RendererTerrain.GetUniformHandleVPShadowMap() + lightIndex, false, ref currentLight._viewProjectionMatrixShadow[0]);
                            else
                                GL.UniformMatrix4(KWEngine.RendererTerrain.GetUniformHandleVPShadowMap() + lightIndex, false, ref KWEngine.Identity);
                        }

                        // Upload skybox for metal reflections:
                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                        GL.BindTexture(TextureTarget.TextureCubeMap, CurrentWorld._textureSkybox > 0 ? CurrentWorld._textureSkybox : KWEngine.TextureCubemapEmpty);
                        GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureSkybox(), textureIndex);
                        textureIndex++;

                        // Upload 2d sky for metal reflections:
                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                        GL.BindTexture(TextureTarget.Texture2D, CurrentWorld._textureBackground > 0 ? CurrentWorld._textureBackground : KWEngine.TextureBlack);
                        GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureSky2D(), textureIndex);
                        textureIndex++;

                        GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureIsSkybox(), CurrentWorld._textureSkybox > 0 ? 1 : CurrentWorld._textureBackground > 0 ? -1 : 0);
                        GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureSkyBoost(), CurrentWorld._textureBackgroundMultiplier);

                        foreach (GameObject g in mInstancesTerrain)
                        {
                            KWEngine.RendererTerrain.Draw(g, ref viewProjection, Frustum, textureIndex);
                        }
                        GL.BindTexture(TextureTarget.Texture2D, 0);
                        GL.UseProgram(0);
                        mInstancesTerrain.Clear();
                    }

                    // Background rendering:
                    if (CurrentWorld._textureBackground > 0)
                    {
                        KWEngine.RendererBackground.Draw(ref _modelViewProjectionMatrixBackground);
                    }
                    else if (CurrentWorld._textureSkybox > 0)
                    {
                        KWEngine.RendererSkybox.Draw(_projectionMatrix);
                    }


                    // =========================
                    // RENDER TRANSPARENT STUFF:
                    // =========================
                    if (mInstancesRenderLast.Count > 0)
                    {
                        mInstancesRenderLast.Sort((x, y) => x == null ? (y == null ? 0 : -1) : (y == null ? 1 : y.DistanceToCamera.CompareTo(x.DistanceToCamera)));
                        GL.UseProgram(KWEngine.RendererStandard.GetProgramId());

                        // How many lights are there?
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleLightCount(), CurrentWorld._lightcount);
                        GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsColors(), KWEngine.MAX_LIGHTS, LightColors);
                        GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsTargets(), KWEngine.MAX_LIGHTS, LightTargets);
                        GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsPositions(), KWEngine.MAX_LIGHTS, LightPositions);
                        GL.Uniform3(KWEngine.RendererStandard.GetUniformHandleLightsMeta(), KWEngine.MAX_LIGHTS, LightMeta);

                        // Ambient color and intensity:
                        GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleSunAmbient(), CurrentWorld._ambientLight);

                        textureIndex = 0;
                        for (int lightIndex = 0; lightIndex < KWEngine.MAX_SHADOWMAPS; textureIndex += 2, lightIndex++)
                        {
                            LightObject currentLight = null;
                            if (lightIndex < numLights)
                                currentLight = lights[lightIndex];

                            // POINT LIGHT:
                            GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                            GL.BindTexture(TextureTarget.TextureCubeMap, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTexturesCubeMap[currentLight._framebufferIndex] : KWEngine.TextureDepthCubeMapEmpty);
                            GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureShadowMapCubeMap() + lightIndex, textureIndex);

                            GL.ActiveTexture(TextureUnit.Texture0 + textureIndex + 1);
                            GL.BindTexture(TextureTarget.Texture2D, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTextures[currentLight._framebufferIndex] : KWEngine.TextureDepthEmpty);
                            GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureShadowMap() + lightIndex, textureIndex + 1);
                            if (currentLight != null)
                                GL.UniformMatrix4(KWEngine.RendererStandard.GetUniformHandleVPShadowMap() + lightIndex, false, ref currentLight._viewProjectionMatrixShadow[0]);
                            else
                                GL.UniformMatrix4(KWEngine.RendererStandard.GetUniformHandleVPShadowMap() + lightIndex, false, ref KWEngine.Identity);
                        }

                        // Upload skybox for metal reflections:
                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                        GL.BindTexture(TextureTarget.TextureCubeMap, CurrentWorld._textureSkybox > 0 ? CurrentWorld._textureSkybox : KWEngine.TextureCubemapEmpty);
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSkybox(), textureIndex);
                        textureIndex++;

                        // Upload 2d sky for metal reflections:
                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                        GL.BindTexture(TextureTarget.Texture2D, CurrentWorld._textureBackground > 0 ? CurrentWorld._textureBackground : KWEngine.TextureBlack);
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSky2D(), textureIndex);
                        textureIndex++;

                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureIsSkybox(), CurrentWorld._textureSkybox > 0 ? 1 : CurrentWorld._textureBackground > 0 ? -1 : 0);
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSkyBoost(), CurrentWorld._textureBackgroundMultiplier);

                        foreach (GameObject g in mInstancesRenderLast)
                        {
                            KWEngine.RendererStandard.Draw(g, ref viewProjection, textureIndex);
                        }
                        GL.UseProgram(0);

                        
                        if (CurrentWorld.DebugShowHitboxes)
                        {
                            GL.UseProgram(KWEngine.RendererSimple.GetProgramId());
                            foreach (GameObject g in mInstancesRenderLast)
                            {
                                KWEngine.RendererSimple.DrawHitbox(g, ref viewProjection);
                            }
                            GL.UseProgram(0);
                        }
                        
                        mInstancesRenderLast.Clear();
                    }
                    
                }
                

                lock (CurrentWorld._explosionObjects)
                {
                    if (CurrentWorld._explosionObjects.Count > 0)
                    {
                        
                        GL.UseProgram(KWEngine.RendererExplosion.GetProgramId());
                        foreach (Explosion ex in CurrentWorld._explosionObjects)
                        {
                            KWEngine.RendererExplosion.Draw(ex, ref viewProjection);
                        }
                        GL.UseProgram(0);
                    }
                }

                lock (CurrentWorld._particleObjects)
                {
                    GL.Enable(EnableCap.Blend);
                    GL.UseProgram(KWEngine.RendererParticle.GetProgramId());
                    foreach (ParticleObject p in CurrentWorld.GetParticleObjects())
                        KWEngine.RendererParticle.Draw(p, ref viewProjection);
                    GL.UseProgram(0);
                    GL.Disable(EnableCap.Blend);
                }
                GL.Enable(EnableCap.Blend);
                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.CullFace);
                lock (CurrentWorld._hudObjects)
                {
                    GL.UseProgram(KWEngine.RendererHUD.GetProgramId());
                    foreach (HUDObject p in CurrentWorld._hudObjects)
                        KWEngine.RendererHUD.Draw(p, ref _viewProjectionMatrixHUD);
                    GL.UseProgram(0);
                }
                GL.Disable(EnableCap.Blend);
                GL.Enable(EnableCap.CullFace);
                if (CurrentWorld.DebugShowCoordinateSystem)
                {
                    KWEngine.DrawCoordinateSystem(ref viewProjection);
                }

                GL.Enable(EnableCap.DepthTest);
            }

            DownsampleFramebuffer();
            ApplyBloom();
            SwapBuffers();

            frameCounter++;
            frameData += (e.Time * 1000.0);
            if (frameCounter > 100)
            {
                int index = Title != null ? Title.LastIndexOf('|') : -1;
                if (KWEngine.DebugShowPerformanceInTitle == PerformanceUnit.FrameTimeInMilliseconds)
                {
                    if (index < 0)
                    {
                        Title = Title + " | " + Math.Round(frameData / frameCounter, 2) + " ms";
                    }
                    else
                    {
                        Title = Title.Substring(0, index - 1);
                        Title += " | " + Math.Round(frameData / frameCounter, 2) + " ms";
                    }
                }
                else if (KWEngine.DebugShowPerformanceInTitle == PerformanceUnit.FramesPerSecond)
                {
                    if (index < 0)
                    {
                        Title = Title + " | " + Math.Round(1000.0 / (frameData / frameCounter), 1) + " fps";
                    }
                    else
                    {
                        Title = Title.Substring(0, index - 1);
                        Title += " | " + Math.Round(1000.0 / (frameData / frameCounter), 1) + " fps";
                    }
                }
                else
                {
                    if (index >= 0)
                        Title = Title.Substring(0, index - 1);
                }
                frameCounter = 0;
                frameData = 0;
            }

            if (!_multithreaded)
            {
                DeltaTime.UpdateDeltaTime();
                KWEngine.TimeElapsed += (float)e.Time;
            }
        }

        internal static void SwitchToBufferAndClear(int id)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, id);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        private List<GameObject> mInstancesLast = new List<GameObject>();

        /// <summary>
        /// EventHandler für den Gameplay-Thread
        /// </summary>
        /// <param name="e">Parameter</param>
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            if (KWEngine.GlobalError)
                return;
            KeyboardState ks = Keyboard.GetState();
            MouseState ms = Mouse.GetCursorState();
            _mousePoint.X = ms.X;
            _mousePoint.Y = ms.Y;

            if (ks.IsKeyDown(Key.AltLeft) && ks.IsKeyDown(Key.F4))
            {
                Close();
                return;
            }

            if (CurrentWorld == null)
                return;

            if (CurrentWorld._prepared)
            {
                lock (CurrentWorld._explosionObjects)
                {
                    foreach (Explosion ex in CurrentWorld._explosionObjects)
                    {
                        ex.Act();
                    }
                }


                lock (CurrentWorld._gameObjects)
                {
                    CurrentWorld.SweepAndPrune();
                    mInstancesLast.Clear();
                    foreach (GameObject g in CurrentWorld.GetGameObjects())
                    {
                        if (g.UpdateLast)
                        {
                            mInstancesLast.Add(g);
                            continue;
                        }
                        g.Act(ks, ms);
                        g.CheckBounds();
                    }
                    foreach (GameObject g in mInstancesLast)
                    {
                        g.Act(ks, ms);
                        g.CheckBounds();
                    }
                    mInstancesLast.Clear();
                }


                lock (CurrentWorld._particleObjects)
                {
                    foreach (ParticleObject p in CurrentWorld.GetParticleObjects())
                    {
                        p.Act();
                    }
                }

                CurrentWorld.Act(ks, ms);

                CurrentWorld.AddRemoveObjects();
                //CurrentWorld.SortByZ();


                if (CurrentWorld.IsFirstPersonMode && Focused)
                {
                    Mouse.SetPosition(_mousePointFPS.X, _mousePointFPS.Y);
                }
            }
            if (_multithreaded)
            {
                DeltaTime.UpdateDeltaTime();
                KWEngine.TimeElapsed += (float)e.Time;
            }
        }

        /// <summary>
        /// Prüft, ob sich der Mauszeiger im Fenster befindet
        /// </summary>
        public bool IsMouseInWindow
        {
            get
            {
                if (_windowRect.Contains(_mousePoint))
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// EventHandler für Fokuswechsel
        /// </summary>
        /// <param name="e">Parameter</param>
        protected override void OnFocusedChanged(EventArgs e)
        {
            base.OnFocusedChanged(e);
            if (Focused)
            {
                if (CurrentWorld != null && CurrentWorld.IsFirstPersonMode)
                {
                    Mouse.SetPosition(_mousePointFPS.X, _mousePointFPS.Y);
                }
            }

        }

        /// <summary>
        /// EventHandler für Größenanpassung des Fensters
        /// </summary>
        /// <param name="e">Parameter</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(ClientRectangle);
            if(ClientRectangle.Width > 0 && ClientRectangle.Height > 0)
            {
                InitializeFramebuffers();

                _mousePointFPS.X = X + Width / 2;
                _mousePointFPS.Y = Y + Height / 2;

                CalculateProjectionMatrix();
                UpdateWindowRect();

                bloomWidth = (float)(Math.Log(Width * Width) / Width) * ((float)Height / Width);
                bloomHeight = (float)(Math.Log(Height * Height) / Height);
            }
            
        }

        /// <summary>
        /// EventHandler für das Bewegen des Fensters
        /// </summary>
        /// <param name="e">Parameter</param>
        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            _mousePointFPS.X = X + Width / 2;
            _mousePointFPS.Y = Y + Height / 2;
            UpdateWindowRect();
        }

        internal void UpdateWindowRect()
        {

            if (WindowState == WindowState.Fullscreen)
            {
                _windowRect = new System.Drawing.Rectangle(this.X, this.Y, this.Width, this.Height);
            }
            else
            {
                _windowRect = new System.Drawing.Rectangle(this.X + 8, this.Y + SystemInformation.CaptionHeight + 8, this.Width, this.Height);
            }

        }

        internal void CalculateProjectionMatrix()
        {
            if (KWEngine.Projection == ProjectionType.Perspective)
                _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(CurrentWorld != null ? CurrentWorld.FOV / 2 : 45f), ClientSize.Width / (float)ClientSize.Height, 0.1f, CurrentWorld != null ? CurrentWorld.ZFar : 1000f);
            else
            {
                float aspectratio = CurrentWindow.Width / (float)CurrentWindow.Height;
                _projectionMatrix = Matrix4.CreateOrthographic(CurrentWorld != null ? CurrentWorld.FOV * aspectratio: 90f * aspectratio, CurrentWorld != null ? CurrentWorld.FOV : 90f, 0.1f, CurrentWorld != null ? CurrentWorld.ZFar : 1000f);
            }

            _modelViewProjectionMatrixBloom = Matrix4.CreateScale(ClientSize.Width / 2f, ClientSize.Height / 2f, 1) * Matrix4.LookAt(0, 0, 1, 0, 0, 0, 0, 1, 0) * Matrix4.CreateOrthographic(ClientSize.Width / 2f, ClientSize.Height / 2f, 0.1f, 100f);
            _modelViewProjectionMatrixBloomMerge = Matrix4.CreateScale(ClientSize.Width, ClientSize.Height, 1) * Matrix4.LookAt(0, 0, 1, 0, 0, 0, 0, 1, 0) * Matrix4.CreateOrthographic(ClientSize.Width, ClientSize.Height, 0.1f, 100f);

            _modelViewProjectionMatrixBackground = Matrix4.CreateScale(ClientSize.Width, ClientSize.Height, 1) * Matrix4.LookAt(0, 0, 1, 0, 0, 0, 0, 1, 0) * Matrix4.CreateOrthographic(ClientSize.Width, ClientSize.Height, 0.1f, 100f);

            _viewProjectionMatrixHUD = Matrix4.LookAt(0, 0, 1, 0, 0, 0, 0, 1, 0) * Matrix4.CreateOrthographic(ClientSize.Width, ClientSize.Height, 0.1f, 100f);
        }

        /// <summary>
        /// Setzt die aktuelle Welt
        /// </summary>
        /// <param name="w">Welt-Instanz</param>
        public void SetWorld(World w)
        {
            void Action() => SetWorldInternal(w);
            if (CurrentWorld != null)
                CurrentWorld._prepared = false;
            HelperGLLoader.AddCall(this, Action);
        }

        internal void SetWorldInternal(World w)
        {

            if (CurrentWorld == null)
            {
                CurrentWorld = w;
                CursorVisible = true;
                KWEngine.CustomTextures.Add(w, new Dictionary<string, int>());
                while (GLAudioEngine.IsInitializing)
                {
                    Thread.Sleep(10);
                }
                CurrentWorld.Prepare();
                CurrentWorld._prepared = true;
                CalculateProjectionMatrix();
            }
            else
            {
                lock (CurrentWorld)
                {
                    if (CurrentWorld != null)
                    {
                        CurrentWorld.Dispose();
                    }
                    CursorVisible = true;
                    CurrentWorld = w;
                    KWEngine.CustomTextures.Add(w, new Dictionary<string, int>());
                    CurrentWorld.Prepare();
                    CurrentWorld._prepared = true;
                    CalculateProjectionMatrix();
                }
            }

            GLWindow.StartGarbageCollection();
            DeltaTime.Watch.Stop();
            DeltaTime.Watch.Reset();
            DeltaTime.lastRealTimeMeasurement_ms = 0;
            DeltaTime.Watch.Start();
        }

        internal static void StartGarbageCollection()
        {
            GC.KeepAlive(DeltaTime.Watch);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false);
        }

        private void ApplyBloom()
        {
            if (KWEngine.PostProcessQuality != PostProcessingQuality.Disabled)
            {
                GL.UseProgram(KWEngine.RendererBloom.GetProgramId());
                GL.Viewport(0, 0, _bloomwidth, _bloomheight);
                int loopCount =
                    KWEngine.PostProcessQuality == PostProcessingQuality.High ? 6 :
                    KWEngine.PostProcessQuality == PostProcessingQuality.Standard ? 4 : 2;
                int sourceTex; // this is the texture that the bloom will be read from
                for (int i = 0; i < loopCount; i++)
                {
                    if (i % 2 == 0)
                    {
                        if (i == 0)
                            SwitchToBufferAndClear(FramebufferBloom1);
                        else
                            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferBloom1);
                        if (i == 0)
                            sourceTex = TextureBloomFinal;
                        else
                            sourceTex = TextureBloom2;
                    }
                    else
                    {
                        sourceTex = TextureBloom1;
                        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferBloom2);
                    }

                    KWEngine.RendererBloom.DrawBloom(
                        _bloomQuad,
                        ref _modelViewProjectionMatrixBloom,
                        i % 2 == 0,
                        sourceTex
                    );
                }

                SwitchToBufferAndClear(0);
                GL.UseProgram(KWEngine.RendererMerge.GetProgramId());
                GL.Viewport(0, 0, Width, Height);
                KWEngine.RendererMerge.DrawMerge(_bloomQuad, ref _modelViewProjectionMatrixBloomMerge, TextureMainFinal, TextureBloom2);
                GL.UseProgram(0); // unload bloom shader program
            }
        }

        internal void DrawSceneForSkybox(Matrix4 viewMatrix, Matrix4 projectionMatrix, int matrixIndex)
        {
            List<GameObject> instancesRenderLast = new List<GameObject>();
            List<GameObject> instancesTerrain = new List<GameObject>();

            CurrentWorld.AddRemoveObjects();

            if (CurrentWorld != null)
            {
                Matrix4 viewProjection = viewMatrix * projectionMatrix;

                CurrentWorld._lightObjects.Sort();
                LightObject.PrepareLightsForRenderPass(CurrentWorld._lightObjects, ref LightColors, ref LightTargets, ref LightPositions, ref LightMeta, ref CurrentWorld._lightcount);

                foreach (GameObject g in CurrentWorld._gameObjects)
                {
                    for (int index = 0; index < g.Model.Meshes.Count; index++)
                    {
                        GeoMesh mesh = g.Model.Meshes.Values.ElementAt(index);
                        bool useMeshTransform = mesh.BoneNames.Count == 0 || !(g.AnimationID >= 0 && g.Model.Animations != null && g.Model.Animations.Count > 0);
                        if (useMeshTransform)
                        {
                            Matrix4.Mult(ref mesh.Transform, ref g._modelMatrix, out g.ModelMatrixForRenderPass[g.Model.IsKWCube6 ? 0 : index]);
                        }
                        else
                        {
                            g.ModelMatrixForRenderPass[g.Model.IsKWCube6 ? 0 : index] = g._modelMatrix;
                        }
                    }
                }

                for (int i = 0; i < CurrentWorld._lightObjects.Count; i++)
                {
                    LightObject currentLight = CurrentWorld._lightObjects[i];
                    if (!currentLight.IsShadowCaster)
                    {
                        continue;
                    }

                    int fbId = -1;
                    if (currentLight.Type == LightType.Point)
                    {
                        fbId = FramebuffersShadowCubeMap[currentLight._framebufferIndex];
                    }
                    else
                    {
                        fbId = FramebuffersShadow[currentLight._framebufferIndex];
                    }
                    if (fbId < 0)
                    {
                        HelperGL.ShowErrorAndQuit("GLWindow::DrawSceneForSkybox()", "Internal error: frame buffer index for light is -1");
                        return;
                    }
                    SwitchToBufferAndClear(fbId);

                    if (currentLight.Type == LightType.Point)
                    {
                        GL.Viewport(0, 0, HelperTexture.RoundDownToPowerOf2(KWEngine.ShadowMapSize / 4), HelperTexture.RoundDownToPowerOf2(KWEngine.ShadowMapSize / 4));
                        GL.UseProgram(KWEngine.RendererShadowCubeMap.GetProgramId());
                        foreach (GameObject g in CurrentWorld._gameObjects)
                        {
                            KWEngine.RendererShadowCubeMap.Draw(g, currentLight.Position, currentLight.GetDistanceMultiplier(), currentLight.GetDistanceMultiplier() * currentLight.GetFrustumMultiplier(), ref currentLight._viewProjectionMatrixShadow);
                        }
                        GL.UseProgram(0);
                    }
                    else
                    {
                        GL.Viewport(0, 0, KWEngine.ShadowMapSize, KWEngine.ShadowMapSize);
                        GL.UseProgram(KWEngine.RendererShadow.GetProgramId());
                        foreach (GameObject g in CurrentWorld._gameObjects)
                        {
                            KWEngine.RendererShadow.Draw(g, ref currentLight._viewProjectionMatrixShadow[0], currentLight._frustumShadowMap);
                        }
                        GL.UseProgram(0);
                    }
                }

                // ==================================================
                // Do the MAIN RENDER PASS:
                // ==================================================
                SwitchToBufferAndClear(HelperSkybox._fbFull);
                GL.Viewport(0, 0, HelperSkybox._resolution, HelperSkybox._resolution);

                lock (CurrentWorld._gameObjects)
                {
                    GL.UseProgram(KWEngine.RendererStandard.GetProgramId());

                    // How many lights are there?
                    GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleLightCount(), CurrentWorld._lightcount);
                    GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsColors(), KWEngine.MAX_LIGHTS, LightColors);
                    GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsTargets(), KWEngine.MAX_LIGHTS, LightTargets);
                    GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsPositions(), KWEngine.MAX_LIGHTS, LightPositions);
                    GL.Uniform3(KWEngine.RendererStandard.GetUniformHandleLightsMeta(), KWEngine.MAX_LIGHTS, LightMeta);

                    // Ambient color and intensity:
                    GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleSunAmbient(), CurrentWorld._ambientLight);

                    List<LightObject> lights = CurrentWorld._lightObjects;
                    int numLights = lights.Count;
                    int textureIndex = 0;
                    for (int lightIndex = 0; lightIndex < KWEngine.MAX_SHADOWMAPS; textureIndex += 2, lightIndex++)
                    {
                        LightObject currentLight = null;
                        if (lightIndex < numLights)
                            currentLight = lights[lightIndex];

                        // POINT LIGHT:
                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                        GL.BindTexture(TextureTarget.TextureCubeMap, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTexturesCubeMap[currentLight._framebufferIndex] : KWEngine.TextureDepthCubeMapEmpty);
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureShadowMapCubeMap() + lightIndex, textureIndex);

                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex + 1);
                        GL.BindTexture(TextureTarget.Texture2D, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTextures[currentLight._framebufferIndex] : KWEngine.TextureDepthEmpty);
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureShadowMap() + lightIndex, textureIndex + 1);
                        if (currentLight != null)
                            GL.UniformMatrix4(KWEngine.RendererStandard.GetUniformHandleVPShadowMap() + lightIndex, false, ref currentLight._viewProjectionMatrixShadow[0]);
                        else
                            GL.UniformMatrix4(KWEngine.RendererStandard.GetUniformHandleVPShadowMap() + lightIndex, false, ref KWEngine.Identity);
                    }

                    // Upload skybox for metal reflections:
                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    GL.BindTexture(TextureTarget.TextureCubeMap, CurrentWorld._textureSkybox > 0 ? CurrentWorld._textureSkybox : KWEngine.TextureCubemapEmpty);
                    GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSkybox(), textureIndex);
                    textureIndex++;

                    // Upload 2d sky for metal reflections:
                    GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                    GL.BindTexture(TextureTarget.Texture2D, CurrentWorld._textureBackground > 0 ? CurrentWorld._textureBackground : KWEngine.TextureBlack);
                    GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSky2D(), textureIndex);
                    textureIndex++;

                    GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureIsSkybox(), CurrentWorld._textureSkybox > 0 ? 1 : CurrentWorld._textureBackground > 0 ? -1 : 0);
                    GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSkyBoost(), CurrentWorld._textureBackgroundMultiplier);


                    foreach (GameObject g in CurrentWorld._gameObjects)
                    {
                        if (g == null || !g.HasModel || g.CurrentWorld == null || g.Opacity <= 0)
                            continue;

                        if (g.Model.IsTerrain)
                        {
                            instancesTerrain.Add(g);
                            continue;
                        }

                        if (g.CurrentWorld.IsFirstPersonMode && g.CurrentWorld.GetFirstPersonObject().Equals(g))
                            continue;

                        bool opacityFound = false;
                        foreach (GeoMesh mesh in g.Model.Meshes.Values)
                        {
                            if (mesh.Material.Opacity < 1f)
                            {
                                opacityFound = true;
                                break;
                            }
                        }
                        if (opacityFound || g.Opacity < 1f)
                        {
                            instancesRenderLast.Add(g);
                            continue;
                        }
                        else
                        {
                            KWEngine.RendererStandard.Draw(g, ref viewProjection, textureIndex);
                        }
                    }
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                    GL.UseProgram(0);

                    if (instancesTerrain.Count > 0)
                    {
                        GL.UseProgram(KWEngine.RendererTerrain.GetProgramId());

                        // How many lights are there?
                        GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleLightCount(), CurrentWorld._lightcount);
                        GL.Uniform4(KWEngine.RendererTerrain.GetUniformHandleLightsColors(), KWEngine.MAX_LIGHTS, LightColors);
                        GL.Uniform4(KWEngine.RendererTerrain.GetUniformHandleLightsTargets(), KWEngine.MAX_LIGHTS, LightTargets);
                        GL.Uniform4(KWEngine.RendererTerrain.GetUniformHandleLightsPositions(), KWEngine.MAX_LIGHTS, LightPositions);
                        GL.Uniform3(KWEngine.RendererTerrain.GetUniformHandleLightsMeta(), KWEngine.MAX_LIGHTS, LightMeta);

                        // Ambient color and intensity:
                        GL.Uniform4(KWEngine.RendererTerrain.GetUniformHandleSunAmbient(), CurrentWorld._ambientLight);

                        textureIndex = 0;
                        for (int lightIndex = 0; lightIndex < KWEngine.MAX_SHADOWMAPS; textureIndex += 2, lightIndex++)
                        {
                            LightObject currentLight = null;
                            if (lightIndex < numLights)
                                currentLight = lights[lightIndex];

                            // POINT LIGHT:
                            GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                            GL.BindTexture(TextureTarget.TextureCubeMap, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTexturesCubeMap[currentLight._framebufferIndex] : KWEngine.TextureDepthCubeMapEmpty);
                            GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureShadowMapCubeMap() + lightIndex, textureIndex);

                            GL.ActiveTexture(TextureUnit.Texture0 + textureIndex + 1);
                            GL.BindTexture(TextureTarget.Texture2D, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTextures[currentLight._framebufferIndex] : KWEngine.TextureDepthEmpty);
                            GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureShadowMap() + lightIndex, textureIndex + 1);
                            if (currentLight != null)
                                GL.UniformMatrix4(KWEngine.RendererTerrain.GetUniformHandleVPShadowMap() + lightIndex, false, ref currentLight._viewProjectionMatrixShadow[0]);
                            else
                                GL.UniformMatrix4(KWEngine.RendererTerrain.GetUniformHandleVPShadowMap() + lightIndex, false, ref KWEngine.Identity);
                        }

                        // Upload skybox for metal reflections:
                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                        GL.BindTexture(TextureTarget.TextureCubeMap, CurrentWorld._textureSkybox > 0 ? CurrentWorld._textureSkybox : KWEngine.TextureCubemapEmpty);
                        GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureSkybox(), textureIndex);
                        textureIndex++;

                        // Upload 2d sky for metal reflections:
                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                        GL.BindTexture(TextureTarget.Texture2D, CurrentWorld._textureBackground > 0 ? CurrentWorld._textureBackground : KWEngine.TextureBlack);
                        GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureSky2D(), textureIndex);
                        textureIndex++;

                        GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureIsSkybox(), CurrentWorld._textureSkybox > 0 ? 1 : CurrentWorld._textureBackground > 0 ? -1 : 0);
                        GL.Uniform1(KWEngine.RendererTerrain.GetUniformHandleTextureSkyBoost(), CurrentWorld._textureBackgroundMultiplier);

                        foreach (GameObject g in instancesTerrain)
                        {
                            KWEngine.RendererTerrain.Draw(g, ref viewProjection, Frustum, textureIndex);
                        }
                        GL.BindTexture(TextureTarget.Texture2D, 0);
                        GL.UseProgram(0);
                        instancesTerrain.Clear();
                    }

                    // Background rendering:
                    if (CurrentWorld._textureBackground > 0)
                    {
                        KWEngine.RendererBackground.Draw(ref _modelViewProjectionMatrixBackground);
                    }
                    else if (CurrentWorld._textureSkybox > 0)
                    {
                        KWEngine.RendererSkybox.Draw(projectionMatrix, matrixIndex);
                    }


                    // =========================
                    // RENDER TRANSPARENT STUFF:
                    // =========================
                    if (instancesRenderLast.Count > 0)
                    {
                        instancesRenderLast.Sort((x, y) => x == null ? (y == null ? 0 : -1) : (y == null ? 1 : y.DistanceToCamera.CompareTo(x.DistanceToCamera)));
                        GL.UseProgram(KWEngine.RendererStandard.GetProgramId());

                        // How many lights are there?
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleLightCount(), CurrentWorld._lightcount);
                        GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsColors(), KWEngine.MAX_LIGHTS, LightColors);
                        GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsTargets(), KWEngine.MAX_LIGHTS, LightTargets);
                        GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleLightsPositions(), KWEngine.MAX_LIGHTS, LightPositions);
                        GL.Uniform3(KWEngine.RendererStandard.GetUniformHandleLightsMeta(), KWEngine.MAX_LIGHTS, LightMeta);

                        // Ambient color and intensity:
                        GL.Uniform4(KWEngine.RendererStandard.GetUniformHandleSunAmbient(), CurrentWorld._ambientLight);

                        textureIndex = 0;
                        for (int lightIndex = 0; lightIndex < KWEngine.MAX_SHADOWMAPS; textureIndex += 2, lightIndex++)
                        {
                            LightObject currentLight = null;
                            if (lightIndex < numLights)
                                currentLight = lights[lightIndex];

                            // POINT LIGHT:
                            GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                            GL.BindTexture(TextureTarget.TextureCubeMap, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTexturesCubeMap[currentLight._framebufferIndex] : KWEngine.TextureDepthCubeMapEmpty);
                            GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureShadowMapCubeMap() + lightIndex, textureIndex);

                            GL.ActiveTexture(TextureUnit.Texture0 + textureIndex + 1);
                            GL.BindTexture(TextureTarget.Texture2D, currentLight != null && currentLight.IsShadowCaster ? GLWindow.CurrentWindow.FramebuffersShadowTextures[currentLight._framebufferIndex] : KWEngine.TextureDepthEmpty);
                            GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureShadowMap() + lightIndex, textureIndex + 1);
                            if (currentLight != null)
                                GL.UniformMatrix4(KWEngine.RendererStandard.GetUniformHandleVPShadowMap() + lightIndex, false, ref currentLight._viewProjectionMatrixShadow[0]);
                            else
                                GL.UniformMatrix4(KWEngine.RendererStandard.GetUniformHandleVPShadowMap() + lightIndex, false, ref KWEngine.Identity);
                        }

                        // Upload skybox for metal reflections:
                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                        GL.BindTexture(TextureTarget.TextureCubeMap, CurrentWorld._textureSkybox > 0 ? CurrentWorld._textureSkybox : KWEngine.TextureCubemapEmpty);
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSkybox(), textureIndex);
                        textureIndex++;

                        // Upload 2d sky for metal reflections:
                        GL.ActiveTexture(TextureUnit.Texture0 + textureIndex);
                        GL.BindTexture(TextureTarget.Texture2D, CurrentWorld._textureBackground > 0 ? CurrentWorld._textureBackground : KWEngine.TextureBlack);
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSky2D(), textureIndex);
                        textureIndex++;

                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureIsSkybox(), CurrentWorld._textureSkybox > 0 ? 1 : CurrentWorld._textureBackground > 0 ? -1 : 0);
                        GL.Uniform1(KWEngine.RendererStandard.GetUniformHandleTextureSkyBoost(), CurrentWorld._textureBackgroundMultiplier);

                        foreach (GameObject g in instancesRenderLast)
                        {
                            KWEngine.RendererStandard.Draw(g, ref viewProjection, textureIndex);
                        }
                        GL.UseProgram(0);
                        instancesRenderLast.Clear();
                    }
                }
            }
        }

        #region Framebuffers

        internal int[] FramebuffersShadow = new int[KWEngine.MAX_SHADOWMAPS];
        internal int[] FramebuffersShadowCubeMap = new int[KWEngine.MAX_SHADOWMAPS];
        internal int[] FramebuffersShadowTextures = new int[KWEngine.MAX_SHADOWMAPS];
        internal int[] FramebuffersShadowTexturesCubeMap = new int[KWEngine.MAX_SHADOWMAPS];

        internal int FramebufferBloom1 = -1;
        internal int FramebufferBloom2 = -1;
        internal int FramebufferMainMultisample = -1;
        internal int FramebufferMainFinal = -1;

        internal int TextureMain = -1;
        internal int TextureMainDepth = -1;
        internal int TextureBloom = -1;
        internal int TextureBloom1 = -1;
        internal int TextureBloom2 = -1;
        internal int TextureMainFinal = -1;
        internal int TextureBloomFinal = -1;

        internal void InitializeFramebuffersLightsList()
        {
            _freeShadowMapIds = new List<int>();
            _freeShadowMapCubeMapIds = new List<int>();

            for (int i = 0; i < KWEngine.MAX_SHADOWMAPS; i++)
            {
                _freeShadowMapIds.Add(i);
                _freeShadowMapCubeMapIds.Add(i);
            }
        }

        internal void InitializeFramebuffersLights()
        {
            InitializeFramebuffersLightsList();

            bool ok = false;
            while (!ok)
            {
                try
                {
                    if (TextureMain >= 0)
                    {
                        GL.DeleteFramebuffers(FramebuffersShadow.Length, FramebuffersShadow);
                        GL.Flush();
                        GL.Finish();
                        GL.DeleteFramebuffers(FramebuffersShadowCubeMap.Length, FramebuffersShadowCubeMap);
                        GL.Flush();
                        GL.Finish();
                        GL.DeleteTextures(FramebuffersShadowTextures.Length, FramebuffersShadowTextures);
                        GL.Flush();
                        GL.Finish();
                        GL.DeleteTextures(FramebuffersShadowTexturesCubeMap.Length, FramebuffersShadowTexturesCubeMap);
                        GL.Flush();
                        GL.Finish();

                        Thread.Sleep(250);
                    }
                    InitFramebuffersShadowMap();
                    InitFramebuffersShadowMapCubeMap();
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Debug.WriteLine("Error building framebuffers: " + ex.Message);
                }
                ok = true;
            }
        }

        internal void InitializeFramebuffers()
        {
            bool ok = false;
            while (!ok)
            {
                try
                {
                    if (TextureMain >= 0)
                    {
                        GL.DeleteFramebuffers(4, new int[] { FramebufferBloom1, FramebufferBloom2, FramebufferMainMultisample, FramebufferMainFinal });
                        GL.Flush();
                        GL.Finish();
                        GL.DeleteTextures(7, new int[] { TextureMainDepth, TextureMain,  TextureBloom1, TextureBloom2, TextureMainFinal, TextureBloomFinal, TextureBloom });
                        GL.Flush();
                        GL.Finish();
                        Thread.Sleep(100);
                        
                    }

                    InitFramebufferOriginal();
                    InitFramebufferOriginalDownsampled();
                    InitFramebufferBloom();
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Debug.WriteLine("Error building framebuffers: " + ex.Message);
                }
                ok = true;
            }

            InitializeFramebuffersLights();
        }

        private void DownsampleFramebuffer()
        {
            if (KWEngine.PostProcessQuality != PostProcessingQuality.Disabled)
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FramebufferMainMultisample);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, FramebufferMainFinal);

                GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
                GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
                GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);

                GL.ReadBuffer(ReadBufferMode.ColorAttachment1);
                GL.DrawBuffer(DrawBufferMode.ColorAttachment1);
                GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            }
            else
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FramebufferMainMultisample);
                GL.ReadBuffer(ReadBufferMode.ColorAttachment0);

                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
                GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            }
        }

        internal int[] GetFramebuffersShadowMap()
        {
            return FramebuffersShadow;
        }

        internal int[] GetFramebuffersShadowMapCubeMap()
        {
            return FramebuffersShadowCubeMap;
        }

        internal List<int> _freeShadowMapIds;
        internal List<int> _freeShadowMapCubeMapIds;

        internal void AddIdForShadowMap(int id, LightType type)
        {
            if(type == LightType.Point)
            {
                if (_freeShadowMapCubeMapIds.Count < KWEngine.MAX_SHADOWMAPS)
                    _freeShadowMapCubeMapIds.Add(id);
                else
                {
                    HelperGL.ShowErrorAndQuit("GLWindow::AddIdForShadowMap()", "Internal error adding shadow map framebuffers to the framebuffer list.");
                    return;
                }
            }
            else
            {
                if (_freeShadowMapIds.Count < KWEngine.MAX_SHADOWMAPS)
                    _freeShadowMapIds.Add(id);
                else
                {
                    HelperGL.ShowErrorAndQuit("GLWindow::AddIdForShadowMap()", "Internal error adding shadow map framebuffers to the framebuffer list.");
                    return;
                }
            }
        }

        internal int GetFreeIdForShadowMap(LightType type)
        {
            if(type == LightType.Point)
            {
                if(_freeShadowMapCubeMapIds.Count > 0)
                {
                    int newId = _freeShadowMapCubeMapIds[0];
                    _freeShadowMapCubeMapIds.RemoveAt(0);
                    return newId;
                }
                else
                {
                    HelperGL.ShowErrorAndQuit("GLWindow::GetFreeIdForShadowMap()", "Shadow light count of " + KWEngine.MAX_SHADOWMAPS + " exceeded. Cannot create shadow light.");
                    return -1;
                }
                
            }
            else
            {
                if (_freeShadowMapIds.Count > 0)
                {
                    int newId = _freeShadowMapIds[0];
                    _freeShadowMapIds.RemoveAt(0);
                    return newId;
                }
                else
                {
                    HelperGL.ShowErrorAndQuit("GLWindow::GetFreeIdForShadowMap()", "Shadow light count of " + KWEngine.MAX_SHADOWMAPS + " exceeded. Cannot create shadow light.");
                    return -1;
                }
            }
        }



        private void InitFramebuffersShadowMap()
        {
            for(int i = 0; i < FramebuffersShadow.Length; i++)
            {
                FramebuffersShadow[i] = GL.GenFramebuffer();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebuffersShadow[i]);

                FramebuffersShadowTextures[i] = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, FramebuffersShadowTextures[i]);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32,
                    KWEngine.ShadowMapSize, KWEngine.ShadowMapSize, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, new int[] { (int)TextureCompareMode.CompareRefToTexture });
                GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, new int[] { (int)DepthFunction.Lequal });
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1, 1, 1, 1 });
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToBorder);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToBorder);

                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, FramebuffersShadowTextures[i], 0);
                GL.DrawBuffer(DrawBufferMode.None);
                GL.ReadBuffer(ReadBufferMode.None);

                GL.BindTexture(TextureTarget.Texture2D, 0);

                FramebufferErrorCode code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if (code != FramebufferErrorCode.FramebufferComplete)
                {
                    HelperGL.ShowErrorAndQuit("GLWindow::InitFramebuffersShadowMap()", "GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
                    return;
                }
            }
        }

        private void InitFramebuffersShadowMapCubeMap()
        {
            for (int i = 0; i < FramebuffersShadow.Length; i++)
            {
                FramebuffersShadowCubeMap[i] = GL.GenFramebuffer();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebuffersShadowCubeMap[i]);

                FramebuffersShadowTexturesCubeMap[i] = GL.GenTexture();
                GL.BindTexture(TextureTarget.TextureCubeMap, FramebuffersShadowTexturesCubeMap[i]);

                for (int j = 0; j < 6; j++)
                {
                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + j, 0, PixelInternalFormat.DepthComponent32,
                        HelperTexture.RoundDownToPowerOf2(KWEngine.ShadowMapSize / 4), HelperTexture.RoundDownToPowerOf2(KWEngine.ShadowMapSize / 4), 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
                }

                GL.TexParameterI(TextureTarget.TextureCubeMap, TextureParameterName.TextureCompareMode, new int[] { (int)TextureCompareMode.CompareRefToTexture });
                GL.TexParameterI(TextureTarget.TextureCubeMap, TextureParameterName.TextureCompareFunc, new int[] { (int)DepthFunction.Lequal });
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureBorderColor, new float[] { 1, 1, 1, 1 });
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToBorder);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToBorder);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (float)TextureParameterName.ClampToBorder);

                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, FramebuffersShadowTexturesCubeMap[i], 0);
                GL.DrawBuffer(DrawBufferMode.None);
                GL.ReadBuffer(ReadBufferMode.None);

                GL.BindTexture(TextureTarget.Texture2D, 0);

                FramebufferErrorCode code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                if (code != FramebufferErrorCode.FramebufferComplete)
                {
                    HelperGL.ShowErrorAndQuit("GLWindow::InitFramebuffersShadowMapCubeMap()", "GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
                    return;
                }
            }
        }

        private void InitFramebufferOriginalDownsampled()
        {
            int framebufferId;
            int renderedTexture;
            int renderedTextureAttachment;

            //Init des frame buffer:
            framebufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);

            // Init der Textur auf die gerendet wird:
            renderedTexture = GL.GenTexture();
            renderedTextureAttachment = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, renderedTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                Width, Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);


            GL.BindTexture(TextureTarget.Texture2D, renderedTextureAttachment);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                Width, Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);

            //Konfig. des frame buffer:
            GL.DrawBuffers(2, new DrawBuffersEnum[2] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 });
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, renderedTexture, 0);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, renderedTextureAttachment, 0);

            FramebufferErrorCode code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (code != FramebufferErrorCode.FramebufferComplete)
            {
                HelperGL.ShowErrorAndQuit("GLWindow::InitFramebufferOriginalDownsampled()", "GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
                return;
            }
            else
            {
                FramebufferMainFinal = framebufferId;
                TextureMainFinal = renderedTexture;
                TextureBloomFinal = renderedTextureAttachment;
            }
        }

        private void InitFramebufferOriginal()
        {
            int framebufferId;
            int renderedTexture;
            int renderedTextureAttachment;
            int renderbufferFSAA;
            int renderbufferFSAA2;
            int depthTexId;

            // FULL RESOLUTION

            //Init des frame buffer:
            framebufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);

            // Init der Textur auf die gerendet wird:
            renderedTexture = GL.GenTexture();
            renderedTextureAttachment = GL.GenTexture();

            depthTexId = GL.GenTexture();


            //Konfig. des frame buffer:
            GL.DrawBuffers(2, new DrawBuffersEnum[2] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 });

            GL.BindTexture(TextureTarget.Texture2D, renderedTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                Width, Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);

            //render buffer fsaa:
            renderbufferFSAA = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbufferFSAA);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, _fsaa, RenderbufferStorage.Rgba8, Width, Height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, renderbufferFSAA);

            GL.BindTexture(TextureTarget.Texture2D, renderedTextureAttachment);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                Width, Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);

            renderbufferFSAA2 = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbufferFSAA2);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, _fsaa, RenderbufferStorage.Rgba8, Width, Height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, RenderbufferTarget.Renderbuffer, renderbufferFSAA2);

            // depth buffer fsaa:
            int depthRenderBuffer = GL.GenRenderbuffer();
            GL.BindTexture(TextureTarget.Texture2D, depthTexId);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32,
                Width, Height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, new int[] { (int)TextureCompareMode.CompareRefToTexture });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, new int[] { (int)DepthFunction.Lequal });
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);

            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthTexId, 0);

            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthRenderBuffer);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, _fsaa, RenderbufferStorage.DepthComponent24, Width, Height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthRenderBuffer);

            FramebufferErrorCode code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (code != FramebufferErrorCode.FramebufferComplete)
            {
                HelperGL.ShowErrorAndQuit("GLWindow::InitFramebufferOriginal()", "GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
                return;
            }
            else
            {
                FramebufferMainMultisample = framebufferId;
                TextureMain = renderedTexture;
                TextureBloom = renderedTextureAttachment;
                TextureMainDepth = depthTexId;
            }
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        private void InitFramebufferBloom()
        {
            int framebufferTempId;
            int renderedTextureTemp;

            // =========== TEMP ===========

            //Init des frame buffer:
            framebufferTempId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferTempId);

            // Init der Textur auf die gerendet wird:
            renderedTextureTemp = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, renderedTextureTemp);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                _bloomwidth, _bloomheight, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);

            //Konfig. des frame buffer:
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, renderedTextureTemp, 0);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            FramebufferErrorCode code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (code != FramebufferErrorCode.FramebufferComplete)
            {
                HelperGL.ShowErrorAndQuit("GLWindow::InitFramebufferBloom()", "GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
                return;
            }
            else
            {
                FramebufferBloom1 = framebufferTempId;
                TextureBloom1 = renderedTextureTemp;
            }

            // =========== TEMP 2 ===========

            //Init des frame buffer:
            int framebufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);

            // Init der Textur auf die gerendet wird:
            int renderedTextureTemp2 = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, renderedTextureTemp2);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                _bloomwidth, _bloomheight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToEdge);
            //Konfig. des frame buffer:
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, renderedTextureTemp2, 0);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (code != FramebufferErrorCode.FramebufferComplete)
            {
                HelperGL.ShowErrorAndQuit("GLWindow::InitFramebufferBloom()", "GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
                return;
            }
            else
            {
                FramebufferBloom2 = framebufferId;
                TextureBloom2 = renderedTextureTemp2;
            }
        }
        #endregion
    }
}
