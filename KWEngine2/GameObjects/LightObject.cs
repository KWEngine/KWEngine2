using System;
using System.Collections.Generic;
using KWEngine2.Helper;
using OpenTK;
using OpenTK.Input;

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
        /// Schattenwerfendes Licht
        /// </summary>
        DirectionalShadow
    };

    /// <summary>
    /// Lichtklasse
    /// </summary>
    public abstract class LightObject
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; } = "undefined light object.";
        /// <summary>
        /// Art des Lichts
        /// </summary>
        public LightType Type
        { 
            get
            {
                return _type;
            }
            set
            {
                _type = value;
                if(value == LightType.DirectionalShadow)
                {
                    UpdateMatrices();
                }
            }
        }
        public Vector4 Color { get; internal set; }
        internal World World { get; set; }
        internal float FOVShadow2 { get; set; } = 60f;
        internal float ShadowMapBiasCoefficient = 0.005f;
        internal Matrix4 _shadowViewMatrix = Matrix4.Identity;
        internal LightType _type = LightType.Point;
        /// <summary>
        /// Aktuelle Welt
        /// </summary>
        /// <returns>Instanz</returns>
        public World GetCurrentWorld()
        {
            return World;
        }

        /// <summary>
        /// Aktuelle Welt
        /// </summary>
        public World CurrentWorld
        {
            get; internal set;
        }

        internal Vector3 _target = new Vector3(0, -1, 0);
        internal Vector3 _position = new Vector3(0, 0, 0);
        internal HelperFrustum _frustumShadowMap2 = new HelperFrustum();
        internal Matrix4 _projectionMatrixShadow2 = Matrix4.Identity;
        internal Matrix4 _viewProjectionMatrixShadow2 = Matrix4.Identity;

        private void UpdateMatrices()
        {
            if (_type == LightType.DirectionalShadow)
            {
                _shadowViewMatrix = Matrix4.LookAt(Position, Target, KWEngine.WorldUp);
                _frustumShadowMap2.CalculateFrustum(_projectionMatrixShadow2, _shadowViewMatrix);
                _viewProjectionMatrixShadow2 = _shadowViewMatrix * _projectionMatrixShadow2;
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
        /// Act
        /// </summary>
        /// <param name="ks">Keyboardinfos</param>
        /// <param name="ms">Mausinfos</param>
        /// <param name="deltaTimeFactor">Delta-Time-Faktor (Standard: 1.0)</param>
        public abstract void Act(KeyboardState ks, MouseState ms, float deltaTimeFactor);

        /// <summary>
        /// Konstruktormethode
        /// </summary>
        protected LightObject()
        {
            Position = new Vector3(0, 0, 0);
            Target = new Vector3(0, 0, 0);
            Color = new Vector4(1, 1, 1, 1);
            Type = LightType.Point;
            DistanceMultiplier = 10;
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
        /// <param name="intensity">Helligkeit (0 bis 1024)</param>
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
                throw new Exception("Light instance is not of type 'Directional'.");
            Target = target;
        }

        private void SetFOVShadowPrivate(float fov)
        {
            FOVShadow2 = HelperGL.Clamp(fov, 60, 179);
            if(KWEngine.CurrentWorld != null)
            {
                _projectionMatrixShadow2 = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOVShadow2 / 2), KWEngine.ShadowMapSize / (float)KWEngine.ShadowMapSize, 1f, CurrentWorld != null ? CurrentWorld.ZFar / 10 : 100f);
                UpdateMatrices();
            }
        }

        /// <summary>
        /// Setzt das Field of View (in Grad) für das schattenwerfende Licht (DirectionalShadow)
        /// </summary>
        /// <param name="fov">Blickfeld nach links und rechts in Grad (Minimum: 60, Maximum: 180)</param>
        public void SetFOVShadow(float fov)
        {
            if (Type != LightType.DirectionalShadow)
                throw new Exception("Cannot set FOV for a LightObject that is not of Type 'DirectionalShadow'.");
            else
            {
                FOVShadow2 = HelperGL.Clamp(fov, 60, 179);
                if (KWEngine.CurrentWorld != null)
                {
                    _projectionMatrixShadow2 = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOVShadow2 / 2), KWEngine.ShadowMapSize / (float)KWEngine.ShadowMapSize, 1f, CurrentWorld != null ? CurrentWorld.ZFar / 10 : 100f);
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
            if (Type != LightType.DirectionalShadow)
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
                    KWEngine.CurrentWindow.Frustum.SphereVsFrustum(l.Position, l.Type == LightType.DirectionalShadow ? l.DistanceMultiplier * 100 : l.DistanceMultiplier * 10);

                if (isInFrustum)
                {
                    if (l.Type == LightType.DirectionalShadow)
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
                    targets[arraycounter + 3] = l.Type == LightType.Directional || l.Type == LightType.DirectionalShadow ? 1 : -1;

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
