using KWEngine2.Collision;
using KWEngine2.Helper;
using KWEngine2.Model;
using OpenTK;
using OpenTK.Input;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using static KWEngine2.KWEngine;
using KWEngine2.Audio;
using System.IO;
using System.Reflection;

namespace KWEngine2.GameObjects
{
    /// <summary>
    /// Spielobjekte-Klasse
    /// </summary>
    public abstract class GameObject : IComparable
    {
        /// <summary>
        /// Legt fest, ob das Objekt jeden Frame zuletzt aktualisiert wird (Standard: Nach Reihenfolge des Hinzufügens)
        /// </summary>
        public bool UpdateLast { get; set; } = false;

        internal GeoMaterial[] _materials;

        internal uint DistanceToCamera { get; set; } = 100;
        /// <summary>
        /// Gibt an, ob das Objekt Schatten wirft und empfängt
        /// </summary>
        public bool IsShadowCaster { get; set; } = false;
        /// <summary>
        /// Höhenkorrektur für den First-Person-Modus (Standard: 0)
        /// </summary>
        public float FPSEyeOffset { get; set; } = 0;
       
        /// <summary>
        /// Gibt an, ob das Objekt von anderen Lichtquellen betroffen ist
        /// </summary>
        public bool IsAffectedByLight { get; set; } = true;

        /// <summary>
        /// Aktuelle Spielwelt
        /// </summary>
        public World CurrentWorld { get; internal set; } = null;

        private IReadOnlyCollection<string> _meshNameList;

        private Vector3 _lookAtVector = new Vector3(0, 0, 1);

        internal Vector2 LeftRightMost { get; set; } = new Vector2(0, 0);
        internal Vector2 BackFrontMost { get; set; } = new Vector2(0, 0);
        internal Vector2 BottomTopMost { get; set; } = new Vector2(0, 0);

        /// <summary>
        /// Aktuelles Fenster
        /// </summary>
        public GLWindow CurrentWindow
        {
            get
            {
                if (GLWindow.CurrentWindow != null)
                    return GLWindow.CurrentWindow;
                else
                    throw new Exception("No window available.");
            }
        }
        internal int _largestHitboxIndex = -1;
        //internal GeoModelCube _cubeModel = null;
        internal List<Hitbox> Hitboxes = new List<Hitbox>();
        internal Matrix4[] ModelMatrixForRenderPass = null;
        internal Dictionary<string, Matrix4[]> BoneTranslationMatrices { get; set; }
        private int _animationId = -1;

        private Vector3 _tintColor = new Vector3(1, 1, 1);
        private Vector4 _glow = new Vector4(0, 0, 0, 0);
        private float _opacity = 1;

        internal List<GameObject> _collisionCandidates = new List<GameObject>();
        internal bool _collisionResetForFrame = false;

        /// <summary>
        /// Sichtbarkeit (0 = Unsichtbar, 1 = Sichtbar)
        /// </summary>
        public float Opacity
        {
            get
            {
                return _opacity;
            }
            set
            {
                CheckIfNotTerrain();
                _opacity = HelperGL.Clamp(value, 0, 1);
            }
        }

        /// <summary>
        /// Färbung
        /// </summary>
        public Vector3 Color
        {
            get
            {
                return _tintColor;
            }
            set
            {
                CheckModel();

                _tintColor.X = HelperGL.Clamp(value.X, 0, 1);
                _tintColor.Y = HelperGL.Clamp(value.Y, 0, 1);
                _tintColor.Z = HelperGL.Clamp(value.Z, 0, 1);

            }
        }

        private Vector4 _emissiveColor = new Vector4(0, 0, 0, 0);
        /// <summary>
        /// Strahlfarbe
        /// </summary>
        public Vector4 ColorEmissive
        {
            get
            {
                return _emissiveColor;
            }
            set
            {
                CheckModel();
                CheckIfNotTerrain();
                _emissiveColor.X = HelperGL.Clamp(value.X, 0, 1);
                _emissiveColor.Y = HelperGL.Clamp(value.Y, 0, 1);
                _emissiveColor.Z = HelperGL.Clamp(value.Z, 0, 1);
                _emissiveColor.W = HelperGL.Clamp(value.W, 0, 1);

            }
        }

        /// <summary>
        /// Glühfarbe
        /// </summary>
        public Vector4 Glow
        {
            get
            {
                return _glow;
            }
            set
            {
                _glow.X = HelperGL.Clamp(value.X, 0, 1);
                _glow.Y = HelperGL.Clamp(value.Y, 0, 1);
                _glow.Z = HelperGL.Clamp(value.Z, 0, 1);
                _glow.W = HelperGL.Clamp(value.W, 0, 1);
            }
        }

        /// <summary>
        /// Setzt die Glühfarbe
        /// </summary>
        /// <param name="red">Rot</param>
        /// <param name="green">Grün</param>
        /// <param name="blue">Blau</param>
        /// <param name="intensity">Helligkeit</param>
        public void SetGlow(float red, float green, float blue, float intensity)
        {
            Glow = new Vector4(red, green, blue, intensity);
        }

        private Vector4 _colorOutline = new Vector4(1, 1, 1, 0);

        /// <summary>
        /// Umrandungsfarbe (nicht für KWCube)
        /// </summary>
        public Vector4 ColorOutline
        {
            get
            {
                return _colorOutline;
            }
            set
            {
                if (Model != null && Model.IsTerrain)
                {
                    HelperGL.ShowErrorAndQuit("Fatal error!", "Outline cannot be set for terrain models.");
                }
                _colorOutline.X = HelperGL.Clamp(value.X, 0, 1);
                _colorOutline.Y = HelperGL.Clamp(value.Y, 0, 1);
                _colorOutline.Z = HelperGL.Clamp(value.Z, 0, 1);
                _colorOutline.W = HelperGL.Clamp(value.W, 0, 1);
            }
        }

        /// <summary>
        /// Setzt die Umrandungsfarbe
        /// </summary>
        /// <param name="red">Rot</param>
        /// <param name="green">Grün</param>
        /// <param name="blue">Blau</param>
        /// <param name="intensity">Sichtbarkeit</param>
        public void SetColorOutline(float red, float green, float blue, float intensity)
        {
            ColorOutline = new Vector4(red, green, blue, intensity);
        }

        /// <summary>
        /// Gibt an, ob das Objekt per Maus wählbar sein soll
        /// </summary>
        public bool IsPickable { get; set; } = false;
        /// <summary>
        /// ID der Animation
        /// </summary>
        public int AnimationID
        {
            get
            {
                return _animationId;
            }
            set
            {
                if (Model == null || !Model.IsValid || Model.Animations.Count == 0 || value >= Model.Animations.Count)
                {
                    HelperGL.ShowErrorAndQuit("Fatal error!", "Cannot set animation id on invalid model. Model might be null, invalid, without any animations or the animation id does not exist.");
                }
                else
                {
                    _animationId = value;
                }
            }
        }

        /// <summary>
        /// Gibt an, ob das Objekt über Animationen verfügt
        /// </summary>
        public bool HasAnimations
        {
            get
            {
                CheckModel();
                return Model.Animations != null && Model.Animations.Count > 0;
            }
        }

        private float _animationPercentage = 0;
        /// <summary>
        /// Prozent der Animation (0 bis 1)
        /// </summary>
        public float AnimationPercentage
        {
            get
            {
                return _animationPercentage;
            }
            set
            {
                _animationPercentage = HelperGL.Clamp(value, 0f, 1f);
                ProcessCurrentAnimation();
            }
        }
        /// <summary>
        /// Gibt an ob mit dem Objekt kollidiert werde kann
        /// </summary>
        public bool IsCollisionObject { get; set; } = false;
        /// <summary>
        /// Optionales Speicherfeld
        /// </summary>
        public object Tag { get; set; } = null;
        private GeoModel _model;
        internal Matrix4 _modelMatrix = Matrix4.Identity;
        private Quaternion _rotation = new Quaternion(0, 0, 0, 1);
        private Vector3 _scale = new Vector3(1, 1, 1);
        /// <summary>
        /// Rotation (als Quaternion)
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                return _rotation;
            }
            internal set
            {
                CheckModel();
                _rotation = value;
                UpdateModelMatrixAndHitboxes();
                UpdateLookAtVector();
            }
        }

        /// <summary>
        /// Gibt die Rotation des First-Person-Objekts zurück
        /// </summary>
        public Quaternion RotationFirstPersonObject
        {
            get
            {
                if (CurrentWorld != null && CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
                {
                    return Quaternion.FromMatrix(HelperCamera.GetViewMatrixInversed());
                }
                else
                    return Rotation;

            }
        }

        internal Quaternion GetRotationNoFPSMode()
        {
            return _rotation;
        }

        internal Vector3 _sceneCenter = new Vector3(0, 0, 0);
        internal Vector3 _sceneDimensions = new Vector3(0, 0, 0);
        internal float _sceneDiameter = 1;

        private Vector3 _position = new Vector3(0, 0, 0);
        /// <summary>
        /// Position des Objekts
        /// </summary>
        public Vector3 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                UpdateModelMatrixAndHitboxes();
            }
        }

        /// <summary>
        /// Setzt die Skalierung des Objekts
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        public void SetScale(float x, float y, float z)
        {
            Scale = new Vector3(x, y, z);
        }

        /// <summary>
        /// Setzt die Skalierung des Objekts
        /// </summary>
        /// <param name="scale"></param>
        public void SetScale(float scale)
        {
            Scale = new Vector3(scale, scale, scale);
        }

        /// <summary>
        /// Skalierung des Objekts
        /// </summary>
        public Vector3 Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                CheckModel();
                if (value.X > 0 && value.Y > 0 && value.Z > 0)
                {
                    _scale = value;
                }
                else
                {
                    _scale = new Vector3(1, 1, 1);
                    Debug.WriteLine("Scale must be > 0 in all dimensions. Resetting to 1.");
                }
                UpdateModelMatrixAndHitboxes();
            }
        }
        private string _name = "undefined gameobject name";
        /// <summary>
        /// Name des Objekts
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value != null && value.Length > 0 ? value : "undefined gameobject name";
            }
        }



        internal Hitbox GetLargestHitbox()
        {
            return Hitboxes[_largestHitboxIndex];
        }

        internal Matrix4 CreateModelMatrix(ref Vector3 s, ref Quaternion r, ref Vector3 t)
        {
            Matrix4 m = Matrix4.CreateFromQuaternion(r);

            m.Row0 *= s.X;
            m.Row1 *= s.Y;
            m.Row2 *= s.Z;

            m.Row3.X = t.X;
            m.Row3.Y = t.Y;
            m.Row3.Z = t.Z;
            m.Row3.W = 1.0f;

            return m;
        }

        internal void UpdateModelMatrixAndHitboxes()
        {
            _modelMatrix = CreateModelMatrix(ref _scale, ref _rotation, ref _position);
            Vector3 sceneCenter = new Vector3(0, 0, 0);
            Vector3 tmpDims = new Vector3(0, 0, 0);
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            int minusHitboxes = 0;
            foreach (Hitbox h in Hitboxes)
            {
                if (!h.IsActive)
                {
                    minusHitboxes++;
                }
                else
                {
                    Vector3 localCenter = h.Update(ref tmpDims);
                    sceneCenter += localCenter;

                    if (localCenter.X + tmpDims.X / 2 > maxX)
                        maxX = localCenter.X + tmpDims.X / 2;
                    if (localCenter.X - tmpDims.X / 2 < minX)
                        minX = localCenter.X - tmpDims.X / 2;

                    if (localCenter.Y + tmpDims.Y / 2 > maxY)
                        maxY = localCenter.Y + tmpDims.Y / 2;
                    if (localCenter.Y - tmpDims.Y / 2 < minY)
                        minY = localCenter.Y - tmpDims.Y / 2;

                    if (localCenter.Z + tmpDims.Z / 2 > maxZ)
                        maxZ = localCenter.Z + tmpDims.Z / 2;
                    if (localCenter.Z - tmpDims.Z / 2 < minZ)
                        minZ = localCenter.Z - tmpDims.Z / 2;
                }
            }
            _sceneCenter.X = sceneCenter.X / (Hitboxes.Count - minusHitboxes);
            _sceneCenter.Y = sceneCenter.Y / (Hitboxes.Count - minusHitboxes);
            _sceneCenter.Z = sceneCenter.Z / (Hitboxes.Count - minusHitboxes);
            LeftRightMost = new Vector2(minX - KWEngine.SweepAndPruneToleranceWidth, maxX + KWEngine.SweepAndPruneToleranceWidth);
            BackFrontMost = new Vector2(minZ - KWEngine.SweepAndPruneToleranceWidth, maxZ + KWEngine.SweepAndPruneToleranceWidth);
            BottomTopMost = new Vector2(minY - KWEngine.SweepAndPruneToleranceWidth, maxY + KWEngine.SweepAndPruneToleranceWidth);
            _sceneDimensions = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
            Vector3 downLeftFront = new Vector3(GetCenterPointForAllHitboxes().X - GetMaxDimensions().X / 2, GetCenterPointForAllHitboxes().Y - GetMaxDimensions().Y / 2, GetCenterPointForAllHitboxes().Z + GetMaxDimensions().Z / 2);
            Vector3 upRightBack = new Vector3(GetCenterPointForAllHitboxes().X + GetMaxDimensions().X / 2, GetCenterPointForAllHitboxes().Y + GetMaxDimensions().Y / 2, GetCenterPointForAllHitboxes().Z - GetMaxDimensions().Z / 2);
            _sceneDiameter = (upRightBack - downLeftFront).LengthFast;

            if (KWEngine.CurrentWorld != null)
                DistanceToCamera = (uint)((KWEngine.CurrentWorld.GetCameraPosition() - GetCenterPointForAllHitboxes()).LengthSquared * 10000);
        }

        internal Vector2 GetExtentsForAxis(int a)
        {
            if (a == 1)
                return BottomTopMost;
            else if (a == 2)
                return BackFrontMost;
            else
                return LeftRightMost;

        }

        /// <summary>
        /// Berechnet das Zentrum des Objekts (über alle Hitboxen)
        /// </summary>
        /// <returns>Zentrum</returns>
        public Vector3 GetCenterPointForAllHitboxes()
        {
            return _sceneCenter;
        }

        /// <summary>
        /// Erfragt die Maße des Objekts
        /// </summary>
        /// <returns>Maße (Breite, Höhe, Tiefe)</returns>
        public Vector3 GetMaxDimensions()
        {
            return _sceneDimensions;
        }

        /// <summary>
        /// Erfragt den maximalen Durchmesser des Objekts
        /// </summary>
        /// <returns>Durchmesser</returns>
        public float GetMaxDiameter()
        {
            return _sceneDiameter;
        }

        /// <summary>
        /// Bewegt das Blickfeld des FP-Objekts gemäß der Mauszeigerposition
        /// </summary>
        /// <param name="ms">Mausinformation</param>
        protected void MoveFPSCamera(MouseState ms)
        {
            CheckModel();

            if (CurrentWorld.IsFirstPersonMode)
            {
                if (CurrentWorld.DebugShadowLight == null)
                {
                    int centerX = CurrentWindow.X + CurrentWindow.Width / 2;
                    int centerY = CurrentWindow.Y + CurrentWindow.Height / 2;
                    HelperCamera.AddRotation(-(ms.X - centerX) * Math.Abs(KWEngine.MouseSensitivity), (centerY - ms.Y) * KWEngine.MouseSensitivity);
                }
            }
            else
                HelperGL.ShowErrorAndQuit("Fatal error!", "First Person Mode is not active!");
        }

        /// <summary>
        /// Gibt an, ob das Objekt ein Modell hat
        /// </summary>
        public bool HasModel
        {
            get
            {
                return (_model != null && _model.IsValid);
            }
        }

        /// <summary>
        /// Gibt das aktuelle Modell zurück
        /// </summary>
        public GeoModel Model
        {
            get
            {
                return _model;
            }
        }

        /// <summary>
        /// Setzt das Modell des Objekts
        /// </summary>
        /// <param name="m">Modellname</param>
        public void SetModel(string m)
        {
            SetModel(KWEngine.GetModel(m));
        }

        private void CopyMaterialsFromMeshes()
        {
            _materials = new GeoMaterial[_model.Meshes.Values.Count];
            for (int i = 0; i < _materials.Length; i++)
            {
                _materials[i] = _model.Meshes.Values.ElementAt(i).Material;
            }
        }

        internal void SetModel(GeoModel m)
        {
            // is m null? throw exception then!
            if(m == null)
            {
                HelperGL.ShowErrorAndQuit("Fatal error!", "Invalid model.");
                return;
            }
            _model = m;

            CopyMaterialsFromMeshes();

            if (m.Name == "kwcube.obj")
            {
                ModelMatrixForRenderPass = new Matrix4[1];
                
            }
            else if (m.Name == "kwcube6.obj")
            {
                ModelMatrixForRenderPass = new Matrix4[1];
            }
            else
            {
                //Init:
                List<string> l = new List<string>();
                foreach (GeoMesh mesh in _model.Meshes.Values)
                    l.Add(mesh.Name);
                _meshNameList = l.AsReadOnly();
                ModelMatrixForRenderPass = new Matrix4[_meshNameList.Count];
            }

            for (int i = 0; i < ModelMatrixForRenderPass.Length; i++)
            {
                ModelMatrixForRenderPass[i] = Matrix4.Identity;
            }

            int hIndex = 0;
            float diameter = float.MinValue;
            foreach (GeoMeshHitbox gmh in m.MeshHitboxes)
            {
                Hitbox h = new Hitbox(this, gmh);
                Hitboxes.Add(h);
                if (h.DiameterFull > diameter)
                {
                    diameter = h.DiameterFull;
                    _largestHitboxIndex = hIndex;
                }
                hIndex++;
            }

            if (m.HasBones)
            {
                BoneTranslationMatrices = new Dictionary<string, Matrix4[]>();
                foreach (GeoMesh mesh in m.Meshes.Values)
                {
                    BoneTranslationMatrices[mesh.Name] = new Matrix4[mesh.BoneIndices.Count];
                    for (int i = 0; i < mesh.BoneIndices.Count; i++)
                        BoneTranslationMatrices[mesh.Name][i] = Matrix4.Identity;
                }
            }

        }

        /// <summary>
        /// Act-Methode
        /// </summary>
        /// <param name="ks">Keyboardinfos</param>
        /// <param name="ms">Mausinfos</param>
        public abstract void Act(KeyboardState ks, MouseState ms);

        #region Gameplay
        /// <summary>
        /// Erfragt die aktuelle Rotation in Eulerschen Winkeln
        /// </summary>
        /// <returns>Euler-Winkel</returns>
        public Vector3 GetRotationEulerAngles()
        {
            return HelperRotation.ConvertQuaternionToEulerAngles(Rotation);

        }

        /// <summary>
        /// Setzt die Rotation des Objekts
        /// </summary>
        /// <param name="rotation">Rotation (als Quaternion)</param>
        public void SetRotation(Quaternion rotation)
        {
            Rotation = rotation;
        }

        /// <summary>
        /// Prüft, ob das Objekt in Richtung des gegebenen Punkts blickt
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="z">z</param>
        /// <param name="diameter">Durchmesser um den Punkt</param>
        /// <returns></returns>
        protected bool IsLookingAt(float x, float y, float z, float diameter)
        {
            return IsLookingAt(new Vector3(x, y, z), diameter);
        }

        /// <summary>
        /// Prüft, ob das Objekt in Richtung des gegebenen Punkts blickt
        /// </summary>
        /// <param name="target">Zielposition</param>
        /// <param name="diameter">Durchmesser um das Ziel</param>
        /// <returns></returns>
        protected bool IsLookingAt(Vector3 target, float diameter)
        {
            CheckModel();

            if (Model.IsTerrain)
            {
                HelperGL.ShowErrorAndQuit("GameObject::IsLookingAt()", "Terrains cannot 'look' at objects.");
            }

            Vector3 position = GetCenterPointForAllHitboxes();
            if (CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
            {
                position = new Vector3(Position.X, Position.Y + FPSEyeOffset, Position.Z);
            }
            Vector3 deltaGO = target - position;
            Vector3 rayDirection = GetLookAtVector();
            Vector3[] aabb = new Vector3[] { new Vector3(-0.5f * diameter, -0.5f * diameter, -0.5f * diameter), new Vector3(0.5f * diameter, 0.5f * diameter, 0.5f * diameter) };

            Matrix4 matrix = Matrix4.CreateTranslation(target);
            Vector3 x = new Vector3(matrix.Row0);
            x.NormalizeFast();
            Vector3 y = new Vector3(matrix.Row1);
            y.NormalizeFast();
            Vector3 z = new Vector3(matrix.Row2);
            z.NormalizeFast();
            Vector3[] axes = new Vector3[] { x, y, z };

            float tMin = 0.0f;
            float tMax = 100000.0f;
            for (int i = 0; i < axes.Length; i++)
            {
                Vector3 axis = axes[i];

                Vector3.Dot(ref axis, ref deltaGO, out float e);
                Vector3.Dot(ref rayDirection, ref axis, out float f);
                float t1 = (e + aabb[0][i]) / f; // Intersection with the "left" plane
                float t2 = (e + aabb[1][i]) / f; // Intersection with the "right" plane
                if (t1 > t2)
                {
                    float w = t1;
                    t1 = t2;
                    t2 = w;
                }
                // tMax is the nearest "far" intersection (amongst the X,Y and Z planes pairs)
                if (t2 < tMax) tMax = t2;
                // tMin is the farthest "near" intersection (amongst the X,Y and Z planes pairs)
                if (t1 > tMin) tMin = t1;


                if (tMax < tMin)
                    return false;
            }
            return true;
        }


        internal void CheckBounds()
        {
            if (_sceneCenter.X > CurrentWorld.WorldCenter.X + CurrentWorld.WorldDistance
                || _sceneCenter.X < CurrentWorld.WorldCenter.X - CurrentWorld.WorldDistance

                || _sceneCenter.Y > CurrentWorld.WorldCenter.Y + CurrentWorld.WorldDistance
                || _sceneCenter.Y < CurrentWorld.WorldCenter.Y - CurrentWorld.WorldDistance

                || _sceneCenter.Z > CurrentWorld.WorldCenter.Z + CurrentWorld.WorldDistance
                || _sceneCenter.Z < CurrentWorld.WorldCenter.Z - CurrentWorld.WorldDistance
                )
            {
                Console.WriteLine("Object '" + this.Name + " " + this.Model.Name + "' position is beyond world's boundaries (currently: " + CurrentWorld.WorldDistance + " units from " + CurrentWorld.WorldCenter + "). Removing object.");
                CurrentWorld.RemoveGameObject(this);
            }
        }

        /// <summary>
        /// Setzt die Rotation des Objekts (Reihenfolge: Z->Y->X)
        /// </summary>
        /// <param name="x">X in Grad</param>
        /// <param name="y">Y in Grad</param>
        /// <param name="z">Z in Grad</param>
        public void SetRotation(float x, float y, float z)
        {
            if (Model.IsTerrain)
            {
                Debug.WriteLine("Setting rotation on a GeoTerrain instance is not supported.");
            }
            else if (CurrentWorld != null && CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
            {
                Debug.WriteLine("Setting rotation on a first person instance is not supported.");
            }
            else if (Model != null)
            {
                Quaternion tmpRotateX = Quaternion.FromAxisAngle(Vector3.UnitX, HelperRotation.CalculateRadiansFromDegrees(x));
                Quaternion tmpRotateY = Quaternion.FromAxisAngle(Vector3.UnitY, HelperRotation.CalculateRadiansFromDegrees(y));
                Quaternion tmpRotateZ = Quaternion.FromAxisAngle(Vector3.UnitZ, HelperRotation.CalculateRadiansFromDegrees(z));
                Rotation = tmpRotateZ * tmpRotateY * tmpRotateX;
            }
            else
            {
                HelperGL.ShowErrorAndQuit("GameObject::SetRotation()", "Cannot set rotation on empty geometry object. Did you assign an instance of GeometryObject to your GameObject instance?");
            }
        }

        /// <summary>
        /// Addiert eine Rotation hinzu
        /// </summary>
        /// <param name="r">Rotation (als Quaternion)</param>
        public void AddRotation(Quaternion r)
        {
            CheckModel();
            if (Model.IsTerrain)
            {
                Debug.WriteLine("Adding rotation for GeoTerrain instance is not supported.");
            }
            else if (CurrentWorld != null && CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
            {
                Debug.WriteLine("Setting rotation on a first person instance is not supported.");
            }
            else
            {
                Rotation *= r;
            }
        }

        /// <summary>
        /// Fügt eine Rotation um die X-Achse hinzu
        /// </summary>
        /// <param name="amount">Rotation in Grad</param>
        /// <param name="absolute">false für relative Drehung, true für eine Rotation um die Weltachse</param>
        public void AddRotationX(float amount, bool absolute = false)
        {
            CheckModel();
            if (Model.IsTerrain)
            {
                Debug.WriteLine("Adding rotation for GeoTerrain instance is not supported.");
            }
            else if (CurrentWorld != null && CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
            {
                Debug.WriteLine("Setting rotation on a first person instance is not supported.");
            }
            else
            {
                if (absolute)
                {
                    Quaternion tmpRotate = Quaternion.FromAxisAngle(Vector3.UnitX, HelperRotation.CalculateRadiansFromDegrees(amount));
                    Rotation = tmpRotate * Rotation;
                }
                else
                {
                    Quaternion tmpRotate = Quaternion.FromAxisAngle(Vector3.UnitX, HelperRotation.CalculateRadiansFromDegrees(amount));
                    Rotation *= tmpRotate;
                }
            }
        }

        /// <summary>
        /// Fügt eine Rotation um die Y-Achse hinzu
        /// </summary>
        /// <param name="amount">Rotation in Grad</param>
        /// <param name="absolute">false für relative Drehung, true für eine Rotation um die Weltachse</param>
        public void AddRotationY(float amount, bool absolute = false)
        {
            CheckModel();
            if (Model.IsTerrain)
            {
                Debug.WriteLine("Adding rotation for GeoTerrain instance is not supported.");
            }
            else
            {

                if (absolute)
                {
                    Quaternion tmpRotate = Quaternion.FromAxisAngle(Vector3.UnitY, HelperRotation.CalculateRadiansFromDegrees(amount));
                    Rotation = tmpRotate * Rotation;
                }
                else
                {
                    Quaternion tmpRotate = Quaternion.FromAxisAngle(Vector3.UnitY, HelperRotation.CalculateRadiansFromDegrees(amount));
                    Rotation *= tmpRotate;
                }
            }
        }

        /// <summary>
        /// Fügt eine Rotation um die Z-Achse hinzu
        /// </summary>
        /// <param name="amount">Rotation in Grad</param>
        /// <param name="absolute">false für relative Drehung, true für eine Rotation um die Weltachse</param>
        public void AddRotationZ(float amount, bool absolute = false)
        {
            CheckModel();
            if (Model.IsTerrain)
            {
                Debug.WriteLine("Adding rotation for GeoTerrain instance is not supported.");
            }
            else if (CurrentWorld != null && CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
            {
                Debug.WriteLine("Setting rotation on a first person instance is not supported.");
            }
            else
            {
                if (absolute)
                {
                    Quaternion tmpRotate = Quaternion.FromAxisAngle(Vector3.UnitZ, HelperRotation.CalculateRadiansFromDegrees(amount));
                    Rotation = tmpRotate * Rotation;
                }
                else
                {
                    Quaternion tmpRotate = Quaternion.FromAxisAngle(Vector3.UnitZ, HelperRotation.CalculateRadiansFromDegrees(amount));
                    Rotation *= tmpRotate;
                }
            }
        }

        /// <summary>
        /// Setzt die x-Position der Instanz auf den gegebenen Wert
        /// </summary>
        /// <param name="x">Positionswert</param>
        public void SetPositionX(float x)
        {
            SetPosition(new Vector3(x, Position.Y, Position.Z));
        }

        /// <summary>
        /// Setzt die y-Position der Instanz auf den gegebenen Wert
        /// </summary>
        /// <param name="y">Positionswert</param>
        public void SetPositionY(float y)
        {
            SetPosition(new Vector3(Position.X, y, Position.Z));
        }

        /// <summary>
        /// Setzt die z-Position der Instanz auf den gegebenen Wert
        /// </summary>
        /// <param name="z">Positionswert</param>
        public void SetPositionZ(float z)
        {
            SetPosition(new Vector3(Position.X, Position.Y, z));
        }

        /// <summary>
        /// Setzt die Position des Objekts
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="z">z</param>
        public void SetPosition(float x, float y, float z)
        {
            CheckModel();
            SetPosition(new Vector3(x, y, z));
        }

        /// <summary>
        /// Setzt die Position des Objekts
        /// </summary>
        /// <param name="newPosition">neue Position</param>
        public void SetPosition(Vector3 newPosition)
        {
            if (Model != null)
            {
                Position = new Vector3(newPosition);
            }
            else
                HelperGL.ShowErrorAndQuit("GameObject::SetPosition()", "Cannot set position on empty model object. Did you assign an instance of GeoModel to your GameObject via SetModel() ?");
        }

        /// <summary>
        /// Setzt die Färbung des Objekts
        /// </summary>
        /// <param name="red">Rot</param>
        /// <param name="green">Grün</param>
        /// <param name="blue">Blau</param>
        public void SetColor(float red, float green, float blue)
        {
            _tintColor.X = red >= 0 && red <= 1 ? red : 1;
            _tintColor.Y = green >= 0 && green <= 1 ? green : 1;
            _tintColor.Z = blue >= 0 && blue <= 1 ? blue : 1;
        }

        /// <summary>
        /// Strahlfarbe
        /// </summary>
        /// <param name="red">Rot</param>
        /// <param name="green">Grün</param>
        /// <param name="blue">Blau</param>
        /// <param name="intensity">Intensität</param>
        public void SetColorEmissive(float red, float green, float blue, float intensity)
        {
            ColorEmissive = new Vector4(red, green, blue, intensity);
        }

        private void UpdateLookAtVector()
        {
            if (CurrentWorld != null && CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
            {
                _lookAtVector = HelperCamera.GetLookAtVector();
            }
            else
            {
                Vector3 standardOrientation = Vector3.UnitZ;
                Vector3 rotatedNormal = Vector3.TransformNormal(standardOrientation, _modelMatrix);
                rotatedNormal.NormalizeFast();
                _lookAtVector = rotatedNormal;
            }
        }

        /// <summary>
        /// Erfragt den normalisierten Blickrichtungsvektor
        /// </summary>
        /// <returns>Blickrichtungsvektor (normalisiert)</returns>
        public Vector3 GetLookAtVector()
        {
            CheckModelAndWorld();
            return _lookAtVector;
        }

        /// <summary>
        /// Bewegt das Objekt in Blickrichtung
        /// </summary>
        /// <param name="amount">Anzahl der Bewegungseinheiten</param>
        public void Move(float amount)
        {
            Position += Vector3.Multiply(GetLookAtVector(), amount);
        }

        /// <summary>
        /// Bewegt das Objekt in Blickrichtung (ohne Höhenunterschied)
        /// </summary>
        /// <param name="amount">Anzahl der Bewegungseinheiten</param>
        public void MoveXZ(float amount)
        {
            Vector3 tmp = GetLookAtVector();
            tmp.Y = 0;

            Position += Vector3.Multiply(tmp, amount);
        }

        /// <summary>
        /// Bewegt das Objekt relativ zur aktuellen Position entlang der gegebenen Achsen
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="z">z</param>
        public void MoveOffset(float x, float y, float z)
        {
            Position = new Vector3(Position.X + x, Position.Y + y, Position.Z + z);
        }

        /// <summary>
        /// Bewegt das Objekt relativ zur aktuellen Position entlang der gegebenen Achsen
        /// </summary>
        /// <param name="offset">Offset-Vektor</param>
        public void MoveOffset(Vector3 offset)
        {
            Position = new Vector3(Position.X + offset.X, Position.Y + offset.Y, Position.Z + offset.Z);
        }

        /// <summary>
        /// Führt Bewegungen für den First-Person-Modus aus
        /// </summary>
        /// <param name="forward">Vorwärtsanteil (-1 bis +1)</param>
        /// <param name="strafe">Seitwärtsanteil (-1 bis +1)</param>
        /// <param name="units">Einheitenmultiplikator (z.B. 0.5)</param>
        protected void MoveAndStrafeFirstPersonXYZ(float forward, float strafe, float units)
        {
            CheckModel();
            MoveAndStrafeFirstPersonXYZ(new Vector2(forward, strafe), units);
        }

        /// <summary>
        /// Bewegt das Objekt um die gegebenen Einheiten entlang eines Vektors
        /// </summary>
        /// <param name="v">Richtungsvektor</param>
        /// <param name="units">Bewegungseinheiten</param>
        public void MoveAlongVector(Vector3 v, float units)
        {
            Position = new Vector3(Position.X + v.X * units, Position.Y + v.Y * units, Position.Z + v.Z * units);
        }

        private void MoveAndStrafeFirstPersonXYZ(Vector2? direction, float units)
        {
            CheckModel();
            if (direction == null || !direction.HasValue)
                return;

            if (CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
            {
                direction.Value.NormalizeFast();
                Vector3 moveVector = HelperCamera.MoveXYZ(direction.Value.X, direction.Value.Y, 1f);

                MoveOffset(moveVector.X * units, moveVector.Y * units, moveVector.Z * units);
            }
            else
            {
                HelperGL.ShowErrorAndQuit("GameObject::MoveAndStrafeFirstPersonXYZ()", "MoveAndStrafeFirstPerson() may only be called from the current FPS object.");
            }
        }

        /// <summary>
        /// Führt Bewegungen für den First-Person-Modus aus (ohne Höhenveränderung)
        /// </summary>
        /// <param name="forward">Vorwärtsanteil (-1 bis +1)</param>
        /// <param name="strafe">Seitwärtsanteil (-1 bis +1)</param>
        /// <param name="units">Einheitenmultiplikator (z.B. 0.5)</param>
        protected void MoveAndStrafeFirstPerson(float forward, float strafe, float units)
        {
            MoveAndStrafeFirstPerson(new Vector2(forward, strafe), units);
        }


        private void MoveAndStrafeFirstPerson(Vector2 direction, float units)
        {
            CheckModel();

            if (CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
            {
                direction.NormalizeFast();
                Vector3 moveVector = HelperCamera.MoveXZ(direction.X, direction.Y, 1f);
                MoveOffset(moveVector.X * units, moveVector.Y * units, moveVector.Z * units);
            }
            else
            {
                HelperGL.ShowErrorAndQuit("GameObject::MoveAndStrafeFirstPersonXZ()", "MoveAndStrafeFirstPerson() may only be called from the current FPS object.");
            }
        }

        #endregion

        internal void ProcessCurrentAnimation()
        {
            if (AnimationID >= 0 && AnimationID < Model.Animations.Count)
            {
                GeoAnimation a = Model.Animations[AnimationID];
                Matrix4 identity = Matrix4.Identity;
                ReadNodeHierarchy(a.DurationInTicks * AnimationPercentage, ref a, AnimationID, Model.Root, ref identity);
            }
        }

        private void ReadNodeHierarchy(float timestamp, ref GeoAnimation animation, int animationId, GeoNode node, ref Matrix4 parentTransform)
        {
            animation.AnimationChannels.TryGetValue(node.Name, out GeoNodeAnimationChannel channel);
            Matrix4 nodeTransformation = node.Transform;

            if (channel != null)
            {
                Vector3 s = channel.ScaleKeys == null ? nodeTransformation.ExtractScale() : CalcInterpolatedScaling(timestamp, ref channel);
                Quaternion r = channel.RotationKeys == null ? nodeTransformation.ExtractRotation() : CalcInterpolatedRotation(timestamp, ref channel);
                Vector3 t = channel.TranslationKeys == null ? nodeTransformation.ExtractTranslation() : CalcInterpolatedTranslation(timestamp, ref channel);

                nodeTransformation = CreateModelMatrix(ref s, ref r, ref t);
            }
            Matrix4 globalTransform = nodeTransformation * parentTransform;

            if (channel != null)
            {
                foreach (GeoMesh mesh in Model.Meshes.Values)
                {
                    int index = mesh.BoneNames.IndexOf(node.Name);
                    if (index >= 0)
                    {
                        BoneTranslationMatrices[mesh.Name][index] = mesh.BoneOffset[index] * globalTransform * Model.TransformGlobalInverse;
                    }
                }
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                ReadNodeHierarchy(timestamp, ref animation, animationId, node.Children[i], ref globalTransform);
            }

        }

        private Vector3 CalcInterpolatedScaling(float timestamp, ref GeoNodeAnimationChannel channel)
        {
            if (channel.ScaleKeys == null)
            {
                return new Vector3(1, 1, 1);
            }
            if (channel.ScaleKeys.Count == 1)
            {
                return channel.ScaleKeys[0].Scale;
            }

            for (int i = 0; i < channel.ScaleKeys.Count - 1; i++)
            {
                GeoAnimationKeyframe key = channel.ScaleKeys[i];
                if (timestamp < channel.ScaleKeys[0].Time)
                {
                    return key.Scale;
                }
                else if (timestamp == key.Time)
                {
                    return key.Scale;
                }

                else
                {
                    if (timestamp >= key.Time && timestamp <= channel.ScaleKeys[i + 1].Time)
                    {
                        GeoAnimationKeyframe key2 = channel.ScaleKeys[i + 1];

                        float deltaTime = (key2.Time - key.Time);
                        float factor = (timestamp - key.Time) / deltaTime;
                        if (factor < 0 || factor > 1)
                        {
                            throw new Exception("Error mapping animation timestamps. Delta time not valid.");
                        }

                        Vector3 scalingStart = key.Scale;
                        Vector3 scalingEnd = key2.Scale;
                        Vector3.Lerp(ref scalingStart, ref scalingEnd, factor, out Vector3 scaling);
                        return scaling;
                    }
                }
            }
            Debug.WriteLine("Error finding scaling timestamp for animation cycle.");
            return new Vector3(1, 1, 1);
        }

        private Vector3 CalcInterpolatedTranslation(float timestamp, ref GeoNodeAnimationChannel channel)
        {
            if (channel.TranslationKeys == null)
            {
                return new Vector3(0, 0, 0);
            }
            if (channel.TranslationKeys.Count == 1)
            {
                return channel.TranslationKeys[0].Translation;
            }

            for (int i = 0; i < channel.TranslationKeys.Count - 1; i++)
            {
                GeoAnimationKeyframe key = channel.TranslationKeys[i];
                if (timestamp < channel.TranslationKeys[0].Time)
                {
                    return key.Translation;
                }
                else if (timestamp == key.Time)
                {
                    return key.Translation;
                }

                else
                {
                    if (timestamp >= key.Time && timestamp <= channel.TranslationKeys[i + 1].Time)
                    {
                        GeoAnimationKeyframe key2 = channel.TranslationKeys[i + 1];

                        float deltaTime = (key2.Time - key.Time);
                        float factor = (timestamp - key.Time) / deltaTime;
                        if (factor < 0 || factor > 1)
                        {
                            throw new Exception("Error mapping animation timestamps. Delta time not valid.");
                        }

                        Vector3 transStart = key.Translation;
                        Vector3 transEnd = key2.Translation;
                        Vector3.Lerp(ref transStart, ref transEnd, factor, out Vector3 trans);
                        return trans;
                    }
                }
            }
            Debug.WriteLine("Error finding translation timestamp for animation cycle.");
            return new Vector3(0, 0, 0);
        }

        private Quaternion CalcInterpolatedRotation(float timestamp, ref GeoNodeAnimationChannel channel)
        {
            if (channel.RotationKeys == null)
            {
                return new Quaternion(0, 0, 0, 1);
            }
            if (channel.RotationKeys.Count == 1)
            {
                return channel.RotationKeys[0].Rotation;
            }
            for (int i = 0; i < channel.RotationKeys.Count - 1; i++)
            {
                if (timestamp < channel.RotationKeys[0].Time)
                {
                    return channel.RotationKeys[i].Rotation;
                }
                else if (timestamp == channel.RotationKeys[i].Time)
                {
                    return channel.RotationKeys[i].Rotation;
                }
                else
                {
                    if (timestamp >= channel.RotationKeys[i].Time && timestamp <= channel.RotationKeys[i + 1].Time)
                    {
                        GeoAnimationKeyframe key2 = channel.RotationKeys[i + 1];

                        float deltaTime = key2.Time - channel.RotationKeys[i].Time;
                        float factor = (timestamp - channel.RotationKeys[i].Time) / deltaTime;
                        if (factor < 0 || factor > 1)
                        {
                            throw new Exception("Error mapping animation timestamps. Delta time not valid.");
                        }

                        OpenTK.Quaternion rotationStart = channel.RotationKeys[i].Rotation;
                        OpenTK.Quaternion rotationEnd = key2.Rotation;
                        Quaternion rotation = OpenTK.Quaternion.Slerp(rotationStart, rotationEnd, factor);
                        rotation.Normalize();
                        return rotation;
                    }

                }
            }
            Debug.WriteLine("Error finding rotation timestamp for animation cycle.");
            return new Quaternion(0, 0, 0, 1);
        }

        private void CheckModelAndWorld()
        {
            if (Model == null || GLWindow.CurrentWindow.CurrentWorld == null)
            {
                HelperGL.ShowErrorAndQuit("- GameObject::CheckModelAndWorld()", "Model and/or World have not been set yet!");
            }
        }

        private void CheckModel()
        {
            if (Model == null)
            {
                HelperGL.ShowErrorAndQuit("- GameObject::CheckModel()", "Model has not been set yet!");
            }
        }

        /// <summary>
        /// Aktuelle Systemzeit in Millisekunden
        /// </summary>
        /// <returns>Systemzeit in ms</returns>
        public static long GetCurrentTimeInMilliseconds()
        {
            return DeltaTime.Watch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Ermittelt ein Objekt, das mit dem aufrufenden Objekt kollidiert
        /// </summary>
        /// <param name="offsetX">Versatz in X-Richtung</param>
        /// <param name="offsetY">Versatz in Y-Richtung</param>
        /// <param name="offsetZ">Versatz in Z-Richtung</param>
        /// <param name="types">Liste der Klassennamen, auf die geprüft werden soll</param>
        protected Intersection GetIntersection(float offsetX, float offsetY, float offsetZ, params Type[] types)
        {
            CheckModelAndWorld();
            if (!IsCollisionObject)
            {
                HelperGL.ShowErrorAndQuit("GameObject::GetIntersection()", "You are calling collision detection on an instance that is marked as a non-colliding object.");
            }
            if (_collisionCandidates.Count == 0)
            {
                return null;
            }

            Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);


            //foreach (GameObject go in CurrentWorld.GetGameObjects())
            foreach (GameObject go in _collisionCandidates)
            {
                if (!go.IsCollisionObject || go.Equals(this)) // skip this line of s&p is working
                {
                    continue;
                }
                bool found = false;
                foreach (Type t in types)
                    if (go.GetType().Equals(t) || go.GetType().IsSubclassOf(t))
                        found = true;
                if (!found)
                    continue;

                foreach (Hitbox hbother in go.Hitboxes)
                {
                    if (!hbother.IsActive)
                        continue;

                    foreach (Hitbox hbcaller in this.Hitboxes)
                    {
                        if (!hbcaller.IsActive)
                            continue;

                        Intersection i;
                        if (hbother.Owner.Model.IsTerrain)
                        {
                            i = Hitbox.TestIntersectionTerrain(hbcaller, hbother, offset);
                        }
                        else
                        {
                            i = Hitbox.TestIntersection(hbcaller, hbother, offset);
                        }

                        if (i != null)
                        {
                            return i;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Ermittelt ein Objekt, das mit dem aufrufenden Objekt kollidiert
        /// </summary>
        /// <param name="offsetX">Versatz in X-Richtung (optional)</param>
        /// <param name="offsetY">Versatz in Y-Richtung (optional)</param>
        /// <param name="offsetZ">Versatz in Z-Richtung (optional)</param>
        protected Intersection GetIntersection(float offsetX = 0, float offsetY = 0, float offsetZ = 0)
        {
            CheckModelAndWorld();
            if (!IsCollisionObject)
            {
                HelperGL.ShowErrorAndQuit("GameObject::GetIntersection()", "You are calling collision detection on an instance that is marked as a non-colliding object.");
            }
            if (_collisionCandidates.Count == 0)
            {
                return null;
            }

            Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);
            foreach (GameObject go in _collisionCandidates)
            {
                if (!go.IsCollisionObject || go.Equals(this)) // maybe skip this line?
                {
                    continue;
                }

                foreach (Hitbox hbother in go.Hitboxes)
                {
                    if (!hbother.IsActive)
                        continue;

                    foreach (Hitbox hbcaller in this.Hitboxes)
                    {
                        if (!hbcaller.IsActive)
                            continue;
                        Intersection i;
                        if (hbother.Owner.Model.IsTerrain)
                        {
                            i = Hitbox.TestIntersectionTerrain(hbcaller, hbother, offset);
                        }
                        else
                        {
                            i = Hitbox.TestIntersection(hbcaller, hbother, offset);
                        }

                        if (i != null)
                        {
                            return i;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Erfragt eine Liste aller Objekte, die mit dem aufrufenden Objekt kollidieren
        /// </summary>
        /// <param name="offsetX">Versatz in X-Richtung</param>
        /// <param name="offsetY">Versatz in Y-Richtung</param>
        /// <param name="offsetZ">Versatz in Z-Richtung</param>
        /// <param name="types">Liste der Klassennamen, auf die geprüft werden soll</param>
        /// <returns></returns>
        protected List<Intersection> GetIntersections(float offsetX, float offsetY, float offsetZ, params Type[] types)
        {
            CheckModelAndWorld();
            List<Intersection> intersections = new List<Intersection>();
            if (!IsCollisionObject)
            {
                HelperGL.ShowErrorAndQuit("GameObject::GetIntersections()", "You are calling GetIntersections() on an instance that is marked as a non-colliding object.");
                return intersections;
            }
            if (_collisionCandidates.Count == 0)
            {
                return intersections;
            }

            Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);

            //Objekte außerhalb der Reichweite ausfiltern:
            foreach (GameObject go in _collisionCandidates)
            {
                if (!go.IsCollisionObject || go.Equals(this))
                {
                    continue;
                }

                bool found = false;
                foreach (Type t in types)
                    if (go.GetType().Equals(t) || go.GetType().IsSubclassOf(t))
                        found = true;
                if (!found)
                    continue;

                foreach (Hitbox hbother in go.Hitboxes)
                {
                    if (!hbother.IsActive)
                        continue;

                    foreach (Hitbox hbcaller in this.Hitboxes)
                    {
                        if (!hbcaller.IsActive)
                            continue;

                        Intersection i;
                        if (hbother.Owner.Model.IsTerrain)
                        {
                            i = Hitbox.TestIntersectionTerrain(hbcaller, hbother, offset);
                        }
                        else
                        {
                            i = Hitbox.TestIntersection(hbcaller, hbother, offset);
                        }

                        if (i != null)
                            intersections.Add(i);
                    }
                }
            }
            return intersections;
        }

        /// <summary>
        /// Erfragt eine Liste aller Objekte, die mit dem aufrufenden Objekt kollidieren
        /// </summary>
        /// <param name="offsetX">Versatz in X-Richtung (optional)</param>
        /// <param name="offsetY">Versatz in Y-Richtung (optional)</param>
        /// <param name="offsetZ">Versatz in Z-Richtung (optional)</param>
        /// <returns></returns>
        protected List<Intersection> GetIntersections(float offsetX = 0, float offsetY = 0, float offsetZ = 0)
        {
            CheckModelAndWorld();
            List<Intersection> intersections = new List<Intersection>();
            if (!IsCollisionObject)
            {
                HelperGL.ShowErrorAndQuit("GameObject::GetIntersections()", "You are calling collision detection on an instance that is marked as a non-colliding object.");
            }
            if (_collisionCandidates.Count == 0)
            {
                return intersections;
            }
            Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);

            //Objekte außerhalb der Reichweite ausfiltern:
            foreach (GameObject go in _collisionCandidates)
            {
                if (!go.IsCollisionObject || go.Equals(this)) // skip?
                {
                    continue;
                }

                foreach (Hitbox hbother in go.Hitboxes)
                {
                    if (!hbother.IsActive)
                        continue;

                    foreach (Hitbox hbcaller in this.Hitboxes)
                    {
                        if (!hbcaller.IsActive)
                            continue;

                        Intersection i;
                        if (hbother.Owner.Model.IsTerrain)
                        {
                            i = Hitbox.TestIntersectionTerrain(hbcaller, hbother, offset);
                        }
                        else
                        {
                            i = Hitbox.TestIntersection(hbcaller, hbother, offset);
                        }

                        if (i != null)
                            intersections.Add(i);
                    }
                }
            }
            return intersections;
        }

        /// <summary>
        /// Vergleichsmethode für den Tiefenpuffer
        /// </summary>
        /// <param name="obj">zu vergleichendes Objekt</param>
        /// <returns>1, wenn das zu vergleichende Objekt weiter weg von der Kamera ist als das aufrufende Objekt</returns>
        public int CompareTo(object obj)
        {
            GameObject g = (GameObject)obj;
            return g.DistanceToCamera > this.DistanceToCamera ? 1 : -1;
        }

        internal void SetTextureTerrainInternal(ref GeoTexture newTex, string texture)
        {
            lock (KWEngine.CustomTextures)
            {
                if (KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(texture))
                {
                    newTex.OpenGLID = KWEngine.CustomTextures[KWEngine.CurrentWorld][texture];
                }
                else
                {
                    int id = HelperTexture.LoadTextureForModelExternal(texture);
                    if (id > 0)
                    {
                        newTex.OpenGLID = id;
                        KWEngine.CustomTextures[KWEngine.CurrentWorld].Add(texture, id);
                    }
                    else
                    {
                        newTex.OpenGLID = KWEngine.TextureDefault;
                        Debug.WriteLine("Error loading texture for terrain '" + Model.Name + "': " + texture);
                    }
                }
            }
        }

        internal void SetTextureInternal(string texture, int meshIndex, TextureType type)
        {
            CheckModelAndWorld();

            int texId = -1;
            if (KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(texture))
            {
                texId = KWEngine.CustomTextures[KWEngine.CurrentWorld][texture];
            }
            else
            {
                texId = HelperTexture.LoadTextureForModelExternal(texture);
                if(texId > 0 && !KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(texture))
                {
                    KWEngine.CustomTextures[KWEngine.CurrentWorld].Add(texture, texId);
                }
            }

            if (texId >= 0)
                _materials[meshIndex].SetTexture(texture, type, texId);
        }

        internal void SetTextureInternal(string texture, TextureType type, CubeSide side)
        {
            CheckModelAndWorld();
            if (!(Model.IsTerrain || Model.Name == "kwsphere.obj" || Model.Name == "kwcube.obj" || Model.Name == "kwcube6.obj" || Model.Name == "kwrect.obj"))
            {
                HelperGL.ShowErrorAndQuit("GameObject::SetTexture()", "Cannot set texture on models other than KWCube, KWSphere or Terrain.");
                return;
            }

            if (Model.IsTerrain)
            {
                GeoMesh terrainMesh = Model.Meshes.Values.ElementAt(0);
                GeoTexture newTex = new GeoTexture()
                {
                    Filename = texture,
                    Type = type
                };

                if (KWEngine.CustomTextures.Count > 0)
                {
                    if (KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(texture))
                    {
                        newTex.OpenGLID = KWEngine.CustomTextures[KWEngine.CurrentWorld][texture];
                    }
                    else
                    {
                        SetTextureTerrainInternal(ref newTex, texture);
                    }
                    if (newTex.OpenGLID >= 0)
                    {
                        GeoMaterial mat = terrainMesh.Material;
                        mat.SetTexture(newTex.Filename, newTex.Type, newTex.OpenGLID);
                        terrainMesh.Material = mat;
                    }
                    else
                    {
                        HelperGL.ShowErrorAndQuit("GameObject::SetTexture()", "Texture " + texture + " not found.");
                        return;
                    }
                }
                else
                    return;
            }
            else
            {
                int texId = -1;
                if (KWEngine.CustomTextures.Count > 0)
                {
                    if (KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(texture))
                    {
                        texId = KWEngine.CustomTextures[KWEngine.CurrentWorld][texture];
                    }
                    else
                    {
                        texId = HelperTexture.LoadTextureForModelExternal(texture);
                        if (texId > 0 && !KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(texture))
                        {
                            KWEngine.CustomTextures[KWEngine.CurrentWorld].Add(texture, texId);
                        }
                    }
                }

                if (texId >= 0)
                {
                    if (side == CubeSide.All)
                    {
                        for(int i = 0; i < _materials.Length; i++)
                        {
                            _materials[i].SetTexture(texture, type, texId);
                        }
                    }
                    else
                    {
                        _materials[(int)side].SetTexture(texture, type, texId);
                    }
                }
            }
        }

        /// <summary>
        /// Aktiviert Lichtreflexionen bei glatten Oberflächen (niedrige Roughness)
        /// </summary>
        /// <param name="enabled">true (an), false  (aus)</param>
        public void SetSpecularReflectionEnabled(bool enabled)
        {
            if (Model == null)
            {
                HelperGL.ShowErrorAndQuit("GameObject::SetSpecularReflectionEnabled()", "Cannot set specular reflections - model is not set.");
                return;
            }

            if (Model.IsTerrain)
            {
                GeoMaterial terrainMaterial = Model.Meshes.Values.ElementAt(0).Material;
                terrainMaterial.SpecularReflection = enabled;
                Model.Meshes.Values.ElementAt(0).Material = terrainMaterial;
            }
            else
            {
                for (int i = 0; i < _materials.Length; i++)
                {
                    _materials[i].SpecularReflection = enabled;
                }
            }
        }

        /// <summary>
        /// Setzt die Rauheit der Objektoberfläche
        /// </summary>
        /// <param name="r">Rauheitsfaktor (Werte zwischen 0 und 1 sind erlaubt)</param>
        public void SetRoughness(float r)
        {
            if (Model == null)
            {
                HelperGL.ShowErrorAndQuit("GameObject::SetRoughness()", "Cannot set roughness - model is not set.");
                return;
            }

            r = HelperGL.Clamp(r, 0, 1);

            if (Model.IsTerrain)
            {
                GeoMaterial terrainMaterial = Model.Meshes.Values.ElementAt(0).Material;
                terrainMaterial.Roughness = r;
                Model.Meshes.Values.ElementAt(0).Material = terrainMaterial;
            }
            else
            {
                for (int i = 0; i < _materials.Length; i++)
                {
                    if(_materials[i].TextureRoughness.OpenGLID > 0)
                    {
                        HelperGL.ShowErrorAndQuit("GameObject::SetRoughness()", "Cannot set roughness - your model's roughness texture has priority!");
                        return;
                    }
                }

                for (int i = 0; i < _materials.Length; i++)
                {
                    _materials[i].Roughness = r;
                }
            }
        }

        /// <summary>
        /// Setzt die Metalligkeit der Objektoberfläche
        /// </summary>
        /// <param name="m">Metalligkeitsfaktor (Werte zwischen 0 und 1 sind erlaubt)</param>
        public void SetMetalness(float m)
        {
            if (Model == null)
            {
                HelperGL.ShowErrorAndQuit("GameObject::SetMetalness()", "Cannot set metalnass - model is empty.");
                return;
            }

            m = HelperGL.Clamp(m, 0, 1);
            if (Model.IsTerrain)
            {
                GeoMaterial terrainMaterial = Model.Meshes.Values.ElementAt(0).Material;
                terrainMaterial.Metalness = m;
                Model.Meshes.Values.ElementAt(0).Material = terrainMaterial;
            }
            else
            {
                for (int i = 0; i < _materials.Length; i++)
                {
                    if (_materials[i].TextureMetalness.OpenGLID > 0)
                    {
                        HelperGL.ShowErrorAndQuit("GameObject::SetMetalness()", "Cannot set metalnass - model metalness texture has priority.");
                        return;
                    }
                }

                for (int i = 0; i < _materials.Length; i++)
                {
                    _materials[i].Metalness = m;
                }
            }
        }

        /// <summary>
        /// Setzt die Textur für das Objekt (KWCube, KWSphere und Terrain)
        /// </summary>
        /// <param name="texture">Texturdatei</param>
        /// <param name="type">Art der Textur (Standard: Diffuse)</param>
        /// <param name="side">Seite des Würfels (für KWCube-Modelle)</param>
        public void SetTexture(string texture, TextureType type = TextureType.Albedo, CubeSide side = CubeSide.All)
        {
            if ((Model != null && Model.IsTerrain) || IsCubeOrSphere())
            {
                if (CurrentWindow._multithreaded)
                {
                    Action a = () => SetTextureInternal(texture, type, side);
                    HelperGLLoader.AddCall(this, a);
                }
                else
                    SetTextureInternal(texture, type, side);
            }
            else
            {
                SetTexture(texture, 0);
            }
        }

        /// <summary>
        /// Ändert die Albedo-Textur für das Objekt
        /// </summary>
        /// <param name="texture">Texturdatei</param>
        /// <param name="meshIndex">ID des Meshs im Model</param>
        /// <param name="type">Art der Textur</param>
        public void SetTexture(string texture, int meshIndex, TextureType type = TextureType.Albedo)
        {
            if (IsCubeOrSphere() || Model.IsTerrain)
            {
                SetTexture(texture);
            }
            else
            {
                if (CurrentWindow._multithreaded)
                {
                    Action a = () => SetTextureInternal(texture, meshIndex, type);
                    HelperGLLoader.AddCall(this, a);
                }
                else
                    SetTextureInternal(texture, meshIndex, type);
            }
        }

        /// <summary>
        /// Setzt die Texturwiederholungen für das Objekt
        /// </summary>
        /// <param name="x">Breitenwiederholungen</param>
        /// <param name="y">Höhenwiederholungen</param>
        /// <param name="side">Seite des Würfels (für KWCube6)</param>
        public void SetTextureRepeat(float x, float y, CubeSide side = CubeSide.All)
        {
            CheckModelAndWorld();
            CheckIfNotTerrain();
            if (!(Model.Name == "kwsphere.obj" || Model.Name == "kwcube.obj" || Model.Name == "kwcube6.obj" || Model.Name == "kwrect.obj"))
            {
                HelperGL.ShowErrorAndQuit("GameObject::SetTextureRepeat()", "Cannot set texture repeat values on models other than KWCube or KWSphere.");
                return;
                
            }

            
            if(side == CubeSide.All)
            {
                for(int i = 0; i < _materials.Length; i++)
                {
                    _materials[i].TextureAlbedo.UVTransform = new Vector2(x, y);
                }
            }
            else
            {
                if (Model.Name == "kwcube6.obj")
                {
                    _materials[(int)side].TextureAlbedo.UVTransform = new Vector2(x, y);
                }
                else
                {
                    for (int i = 0; i < _materials.Length; i++)
                    {
                        _materials[i].TextureAlbedo.UVTransform = new Vector2(x, y);
                    }
                }
            }
        }

        /// <summary>
        /// Erfragt den Blickrichtungsvektor der Kamera
        /// </summary>
        /// <returns>Blickrichtung</returns>
        protected Vector3 GetCameraLookAtVector()
        {
            if(CurrentWorld != null)
            {
                return CurrentWorld.GetCameraLookAtVector();
            }
            else
            {
                throw new Exception("Current world is null. Cannot compute camera vector.");
            }
        }

        /// <summary>
        /// Erfragt den Kollisionspunkt des Mauszeigers mit der 3D-Welt (auf Höhe des Aufrufers)
        /// </summary>
        /// <param name="ms">Mausinformationen</param>
        /// <param name="plane">Kollisionsebene (Standard: Camera)</param>
        /// <returns></returns>
        protected Vector3 GetMouseIntersectionPoint(MouseState ms, Plane plane = Plane.Camera)
        {
            if (CurrentWorld.IsFirstPersonMode)
            {
                HelperGL.ShowErrorAndQuit("GameObject::GetMouseIntersectionPoint()", "Method GetMouseIntersectionPoint() may not be called from First Person Mode object.");
                return Vector3.Zero;
            }
            Vector2 mc = HelperGL.GetNormalizedMouseCoords(ms.X, ms.Y, CurrentWindow);
            Vector3 worldRay = Get3DMouseCoords(mc.X, mc.Y);
            Vector3 normal;
            if (plane == Plane.Y)
            {
                normal = new Vector3(0, 1, 0.000001f);
            }
            else if (plane == Plane.X)
            {
                normal = new Vector3(1, 0, 0);
            }
            else if (plane == Plane.Z)
            {
                normal = new Vector3(0, 0.000001f, 1);
            }
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
            bool contact;
            Vector3 intersection;
            if (KWEngine.Projection == ProjectionType.Perspective)
            {
                contact = GameObject.LinePlaneIntersection(out intersection, worldRay, KWEngine.CurrentWorld.GetCameraPosition(), normal, Position);
            }
            else
            {
                Vector3 rayOrigin = HelperGL.GetRayOriginForOrthographicProjection(mc);
                contact = GameObject.LinePlaneIntersection(out intersection, worldRay, rayOrigin, normal, Position);
            }
            if (contact)
            {
                return intersection;
            }
            else
                return Position;
        }

        /// <summary>
        /// Erfragt, ob der Mauszeiger (näherungsweise) auf dem Objekt liegt 
        /// </summary>
        /// <param name="ms">Mausinformationen</param>
        /// <returns>true, wenn der Mauszeiger auf dem Objekt liegt</returns>
        public bool IsMouseCursorInsideMyHitbox(MouseState ms)
        {
            Vector2 mc = HelperGL.GetNormalizedMouseCoords(ms.X, ms.Y, CurrentWindow);
            Vector3 worldRay = Get3DMouseCoords(mc.X, mc.Y);
            Vector3 normal;
            bool result;
            Vector3 intersection;
            if (CurrentWorld != null && CurrentWorld.IsFirstPersonMode)
            {
                normal = HelperCamera.GetLookAtVector();
                normal.Y += 0.000001f;
                normal.Z += 0.000001f;
                if(KWEngine.Projection == ProjectionType.Perspective)
                {
                    Vector3 fpPos = CurrentWorld.GetFirstPersonObject().Position;
                    fpPos.Y += CurrentWorld.GetFirstPersonObject().FPSEyeOffset;
                    result = LinePlaneIntersection(out intersection, worldRay, fpPos, normal, GetCenterPointForAllHitboxes());
                }
                else
                {
                    Vector3 fpPos = HelperGL.GetRayOriginForOrthographicProjection(mc);
                    result = LinePlaneIntersection(out intersection, worldRay, fpPos, normal, GetCenterPointForAllHitboxes());
                }
            }
            else
            {
                normal = -GetCameraLookAtVector();
                normal.Y += 0.000001f;
                normal.Z += 0.000001f;

                if (KWEngine.Projection == ProjectionType.Perspective)
                {
                    result = LinePlaneIntersection(out intersection, worldRay, CurrentWorld.GetCameraPosition(), normal, GetCenterPointForAllHitboxes());
                }
                else
                {
                    Vector3 rayOrigin = HelperGL.GetRayOriginForOrthographicProjection(mc);
                    result = LinePlaneIntersection(out intersection, worldRay, rayOrigin, normal, GetCenterPointForAllHitboxes());
                }
                    
            }
            
            if (result)
            {
                foreach(Hitbox hb in Hitboxes)
                {
                    if(IsPointInsideBox(intersection, hb.GetCenter(), hb.DiameterAveraged))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
                return false;
        }

        private bool IsPointInsideBox(Vector3 pos, Vector3 center, float diameter)
        {
            return (
                pos.X >= center.X - diameter/ 2 &&
                pos.X <= center.X + diameter / 2 &&
                pos.Y >= center.Y - diameter / 2 &&
                pos.Y <= center.Y + diameter / 2 &&
                pos.Z >= center.Z - diameter / 2 &&
                pos.Z <= center.Z + diameter / 2
                );
        }
    
        internal static bool LinePlaneIntersection(out Vector3 contact, Vector3 ray, Vector3 rayOrigin,
                                            Vector3 normal, Vector3 coord)
        {
            contact = Vector3.Zero;
            float d = Vector3.Dot(normal, coord);
            if (Vector3.Dot(normal, ray) == 0)
            {
                return false;
            }
            float x = (d - Vector3.Dot(normal, rayOrigin)) / Vector3.Dot(normal, ray);
            contact = rayOrigin + ray * x;
            return true;
        }

        /// <summary>
        /// Erfragt die Rotation, die zu einem bestimmten Ziel notwendig wäre
        /// </summary>
        /// <param name="target">Ziel</param>
        /// <returns>Rotation (als Quaternion)</returns>
        protected Quaternion GetRotationToTarget(Vector3 target)
        {
            target.Z += 0.000001f;
            Matrix4 lookat = Matrix4.LookAt(target, Position, KWEngine.WorldUp);
            lookat.Transpose();
            Quaternion q = Quaternion.FromMatrix(new Matrix3(lookat));
            q.Invert();
            return q;
        }

        /// <summary>
        /// Dreht das Objekt, so dass es zur Zielkoordinate blickt
        /// </summary>
        /// <param name="target">Zielkoordinate</param>
        public void TurnTowardsXYZ(Vector3 target)
        {
            CheckModel();
            Rotation = GetRotationToTarget(target);
        }

        /// <summary>
        /// Dreht die Instanz zur Kamera
        /// </summary>
        public void TurnTowardsCamera()
        {
            if (CurrentWorld != null)
            {
                Vector3 target;
                if(CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
                {
                    target = CurrentWorld.GetFirstPersonObject().Position;
                    target.Y += CurrentWorld.GetFirstPersonObject().FPSEyeOffset;
                    TurnTowardsXYZ(target);
                }
                else
                {
                    TurnTowardsXYZ(CurrentWorld.GetCameraPosition());
                }
            }
        }

        /// <summary>
        /// Dreht das Objekt, so dass es zur Zielkoordinate blickt
        /// </summary>
        /// <param name="targetX">X-Koordinate</param>
        /// <param name="targetY">Y-Koordinate</param>
        public void TurnTowardsXY(float targetX, float targetY)
        {
            Vector3 target = new Vector3(targetX, targetY, 0);
            TurnTowardsXY(target);
        }

        /// <summary>
        /// Verändert die Rotation der Instanz, so dass sie in Richtung der XY-Koordinaten blickt. Z-Unterschiede Unterschiede werden ignoriert.
        /// [Geeignet, wenn die Kamera entlang der z-Achse blickt (Standard)]
        /// </summary>
        /// <param name="target">Zielkoordinaten</param>
        public void TurnTowardsXY(Vector3 target)
        {
            CheckModel();
           
            target.Z = Position.Z + 0.000001f;
            Matrix4 lookat = Matrix4.LookAt(target, Position, Vector3.UnitZ);
            lookat.Transpose();
            if (lookat.Determinant != 0)
            {
                lookat.Invert();
                Rotation = Quaternion.FromMatrix(new Matrix3(lookat));
            }
        }

        /// <summary>
        /// Verändert die Rotation der Instanz, so dass sie in Richtung der XZ-Koordinaten blickt. Vertikale Unterschiede werden ignoriert.
        /// (Geeignet, wenn die Kamera entlang der y-Achse blickt)
        /// </summary>
        /// <param name="targetX">Zielkoordinate der x-Achse</param>
        /// <param name="targetZ">Zielkoordinate der z-Achse</param>
        public void TurnTowardsXZ(float targetX, float targetZ)
        {
            Vector3 target = new Vector3(targetX, 0, targetZ);
            TurnTowardsXZ(target);
        }

        /// <summary>
        /// Verändert die Rotation der Instanz, so dass sie in Richtung der XZ-Koordinaten blickt. Vertikale Unterschiede werden ignoriert.
        /// (Geeignet, wenn die Kamera entlang der y-Achse blickt)
        /// </summary>
        /// <param name="target">Zielkoordinaten</param>
        public void TurnTowardsXZ(Vector3 target)
        {
            CheckModel();
            target.Y = Position.Y + 0.000001f;
            Matrix4 lookat = Matrix4.LookAt(target, Position, Vector3.UnitY);
            lookat.Transpose();
            if (lookat.Determinant != 0)
            {
                lookat.Invert();
                Rotation = Quaternion.FromMatrix(new Matrix3(lookat));
            }
        }

        /// <summary>
        /// Misst die Distanz zu einem Punkt
        /// </summary>
        /// <param name="position">Zielpunkt</param>
        /// <param name="absolute">wenn true, wird die Position statt des Hitbox-Mittelpunkts zur Berechnung verwendet</param>
        /// <returns>Distanz</returns>
        protected float GetDistanceTo(Vector3 position, bool absolute = false)
        {
            CheckModel();
            if (absolute)
                return (Position - position).LengthFast;
            else
                return (GetCenterPointForAllHitboxes() - position).LengthFast;
        }

        /// <summary>
        /// Misst die Distanz zu einem GameObject
        /// </summary>
        /// <param name="g">GameObject-Instanz</param>
        /// <param name="absolute">wenn true, wird die Position statt des Hitbox-Mittelpunkts zur Berechnung verwendet</param>
        /// <returns>Distanz</returns>
        protected float GetDistanceTo(GameObject g, bool absolute = false)
        {
            CheckModel();
            g.CheckModel();
            if (absolute)
                return (Position - g.Position).LengthFast;
            else
                return (GetCenterPointForAllHitboxes() - g.GetCenterPointForAllHitboxes()).LengthFast;
        }

        /// <summary>
        /// Erfragt, ob das Objekt (mit kompletter Hitbox) noch von der Kamera gesehen werde kann
        /// </summary>
        public bool IsInsideScreenSpace { get; internal set; } = true;

        /// <summary>
        /// Gibt das GameObject zurück, das unter dem Mauszeiger liegt (Instanzen müssen mit IsPickable = true gesetzt haben)
        /// </summary>
        /// <param name="ms">Mausinformationen</param>
        /// <returns>Gewähltes GameObject</returns>
        public static GameObject PickGameObject(MouseState ms)
        {
            GLWindow w = GLWindow.CurrentWindow;
            if (w == null || w.CurrentWorld == null || !w.Focused)
            {
                return null;
            }
            Vector2 mouseCoords = HelperGL.GetNormalizedMouseCoords(ms.X, ms.Y, KWEngine.CurrentWindow);
            Vector3 ray = Get3DMouseCoords(mouseCoords.X, mouseCoords.Y);
            Vector3 pos;
            if (KWEngine.Projection == ProjectionType.Perspective)
            {
                pos = w.CurrentWorld.GetCameraPosition() + ray;
            }
            else
            {
                pos = HelperGL.GetRayOriginForOrthographicProjection(mouseCoords) + ray;
            }

            GameObject pickedObject = null;
            float pickedDistance = float.MaxValue;

            foreach (GameObject go in w.CurrentWorld.GetGameObjects())
            {
                if (go.IsPickable && go.IsInsideScreenSpace)
                {
                    if (IntersectRaySphere(pos, ray, go.GetCenterPointForAllHitboxes(), go.GetMaxDiameter() / 2))
                    {
                        float distance = (go.GetCenterPointForAllHitboxes() - pos).LengthSquared;
                        if (distance < pickedDistance)
                        {
                            pickedDistance = distance;
                            pickedObject = go;
                        }
                    }
                }
            }
            
            return pickedObject;
        }

        /// <summary>
        /// Gibt eine Liste von GameObject-Instanzen zurück, die unter dem Mauszeiger liegen (Instanzen müssen mit IsPickable = true gesetzt haben)
        /// </summary>
        /// <param name="ms">Mausinformationen</param>
        /// <returns>Liste betroffener GameObject-Instanzen</returns>
        public static List<GameObject> PickGameObjects(MouseState ms)
        {
            List<GameObject> pickedObjects = new List<GameObject>();
            GLWindow w = GLWindow.CurrentWindow;
            if (w == null || w.CurrentWorld == null || !w.Focused)
            {
                return pickedObjects;
            }
            Vector2 mouseCoords = HelperGL.GetNormalizedMouseCoords(ms.X, ms.Y, KWEngine.CurrentWindow);
            Vector3 ray = Get3DMouseCoords(mouseCoords.X, mouseCoords.Y);
            Vector3 pos;
            if (KWEngine.Projection == ProjectionType.Perspective)
            {
                pos = w.CurrentWorld.GetCameraPosition() + ray;
            }
            else
            {
                pos = HelperGL.GetRayOriginForOrthographicProjection(mouseCoords) + ray;
            }

            foreach (GameObject go in w.CurrentWorld.GetGameObjects())
            {
                if (go.IsPickable && go.IsInsideScreenSpace)
                {
                    if (IntersectRaySphere(pos, ray, go.GetCenterPointForAllHitboxes(), go.GetMaxDiameter() / 2))
                    {
                        pickedObjects.Add(go);
                    }
                }
            }
            return pickedObjects;
        }
        private static bool IntersectRaySphere(Vector3 rayStart, Vector3 ray, Vector3 sphereCenter, float sphereRadius)
        {
            Vector3 p = rayStart - sphereCenter;

            float rSquared = sphereRadius * sphereRadius;
            float p_d = Vector3.Dot(p, ray);

            // The sphere is behind or surrounding the start point.
            if (p_d > 0 || Vector3.Dot(p, p) < rSquared)
                return false;

            // Flatten p into the plane passing through c perpendicular to the ray.
            // This gives the closest approach of the ray to the center.
            Vector3 a = p - p_d * ray;

            float aSquared = Vector3.Dot(a, a);

            // Closest approach is outside the sphere.
            if (aSquared > rSquared)
                return false;

            return true;
        }

        
        internal static Vector3 Get3DMouseCoords(float x, float y)
        {
            if (KWEngine.Projection == ProjectionType.Perspective)
            {
                HelperMouseRay r = new HelperMouseRay(x, y, GLWindow.CurrentWindow._viewMatrix, GLWindow.CurrentWindow._projectionMatrix);
                return Vector3.NormalizeFast(r.End - r.Start);
            }
            else
            {
                return KWEngine.CurrentWorld.GetCameraLookAtVector();
            }
        }
        

        private void CheckIfNotTerrain()
        {
            if (Model != null && Model.IsTerrain)
            {
                HelperGL.ShowErrorAndQuit("GameObject::CheckIfNotTerrain()", "Not a valid call for Terrain objects.");
            }
        }

        /// <summary>
        /// Setzt die Texturwiederholungen für eine KWCube- oder KWSphere-Instanz
        /// </summary>
        /// <param name="repeatX">Breitenwiederholungen</param>
        /// <param name="repeatY">Höhenwiederholungen</param>
        public void SetTextureRepeat(float repeatX, float repeatY)
        {
            CheckModel();
            CheckIfNotTerrain();
            if (!(Model.Name == "kwsphere.obj" || Model.Name == "kwcube.obj" || Model.Name == "kwcube6.obj" || Model.Name == "kwrect.obj"))
            {
                HelperGL.ShowErrorAndQuit("GameObject::SetTextureRepeat()", "Cannot set texture repeat values on models other than KWCube or KWSphere.");
                return;
            }

            SetTextureRepeat(repeatX, repeatY, CubeSide.All);
        }

        internal void SetTextureTerrainBlendMappingInternal(string blendTexture, string redTexture, string greenTexture, string blueTexture, bool isFile)
        {
            if (isFile)
            {
                if (blendTexture != null && !File.Exists(blendTexture))
                {
                    HelperGL.ShowErrorAndQuit("GameObject::SetTextureTerrainBlendMapping()", "Texture not found.");
                    return;
                }
                    

                if (redTexture != null && !File.Exists(redTexture))
                {
                    HelperGL.ShowErrorAndQuit("GameObject::SetTextureTerrainBlendMapping()", "Texture not found.");
                    return;
                }

                if (greenTexture != null && !File.Exists(greenTexture))
                {
                    HelperGL.ShowErrorAndQuit("GameObject::SetTextureTerrainBlendMapping()", "Texture not found.");
                    return;
                }

                if (blueTexture != null && !File.Exists(blueTexture))
                {
                    HelperGL.ShowErrorAndQuit("GameObject::SetTextureTerrainBlendMapping()", "Texture not found.");
                    return;
                }
            }

            if (Model != null && Model.IsTerrain)
            {
                if(KWEngine.CustomTextures.Count == 0)
                {
                    return;
                }
                lock (KWEngine.CustomTextures)
                {

                    GeoTerrain terrain = Model.Meshes.Values.ElementAt(0).Terrain;
                    if (blendTexture != null && redTexture != null)
                    {
                        if (KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(blendTexture))
                        {
                            terrain._texBlend = KWEngine.CustomTextures[KWEngine.CurrentWorld][blendTexture];
                        }
                        else
                        {
                            terrain._texBlend = isFile ? HelperTexture.LoadTextureForModelExternal(blendTexture) : HelperTexture.LoadTextureForModelInternal(blendTexture);
                            if (terrain._texBlend < 0)
                                terrain._texBlend = KWEngine.TextureBlack;

                            if (terrain._texBlend > 0 && terrain._texBlend != KWEngine.TextureBlack)
                            {
                                KWEngine.CustomTextures[KWEngine.CurrentWorld].Add(blendTexture, terrain._texBlend);
                            }
                        }

                        if (KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(redTexture))
                        {
                            terrain._texR = KWEngine.CustomTextures[KWEngine.CurrentWorld][redTexture];
                        }
                        else
                        {
                            terrain._texR = isFile ? HelperTexture.LoadTextureForModelExternal(redTexture) : HelperTexture.LoadTextureForModelInternal(redTexture);
                            if (terrain._texR > 0 && terrain._texR != KWEngine.TextureAlpha)
                            {
                                KWEngine.CustomTextures[KWEngine.CurrentWorld].Add(redTexture, terrain._texR);
                            }
                        }

                        if (greenTexture != null && KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(greenTexture))
                        {
                            terrain._texG = KWEngine.CustomTextures[KWEngine.CurrentWorld][greenTexture];
                        }
                        else
                        {
                            terrain._texG = greenTexture == null ? KWEngine.TextureAlpha : isFile ? HelperTexture.LoadTextureForModelExternal(greenTexture) : HelperTexture.LoadTextureForModelInternal(greenTexture);
                            if (terrain._texG > 0 && terrain._texG != KWEngine.TextureAlpha)
                            {
                                KWEngine.CustomTextures[KWEngine.CurrentWorld].Add(greenTexture, terrain._texG);
                            }
                        }

                        if (blueTexture != null && KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(blueTexture))
                        {
                            terrain._texB = KWEngine.CustomTextures[KWEngine.CurrentWorld][blueTexture];
                        }
                        else
                        {
                            terrain._texB = blueTexture == null ? KWEngine.TextureAlpha : isFile ? HelperTexture.LoadTextureForModelExternal(blueTexture) : HelperTexture.LoadTextureForModelInternal(blueTexture);
                            if (terrain._texB > 0 && terrain._texB != KWEngine.TextureAlpha)
                            {
                                KWEngine.CustomTextures[KWEngine.CurrentWorld].Add(blueTexture, terrain._texB);
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("No valid blend and red textures chosen. Please check your image files.");
                        terrain._texBlend = KWEngine.TextureBlack;
                        terrain._texR = KWEngine.TextureAlpha;
                        terrain._texG = KWEngine.TextureAlpha;
                        terrain._texB = KWEngine.TextureAlpha;
                    }
                }
            }
            else
            {
                HelperGL.ShowErrorAndQuit("HelperTexture::SetTextureTerrainBlendMapping()", "Method SetTextureTerrainBlendMapping() may only be called from a GameObject that has a terrain attached to it.");
                return;
            }
        }

        /// <summary>
        /// Setzt Blendmapping für Terrains
        /// </summary>
        /// <param name="blendTexture">Blend Map (schwarz, rot, grün, blau)</param>
        /// <param name="redTexture">Rottextur</param>
        /// <param name="greenTexture">Grüntextur</param>
        /// <param name="blueTexture">Blautextur</param>
        /// <param name="isFile">false, wenn die Texturen Teil der EXE sind ("Eingebettete Ressource")</param>
        public void SetTextureTerrainBlendMapping(string blendTexture, string redTexture, string greenTexture = null, string blueTexture = null, bool isFile = true)
        {
            if (CurrentWindow._multithreaded)
            {
                Action a = () => SetTextureTerrainBlendMappingInternal(blendTexture, redTexture, greenTexture, blueTexture, isFile);
                HelperGLLoader.AddCall(this, a);
            }
            else
                SetTextureTerrainBlendMappingInternal(blendTexture, redTexture, greenTexture, blueTexture, isFile);
        }

        /// <summary>
        /// Berechnet die Position eines Punkts, der um einen angegeben Punkt entlang einer Achse rotiert wird
        /// </summary>
        /// <param name="point">Mittelpunkt der Rotation</param>
        /// <param name="distance">Distanz zum Mittelpunkt</param>
        /// <param name="degrees">Grad der Rotation</param>
        /// <param name="plane">Achse der Rotation (Standard: Y)</param>
        /// <returns>Position des rotierten Punkts</returns>
        /// <returns></returns>
        public static Vector3 CalculateRotationAroundPointOnAxis(Vector3 point, float distance, float degrees, Plane plane = Plane.Y)
        {
            return HelperRotation.CalculateRotationAroundPointOnAxis(point, distance, degrees, plane);
        }

        /// <summary>
        /// Berechnet den Vektor, der entsteht, wenn der übergebene Vektor um die angegebenen Grad rotiert wird
        /// </summary>
        /// <param name="vector">zu rotierender Vektor</param>
        /// <param name="degrees">Rotation (in Grad)</param>
        /// <param name="plane">Einheitsvektor, um den rotiert wird</param>
        /// <returns>Rotierter Vektor</returns>
        public static Vector3 RotateVector(Vector3 vector, float degrees, Plane plane)
        {
            return HelperRotation.RotateVector(vector, degrees, plane);
        }

        /// <summary>
        /// Reflektiert den eingehenden Vektor 'vectorIn' am Ebenenvektor 'surfaceNormal'
        /// </summary>
        /// <param name="vectorIn">Eingehender Vektor</param>
        /// <param name="surfaceNormal">Ebenenvektor</param>
        /// <returns>Reflektierter Vektor</returns>
        public static Vector3 ReflectVector(Vector3 vectorIn, Vector3 surfaceNormal)
        {
            return HelperVector.Reflect(vectorIn, surfaceNormal);
        }

        /// <summary>
        /// Spielt einen Ton ab
        /// </summary>
        /// <param name="audiofile">Audiodatei</param>
        /// <param name="playLooping">looped playback?</param>
        /// <param name="volume">Lautstärke</param>
        /// <returns>ID des verwendeten Audiokanals</returns>
        protected static int SoundPlay(string audiofile, bool playLooping = false, float volume = 1.0f)
        {
            return GLAudioEngine.SoundPlay(audiofile, playLooping, volume);
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
        /// Stoppt einen Ton
        /// </summary>
        /// <param name="audiofile">Audiodatei</param>
        protected static void SoundStop(string audiofile)
        {
            GLAudioEngine.SoundStop(audiofile);
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
        /// Stoppt den angegebenen Audiokanal
        /// </summary>
        /// <param name="sourceId">Lanalnummer</param>
        protected static void SoundStop(int sourceId)
        {
            GLAudioEngine.SoundStop(sourceId);
        }

        /// <summary>
        /// Stoppt alle Töne
        /// </summary>
        protected static void SoundStopAll()
        {
            GLAudioEngine.SoundStopAll();
        }

        /// <summary>
        /// Zusammenfassung dieser Instanz
        /// </summary>
        /// <returns>String mit Instanzdetails</returns>
        public override string ToString()
        {
            return Name + " (" + Model.Name + " | " + base.ToString() + ")";
        }

        internal bool IsCubeOrSphere()
        {
            return Model != null && (Model.Filename == "kwcube.obj" || Model.Filename == "kwsphere.obj" || Model.Filename == "kwcube6.obj");
        }

        internal bool IsSpherePerfect()
        {
            return Model != null && Model.Filename == "kwsphere.obj" && Scale.X == Scale.Y && Scale.X == Scale.Z;
        }

        internal bool IsSpheroid()
        {
            return Model != null && Model.Filename == "kwsphere.obj" && (Scale.X != Scale.Y || Scale.X != Scale.Z || Scale.Y != Scale.Z);
        }
    }
}
