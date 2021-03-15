using System;
using System.Collections.Generic;
using OpenTK.Input;
using OpenTK;
using KWEngine2.Helper;
using System.Diagnostics;
using KWEngine2.Model;

namespace KWEngine2.GameObjects
{
    /// <summary>
    /// Art der Animationsbewegung
    /// </summary>
    public enum ExplosionAnimation
    {
        /// <summary>
        /// Standard-Animationsalgorithmus
        /// </summary>
        Spread = 0,
        /// <summary>
        /// Partikel wandern entlang der positiven y-Achse nach oben
        /// </summary>
        WindUp = 1,
        /// <summary>
        /// Partikel wirbeln entlang der positiven y-Achse nach oben
        /// </summary>
        WhirlwindUp = 2

    }

    /// <summary>
    /// Art der Explosion
    /// </summary>
    public enum ExplosionType { 
        /// <summary>
        /// Würfelpartikel in alle Richtungen
        /// </summary>
        Cube = 0, 
        /// <summary>
        /// Würfelpartikel um die Y-Achse
        /// </summary>
        CubeRingY = 100, 
        /// <summary>
        /// Würfelpartikel um die Z-Achse
        /// </summary>
        CubeRingZ = 1000,
        /// <summary>
        /// Kugelpartikel in alle Richtungen
        /// </summary>
        Sphere = 1,
        /// <summary>
        /// Kugelpartikel um die Y-Achse
        /// </summary>
        SphereRingY = 101,
        /// <summary>
        /// Kugelpartikel um die Z-Achse
        /// </summary>
        SphereRingZ = 1001,
        /// <summary>
        /// Sternenpartikel in alle Richtungen
        /// </summary>
        Star = 2,
        /// <summary>
        /// Sternenpartikel um die Y-Achse
        /// </summary>
        StarRingY = 102,
        /// <summary>
        /// Sternenpartikel um die Z-Achse
        /// </summary>
        StarRingZ = 1002,
        /// <summary>
        /// Herzpartikel in alle Richtungen
        /// </summary>
        Heart = 3,
        /// <summary>
        /// Herzpartikel um die Y-Achse
        /// </summary>
        HeartRingY = 103,
        /// <summary>
        /// Herzpartikel um die Z-Achse
        /// </summary>
        HeartRingZ = 1003,
        /// <summary>
        /// Schädelpartikel in alle Richtungen
        /// </summary>
        Skull = 4,
        /// <summary>
        /// Schädelpartikel um die Y-Achse
        /// </summary>
        SkullRingY = 104,
        /// <summary>
        /// Schädelpartikel um die Z-Achse
        /// </summary>
        SkullRingZ = 1004,
        /// <summary>
        /// Dollarpartikel in alle Richtungen
        /// </summary>
        Dollar = 5,
        /// <summary>
        /// Dollarpartikel um die Y-Achse
        /// </summary>
        DollarRingY = 105,
        /// <summary>
        /// Dollarpartikel um die Z-Achse
        /// </summary>
        DollarRingZ = 1005
    }

    /// <summary>
    /// Explosionsklasse
    /// </summary>
    public sealed class Explosion
    {
        internal static readonly Vector3[] Axes = new Vector3[] {
            Vector3.UnitX,
            Vector3.UnitY,
            Vector3.UnitZ,
            -Vector3.UnitX,
            -Vector3.UnitY,
            -Vector3.UnitZ,

            new Vector3(0.707107f,0.707107f,0),   // right       up
            new Vector3(0.577351f,0.577351f,0.577351f),   // right front up
            new Vector3(0,0.707107f,0.707107f),   // front       up
            new Vector3(-0.577351f,0.577351f,0.577351f),  // left front  up
            new Vector3(-0.707107f,0.707107f,0),  // left        up
            new Vector3(-0.577351f,0.577351f,-0.577351f), // left back   up
            new Vector3(0,0.707107f,-0.707107f),  // back        up
            new Vector3(0.577351f,0.577351f,-0.577351f),  // right back  up

            new Vector3(0.707107f,-0.707107f,0),   // right       down
            new Vector3(0.707107f,-0.707107f,0.707107f),   // right front down
            new Vector3(0,-0.707107f,0.707107f),   // front       down
            new Vector3(-0.577351f,-0.577351f,0.577351f),  // left front  down
            new Vector3(-0.707107f,-0.707107f,0),  // left        down
            new Vector3(-0.577351f,-0.577351f,-0.577351f), // left back   down
            new Vector3(0,-0.707107f,-0.707107f),  // back        down
            new Vector3(0.577351f,-0.577351f,-0.577351f),  // right back  down
        };
        internal static int AxesCount = Axes.Length;
        internal const int MAX_PARTICLES = 512;

        // GameObject stuff
        internal GeoModel _model;
        internal World _currentWorld;
        /// <summary>
        /// Position der Explosion
        /// </summary>
        public Vector3 Position
        {
            get; set;
        }
        private Vector4 _glow = Vector4.Zero;
        /// <summary>
        /// Glühfarbe der Explosion
        /// </summary>
        public Vector4 Glow
        {
            get
            {
                return _glow;
            }
            private set
            {
                _glow.X = HelperGL.Clamp(value.X, 0, 1);
                _glow.Y = HelperGL.Clamp(value.Y, 0, 1);
                _glow.Z = HelperGL.Clamp(value.Z, 0, 1);
                _glow.W = HelperGL.Clamp(value.W, 0, 1);
            }
        }

        private Vector3 _color = Vector3.One;
        /// <summary>
        /// Farbe der Explosionsteilchen (ohne Glühfarbe)
        /// </summary>
        public Vector3 Color
        {
            get
            {
                return _color;
            }
            private set
            {
                _color.X = HelperGL.Clamp(value.X, 0, 1);
                _color.Y = HelperGL.Clamp(value.Y, 0, 1);
                _color.Z = HelperGL.Clamp(value.Z, 0, 1);
            }
        }

        /// <summary>
        /// Setzt die Färbung der Explosionspartikel
        /// </summary>
        /// <param name="red">Rot</param>
        /// <param name="green">Grün</param>
        /// <param name="blue">Blau</param>
        public void SetColor(float red, float green, float blue)
        {
            _color.X = red >= 0 && red <= 1 ? red : 1;
            _color.Y = green >= 0 && green <= 1 ? green : 1;
            _color.Z = blue >= 0 && blue <= 1 ? blue : 1;
        }

        /// <summary>
        /// Setzt die Glühfarbe der Explosionspartikel
        /// </summary>
        /// <param name="red">Rot</param>
        /// <param name="green">Grün</param>
        /// <param name="blue">Blau</param>
        /// <param name="intensity">Helligkeit</param>
        public void SetGlow(float red, float green, float blue, float intensity)
        {
            Glow = new Vector4(red, green, blue, intensity);
        }

        /// <summary>
        /// Setzt die Position des Objekts
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="z">z</param>
        public void SetPosition(float x, float y, float z)
        {
            Position = new Vector3(x, y, z);
        }

        internal int _textureId = -1;
        internal Vector2 _textureTransform = new Vector2(1, 1);
        internal int _amount = 32;
        internal float _spread = 10;
        internal float _duration = 2;
        internal long _starttime = -1;
        internal float _secondsAlive = 0;
        internal float _particleSize = 0.5f;
        internal float[] _directions = new float[MAX_PARTICLES * 4];
        internal int _algorithm = 0;
        internal ExplosionType _type = ExplosionType.Cube;

        /// <summary>
        /// Setzt den Explosionsradius
        /// </summary>
        /// <param name="radius">Radius</param>
        public void SetRadius(float radius)
        {
            _spread = radius > 0 ? radius : 10;
        }

        /// <summary>
        /// Setzt die Partikelgröße
        /// </summary>
        /// <param name="size">Größe je Partikel</param>
        public void SetParticleSize(float size)
        {
            _particleSize = size > 0 ? size : 1f;
        }

        /// <summary>
        /// Setzt den Bewegungsalgorithmus der Partikel
        /// </summary>
        /// <param name="e">Algorithmustyp</param>
        public void SetAnimationAlgorithm(ExplosionAnimation e)
        {
            _algorithm = (int)e;
        }

        /// <summary>
        /// Explosionskonstruktormethode
        /// </summary>
        /// <param name="position">Position der Explosion</param>
        /// <param name="particleCount">Anzahl der Partikel</param>
        /// <param name="durationInSeconds">Dauer der Explosion in Sekunden</param>
        /// <param name="type">Art der Explosion</param>
        public Explosion(Vector3 position, int particleCount, float durationInSeconds = 2, ExplosionType type = ExplosionType.Sphere)
            : this(position, particleCount, 1f, 10f, durationInSeconds, type, new Vector4(1f,1f,1f,1f), null)
        {

        }

        /// <summary>
        /// Explosionskonstruktormethode
        /// </summary>
        /// <param name="position">Position der Explosion</param>
        /// <param name="type">Art der Explosion</param>
        public Explosion(Vector3 position, ExplosionType type = ExplosionType.Sphere)
            : this(position, 16, 1f, 10f, 2, type, new Vector4(1f, 1f, 1f, 1f), null)
        {

        }

        /// <summary>
        /// Explosionskonstruktormethode
        /// </summary>
        /// <param name="position">Position der Explosion</param>
        /// <param name="particleCount">Anzahl der Partikel</param>
        /// <param name="particleSize">Größe der Partikel</param>
        /// <param name="radius">Radius der Explosion</param>
        /// <param name="durationInSeconds">Dauer der Explosion in Sekunden</param>
        /// <param name="type">Art der Explosion</param>
        /// <param name="glow">Glühfarbe der Explosion</param>
        /// <param name="texture">Textur der Explosion (optional)</param>
        public Explosion(Vector3 position, int particleCount, float particleSize, float radius, float durationInSeconds, ExplosionType type, Vector4 glow, string texture = null)
        {
            _currentWorld = GLWindow.CurrentWindow.CurrentWorld;

            if (_currentWorld == null)
            {
                HelperGL.ShowErrorAndQuit("Explosion::Explosion()", "World is null. Cannot create Explosion in an empty world.");
                return;
            }

            
            if (type == ExplosionType.Cube || type == ExplosionType.CubeRingY || type == ExplosionType.CubeRingZ)
            {
                _model = KWEngine.GetModel("KWCube");
            }
            else if (type == ExplosionType.Sphere || type == ExplosionType.SphereRingY || type == ExplosionType.SphereRingZ)
            {
                _model = KWEngine.GetModel("KWSphere");
            }
            else if (type == ExplosionType.Star || type == ExplosionType.StarRingY || type == ExplosionType.StarRingZ)
            {
                _model = KWEngine.KWStar;
            }
            else if (type == ExplosionType.Heart || type == ExplosionType.HeartRingY || type == ExplosionType.HeartRingZ)
            {
                _model = (KWEngine.KWHeart);
            }
            else if(type == ExplosionType.Skull || type == ExplosionType.SkullRingY || type == ExplosionType.SkullRingZ)
            {
                _model = (KWEngine.KWSkull);
            }
            else
            {
                _model = (KWEngine.KWDollar);
            }

            _type = type;
            Glow = glow;
            Position = position;
            _amount = particleCount >= 4 && particleCount <= MAX_PARTICLES ? particleCount : particleCount < 4 ? 4 : MAX_PARTICLES;
            _spread = radius > 0 ? radius : 10;
            _duration = durationInSeconds > 0 ? durationInSeconds : 2;
            _particleSize = particleSize > 0 ? particleSize : 1f;

            for (int i = 0, arrayIndex = 0; i < _amount; i++, arrayIndex += 4)
            {
                
                if ((int)type < 100)
                {
                    int randomIndex = HelperRandom.GetRandomNumber(0, AxesCount - 1);
                    int randomIndex2 = HelperRandom.GetRandomNumber(0, AxesCount - 1);
                    int randomIndex3 = HelperRandom.GetRandomNumber(0, AxesCount - 1);
                    _directions[arrayIndex] = Axes[randomIndex].X;
                    _directions[arrayIndex + 1] = Axes[randomIndex2].Y;
                    _directions[arrayIndex + 2] = Axes[randomIndex3].Z;
                    _directions[arrayIndex + 3] = HelperRandom.GetRandomNumber(0.1f, 1.0f);
                }
                else if((int)type >= 100 && (int)type < 1000)
                {
                    _directions[arrayIndex] = 0;
                    _directions[arrayIndex + 1] = 1;
                    _directions[arrayIndex + 2] = 0;
                    _directions[arrayIndex + 3] = HelperRandom.GetRandomNumber(0.01f, 1.0f);
                }
                else
                {
                    _directions[arrayIndex] = 0;
                    _directions[arrayIndex + 1] = 0;
                    _directions[arrayIndex + 2] = 1;
                    _directions[arrayIndex + 3] = HelperRandom.GetRandomNumber(0.01f, 1.0f);
                }
            }
            int texId = -1;
            bool textureFound = false;
            if (texture != null && texture.Length > 0)
            {
                textureFound = KWEngine.CustomTextures[_currentWorld].TryGetValue(texture, out texId);
            }
            if(textureFound)
                _textureId = texId;
            else
            {
                if (texture != null)
                {
                    Action a = () => SetTexture(texture);
                    HelperGLLoader.AddCall(this, a);
                }
            }
        }

        internal void SetTexture(string texture)
        {

            lock (KWEngine.CustomTextures)
            {
                if (KWEngine.CustomTextures[_currentWorld].ContainsKey(texture))
                {
                    _textureId = KWEngine.CustomTextures[_currentWorld][texture];
                }
                else
                {
                    _textureId = HelperTexture.LoadTextureForModelExternal(texture);
                    if (_textureId > 0)
                        KWEngine.CustomTextures[_currentWorld].Add(texture, _textureId);
                }
            }
        }

        internal void Act()
        {
            if(_starttime >= 0)
            {
                long currentTime = DeltaTime.Watch.ElapsedMilliseconds;
                _secondsAlive = (currentTime - _starttime) / 1000f;
                if(_secondsAlive > _duration)
                {
                    _currentWorld.RemoveExplosionObject(this);
                    return;
                }
            }
        }
    }
}
