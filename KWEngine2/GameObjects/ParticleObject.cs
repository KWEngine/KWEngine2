using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KWEngine2.Helper;
using KWEngine2.Model;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace KWEngine2.GameObjects
{
    /// <summary>
    /// Art der Partikel
    /// </summary>
    public enum ParticleType { 
        /// <summary>
        /// Feuer 1
        /// </summary>
        BurstFire1,
        /// <summary>
        /// Feuer 2
        /// </summary>
        BurstFire2,
        /// <summary>
        /// Feuer 3
        /// </summary>
        BurstFire3,
        /// <summary>
        /// Elektroschock
        /// </summary>
        BurstElectricity,
        /// <summary>
        /// Bälle
        /// </summary>
        BurstBubblesColored, 
        /// <summary>
        /// Bälle (farblos)
        /// </summary>
        BurstBubblesMonochrome,
        /// <summary>
        /// Feuerwerk 1
        /// </summary>
        BurstFirework1,
        /// <summary>
        /// Feuerwerk 2
        /// </summary>
        BurstFirework2,
        /// <summary>
        /// Herzen
        /// </summary>
        BurstHearts,
        /// <summary>
        /// Pluszeichen
        /// </summary>
        BurstOneUps,
        /// <summary>
        /// Schild
        /// </summary>
        BurstShield,
        /// <summary>
        /// Teleport 1
        /// </summary>
        BurstTeleport1,
        /// <summary>
        /// Teleport 2
        /// </summary>
        BurstTeleport2,
        /// <summary>
        /// Teleport 3
        /// </summary>
        BurstTeleport3,
        /// <summary>
        /// Rauch 1 (Loop)
        /// </summary>
        LoopSmoke1,
        /// <summary>
        /// Rauch 2 (Loop)
        /// </summary>
        LoopSmoke2,
        /// <summary>
        /// Rauch 3 (Loop)
        /// </summary>
        LoopSmoke3
    }

    /// <summary>
    /// Partikelklasse
    /// </summary>
    public sealed class ParticleObject
    {
        internal GeoModel _model = KWEngine.KWRect;
        internal Vector3 _position = new Vector3(0, 0, 0);
        /// <summary>
        /// Position des Partikels
        /// </summary>
        public Vector3 Position { get; set; } = new Vector3(0, 0, 0);
        private Vector3 _scale = new Vector3(1, 1, 1);
        private Vector3 _scaleCurrent = new Vector3(1, 1, 1);
        internal Matrix4 _rotation = Matrix4.Identity;
        internal Matrix4 _modelMatrix = Matrix4.Identity;
        internal Vector4 _tint = new Vector4(1, 1, 1, 1);
        internal ParticleType _type = ParticleType.BurstFire1;
        internal long _starttime = -1;
        internal long _lastUpdate = -1;
        internal long _durationInMS = 5000;
        internal int _frame = 0;
        internal long _aliveInMS = 0;
        internal float _scaleFactor = 1;
        internal ParticleInfo _info;
        private static Quaternion Turn180 = Quaternion.FromAxisAngle(Vector3.UnitZ, (float)Math.PI);

        /// <summary>
        /// Setzt die Dauer der Loop-Partikel
        /// </summary>
        /// <param name="durationInSeconds">Dauer (in Sekunden)</param>
        public void SetDuration(float durationInSeconds)
        {
            if (_type == ParticleType.LoopSmoke1 || _type == ParticleType.LoopSmoke2 || _type == ParticleType.LoopSmoke3)
                _durationInMS = durationInSeconds > 0 ? (int)(durationInSeconds * 1000) : 5000;
            else
                throw new Exception("Duration may only be set for loop particles.");
        }

        /// <summary>
        /// Setzt die Positon
        /// </summary>
        /// <param name="pos">Positionsdaten</param>
        public void SetPosition(Vector3 pos)
        {
            Position = pos;
        }

        /// <summary>
        /// Setzt die Partikelfärbung
        /// </summary>
        /// <param name="red">Rot</param>
        /// <param name="green">Grün</param>
        /// <param name="blue">Blau</param>
        /// <param name="intensity">Helligkeit</param>
        public void SetColor(float red, float green, float blue, float intensity)
        {
            _tint.X = HelperGL.Clamp(red, 0, 1);
            _tint.Y = HelperGL.Clamp(green, 0, 1);
            _tint.Z = HelperGL.Clamp(blue, 0, 1);
            _tint.W = HelperGL.Clamp(intensity, 0, 1);
        }

        /// <summary>
        /// Konstruktormethode für Partikel
        /// </summary>
        /// <param name="position">Position</param>
        /// <param name="scale">Größe in x-, y- und z-Richtung</param>
        /// <param name="type">Art der Partikelanimation</param>
        public ParticleObject(Vector3 position, Vector3 scale, ParticleType type)
        {
            _scale.X = HelperGL.Clamp(scale.X, 0.001f, float.MaxValue);
            _scale.Y = HelperGL.Clamp(scale.Y, 0.001f, float.MaxValue);
            _scale.Z = HelperGL.Clamp(scale.Z, 0.001f, float.MaxValue);
            _scaleCurrent.X = HelperGL.Clamp(scale.X, 0.001f, float.MaxValue);
            _scaleCurrent.Y = HelperGL.Clamp(scale.Y, 0.001f, float.MaxValue);
            _scaleCurrent.Z = HelperGL.Clamp(scale.Z, 0.001f, float.MaxValue);

            Position = position;

            _type = type;

            _info = KWEngine.ParticleDictionary[_type];
            _modelMatrix = Matrix4.CreateScale(_scaleCurrent) * _rotation * Matrix4.CreateTranslation(Position);
        }

        /// <summary>
        /// Konstruktormethode für Partikel
        /// </summary>
        /// <param name="x">x-Position</param>
        /// <param name="y">y-Position</param>
        /// <param name="z">z-Position</param>
        /// <param name="scale">Skalierung</param>
        /// <param name="type">Art der Partikelanimation</param>
        public ParticleObject(float x, float y, float z, float scale, ParticleType type)
        {
            _scale.X = HelperGL.Clamp(scale, 0.0001f, float.MaxValue);
            _scale.Y = HelperGL.Clamp(scale, 0.0001f, float.MaxValue);
            _scale.Z = HelperGL.Clamp(scale, 0.0001f, float.MaxValue);
            _scaleCurrent.X = HelperGL.Clamp(scale, 0.0001f, float.MaxValue);
            _scaleCurrent.Y = HelperGL.Clamp(scale, 0.0001f, float.MaxValue);
            _scaleCurrent.Z = HelperGL.Clamp(scale, 0.0001f, float.MaxValue);

            Position = new Vector3(x, y, z);

            _type = type;

            _info = KWEngine.ParticleDictionary[_type];
            _modelMatrix = Matrix4.CreateScale(_scaleCurrent) * _rotation * Matrix4.CreateTranslation(Position);
        }

        internal void Act()
        {
            long now = DeltaTime.Watch.ElapsedMilliseconds;
            if (KWEngine.CurrentWorld.IsFirstPersonMode)
            {
                Vector3 fpPos = KWEngine.CurrentWorld.GetFirstPersonObject().Position;
                fpPos.Y += KWEngine.CurrentWorld.GetFirstPersonObject().FPSEyeOffset;
                Quaternion tmp = HelperRotation.GetRotationForPoint(Position, fpPos);
                _rotation = Matrix4.CreateFromQuaternion(tmp * Turn180);
            }
            else
            {
                Quaternion tmp = HelperRotation.GetRotationForPoint(Position, KWEngine.CurrentWorld.GetCameraPosition());
                _rotation = Matrix4.CreateFromQuaternion(tmp * Turn180);
            }

            
            long diff = _lastUpdate < 0 ? 0 : now - _lastUpdate;
            _aliveInMS += diff;
            _frame = (int)(_aliveInMS / 32);
            int frameloop = _frame % _info.Samples;

            if (_type == ParticleType.LoopSmoke1 || _type == ParticleType.LoopSmoke2 || _type == ParticleType.LoopSmoke3)
            {
                _frame = frameloop;
                float liveInPercent = _aliveInMS / (float)_durationInMS;
                // f(x) = -64000(x - 0.5)¹⁶ + 1
                _scaleFactor = -64000f * (float)Math.Pow(liveInPercent - 0.5f, 16) + 1;
                _scaleCurrent.X = _scale.X * _scaleFactor;
                _scaleCurrent.Y = _scale.Y * _scaleFactor;
                _scaleCurrent.Z = _scale.Z * _scaleFactor;

                if (_aliveInMS > _durationInMS)
                {
                    KWEngine.CurrentWorld.RemoveParticleObject(this);
                }
            }
            else
            {
                if (_frame > _info.Samples - 1)
                {
                    KWEngine.CurrentWorld.RemoveParticleObject(this);
                }
            }
                    
            _modelMatrix = Matrix4.CreateScale(_scaleCurrent) * _rotation * Matrix4.CreateTranslation(Position);
            _lastUpdate = now;
        }
    }
}
