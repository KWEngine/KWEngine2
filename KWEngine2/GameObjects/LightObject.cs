using System;
using System.Collections.Generic;
using KWEngine2.Helper;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace KWEngine2.GameObjects
{
    /// <summary>
    /// Lichttyp
    /// </summary>
    public enum LightType {
        /// <summary>
        /// Punktlicht
        /// </summary>
        Point, 
        /// <summary>
        /// Gerichtetes Licht
        /// </summary>
        Directional,
        /// <summary>
        /// Gerichtetes Licht mit unbegrenzter Reichweite
        /// </summary>
        Sun
    };

    /// <summary>
    /// Lichtklasse
    /// </summary>
    public class LightObject
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; } = "undefined light object.";

        private bool _isShadowCaster = false;

        /// <summary>
        /// Gibt an, ob das Licht Schatten wirft (maximal 3x pro Welt erlaubt)
        /// </summary>
        public bool IsShadowCaster
        {
            get
            {
                return _isShadowCaster;
            }
            set
            {
                _isShadowCaster = value;
                if(value == true)
                {
                    ApplyFramebuffer();
                    UpdateMatrices();
                }
                else
                {
                    RemoveFramebuffer();
                }
                    
            }
        }

        private void ApplyFramebuffer()
        {

        }

        private void RemoveFramebuffer()
        {

        }

        /// <summary>
        /// Art des Lichts
        /// </summary>
        public LightType Type
        { 
            get
            {
                return _type;
            }
            internal set
            {
                _type = value;
                if(IsShadowCaster)
                {
                    UpdateMatrices();
                }
            }
        }

        /// <summary>
        /// Erfragt die aktuelle Lichtfärbung
        /// </summary>
        public Vector4 Color { get; internal set; }
        internal World World { get; set; }
        internal float FOVShadow { get; set; } = 60f;
        internal float ShadowMapBiasCoefficient = 0.005f;
        internal LightType _type = LightType.Point;

        /// <summary>
        /// Aktuelle Welt
        /// </summary>
        public World CurrentWorld
        {
            get; internal set;
        }

        internal Vector3 _target = new Vector3(0, -1, 0);
        internal Vector3 _position = new Vector3(0, 0, 0);
        internal HelperFrustum _frustumShadowMap = new HelperFrustum();
        internal Matrix4 _projectionMatrixShadow = Matrix4.Identity;
        internal Matrix4 _viewProjectionMatrixShadow = Matrix4.Identity;
        internal Matrix4[] _viewMatrixShadow = new Matrix4[6];

        private void UpdateMatrices()
        {
            if (IsShadowCaster)
            {
                if(Type == LightType.Point)
                {
                    _viewMatrixShadow[0] = Matrix4.LookAt(Position, Position + new Vector3(1,0,0), new Vector3(0, -1, 0));
                    _viewMatrixShadow[1] = Matrix4.LookAt(Position, Position + new Vector3(-1,0,0), new Vector3(0, -1, 0));
                    
                    _viewMatrixShadow[2] = Matrix4.LookAt(Position, Position + new Vector3(0, 1, 0), new Vector3(0, 0, 1));
                    _viewMatrixShadow[3] = Matrix4.LookAt(Position, Position + new Vector3(0, -1, 0), new Vector3(0, 0, -1));

                    _viewMatrixShadow[4] = Matrix4.LookAt(Position, Position + new Vector3(0, 0, 1), new Vector3(0, -1, 0));
                    _viewMatrixShadow[5] = Matrix4.LookAt(Position, Position + new Vector3(0, 0, -1), new Vector3(0, -1, 0));
                }
                else
                {
                    _viewMatrixShadow[0] = Matrix4.LookAt(Position, Target, KWEngine.WorldUp);
                    _frustumShadowMap.CalculateFrustum(_projectionMatrixShadow, _viewMatrixShadow[0]);
                    _viewProjectionMatrixShadow = _viewMatrixShadow[0] * _projectionMatrixShadow;
                }
                
            }
        }

        /// <summary>
        /// Position des Lichts
        /// </summary>
        public Vector3 Position 
        { 
            get { return _position; }
            set
            {
                _position = value;
                UpdateMatrices();
            }
        }
        /// <summary>
        /// Ziel des Lichts
        /// </summary>
        public Vector3 Target
        { 
            get
            {
                return _target;
            }
            set
            {
                _target = value;
                UpdateMatrices();
            }
        }
        /// <summary>
        /// Distanzmultiplikator (Standard: 10)
        /// </summary>
        public float DistanceMultiplier { get; private set; } = 10;

        /// <summary>
        /// Konstruktormethode
        /// </summary>
        protected LightObject()
            : this(LightType.Point)
        {
            Position = new Vector3(0, 0, 0);
            Target = new Vector3(0, -1, 0);
            Color = new Vector4(1, 1, 1, 1);
            Type = LightType.Point;
            DistanceMultiplier = 10;
            IsShadowCaster = false;
            SetFOVShadowPrivate(179);
        }

        /// <summary>
        /// Erstellt ein Lichtinstanz des angegebenen Typs
        /// </summary>
        /// <param name="type">Art des Lichts</param>
        /// <param name="isShadowCaster">Art des Lichts</param>
        public LightObject(LightType type, bool isShadowCaster = false)
        {
            Position = new Vector3(0, 0, 0);
            Target = new Vector3(0, -1, 0);
            Color = new Vector4(1, 1, 1, 1);
            Type = type;
            DistanceMultiplier = 10;
            IsShadowCaster = false;
            SetFOVShadowPrivate(179);
        }



        /// <summary>
        /// Ändert den Distanzmultiplikator des Lichts
        /// </summary>
        /// <param name="multiplier">Multiplikator (Standard: 10)</param>
        public void SetDistanceMultiplier(float multiplier)
        {
            if (multiplier > 0)
            {
                DistanceMultiplier = multiplier;
            }
            else
                DistanceMultiplier = 1;
        }

        /// <summary>
        /// Setzt die Lichtfarbe
        /// </summary>
        /// <param name="red">Rot</param>
        /// <param name="green">Grün</param>
        /// <param name="blue">Blau</param>
        /// <param name="intensity">Helligkeit (0 bis 1024, Standard: 1)</param>
        public void SetColor(float red, float green, float blue, float intensity)
        {
            Color = new Vector4(
                    Helper.HelperGL.Clamp(red, 0, 1),
                    Helper.HelperGL.Clamp(green, 0, 1),
                    Helper.HelperGL.Clamp(blue, 0, 1),
                    Helper.HelperGL.Clamp(intensity, 0, 1024)
                );
        }

        /// <summary>
        /// Setzt die Position des Lichts
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="z">z</param>
        public void SetPosition(float x, float y, float z)
        {
            Position = new Vector3(x, y, z);
        }

        /// <summary>
        /// Setzt die Position des Lichts
        /// </summary>
        /// <param name="position">Positionsdaten</param>
        public void SetPosition(Vector3 position)
        {
            Position = position;
        }

        /// <summary>
        /// Setzt das Ziel des gerichteten Lichts
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="z">z</param>
        public void SetTarget(float x, float y, float z)
        {
            if (Type == LightType.Point)
                throw new Exception("Light instance is not of type 'Directional'.");
            Target = new Vector3(x, y, z);
        }

        /// <summary>
        /// Setzt das Ziel des gerichteten Lichts
        /// </summary>
        /// <param name="target">Zielkoordinaten</param>
        public void SetTarget(Vector3 target)
        {
            if (Type == LightType.Point)
                throw new Exception("Light instance is not of type 'Directional' or 'Sun'.");
            Target = target;
        }

        private void SetFOVShadowPrivate(float fov)
        {
            FOVShadow = HelperGL.Clamp(fov, 30, 179);
            if(KWEngine.CurrentWorld != null)
            {
                _projectionMatrixShadow = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOVShadow / 2), KWEngine.ShadowMapSize / (float)KWEngine.ShadowMapSize, 1f, CurrentWorld != null ? CurrentWorld.ZFar / 10 : 100f);
                UpdateMatrices();
            }
        }

        /// <summary>
        /// Setzt das Field of View (in Grad) für das schattenwerfende Licht
        /// </summary>
        /// <param name="fov">Blickfeld nach links und rechts in Grad (Minimum: 30, Maximum: 180)</param>
        public void SetFOVShadow(float fov)
        {
            if(IsShadowCaster)
            {
                FOVShadow = HelperGL.Clamp(fov, 30, 179);
                if (KWEngine.CurrentWorld != null)
                {
                    if (Type == LightType.Point)
                    {
                        FOVShadow = 90;
                        _projectionMatrixShadow = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOVShadow / 2), KWEngine.ShadowMapSize / (float)KWEngine.ShadowMapSize, 0.1f, DistanceMultiplier);
                        
                    }
                    else
                        _projectionMatrixShadow = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOVShadow / 2), KWEngine.ShadowMapSize / (float)KWEngine.ShadowMapSize, 0.1f, DistanceMultiplier);
                    UpdateMatrices();
                }
            }
        }

        /// <summary>
        /// Setzt den Koeffizienten für die Berechnung der Schatten der LightObject-Instanz
        /// </summary>
        /// <param name="bias">Biaswert (Standard: 0.005f; Bereich: 0.00001f bis 1f)</param>
        public void SetFOVShadowBiasCoefficient(float bias = 0.005f)
        {
            if (!IsShadowCaster)
                throw new Exception("Cannot set FOV for a LightObject that is not of Type 'DirectionalShadow'.");
            else
            {
                ShadowMapBiasCoefficient = HelperGL.Clamp(bias, 0.00001f, 1);
            }
        }

        internal static void PrepareLightsForRenderPass(List<LightObject> lights, ref float[] colors, ref float[] targets, ref float[] positions, ref int count, ref int shadowLight)
        {
            int countTemp = 0;
            IEnumerator<LightObject> enumerator = lights.GetEnumerator();
            enumerator.Reset();
            for (int i = 0, arraycounter = 0; i < lights.Count; i++)
            {

                enumerator.MoveNext();
                LightObject l = enumerator.Current;
                bool isInFrustum =
                    KWEngine.CurrentWindow.Frustum.SphereVsFrustum(l.Position, l.DistanceMultiplier * 10);

                if (isInFrustum)
                {
                    if (l.IsShadowCaster)
                    {
                        shadowLight = i;
                    }

                    colors[arraycounter + 0] = l.Color.X;
                    colors[arraycounter + 1] = l.Color.Y;
                    colors[arraycounter + 2] = l.Color.Z;
                    colors[arraycounter + 3] = l.Color.W; // Intensity of color

                    targets[arraycounter + 0] = l.Target.X;
                    targets[arraycounter + 1] = l.Target.Y;
                    targets[arraycounter + 2] = l.Target.Z;
                    if(l.Type == LightType.Point)
                    {
                        if (l.IsShadowCaster)
                            targets[arraycounter + 3] = -2;
                        else
                            targets[arraycounter + 3] = -1;
                    }
                    else if(l.Type== LightType.Directional)
                    {
                        if (l.IsShadowCaster)
                            targets[arraycounter + 3] = 2;
                        else
                            targets[arraycounter + 3] = 1;
                    }
                    else
                    {
                        if (l.IsShadowCaster)
                            targets[arraycounter + 3] = 200;
                        else
                            targets[arraycounter + 3] = 100;
                    }
                    //targets[arraycounter + 3] = l.Type == LightType.Directional || l.Type == LightType.DirectionalShadow ? 1 : -1;

                    positions[arraycounter + 0] = l.Position.X;
                    positions[arraycounter + 1] = l.Position.Y;
                    positions[arraycounter + 2] = l.Position.Z;
                    positions[arraycounter + 3] = l.DistanceMultiplier;

                    countTemp++;
                    arraycounter += 4;
                }
            }

            count = countTemp;
        }

    }
}
