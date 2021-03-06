﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public sealed class LightObject : IComparable
    {
        private int _frustumMultiplier = 10;

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; } = "undefined light object.";

        private bool _isShadowCaster = false;
        internal int _framebufferIndex = -1;

        /// <summary>
        /// Gibt an, ob das Licht Schatten wirft (maximal 3x pro Welt erlaubt)
        /// </summary>
        internal bool IsShadowCaster
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
                    
                    UpdateMatrices();
                }
                else
                {
                    if(_framebufferIndex >= 0)
                        RemoveFramebuffer();
                }
                    
            }
        }

        internal void ApplyFramebuffer()
        {
            _framebufferIndex = KWEngine.CurrentWindow.GetFreeIdForShadowMap(this.Type);
        }

        internal void RemoveFramebuffer()
        {
            if (_framebufferIndex >= 0 && _framebufferIndex < KWEngine.MAX_SHADOWMAPS)
            {
                KWEngine.CurrentWindow.AddIdForShadowMap(_framebufferIndex, this.Type);
                _framebufferIndex = -1;
            }
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
            }
        }

        /// <summary>
        /// Erfragt die aktuelle Lichtfärbung
        /// </summary>
        public Vector4 Color { get; internal set; }
        internal World World { get; set; }
        internal float FOVShadow { get; set; } = 90f;
        internal float ShadowMapBiasCoefficient = 0.0005f;
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
        internal Matrix4[] _viewProjectionMatrixShadow = new Matrix4[6];
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

                    for(int i =0; i < 6; i++)
                    {
                        _viewProjectionMatrixShadow[i] = _viewMatrixShadow[i] * _projectionMatrixShadow;
                    }
                }
                else
                {
                    _viewMatrixShadow[0] = Matrix4.LookAt(Position, Target, KWEngine.WorldUp);
                    _frustumShadowMap.CalculateFrustum(_projectionMatrixShadow, _viewMatrixShadow[0]);
                    _viewProjectionMatrixShadow[0] = _viewMatrixShadow[0] * _projectionMatrixShadow;
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
                _target = new Vector3(value.X, value.Y, value.Z + 0.000001f);
                UpdateMatrices();
            }
        }

        private float _distanceMultiplier = 10;
        internal float GetDistanceMultiplier()
        {
            return _distanceMultiplier;
        }

        /// <summary>
        /// Konstruktormethode
        /// </summary>
        private LightObject()
            : this(LightType.Point, false)
        {
            
        }

        /// <summary>
        /// Erstellt ein Lichtinstanz des angegebenen Typs
        /// </summary>
        /// <param name="type">Art des Lichts</param>
        /// <param name="isShadowCaster">Wirft das Licht Schatten?</param>
        public LightObject(LightType type, bool isShadowCaster)
        {
            Position = new Vector3(0, 0, 0);
            Target = new Vector3(0, -1, 0);
            Color = new Vector4(1, 1, 1, 1);
            Type = type;
            _distanceMultiplier = 10;
            IsShadowCaster = isShadowCaster;
            if (type == LightType.Sun)
                SetFOV(45);
            else
                SetFOV(120);
        }



        /// <summary>
        /// Ändert den Distanzmultiplikator des Lichts
        /// </summary>
        /// <param name="multiplier">Multiplikator (Standard: 10)</param>
        public void SetDistanceMultiplier(float multiplier)
        {
            if(Type == LightType.Sun)
            {
                HelperGL.ShowErrorAndQuit("LightObject::SetDistanceMultiplier()", "Cannot set distance for sun light because sun light is always max distance!");
                return;
                
            }
            if (multiplier > 0)
            {
                _distanceMultiplier = multiplier;
            }
            else
                _distanceMultiplier = 10;
            SetFOV(FOVShadow);
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
            {
                HelperGL.ShowErrorAndQuit("LightObject::SetTarget()", "Light instance is not of type 'Directional' or 'Sun'.");
                return;
            }
            Target = new Vector3(x, y, z);
        }

        /// <summary>
        /// Setzt das Ziel des gerichteten Lichts
        /// </summary>
        /// <param name="target">Zielkoordinaten</param>
        public void SetTarget(Vector3 target)
        {
            if (Type == LightType.Point)
            {
                HelperGL.ShowErrorAndQuit("LightObject::SetTarget()", "Light instance is not of type 'Directional' or 'Sun'.");
                return;
            }
            Target = target;
        }

        internal float GetFrustumMultiplier()
        {
            return _frustumMultiplier;
        }

        /// <summary>
        /// Setzt das Field of View (in Grad) für das schattenwerfende Licht
        /// </summary>
        /// <param name="fov">Blickfeld nach links und rechts in Grad (Minimum: 10, Maximum: 180)</param>
        public void SetFOV(float fov)
        {
            if(IsShadowCaster)
            {
                
                if (KWEngine.CurrentWorld != null)
                {
                    if (Type == LightType.Point)
                    {
                        FOVShadow = 90;
                        _projectionMatrixShadow = Matrix4.CreatePerspectiveFieldOfView(
                            MathHelper.DegreesToRadians(FOVShadow),
                            KWEngine.ShadowMapSize / (float)KWEngine.ShadowMapSize,
                            0.1f,
                            _distanceMultiplier * _frustumMultiplier);

                    }
                    else
                    {
                        FOVShadow = HelperGL.Clamp(fov, 10, 179);
                        _projectionMatrixShadow = Matrix4.CreatePerspectiveFieldOfView(
                            MathHelper.DegreesToRadians(FOVShadow / 2),
                            KWEngine.ShadowMapSize / (float)KWEngine.ShadowMapSize,
                            0.1f,
                            Type == LightType.Sun ? KWEngine.CurrentWorld.ZFar : _distanceMultiplier * _frustumMultiplier);
                    }
                    UpdateMatrices();
                }
            }
        }

        /// <summary>
        /// Setzt den Koeffizienten für die Berechnung der Schatten der LightObject-Instanz
        /// </summary>
        /// <param name="bias">Biaswert (Standard: 0.0005f; Bereich: -1 bis +1)</param>
        public void SetFOVBiasCoefficient(float bias = 0.0005f)
        {
            if (!IsShadowCaster)
            {
                HelperGL.ShowErrorAndQuit("LightObject::SetFOVBiasCoefficient()", "Setting FOV is available for shadow casting lights only.");
            }
            else
            {
                ShadowMapBiasCoefficient = HelperGL.Clamp(bias, -1, 1);
            }
        }

        internal static void PrepareLightsForRenderPass(List<LightObject> lights, ref float[] colors, ref float[] targets, ref float[] positions, ref float[] meta, ref int count)
        {
            int countTemp = 0;
            IEnumerator<LightObject> enumerator = lights.GetEnumerator();
            enumerator.Reset();

            Vector3 viewDirection;
            Vector3 camPosition;
            if (KWEngine.CurrentWorld.IsFirstPersonMode)
            {
                viewDirection = HelperCamera.GetLookAtVector();
                camPosition = KWEngine.CurrentWorld.GetFirstPersonObject().Position;
            }
            else
            {
                viewDirection = KWEngine.CurrentWorld.GetCameraLookAtVector();
                camPosition = KWEngine.CurrentWorld.GetCameraPosition();
            }

            for (int i = 0, threecounter = 0, arraycounter = 0; i < lights.Count; i++)
            {
                bool isInFrustum = true;
                enumerator.MoveNext();
                LightObject l = enumerator.Current;
                
                Vector3 cameraToLight = Vector3.NormalizeFast(l.Position - camPosition);
                float dotProductViewAndLight = Vector3.Dot(viewDirection, cameraToLight);

                if (dotProductViewAndLight < -0.25f && (camPosition - l.Position).LengthFast > (l.Type != LightType.Sun ? l._distanceMultiplier : (KWEngine.CurrentWorld.ZFar * 2))) 
                    isInFrustum = false;

                if (isInFrustum)
                {
                    colors[arraycounter + 0] = l.Color.X;
                    colors[arraycounter + 1] = l.Color.Y;
                    colors[arraycounter + 2] = l.Color.Z;
                    colors[arraycounter + 3] = l.Color.W; // Intensity of color

                    targets[arraycounter + 0] = l.Target.X;
                    targets[arraycounter + 1] = l.Target.Y;
                    targets[arraycounter + 2] = l.Target.Z;
                    if(l.Type == LightType.Point)
                    {
                        targets[arraycounter + 3] = 0; // Point
                    }
                    else if(l.Type== LightType.Directional)
                    {
                        targets[arraycounter + 3] = 1; // Directional
                    }
                    else
                    {
                        targets[arraycounter + 3] = -1; // Sun
                    }

                    positions[arraycounter + 0] = l.Position.X;
                    positions[arraycounter + 1] = l.Position.Y;
                    positions[arraycounter + 2] = l.Position.Z;
                    positions[arraycounter + 3] = l._distanceMultiplier;

                    meta[threecounter] = l.ShadowMapBiasCoefficient;
                    meta[threecounter + 1] = l.IsShadowCaster ? 1 : 0;
                    meta[threecounter + 2] = l._distanceMultiplier * l._frustumMultiplier;
                    countTemp++;
                    arraycounter += 4;
                    threecounter += 3;
                }
            }

            count = countTemp;
        }

        /// <summary>
        /// Vergleicht zwei Lichter miteinander
        /// </summary>
        /// <param name="obj">Zu vergleichendes Licht</param>
        /// <returns>1, -1</returns>
        public int CompareTo(object obj)
        {
            LightObject light = (LightObject)obj;
            //return light.IsShadowCaster && !this.IsShadowCaster ? 1 : -1;
            return light.IsShadowCaster ? 1 : -1;
        }
    }
}
