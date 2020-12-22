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
        internal Matrix4 _projectionMatrixShadow = Matrix4.Identity;
        internal Matrix4 _viewProjectionMatrixHUD = Matrix4.Identity;

        internal static float[] LightColors = new float[KWEngine.MAX_LIGHTS * 4];
        internal static float[] LightTargets = new float[KWEngine.MAX_LIGHTS * 4];
        internal static float[] LightPositions = new float[KWEngine.MAX_LIGHTS * 4];

        internal HelperFrustum Frustum = new HelperFrustum();
        internal HelperFrustum FrustumShadowMap = new HelperFrustum();

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
           : this(width, height, GameWindowFlags.Default, 0, true, false)
        {

        }

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
        protected GLWindow(int width, int height, GameWindowFlags flag, int antialiasing = 0, bool vSync = true, bool multithreading = false, int textureAnisotropy = 1)
            : base(width, height, GraphicsMode.Default, "KWEngine2 - C# 3D Gaming", flag == GameWindowFlags.Default ? GameWindowFlags.FixedWindow : flag, DisplayDevice.Default, 4, 5, GraphicsContextFlags.ForwardCompatible, null, !multithreading)
        {
            _multithreaded = multithreading;
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Width = width;
            Height = height;
            if (textureAnisotropy >= 1 && textureAnisotropy <= 16)
                _anisotropy = textureAnisotropy;
            else
                _anisotropy = 1;

            GLAudioEngine.InitAudioEngine();

            if (antialiasing >= 0 && antialiasing <= 8)
            {
                if (antialiasing == 1 || antialiasing == 3 || antialiasing == 5 || antialiasing == 6 || antialiasing == 7)
                    antialiasing = 0;
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
            CurrentWindow = this;
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
            KWEngine.TextureAlpha = HelperTexture.LoadTextureInternal("alpha.png");

            KWEngine.InitializeShaders();
            KWEngine.InitializeModels();
            KWEngine.InitializeParticles();
            KWEngine.InitializeFont("Anonymous.ttf");
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
            GLAudioEngine.SoundStopAll();
            GLAudioEngine.Dispose();

            base.Dispose(manual);


        }

        private List<GameObject> mInstancesRenderLast = new List<GameObject>();

        /// <summary>
        /// EventHandler für den Render-Thread
        /// </summary>
        /// <param name="e">Parameter</param>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
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

                int shadowLight = -1;
                lock (CurrentWorld._lightObjects)
                {
                    LightObject.PrepareLightsForRenderPass(CurrentWorld._lightObjects, ref LightColors, ref LightTargets, ref LightPositions, ref CurrentWorld._lightcount, ref shadowLight);
                }
                LightObject sLight = shadowLight >= 0 ? CurrentWorld._lightObjects.ElementAt(shadowLight) : null;
                if (CurrentWorld.DebugShadowCaster)
                {
                    if (sLight != null)
                    {
                        _viewMatrix = sLight._shadowViewMatrix; // Matrix4.LookAt(sLight.Position, sLight.Target, KWEngine.WorldUp);
                    }
                    else
                    {
                        _viewMatrix = Matrix4.LookAt(CurrentWorld.GetSunPosition(), CurrentWorld.GetSunTarget(), KWEngine.WorldUp);
                    }
                }
                else
                {
                    if (CurrentWorld.IsFirstPersonMode)
                        _viewMatrix = HelperCamera.GetViewMatrix(CurrentWorld.GetFirstPersonObject().Position);
                    else
                        _viewMatrix = Matrix4.LookAt(CurrentWorld.GetCameraPosition(), CurrentWorld.GetCameraTarget(), KWEngine.WorldUp);
                }
                Matrix4 viewProjection;
                if (CurrentWorld.DebugShadowCaster)
                {
                    if (sLight != null)
                    {
                        viewProjection = _viewMatrix * sLight._projectionMatrixShadow2;
                    }
                    else
                        viewProjection = _viewMatrix * _projectionMatrixShadow;
                }
                else
                {
                    viewProjection = _viewMatrix * _projectionMatrix;
                }

                Matrix4 viewProjectionShadow = CurrentWorld._viewMatrixShadow * _projectionMatrixShadow;
                Frustum.CalculateFrustum(_projectionMatrix, _viewMatrix);
                FrustumShadowMap.CalculateFrustum(_projectionMatrixShadow, CurrentWorld._viewMatrixShadow);

                SwitchToBufferAndClear(FramebufferShadowMap);
                GL.Viewport(0, 0, KWEngine.ShadowMapSize, KWEngine.ShadowMapSize);
                GL.UseProgram(KWEngine.Renderers["Shadow"].GetProgramId());
                lock (CurrentWorld._gameObjects)
                {

                    foreach (GameObject g in CurrentWorld._gameObjects)
                    {
                        g.ProcessCurrentAnimation();
                        KWEngine.Renderers["Shadow"].Draw(g, ref viewProjectionShadow, FrustumShadowMap, true);
                    }
                }
                GL.UseProgram(0);

                if (sLight != null)
                {
                    SwitchToBufferAndClear(FramebufferShadowMap2);
                    GL.Viewport(0, 0, KWEngine.ShadowMapSize, KWEngine.ShadowMapSize);
                    GL.UseProgram(KWEngine.Renderers["Shadow"].GetProgramId());
                    sLight._frustumShadowMap2.CalculateFrustum(sLight._projectionMatrixShadow2, sLight._shadowViewMatrix);
                    lock (CurrentWorld._gameObjects)
                    {
                        foreach (GameObject g in CurrentWorld._gameObjects)
                        {
                            KWEngine.Renderers["Shadow"].Draw(g, ref sLight._viewProjectionMatrixShadow2, sLight._frustumShadowMap2, false);
                        }
                    }
                    GL.UseProgram(0);

                }


                SwitchToBufferAndClear(FramebufferMainMultisample);
                GL.Viewport(ClientRectangle);

                // Background rendering:
                if (CurrentWorld._textureBackground > 0)
                {

                    KWEngine.Renderers["Background"].Draw(_dummy, ref _modelViewProjectionMatrixBackground);
                }
                else if (CurrentWorld._textureSkybox > 0)
                {
                    KWEngine.Renderers["Skybox"].Draw(_dummy, ref _projectionMatrix);
                }

                Matrix4 viewProjectionShadowBiased = viewProjectionShadow * HelperMatrix.BiasedMatrixForShadowMapping;
                Matrix4 viewProjectionShadowBiased2 = sLight == null ? Matrix4.Identity : sLight._viewProjectionMatrixShadow2 * HelperMatrix.BiasedMatrixForShadowMapping;

                lock (CurrentWorld._gameObjects)
                {
                    foreach (GameObject g in CurrentWorld._gameObjects)
                    {
                        g._collisionCandidates.Clear(); // clear collision list for this objects
                        if (g.CurrentWorld.IsFirstPersonMode && g.CurrentWorld.GetFirstPersonObject().Equals(g))
                            continue;
                        if (g.Model.IsTerrain)
                        {
                            KWEngine.Renderers["Terrain"].Draw(g, ref viewProjection, ref viewProjectionShadowBiased, ref viewProjectionShadowBiased2, Frustum, ref LightColors, ref LightTargets, ref LightPositions, CurrentWorld._lightcount, ref shadowLight);
                        }
                        else
                        {
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
                                KWEngine.Renderers["Standard"].Draw(g, ref viewProjection, ref viewProjectionShadowBiased, ref viewProjectionShadowBiased2, Frustum, ref LightColors, ref LightTargets, ref LightPositions, CurrentWorld._lightcount, ref shadowLight);
                                if (CurrentWorld.DebugShowHitboxes)
                                    KWEngine.RendererSimple.DrawHitbox(g, ref viewProjection);
                            }
                        }
                    }

                    mInstancesRenderLast.Sort((x, y) => x == null ? (y == null ? 0 : -1) : (y == null ? 1 : y.DistanceToCamera.CompareTo(x.DistanceToCamera)));
                    foreach (GameObject g in mInstancesRenderLast)
                    {
                        KWEngine.Renderers["Standard"].Draw(g, ref viewProjection, ref viewProjectionShadowBiased, ref viewProjectionShadowBiased2, Frustum, ref LightColors, ref LightTargets, ref LightPositions, CurrentWorld._lightcount, ref shadowLight);
                        if (CurrentWorld.DebugShowHitboxes)
                            KWEngine.RendererSimple.DrawHitbox(g, ref viewProjection);
                    }
                    mInstancesRenderLast.Clear();
                }
                GL.UseProgram(0);

                lock (CurrentWorld._explosionObjects)
                {
                    if (CurrentWorld._explosionObjects.Count > 0)
                    {
                        RendererExplosion r = (RendererExplosion)KWEngine.Renderers["Explosion"];
                        GL.UseProgram(r.GetProgramId());
                        foreach (Explosion ex in CurrentWorld._explosionObjects)
                        {
                            r.Draw(ex, ref viewProjection);
                        }
                        GL.UseProgram(0);
                    }
                }

                lock (CurrentWorld._particleObjects)
                {
                    GL.Enable(EnableCap.Blend);
                    GL.UseProgram(KWEngine.Renderers["Particle"].GetProgramId());
                    foreach (ParticleObject p in CurrentWorld.GetParticleObjects())
                        KWEngine.Renderers["Particle"].Draw(p, ref viewProjection);
                    GL.UseProgram(0);
                    GL.Disable(EnableCap.Blend);
                }
                GL.Enable(EnableCap.Blend);
                GL.Disable(EnableCap.DepthTest);
                GL.Disable(EnableCap.CullFace);
                lock (CurrentWorld._hudObjects)
                {
                    foreach (HUDObject p in CurrentWorld._hudObjects)
                        KWEngine.Renderers["HUD"].Draw(p, ref _viewProjectionMatrixHUD);
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
            HelperGL.CheckGLErrors();

            SwapBuffers();

            frameCounter++;
            frameData += (e.Time * 1000.0);
            if (frameCounter > 100)
            {
                int index = Title != null ? Title.LastIndexOf('|') : -1;
                if (KWEngine.DebugShowPerformanceInTitle == KWEngine.PerformanceUnit.FrameTimeInMilliseconds)
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
                else if (KWEngine.DebugShowPerformanceInTitle == KWEngine.PerformanceUnit.FramesPerSecond)
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

        private static void SwitchToBufferAndClear(int id)
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
                        g.Act(ks, ms, DeltaTime.GetDeltaTimeFactor());
                        g.CheckBounds();
                    }
                    foreach (GameObject g in mInstancesLast)
                    {
                        g.Act(ks, ms, DeltaTime.GetDeltaTimeFactor());
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
                lock (CurrentWorld._lightObjects)
                {
                    foreach (LightObject l in CurrentWorld.GetLightObjects())
                    {
                        l.Act(ks, ms, DeltaTime.GetDeltaTimeFactor());
                    }
                }

                CurrentWorld.Act(ks, ms, DeltaTime.GetDeltaTimeFactor());

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
            _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(CurrentWorld != null ? CurrentWorld.FOV / 2 : 45f), ClientSize.Width / (float)ClientSize.Height, 0.1f, CurrentWorld != null ? CurrentWorld.ZFar : 1000f);
            _projectionMatrixShadow = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(CurrentWorld != null ? CurrentWorld.FOVShadow / 2 : 45f), KWEngine.ShadowMapSize / (float)KWEngine.ShadowMapSize, 1f, CurrentWorld != null ? CurrentWorld.ZFar : 1000f);


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
            if (KWEngine.PostProcessQuality != KWEngine.PostProcessingQuality.Disabled)
            {

                RendererBloom r = (RendererBloom)KWEngine.Renderers["Bloom"];
                RendererMerge m = (RendererMerge)KWEngine.Renderers["Merge"];
                GL.UseProgram(r.GetProgramId());
                GL.Viewport(0, 0, Width, Height);
                int loopCount =
                    KWEngine.PostProcessQuality == KWEngine.PostProcessingQuality.High ? 6 :
                    KWEngine.PostProcessQuality == KWEngine.PostProcessingQuality.Standard ? 4 : 2;
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

                    r.DrawBloom(
                        _bloomQuad,
                        ref _modelViewProjectionMatrixBloom,
                        i % 2 == 0,
                        sourceTex
                    );
                }

                SwitchToBufferAndClear(0);
                GL.UseProgram(m.GetProgramId());
                GL.Viewport(0, 0, Width, Height);
                m.DrawMerge(_bloomQuad, ref _modelViewProjectionMatrixBloomMerge, TextureMainFinal, TextureBloom2);
                GL.UseProgram(0); // unload bloom shader program
            }
        }

        #region Framebuffers

        internal int FramebufferShadowMap = -1;
        internal int FramebufferShadowMap2 = -1;
        internal int FramebufferBloom1 = -1;
        internal int FramebufferBloom2 = -1;
        internal int FramebufferMainMultisample = -1;
        internal int FramebufferMainFinal = -1;

        internal int TextureShadowMap = -1;
        internal int TextureShadowMap2 = -1;
        internal int TextureMain = -1;
        internal int TextureMainDepth = -1;
        internal int TextureBloom = -1;
        internal int TextureBloom1 = -1;
        internal int TextureBloom2 = -1;
        internal int TextureMainFinal = -1;
        internal int TextureBloomFinal = -1;


        internal void InitializeFramebuffers()
        {
            bool ok = false;
            while (!ok)
            {
                try
                {
                    if (TextureMain >= 0)
                    {
                        GL.DeleteTextures(9, new int[] { TextureMainDepth, TextureMain, TextureShadowMap, TextureShadowMap2, TextureBloom1, TextureBloom2, TextureMainFinal, TextureBloomFinal, TextureBloom });
                        GL.DeleteFramebuffers(6, new int[] { FramebufferShadowMap, FramebufferShadowMap2, FramebufferBloom1, FramebufferBloom2, FramebufferMainMultisample, FramebufferMainFinal, });

                        Thread.Sleep(250);
                    }

                    InitFramebufferOriginal();
                    InitFramebufferOriginalDownsampled();
                    InitFramebufferShadowMap();
                    InitFramebufferShadowMap2();
                    InitFramebufferBloom();
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Debug.WriteLine("Error building framebuffers: " + ex.Message);
                }
                ok = true;
            }

        }

        private void DownsampleFramebuffer()
        {
            if (KWEngine.PostProcessQuality != KWEngine.PostProcessingQuality.Disabled)
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

        private void InitFramebufferShadowMap()
        {
            int framebufferId;
            int depthTexId;

            framebufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);

            depthTexId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, depthTexId);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32,
                KWEngine.ShadowMapSize, KWEngine.ShadowMapSize, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, new int[] { (int)TextureCompareMode.CompareRefToTexture });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, new int[] { (int)DepthFunction.Lequal });
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1, 1, 1, 1 });
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (float)TextureParameterName.ClampToBorder);

            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthTexId, 0);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);

            FramebufferErrorCode code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (code != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception("GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
            }
            else
            {
                FramebufferShadowMap = framebufferId;
                TextureShadowMap = depthTexId;
            }
        }

        private void InitFramebufferShadowMap2()
        {
            int framebufferId;
            int depthTexId;

            framebufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferId);

            depthTexId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, depthTexId);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32,
                KWEngine.ShadowMapSize, KWEngine.ShadowMapSize, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, new int[] { (int)TextureCompareMode.CompareRefToTexture });
            GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, new int[] { (int)DepthFunction.Lequal });
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1, 1, 1, 1 });
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureParameterName.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureParameterName.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (float)TextureParameterName.ClampToBorder);

            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthTexId, 0);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);

            FramebufferErrorCode code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (code != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception("GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
            }
            else
            {
                FramebufferShadowMap2 = framebufferId;
                TextureShadowMap2 = depthTexId;
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
                throw new Exception("GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
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
                throw new Exception("GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
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
                Width, Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
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
                throw new Exception("GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
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
                Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
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
                throw new Exception("GL_FRAMEBUFFER_COMPLETE failed. Cannot use FrameBuffer object.");
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
