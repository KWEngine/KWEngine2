﻿using KWEngine2.GameObjects;
using KWEngine2.Helper;
using KWEngine2.Model;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using System.Linq;
using KWEngine2.Audio;
using KWEngine2.Collision;
using static KWEngine2.KWEngine;

namespace KWEngine2
{
    /// <summary>
    /// Welt-Klasse
    /// </summary>
    public abstract class World
    {
        /// <summary>
        /// Zeige Hitbox von allen als Kollisionsobjekt markierten GameObject-Instanzen
        /// </summary>
        public bool DebugShowHitboxes { get; set; } = false;
        /// <summary>
        /// Zeige die Welt aus der Sicht der Sonne
        /// </summary>
        public bool DebugShadowCaster { get; set; } = false;
        /// <summary>
        /// Zeige Koordinatensystem
        /// </summary>
        public bool DebugShowCoordinateSystem { get; set; } = false;

        /// <summary>
        /// Zeigt die Performance im Titelbereich des Fensters an
        /// </summary>
        public PerformanceUnit DebugShowPerformanceInTitle
        {
            get
            {
                return KWEngine.DebugShowPerformanceInTitle;
            }
            set
            {
                KWEngine.DebugShowPerformanceInTitle = value;
            }
        }

        internal bool _prepared = false;
        private float _worldDistance = 100;
        internal Matrix4 _skyboxRotation = Matrix4.Identity;
        internal Matrix4 _viewMatrixShadow = Matrix4.Identity;

        /// <summary>
        /// Zentrum der Welt
        /// </summary>
        public Vector3 WorldCenter { get; set; } = new Vector3(0, 0, 0);
        /// <summary>
        /// Radius der Welt
        /// </summary>
        public float WorldDistance
        {
            get
            {
                return _worldDistance;
            }
            set
            {
                if (value > 0)
                {
                    _worldDistance = value;
                }
                else
                {
                    _worldDistance = 200f;
                    Debug.WriteLine("WorldDistance needs to be > 0");
                }
            }
        }
        private GameObject _firstPersonObject = null;

        /// <summary>
        /// Gibt an, ob der First-Person-Modus aktiv ist
        /// </summary>
        public bool IsFirstPersonMode
        {
            get
            {
                return _firstPersonObject != null; // && _gameObjects.Contains(_firstPersonObject);
            }
        }



        /// <summary>
        /// Erfragt das aktuelle FP-Objekt
        /// </summary>
        /// <returns>FP-Objekt</returns>
        public GameObject GetFirstPersonObject()
        {
            return _firstPersonObject;
        }

        /// <summary>
        /// Aktuelle Zeit in Millisekunden
        /// </summary>
        /// <returns>Zeit (in ms)</returns>
        public long GetCurrentTimeInMilliseconds()
        {
            return DeltaTime.Watch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Startet den FP-Modus mit dem übergebenen Objekt
        /// </summary>
        /// <param name="go">FP-Objekt</param>
        /// <param name="startRotationInDegrees">Startrotation (z.B. 180 Grad)</param>
        public void SetFirstPersonObject(GameObject go, float startRotationInDegrees = 0)
        {
            if (go != null)
            {
                _firstPersonObject = go;
                CurrentWindow.CursorVisible = false;
                Mouse.SetPosition(CurrentWindow.X + CurrentWindow.Width / 2, CurrentWindow.Y + CurrentWindow.Height / 2);
                go.SetRotation(0, startRotationInDegrees, 0);
                HelperCamera.SetStartRotation(go);
                //HelperCamera.SetStartRotationY(Quaternion.FromAxisAngle(KWEngine.WorldUp, MathHelper.DegreesToRadians(startRotationInDegrees)), go);
            }
            else
            {
                CurrentWindow.CursorVisible = true;
                _firstPersonObject = null;
                HelperCamera.DeleteFirstPersonObject();
            }
        }

        internal List<HUDObject> _hudObjects = new List<HUDObject>();
        internal List<HUDObject> _hudObjectsTBA = new List<HUDObject>();
        internal List<HUDObject> _hudObjectsTBR = new List<HUDObject>();

        internal List<GameObject> _gameObjects = new List<GameObject>();
        internal List<LightObject> _lightObjects = new List<LightObject>();
        internal List<ParticleObject> _particleObjects = new List<ParticleObject>();

        internal List<GameObject> _gameObjectsTBA = new List<GameObject>();
        internal List<LightObject> _lightObjectsTBA = new List<LightObject>();
        internal List<ParticleObject> _particleObjectsTBA = new List<ParticleObject>();

        internal List<GameObject> _gameObjectsTBR = new List<GameObject>();
        internal List<LightObject> _lightObjectsTBR = new List<LightObject>();
        internal List<ParticleObject> _particleObjectsTBR = new List<ParticleObject>();

        internal List<Explosion> _explosionObjects = new List<Explosion>();
        internal List<Explosion> _explosionObjectsTBA = new List<Explosion>();
        internal List<Explosion> _explosionObjectsTBR = new List<Explosion>();

        //internal List<SceneryObject> _sceneryObjects = new List<SceneryObject>();
        //internal List<SceneryObject> _sceneryObjectsTBA = new List<SceneryObject>();
        //internal List<SceneryObject> _sceneryObjectsTBR = new List<SceneryObject>();


        internal int _lightcount = 0;
        /// <summary>
        /// Anzahl der Lichter in der Welt
        /// </summary>
        public int LightCount
        {
            get
            {
                return _lightcount;
            }
        }

        internal int _textureBackground = -1;
        internal Vector4 _textureBackgroundTint = new Vector4(1, 1, 1, 1);
        internal Vector2 _textureBackgroundTransform = new Vector2(1, 1);
        internal int _textureSkybox = -1;

        internal void SetTextureBackgroundInternal(string filename, float repeatX = 1, float repeatY = 1, float red = 1, float green = 1, float blue = 1, float intensity = 1, bool isFile = true)
        {
            if (filename == null || filename.Length < 1)
            {
                _textureBackground = -1;
                _textureBackgroundTint = Vector4.Zero;
            }
            else
            {
                if (KWEngine.CustomTextures[this].ContainsKey(filename))
                {
                    _textureBackground = KWEngine.CustomTextures[this][filename];
                }
                else
                {

                    _textureBackground = isFile ? HelperTexture.LoadTextureForBackgroundExternal(filename) : HelperTexture.LoadTextureForBackgroundInternal(filename);
                    KWEngine.CustomTextures[this].Add(filename, _textureBackground);
                }
                _textureBackgroundTint.X = HelperGL.Clamp(red, 0, 1);
                _textureBackgroundTint.Y = HelperGL.Clamp(green, 0, 1);
                _textureBackgroundTint.Z = HelperGL.Clamp(blue, 0, 1);
                _textureBackgroundTint.W = HelperGL.Clamp(intensity, 0, 1);
                _textureBackgroundTransform.X = HelperGL.Clamp(repeatX, 0.001f, 8192);
                _textureBackgroundTransform.Y = HelperGL.Clamp(repeatY, 0.001f, 8192);
                _textureSkybox = -1;
            }
        }

        /// <summary>
        /// Setzt das Hintergrundbild (2D)
        /// </summary>
        /// <param name="filename">Textur</param>
        /// <param name="repeatX">Wiederholung Breite</param>
        /// <param name="repeatY">Wiederholung Höhe</param>
        /// <param name="red">Rotfärbung</param>
        /// <param name="green">Grünfärbung</param>
        /// <param name="blue">Blaufärbung</param>
        /// <param name="intensity">Helligkeit</param>
        /// <param name="isFile">false, falls der Pfad Teil der EXE-Datei ist</param>
        public void SetTextureBackground(string filename, float repeatX = 1, float repeatY = 1, float red = 1, float green = 1, float blue = 1, float intensity = 1, bool isFile = true)
        {
            if (GLWindow.CurrentWindow._multithreaded)
            {
                Action a = () => SetTextureBackgroundInternal(filename, repeatX, repeatY, red, green, blue, intensity, isFile);
                HelperGLLoader.AddCall(this, a);
            }
            else
                SetTextureBackgroundInternal(filename, repeatX, repeatY, red, green, blue, intensity, isFile);

        }

        /// <summary>
        /// Setzt das 3D-Hintergrundbild
        /// </summary>
        /// <param name="filename">Skybox-Textur</param>
        /// <param name="red">Rotfärbung</param>
        /// <param name="green">Grünfärbung</param>
        /// <param name="blue">Blaufärbung</param>
        /// <param name="intensity">Helligkeit</param>
        /// <param name="isFile">false, falls der Pfad Teil der EXE-Datei ist</param>
        public void SetTextureSkybox(string filename, float red = 1, float green = 1, float blue = 1, float intensity = 1, bool isFile = true)
        {
            if (GLWindow.CurrentWindow._multithreaded)
            {
                Action a = () => SetTextureSkyboxInternal(filename, red, green, blue, intensity, isFile);
                HelperGLLoader.AddCall(this, a);
            }
            else
            {
                SetTextureSkyboxInternal(filename, red, green, blue, intensity, isFile);
            }
        }

        /// <summary>
        /// Setzt die Rotation der Skybox (falls vorhanden)
        /// </summary>
        /// <param name="degrees">Grad der Rotation</param>
        public void SetTextureSkyboxRotation(float degrees)
        {
            _skyboxRotation = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(degrees));
        }

        internal void SetTextureSkyboxInternal(string filename, float red = 1, float green = 1, float blue = 1, float intensity = 1, bool isFile = true)
        {
            if (filename == null || filename.Length < 1)
                _textureSkybox = -1;
            else
            {
                if (KWEngine.CustomTextures[this].ContainsKey(filename))
                {
                    _textureSkybox = KWEngine.CustomTextures[this][filename];
                }
                else
                {
                    _textureSkybox = HelperTexture.LoadTextureSkybox(filename, !isFile);
                }
                _textureBackgroundTint.X = HelperGL.Clamp(red, 0, 1);
                _textureBackgroundTint.Y = HelperGL.Clamp(green, 0, 1);
                _textureBackgroundTint.Z = HelperGL.Clamp(blue, 0, 1);
                _textureBackgroundTint.W = HelperGL.Clamp(intensity, 0, 1);
                _textureBackground = -1;
            }
        }


        private Vector3 _cameraPosition = new Vector3(0, 0, 25);
        private Vector3 _cameraTarget = new Vector3(0, 0, 0);
        private Vector3 _cameraLookAt = new Vector3(0, 0, 1);
        private Vector3 _sunPosition = new Vector3(50, 50, 50);
        private Vector3 _sunTarget = new Vector3(0, 0, 0);
        private Vector4 _sunColor = new Vector4(1, 1, 1, 1);
        private float _sunAmbient = 0.25f;

        private float _fov = 45f;
        private float _fovShadow = 45f;
        private float _zFar = 1000f;

        /// <summary>
        /// Aktuelles Fenster
        /// </summary>
        public GLWindow CurrentWindow
        {
            get
            {
                return GLWindow.CurrentWindow;
            }
        }

        /// <summary>
        /// Entfernung bis zu der die Kamera noch Objekte wahrnimmt
        /// </summary>
        public float ZFar
        {
            get
            {
                return _zFar;
            }
            set
            {
                _zFar = value >= 50f ? value : 1000f;
                CurrentWindow.CalculateProjectionMatrix();
            }
        }

        /// <summary>
        /// Field of View (Standard: 45 Grad)
        /// </summary>
        public float FOV
        {
            get
            {
                return _fov;
            }
            set
            {
                _fov = HelperGL.Clamp(value, 20, 175);
                CurrentWindow.CalculateProjectionMatrix();
            }
        }

        /// <summary>
        /// Field of View (Standard: 45 Grad)
        /// </summary>
        public float FOVShadow
        {
            get
            {
                return _fovShadow;
            }
            set
            {
                _fovShadow = HelperGL.Clamp(value, 20, 175);
                CurrentWindow.CalculateProjectionMatrix();
            }
        }

        /// <summary>
        /// Kameraposition
        /// </summary>
        /// <returns>Positionswert</returns>
        public Vector3 GetCameraPosition()
        {
            return _cameraPosition;
        }

        /// <summary>
        /// Zielposition
        /// </summary>
        /// <returns>Positionswert</returns>
        public Vector3 GetCameraTarget()
        {
            return _cameraTarget;
        }

        /// <summary>
        /// Setzt die Kameraposition
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="z">z</param>
        public void SetCameraPosition(float x, float y, float z)
        {
            _cameraPosition = new Vector3(x, y, z + 0.000001f);
            UpdateCameraLookAtVector();
        }

        /// <summary>
        /// Setzt die Kameraposition
        /// </summary>
        /// <param name="p">Position</param>
        public void SetCameraPosition(Vector3 p)
        {
            p.Z += 0.000001f;
            _cameraPosition = p;
            UpdateCameraLookAtVector();
        }

        /// <summary>
        /// Setzt das Blickziel der Kamera
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="z">z</param>
        public void SetCameraTarget(float x, float y, float z)
        {
            _cameraTarget = new Vector3(x, y, z);
            UpdateCameraLookAtVector();
        }
        /// <summary>
        /// Setzt das Blickziel der Kamera
        /// </summary>
        /// <param name="p">Zielkoordinaten</param>
        public void SetCameraTarget(Vector3 p)
        {
            _cameraTarget = p;
            UpdateCameraLookAtVector();
        }

        private void UpdateCameraLookAtVector()
        {
            _cameraLookAt = _cameraTarget - _cameraPosition;
            _cameraLookAt.NormalizeFast();
        }

        /// <summary>
        /// Erfragt den normalisierten Sichtvektor der Kamera
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCameraLookAtVector()
        {
            return _cameraLookAt;
        }

        /// <summary>
        /// Erfragt die Position der Sonne
        /// </summary>
        /// <returns>Position</returns>
        public Vector3 GetSunPosition()
        {
            return _sunPosition;
        }

        /// <summary>
        /// Erfragt das Blickziel der Sonne
        /// </summary>
        /// <returns>Position</returns>
        public Vector3 GetSunTarget()
        {
            return _sunTarget;
        }

        /// <summary>
        /// Setzt die Position der Sonne
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="z">z</param>
        public void SetSunPosition(float x, float y, float z)
        {
            SetSunPosition(new Vector3(x, y, z + 0.000001f));
        }

        /// <summary>
        /// Setzt die Position der Sonne
        /// </summary>
        /// <param name="p">Position</param>
        public void SetSunPosition(Vector3 p)
        {
            p.Z += +0.000001f;
            _sunPosition = p;
            _viewMatrixShadow = Matrix4.LookAt(_sunPosition, _sunTarget, KWEngine.WorldUp);
        }

        /// <summary>
        /// Setzt das Blickziel der Sonne
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="z">z</param>
        public void SetSunTarget(float x, float y, float z)
        {
            SetSunTarget(new Vector3(x, y, z));
        }

        /// <summary>
        /// Setzt das Blickziel der Sonne
        /// </summary>
        /// <param name="p">Position</param>
        public void SetSunTarget(Vector3 p)
        {
            _sunTarget = p;
            _viewMatrixShadow = Matrix4.LookAt(_sunPosition, _sunTarget, KWEngine.WorldUp);
        }

        /// <summary>
        /// Helligkeit des Umgebungslichts (dort wo die Sonne nicht scheint)
        /// </summary>
        public float SunAmbientFactor
        {
            get
            {
                return _sunAmbient;
            }
            set
            {
                _sunAmbient = Helper.HelperGL.Clamp(value, 0f, 1f);
            }
        }

        /// <summary>
        /// Setzt die Farbe des Sonnenlichts
        /// </summary>
        /// <param name="red">Rotanteil</param>
        /// <param name="green">Grünanteil</param>
        /// <param name="blue">Blauanteil</param>
        /// <param name="intensity">Helligkeitsanteil</param>
        public void SetSunColor(float red, float green, float blue, float intensity)
        {
            _sunColor.X = HelperGL.Clamp(red, 0, 1);
            _sunColor.Y = HelperGL.Clamp(green, 0, 1);
            _sunColor.Z = HelperGL.Clamp(blue, 0, 1);
            _sunColor.W = HelperGL.Clamp(intensity, 0, 1);
        }

        /// <summary>
        /// Erfragt die Farbe der Sonne
        /// </summary>
        /// <returns>Farbinfos</returns>
        public Vector4 GetSunColor()
        {
            return _sunColor;
        }


        /// <summary>
        /// Vorbereitungsmethode
        /// </summary>
        public abstract void Prepare();

        /// <summary>
        /// Act-Methode
        /// </summary>
        /// <param name="ks">Keyboardinfos</param>
        /// <param name="ms">Mausinfos</param>
        /// <param name="deltaTimeFactor">Delta-Time-Faktor (Standard: 1.0)</param>
        public abstract void Act(KeyboardState ks, MouseState ms, float deltaTimeFactor);

        /// <summary>
        /// Erfragt ein Modell aus der Engine-Datenbank
        /// </summary>
        /// <param name="name">Modellname</param>
        /// <returns>Modelldaten</returns>
        public GeoModel GetModel(string name)
        {
            return KWEngine.GetModel(name);
        }

        internal void AddRemoveObjects()
        {
            lock (_gameObjects)
            {
                foreach (GameObject g in _gameObjectsTBA)
                {
                    if (!_gameObjects.Contains(g))
                    {
                        _gameObjects.Add(g);
                        g.CurrentWorld = this;
                        g.UpdateModelMatrixAndHitboxes();
                    }
                }
                _gameObjectsTBA.Clear();

                foreach (GameObject g in _gameObjectsTBR)
                {
                    g.CurrentWorld = null;
                    _gameObjects.Remove(g);
                }
                _gameObjectsTBR.Clear();


            }

            lock (_hudObjects)
            {
                foreach (HUDObject h in _hudObjectsTBA)
                {
                    if (!_hudObjects.Contains(h))
                    {
                        _hudObjects.Add(h);
                        h.CurrentWorld = this;
                    }
                }
                _hudObjectsTBA.Clear();

                foreach (HUDObject h in _hudObjectsTBR)
                {
                    h.CurrentWorld = null;
                    _hudObjects.Remove(h);
                }
                _hudObjectsTBR.Clear();
            }

            lock (_particleObjects)
            {
                foreach (ParticleObject g in _particleObjectsTBA)
                {
                    if (!_particleObjects.Contains(g))
                    {
                        g._starttime = DeltaTime.Watch.ElapsedMilliseconds; // Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
                        _particleObjects.Add(g);
                    }
                }
                _particleObjectsTBA.Clear();

                foreach (ParticleObject g in _particleObjectsTBR)
                {
                    _particleObjects.Remove(g);
                }
                _particleObjectsTBR.Clear();
            }

            lock(_explosionObjects)
            {
                foreach(Explosion ex in _explosionObjectsTBA)
                {
                    if (!_explosionObjects.Contains(ex))
                    {
                        ex._starttime = DeltaTime.Watch.ElapsedMilliseconds;  //Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
                        _explosionObjects.Add(ex);
                    }
                }
                _explosionObjectsTBA.Clear();

                foreach (Explosion ex in _explosionObjectsTBR)
                {
                    _explosionObjects.Remove(ex);
                }
                _explosionObjectsTBR.Clear();

            }

            lock (_lightObjects)
            {
                foreach (LightObject g in _lightObjectsTBR)
                {
                    g.CurrentWorld = null;
                    _lightObjects.Remove(g);
                }
                _lightObjectsTBR.Clear();
                _lightcount = _lightObjects.Count;

                foreach (LightObject g in _lightObjectsTBA)
                {
                    if(g.Type == LightType.DirectionalShadow)
                    {
                        LightObject shadowLight = _lightObjects.FirstOrDefault(l => l.Type == LightType.DirectionalShadow);
                        if(shadowLight != null)
                        {
                            throw new Exception("Only one light of type DirectionalShadow is allowed per World instance.");
                        }
                    }
                    if (!_lightObjects.Contains(g) && _lightcount <= 10)
                    {
                        _lightObjects.Add(g);
                        g.CurrentWorld = this;
                    }
                    else
                    {
                        throw new Exception("Please do not add more than 10 lights.");
                    }
                }
                _lightObjectsTBA.Clear();

                _lightcount = _lightObjects.Count;
            }
        }

        internal List<ParticleObject> GetParticleObjects()
        {
            return _particleObjects;
        }

        /// <summary>
        /// Erfragt eine Liste der HUD-Objekte
        /// </summary>
        /// <returns>HUD-Objekte der Welt</returns>
        public IReadOnlyCollection<HUDObject> GetHUDObjects()
        {
            return _hudObjects.AsReadOnly();
        }

        /// <summary>
        /// Fügt ein HUD-Objekt hinzu
        /// </summary>
        /// <param name="h">Objekt</param>
        public void AddHUDObject(HUDObject h)
        {
            if (!_hudObjects.Contains(h))
            {
                _hudObjectsTBA.Add(h);
            }
            else
            {
                throw new Exception("This HUD object already exists in this world.");
            }
        }

        /// <summary>
        /// Löscht ein HUD-Objekt
        /// </summary>
        /// <param name="h">Objekt</param>
        public void RemoveHUDObject(HUDObject h)
        {
            _hudObjectsTBR.Add(h);
        }

        /// <summary>
        /// Fügt ein Lichtobjekt hinzu
        /// </summary>
        /// <param name="l">Objekt</param>
        public void AddLightObject(LightObject l)
        {
            if (!_lightObjects.Contains(l))
            {
                _lightObjectsTBA.Add(l);
            }
            else
            {
                throw new Exception("This light already exists in this world.");
            }

        }

        /// <summary>
        /// Löscht ein Lichtobjekt
        /// </summary>
        /// <param name="l">Objekt</param>
        public void RemoveLightObject(LightObject l)
        {
            _lightObjectsTBR.Add(l);
        }

        /// <summary>
        /// Fügt ein neues GameObject der Welt hinzu
        /// </summary>
        /// <param name="g">Objekt</param>
        public void AddGameObject(GameObject g)
        {
            lock (_gameObjects)
            {
                if (g != null && !_gameObjects.Contains(g))
                {
                    _gameObjectsTBA.Add(g);
                }
                else
                    throw new Exception("GameObject instance " + g.Name + " already exists in current world.");
            }

        }

        /// <summary>
        /// Fügt ein neues Explosionsobjekt der Welt hinzu
        /// </summary>
        /// <param name="ex">Objekt</param>
        public void AddGameObject(Explosion ex)
        {
            lock (_explosionObjects)
            {
                if (ex != null && !_explosionObjects.Contains(ex))
                {
                    _explosionObjectsTBA.Add(ex);
                }
                else
                    throw new Exception("Explosion instance already exists in current world.");
            }
        }

        internal void RemoveExplosionObject(Explosion ex)
        {
            _explosionObjectsTBR.Add(ex);
        }

        /// <summary>
        /// Fügt ein neues Partikelobjekt hinzu
        /// </summary>
        /// <param name="g">Objekt</param>
        public void AddParticleObject(ParticleObject g)
        {
            lock (_particleObjects)
            {
                if (!_particleObjects.Contains(g))
                {
                    _particleObjectsTBA.Add(g);
                }
            }
        }

        internal void RemoveParticleObject(ParticleObject g)
        {
            _particleObjectsTBR.Add(g);
        }

        /// <summary>
        /// Löscht ein GameObject aus der Welt
        /// </summary>
        /// <param name="g">Objekt</param>
        public void RemoveGameObject(GameObject g)
        {
            _gameObjectsTBR.Add(g);
        }

        internal void Dispose()
        {
            GLAudioEngine.SoundStopAll();
            lock (_gameObjects)
            {
                _gameObjects.Clear();
                _gameObjectsTBA.Clear();
                _gameObjectsTBR.Clear();
            }

            lock (_particleObjects)
            {
                _particleObjects.Clear();
                _particleObjectsTBA.Clear();
                _particleObjectsTBR.Clear();
            }

            lock (_hudObjects)
            {
                _hudObjects.Clear();
                _hudObjectsTBA.Clear();
                _hudObjectsTBR.Clear();
            }

            lock (_lightObjects)
            {
                _lightObjects.Clear();
                _lightObjectsTBA.Clear();
                _lightObjectsTBR.Clear();
            }

            lock (_explosionObjects)
            {
                _explosionObjects.Clear();
                _explosionObjectsTBA.Clear();
                _explosionObjectsTBR.Clear();
            }

            lock (KWEngine.Models)
            {
                List<string> removableModels = new List<string>();
                foreach (string m in KWEngine.Models.Keys)
                {
                    if (KWEngine.Models[m].AssemblyMode != SceneImporter.AssemblyMode.Internal)
                    {
                        KWEngine.Models[m].Dispose();
                        removableModels.Add(m);
                    }
                }
                foreach (string m in removableModels)
                    KWEngine.Models.Remove(m);
            }

            if (KWEngine.CustomTextures.ContainsKey(this))
            {
                Dictionary<string, int> dict = KWEngine.CustomTextures[this];
                foreach (int texId in dict.Values)
                {
                    GL.DeleteTexture(texId);
                }
                dict.Clear();
                KWEngine.CustomTextures.Remove(this);
            }
        }

        /// <summary>
        /// Erfragt eine Liste mit aktuellen GameObjekt-Instanzen
        /// </summary>
        /// <returns>Instanzen</returns>
        public IReadOnlyCollection<GameObject> GetGameObjects()
        {
            IReadOnlyCollection<GameObject> returnCollection = null;
            lock (_gameObjects)
            {
                returnCollection = _gameObjects.AsReadOnly();
            }
            return returnCollection;
        }

        /// <summary>
        /// Erfragt eine Liste mit aktuellen LightObject-Instanzen
        /// </summary>
        /// <returns>Instanzen</returns>
        public IReadOnlyCollection<LightObject> GetLightObjects()
        {
            IReadOnlyCollection<LightObject> returnCollection = null;
            lock (_lightObjects)
            {
                returnCollection = _lightObjects.AsReadOnly();
            }
            return returnCollection;
        }

        internal void SortByZ()
        {
            _gameObjects.Sort((x, y) => x == null ? (y == null ? 0 : -1)
                : (y == null ? 1 : y.DistanceToCamera.CompareTo(x.DistanceToCamera)));
        }

        internal void SweepAndPrune()
        {

            if (_gameObjects.Count < 2)
                return;
            List<CollisionPair> pairs = new List<CollisionPair>();
            IOrderedEnumerable<GameObject> axisList = _gameObjects.OrderBy(x => x.LeftRightMost.X);
            List<GameObject> activeList = new List<GameObject>();
            activeList.Add(axisList.ElementAt(0));
            int axisListCount = _gameObjects.Count;
            for (int i = 0; i < axisListCount; i++)
            {
                GameObject currentFromAxisList = axisList.ElementAt(i);
                currentFromAxisList._collisionCandidates.Clear();
                if (currentFromAxisList.IsCollisionObject == false)
                {
                    continue;
                }

                for (int j = 0; j < activeList.Count; )
                {
                    GameObject goActiveList = activeList[j];
                    if (currentFromAxisList.LeftRightMost.X > goActiveList.LeftRightMost.Y)
                    {
                        activeList.RemoveAt(j);
                    }
                    else
                    {
                        if (!currentFromAxisList.Equals(goActiveList))
                        {
                            if ((currentFromAxisList._sceneCenter - goActiveList._sceneCenter).LengthFast <
                                currentFromAxisList.GetMaxDiameter() / 2 + goActiveList.GetMaxDiameter() / 2)
                                pairs.Add(new CollisionPair(currentFromAxisList, goActiveList));
                            if(!activeList.Contains(currentFromAxisList))
                                activeList.Add(currentFromAxisList);
                        }
                        j++;
                    }
                }
            }

            foreach (CollisionPair p in pairs)
            {
                p.A._collisionCandidates.Add(p.B);
                p.B._collisionCandidates.Add(p.A);
            }
        }

        /// <summary>
        /// Spielt einen Ton ab (ogg)
        /// </summary>
        /// <param name="audiofile">Audiodatei (ogg)</param>
        /// <param name="playLooping">Looped playback?</param>
        /// <param name="volume">Lautstärke</param>
        public void SoundPlay(string audiofile, bool playLooping = false, float volume = 1.0f)
        {
            GLAudioEngine.SoundPlay(audiofile, playLooping, volume);
        }

        /// <summary>
        /// Stoppt einen bestimmten Ton
        /// </summary>
        /// <param name="audiofile">zu stoppender Ton</param>
        public void SoundStop(string audiofile)
        {
            GLAudioEngine.SoundStop(audiofile);
        }

        /// <summary>
        /// Stoppt die Wiedergabe aller Töne
        /// </summary>
        public void SoundStopAll()
        {
            GLAudioEngine.SoundStopAll();
        }

        /// <summary>
        /// Erstellt eine Liste aller GameObject-Instanzen mit einem bestimmten Namen
        /// </summary>
        /// <param name="name">gesuchter Name</param>
        /// <returns>Liste der gefundenen Instanzen</returns>
        public List<GameObject> GetGameObjectsByName(string name)
        {
            name = name.Trim();
            List<GameObject> os = _gameObjects.FindAll(go => go.Name == name);
            return os;
        }

        /// <summary>
        /// Durchsucht die Liste der GameObject-Instanzen nach Objekten des gegebenen Typs mit dem gegebenen Namen
        /// </summary>
        /// <typeparam name="T">Klassenname</typeparam>
        /// <param name="name">Name der gesuchten Objekte</param>
        /// <returns>Liste der gefundenen Objekte</returns>
        public List<T> GetGameObjectsByName<T>(string name) where T : class
        {
            name = name.Trim();
            List<T> os = new List<T>();
            var list = _gameObjects.FindAll(go => go is T && go.Name == name);
            if(list.Count > 0)
            {
                foreach (object o in list)
                {
                    os.Add((T)o);
                }
            }
            return os;
        }

        /// <summary>
        /// Durchsucht die Liste der GameObject-Instanzen nach Objekten des gegebenen Typs
        /// </summary>
        /// <typeparam name="T">Klassenname</typeparam>
        /// <returns>Liste der gefundenen Objekte</returns>
        public List<T> GetGameObjectsByType<T>()
        {
            List<T> os = new List<T>();
            var list = _gameObjects.FindAll(go => go is T);
            if (list.Count > 0)
            {
                foreach (object o in list)
                {
                    os.Add((T)o);
                }
            }
            return os;
        }

        /// <summary>
        /// Durchsucht die Liste der GameObject-Instanzen nach einem Objekt des gegebenen Typs mit dem gegebenen Namen
        /// </summary>
        /// <typeparam name="T">Klasse des gesuchten Objekts</typeparam>
        /// <param name="name">Name des gesuchten Objekts</param>
        /// <returns>Gesuchtes Objekt oder null (falls nicht gefunden)</returns>
        public T GetGameObjectByName<T>(string name) where T : class
        {
            name = name.Trim();
            GameObject g = _gameObjects.FirstOrDefault(go => go is T && go.Name == name);
            if(g != null)
            {
                return (T)(object)g;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Durchsucht die Liste der GameObject-Instanzen nach einem Objekt mit dem gegebenen Namen
        /// </summary>
        /// <param name="name">Name des gesuchten Objekts</param>
        /// <returns>Gesuchtes Objekt oder null (falls nicht gefunden)</returns>
        public GameObject GetGameObjectByName(string name)
        {
            name = name.Trim();
            GameObject g = _gameObjects.FirstOrDefault(go => go.Name == name);
            return g;
        }

        /// <summary>
        /// Durchsucht die Liste der LightObject-Instanzen nach einem Objekt mit dem gegebenen Namen
        /// </summary>
        /// <param name="name">Name des gesuchten Objekts</param>
        /// <returns>Gesuchtes Objekt oder null (falls nicht gefunden)</returns>
        public LightObject GetLightObjectByName(string name)
        {
            name = name.Trim();
            LightObject l = _lightObjects.FirstOrDefault(lo => lo.Name == name);
            return l;
        }

        /// <summary>
        /// Durchsucht die Liste der HUDObject-Instanzen nach einem Objekt mit dem gegebenen Namen
        /// </summary>
        /// <param name="name">Name des gesuchten Objekts</param>
        /// <returns>Gesuchtes Objekt oder null (falls nicht gefunden)</returns>
        public HUDObject GetHUDObjectByName(string name)
        {
            name = name.Trim();
            HUDObject h = _hudObjects.FirstOrDefault(ho => ho.Name == name);
            return h;
        }

        /// <summary>
        /// Gibt das GameObject zurück, das unter dem Mauszeiger liegt (Instanzen müssen mit IsPickable = true gesetzt haben)
        /// </summary>
        /// <param name="ms">Mausinformationen</param>
        /// <returns>Gewähltes GameObject</returns>
        public static GameObject PickGameObject(MouseState ms)
        {
            return GameObject.PickGameObject(ms);
        }
    }
}
