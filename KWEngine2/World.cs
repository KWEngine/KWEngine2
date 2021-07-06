using KWEngine2.GameObjects;
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
using static KWEngine2.KWEngine;
using System.Windows.Forms;

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

        private LightObject _debugShadowLight= null;
        /// <summary>
        /// Hält eine Referenz zu einem zu untersuchenden Lichtobjekt (nur Directional und Point Lights!)
        /// </summary>
        public LightObject DebugShadowLight
        {
            get
            {
                return _debugShadowLight;
            }
            set
            {
                if(value != null && value.Type != LightType.Point && value.IsShadowCaster)
                {
                    _debugShadowLight = value;
                }
                else
                {
                    _debugShadowLight = null;
                    HelperGL.ShowErrorAndQuit("World::DebugShadowLight", "Cannot set debug mode on this light because it either is null, is of type 'Point' or it does not cast shadows!");
                }
            }
        }

        /// <summary>
        /// Zeige Koordinatensystem
        /// </summary>
        public bool DebugShowCoordinateSystem { get; set; } = false;

        /// <summary>
        /// Zeige 10 Einheiten großes Hilfsgitter auf der angegebenen Ebene
        /// </summary>
        public GridType DebugShowCoordinateSystemGrid { get; set; } = GridType.None;

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

        internal Vector2 _textureBackgroundOffset = new Vector2(0, 0);
        internal Vector2 _textureBackgroundClip = new Vector2(1, 1);

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
        internal Vector2 _textureBackgroundTransform = new Vector2(1, 1);
        internal int _textureSkybox = -1;

        internal void SetTextureBackgroundInternal(string filename, float repeatX = 1, float repeatY = 1, float clipX = 1, float clipY = 1, bool isFile = true)
        {
            if (filename == null || filename.Length < 1)
            {
                _textureBackground = -1;
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
                _textureBackgroundTransform = new Vector2(HelperGL.Clamp(repeatX, 0.001f, 8192), HelperGL.Clamp(repeatY, 0.001f, 8192));
                _textureBackgroundClip = new Vector2(HelperGL.Clamp(clipX, 0, 1), HelperGL.Clamp(clipY, 0, 1));
                _textureBackgroundOffset = new Vector2(0, 0);
                _textureSkybox = -1;
            }
        }

        /// <summary>
        /// Setzt das Hintergrundbild (2D)
        /// </summary>
        /// <param name="filename">Textur</param>
        /// <param name="repeatX">Wiederholung Breite</param>
        /// <param name="repeatY">Wiederholung Höhe</param>
        /// <param name="clipX">Regelt, wie viel der Bildbreite genutzt wird (1 = 100%)</param>
        /// <param name="clipY">Regelt, wie viel der Bildhöhe genutzt wird (1 = 100%)</param>
        /// <param name="isFile">false, falls der Pfad Teil der EXE-Datei ist</param>
        public void SetTextureBackground(string filename, float repeatX = 1, float repeatY = 1, float clipX = 1, float clipY = 1, bool isFile = true)
        {
            if (GLWindow.CurrentWindow._multithreaded)
            {
                Action a = () => SetTextureBackgroundInternal(filename, repeatX, repeatY, clipX, clipY, isFile);
                HelperGLLoader.AddCall(this, a);
            }
            else
                SetTextureBackgroundInternal(filename, repeatX, repeatY, clipX, clipY, isFile);
        }

        /// <summary>
        /// Versetzt den sichtbaren Teil des Hintergrundbilds um die angegebene Menge
        /// </summary>
        /// <param name="x">Verschiebung nach links (negativer Wert) oder rechts (positiver Wert)</param>
        /// <param name="y">Verschiebung nach oben (negativer Wert) oder unten (positiver Wert)</param>
        public void SetTextureBackgroundOffset(float x, float y)
        {
            _textureBackgroundOffset = new Vector2(x, y);
        }

        internal float _textureBackgroundMultiplier = 1;
        /// <summary>
        /// Verstärkt die Helligkeit des Hintergrundbilds (2D und 3D-Skybox)
        /// </summary>
        /// <param name="m">Verstärkung der Helligkeit (0.000001 bis 10) - Standardwert: 1</param>
        public void SetTextureBackgroundBrightnessMultiplier(float m)
        {
            _textureBackgroundMultiplier = HelperGL.Clamp(m, 0.000001f, 10);
        }

        /// <summary>
        /// Setzt das 3D-Hintergrundbild
        /// </summary>
        /// <param name="filename">Skybox-Textur</param>
        /// <param name="isFile">false, falls der Pfad Teil der EXE-Datei ist</param>
        public void SetTextureSkybox(string filename, bool isFile = true)
        {
            if (GLWindow.CurrentWindow._multithreaded)
            {
                Action a = () => SetTextureSkyboxInternal(filename, 1, 1, 1, 1, isFile);
                HelperGLLoader.AddCall(this, a);
            }
            else
            {
                SetTextureSkyboxInternal(filename, 1, 1, 1, 1, isFile);
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
                _textureBackground = -1;
            }
        }


        private Vector3 _cameraPosition = new Vector3(0, 0, 25);
        private Vector3 _cameraTarget = new Vector3(0, 0, 0);
        private Vector3 _cameraLookAt = new Vector3(0, 0, 1);

        internal Vector4 _ambientLight = new Vector4(1, 1, 1, 0.75f);

        private float _fov = 45f;
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

        internal Vector3 GetCameraLookAtVectorEitherWay()
        {
            if(IsFirstPersonMode && _firstPersonObject != null)
            {
                return HelperCamera.GetLookAtVector();
            }
            else
            {
                return GetCameraLookAtVector();
            }
        }

        internal Vector3 GetCameraPositionEitherWay()
        {
            if (IsFirstPersonMode && _firstPersonObject != null)
            {
                return _firstPersonObject.Position + KWEngine.WorldUp * _firstPersonObject.FPSEyeOffset;
            }
            else
            {
                return GetCameraPosition();
            }
        }

        /// <summary>
        /// Setzt das Umgebungslichts (dort wo die Sonne nicht scheint)
        /// </summary>
        /// <param name="ambient">Umgebungslicht</param>
        public void SetAmbientLight(Vector4 ambient)
        {
            _ambientLight.X = HelperGL.Clamp(ambient.X, 0, 1);
            _ambientLight.Y = HelperGL.Clamp(ambient.Y, 0, 1);
            _ambientLight.Z = HelperGL.Clamp(ambient.Z, 0, 1);
            _ambientLight.W = HelperGL.Clamp(ambient.W, 0, 1);
        }


        /// <summary>
        /// Setzt das Umgebungslichts (dort wo sonst kein Licht scheint)
        /// </summary>
        /// <param name="r">Rotanteil (0 - 1)</param>
        /// <param name="g">Grünanteil (0 - 1)</param>
        /// <param name="b">Blauanteil (0 - 1)</param>
        /// <param name="intensity">Intensität (0 - 1)</param>
        public void SetAmbientLight(float r, float g, float b, float intensity)
        {
            _ambientLight.X = HelperGL.Clamp(r, 0, 1);
            _ambientLight.Y = HelperGL.Clamp(g, 0, 1);
            _ambientLight.Z = HelperGL.Clamp(b, 0, 1);
            _ambientLight.W = HelperGL.Clamp(intensity, 0, 1);
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
        public abstract void Act(KeyboardState ks, MouseState ms);

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
                        g._starttime = DeltaTime.Watch.ElapsedMilliseconds;
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
                        ex._starttime = DeltaTime.Watch.ElapsedMilliseconds;
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
                    g.RemoveFramebuffer();
                    _lightObjects.Remove(g);
                }
                _lightObjectsTBR.Clear();
                _lightcount = _lightObjects.Count;

                foreach (LightObject g in _lightObjectsTBA)
                {
                    if (!_lightObjects.Contains(g) && _lightcount <= KWEngine.MAX_LIGHTS)
                    {
                        g.ApplyFramebuffer();

                        _lightObjects.Add(g);
                        g.CurrentWorld = this;
                    }
                    else
                    {
                        HelperGL.ShowErrorAndQuit("World::AddLightObject()", "Please do not add more than " + KWEngine.MAX_LIGHTS + " lights.");
                        _lightObjectsTBA.Remove(g);
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
                HelperGL.ShowErrorAndQuit("World::AddHUDObject()", "This HUD object already exists in this world.");
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
            int shadowLightCount = 0;
            bool alreadyInWorld = false;
            int lightCount = 0;

            for (int i = 0; i < _lightObjectsTBA.Count; i++)
            {
                if (_lightObjectsTBA[i].IsShadowCaster)
                    shadowLightCount++;

                if (_lightObjectsTBA[i] == l)
                    alreadyInWorld = true;

                lightCount++;
            }
            if (!alreadyInWorld && shadowLightCount < KWEngine.MAX_SHADOWMAPS)
            {
                for (int i = 0; i < _lightObjects.Count; i++)
                {
                    if (_lightObjects[i].IsShadowCaster)
                        shadowLightCount++;

                    if (_lightObjects[i] == l)
                        alreadyInWorld = true;

                    lightCount++;
                }
            }


            if (!alreadyInWorld && lightCount < KWEngine.MAX_LIGHTS && shadowLightCount < KWEngine.MAX_SHADOWMAPS)
            {
                _lightObjectsTBA.Add(l);
            }
            else
            {
                HelperGL.ShowErrorAndQuit("Fatal error!", "Either this light already exists in this world or you have exceeded the maximum number of lights(" + KWEngine.MAX_SHADOWMAPS + "x shadow, " + KWEngine.MAX_LIGHTS + "x lights total)");
            }

        }

        /// <summary>
        /// Löscht ein Lichtobjekt
        /// </summary>
        /// <param name="l">Objekt</param>
        public void RemoveLightObject(LightObject l)
        {
            l.RemoveFramebuffer();
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
                    HelperGL.ShowErrorAndQuit("Fatal error!", "GameObject instance " + g.Name + " already exists in current world.");
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
                if (ex != null && !_explosionObjects.Contains(ex) && !_explosionObjectsTBA.Contains(ex))
                {
                    _explosionObjectsTBA.Add(ex);
                }
                else
                {
                    HelperGL.ShowErrorAndQuit("World::AddGameObject()", "This Explosion instance already exists in the current world.");
                }
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
                CurrentWindow.InitializeFramebuffersLightsList();

                for(int i = 0; i < _lightObjects.Count; i++)
                {
                    if(_lightObjects[i].IsShadowCaster)
                    {
                        _lightObjects[i] = null;
                    }
                }
                _lightObjects.Clear();

                for (int i = 0; i < _lightObjectsTBA.Count; i++)
                {
                    if (_lightObjectsTBA[i].IsShadowCaster)
                    {
                        _lightObjectsTBA[i] = null;
                    }
                }
                _lightObjectsTBA.Clear();

                for (int i = 0; i < _lightObjectsTBR.Count; i++)
                {
                    if (_lightObjectsTBR[i].IsShadowCaster)
                    {
                        _lightObjectsTBR[i] = null;
                    }
                }
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
            GL.Flush();
            GL.Finish();
            GC.Collect(GC.MaxGeneration);
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


        internal int _sweepTestAxisIndex = 0;
        internal void SweepAndPrune()
        {
            if (_gameObjects.Count < 2)
                return;

            List<GameObject> axisList = null;
            if (_sweepTestAxisIndex == 0)
                axisList = _gameObjects.OrderBy(x => x.LeftRightMost.X).ToList();
            else if (_sweepTestAxisIndex == 1)
                axisList = _gameObjects.OrderBy(x => x.BottomTopMost.X).ToList();
            else if (_sweepTestAxisIndex == 2)
                axisList = _gameObjects.OrderBy(x => x.BackFrontMost.X).ToList();

            Vector3 centerSum = new Vector3(0, 0, 0);
            Vector3 centerSqSum = new Vector3(0, 0, 0);
            for(int i = 0; i < axisList.Count(); i++)
            {
                if(axisList[i].IsCollisionObject == false)
                {
                    continue;
                }

                Vector3 currentCenter = axisList[i].GetCenterPointForAllHitboxes();
                centerSum += currentCenter;
                centerSqSum += (currentCenter * currentCenter);
  
                for(int j = i+1; j < axisList.Count; j++)
                {
                    GameObject fromJ = axisList[j];
                    if(fromJ.IsCollisionObject == false)
                    {
                        continue;
                    }

                    GameObject fromI = axisList[i];
                    if (fromJ.GetExtentsForAxis(_sweepTestAxisIndex).X > fromI.GetExtentsForAxis(_sweepTestAxisIndex).Y)
                    {
                        break;
                    }
                    fromI._collisionCandidates.Add(fromJ);
                    fromJ._collisionCandidates.Add(fromI);
                }
            }
            centerSum /= axisList.Count;
            centerSqSum /= axisList.Count;
            Vector3 variance = centerSqSum - (centerSum * centerSum);
            float maxVar = Math.Abs(variance.X);
            _sweepTestAxisIndex = 0;
            if (Math.Abs(variance.Y) > maxVar)
            {
                maxVar = Math.Abs(variance.Y);
                _sweepTestAxisIndex = 1;
            }
            if(Math.Abs(variance.Z) > maxVar)
            {
                maxVar = Math.Abs(variance.Z);
                _sweepTestAxisIndex = 2;
            }        
        }


        /// <summary>
        /// Spielt einen Ton ab (ogg)
        /// </summary>
        /// <param name="audiofile">Audiodatei (ogg)</param>
        /// <param name="playLooping">Looped playback?</param>
        /// <param name="volume">Lautstärke</param>
        /// <returns>ID des verwendeten Audiokanals</returns>
        public int SoundPlay(string audiofile, bool playLooping = false, float volume = 1.0f)
        {
            return GLAudioEngine.SoundPlay(audiofile, playLooping, volume);
        }

        /// <summary>
        /// Lädt eine Audiodatei in den Arbeitsspeicher
        /// </summary>
        /// <param name="audiofile">Audiodatei</param>
        protected static void SoundPreload(string audiofile)
        {
            GLAudioEngine.SoundPreload(audiofile);
        }

        /// <summary>
        /// Ändert die Lautstärke eines Tons
        /// </summary>
        /// <param name="sourceId">id der Audiospur</param>
        /// <param name="gain">Lautstärke (0.0f bis 1.0f)</param>
        public static void SoundChangeGain(int sourceId, float gain)
        {
            GLAudioEngine.SoundChangeGain(sourceId, gain);
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
        /// Stoppt den angegebenen Audiokanal
        /// </summary>
        /// <param name="sourceId">Lanalnummer</param>
        protected static void SoundStop(int sourceId)
        {
            GLAudioEngine.SoundStop(sourceId);
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
        
        /// <summary>
        /// Gibt eine Liste von GameObject-Instanzen zurück, die unter dem Mauszeiger liegen (Instanzen müssen mit IsPickable = true gesetzt haben)
        /// </summary>
        /// <param name="ms">Mausinformationen</param>
        /// <returns>Liste betroffener GameObject-Instanzen</returns>
        public static List<GameObject> PickGameObjects(MouseState ms)
        {
            return GameObject.PickGameObjects(ms);
        }
        
        /// <summary>
        /// Konvertiert 2D-Mauskoordinaten in 3D-Koordinaten
        /// </summary>
        /// <param name="ms">Mausinformationen</param>
        /// <param name="planeNormal">Kollisionsebene (Standard: Camera)</param>
        /// <param name="planeHeight">Höhe der Kollisionsebene</param>
        /// <returns>3D-Mauskoordinaten</returns>
        protected static Vector3 GetMouseIntersectionPoint(MouseState ms, Plane planeNormal, float planeHeight)
        {
            Vector3 normal;
            if (planeNormal == Plane.Y)
                normal = new Vector3(0, 1, 0.000001f);
            else if (planeNormal == Plane.X)
                normal = new Vector3(1, 0, 0);
            else if (planeNormal == Plane.Z)
                normal = new Vector3(0, 0.000001f, 1);
            else
            {
                if (KWEngine.CurrentWorld != null)
                {
                    normal = -KWEngine.CurrentWorld.GetCameraLookAtVector();
                }
                else
                {
                    normal = new Vector3(0, 1, 0.000001f);
                }
            }

            Vector2 mc = HelperGL.GetNormalizedMouseCoords(ms.X, ms.Y, KWEngine.CurrentWindow);
            Vector3 worldRay = GameObject.Get3DMouseCoords(mc.X, mc.Y);
            bool result;
            Vector3 intersection;
            if (Projection == ProjectionType.Perspective)
                result = GameObject.LinePlaneIntersection(out intersection, worldRay, CurrentWorld.GetCameraPosition(), normal, normal * planeHeight);
            else
            {
                Vector3 rayOrigin = HelperGL.GetRayOriginForOrthographicProjection(mc);
                result = GameObject.LinePlaneIntersection(out intersection, worldRay, rayOrigin, normal, normal * planeHeight);
            }
            if (result)
            {
                return intersection;
            }
            else
                return normal * planeHeight;
        }

        /// <summary>
        /// Ermittelt bei GLWindow.CursorGrabbed = true die relative Mausbewegung des aktuellen Frames
        /// </summary>
        /// <param name="ms">Mausstatus</param>
        /// <returns>Relative X- und Y-Abweichung des Mauscursors als Vector2-InstanzS</returns>
        public static Vector2 GetMouseCursorMovement(MouseState ms)
        {
            if (KWEngine.CurrentWindow.CursorGrabbed || (KWEngine.CurrentWorld != null && KWEngine.CurrentWorld.IsFirstPersonMode))
            {
                int centerX = KWEngine.CurrentWindow.X + KWEngine.CurrentWindow.Width / 2;
                int centerY = KWEngine.CurrentWindow.Y + KWEngine.CurrentWindow.Height / 2;
                return new Vector2((ms.X - centerX) * Math.Abs(KWEngine.MouseSensitivity), (centerY - ms.Y) * KWEngine.MouseSensitivity);
            }
            else
            {
                return Vector2.Zero;
            }
        }
    }
}
