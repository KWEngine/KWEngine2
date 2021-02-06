using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using KWEngine2.Helper;
using KWEngine2;
using System.IO;
using OpenTK.Input;

namespace KWEngine2.GameObjects
{
    /// <summary>
    /// Art des HUD-Objekts
    /// </summary>
    public enum HUDObjectType { 
        /// <summary>
        /// Bild
        /// </summary>
        Image, 
        /// <summary>
        /// Text
        /// </summary>
        Text }

    /// <summary>
    /// HUD-Klasse
    /// </summary>
    public sealed class HUDObject
    {
        /// <summary>
        /// Gibt an, ob das HUDObject sichtbar ist oder nicht (Standard: true)
        /// </summary>
        public bool IsVisible { get; set; } = true;
        internal Vector2 _absolute = new Vector2(0, 0);
        internal Vector4 _tint = new Vector4(1, 1, 1, 1);
        internal Vector4 _glow = new Vector4(0, 0, 0, 0);
        internal HUDObjectType _type = HUDObjectType.Image;
        internal int _textureId = KWEngine.TextureAlpha;
        internal int[] _offsets = new int[] { 0 };
        /// <summary>
        /// Position
        /// </summary>
        public Vector3 Position { get; internal set; } = Vector3.Zero;
        /// <summary>
        /// Aktuelle Welt
        /// </summary>
        public World CurrentWorld { get; internal set; } = null;
        internal Vector3 _scale = new Vector3(32f, 32f, 1f);
        internal Quaternion _rotation = Quaternion.Identity;
        internal Matrix4 _rotationMatrix = Matrix4.Identity;
        internal List<Vector3> _positions = new List<Vector3>();
        internal List<Matrix4> _modelMatrices = new List<Matrix4>();
        internal Matrix4 _scaleMatrix = Matrix4.CreateScale(32f,32f, 1f);
        internal string _text = null;
        internal int _count = 1;

        /// <summary>
        /// Name der Instanz
        /// </summary>
        public string Name { get; set; } = "undefined HUD object.";

        /// <summary>
        /// Schriftart des HUD-Objekts
        /// </summary>
        public FontFace Font { get; private set; } = FontFace.Anonymous;

        internal float _spread = 26f;
        /// <summary>
        /// Laufweite der Buchstaben (Standard: 26)
        /// </summary>
        public float CharacterSpreadFactor
        {
            get
            {
                return _spread;
            }
            set
            {
                _spread = HelperGL.Clamp(value, 1f, 1024f);
                UpdatePositions();
            }
        }

        /// <summary>
        /// Setzt die Rotation des HUD-Objekts
        /// </summary>
        /// <param name="x">X-Rotation in Grad</param>
        /// <param name="y">Y-Rotation in Grad</param>
        public void SetRotation(float x, float y)
        {
            _rotation = HelperRotation.GetQuaternionForEulerDegrees(x, y, 0);
            _rotationMatrix = Matrix4.CreateFromQuaternion(_rotation);
            UpdatePositions();
        }

        /// <summary>
        /// Färbung des Objekts
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
        /// Glow-Effekt des Objekts
        /// </summary>
        /// <param name="red">Rot</param>
        /// <param name="green">Grün</param>
        /// <param name="blue">Blau</param>
        /// <param name="intensity">Intensität</param>
        public void SetGlow(float red, float green, float blue, float intensity)
        {
            _glow.X = HelperGL.Clamp(red, 0, 1);
            _glow.Y = HelperGL.Clamp(green, 0, 1);
            _glow.Z = HelperGL.Clamp(blue, 0, 1);
            _glow.W = HelperGL.Clamp(intensity, 0, 1);
        }

        private void UpdateTextures()
        {
            _textureId = KWEngine.FontTextureArray[(int)Font];
            for (int i = 0; i < _count; i++)
            {
                int offset = (int)_text[i] - 32;
                _offsets[i] = offset;
            }
        }

        /// <summary>
        /// Setzt die Größe
        /// </summary>
        /// <param name="width">Breite</param>
        /// <param name="height">Höhe</param>
        public void SetScale(float width, float height)
        {
            _scale.X = HelperGL.Clamp(width, 0.001f, float.MaxValue);
            _scale.Y = HelperGL.Clamp(height, 0.001f, float.MaxValue);
            _scale.Z = 1;
            _scaleMatrix = Matrix4.CreateScale(_scale);
            UpdatePositions();
        }

        /// <summary>
        /// Setzt den Text
        /// </summary>
        /// <param name="text">Text</param>
        public void SetText(string text)
        {
            if(_type == HUDObjectType.Text && text != null && text.Length > 0)
            {
                _text = text.Trim();
                _count = _text.Length;
                _offsets = new int[_count];
                UpdatePositions();
                UpdateTextures();
            }
            else
            {
                throw new Exception("SetText() may only be called if the HUDObject is of type 'Text'. Text may also be not empty.");
            }
        }

        internal void SetTextureInternal(string filename, bool isFile)
        {
            if (File.Exists(filename) && _type == HUDObjectType.Image)
            {
                lock (KWEngine.CustomTextures)
                {
                    if (KWEngine.CustomTextures[KWEngine.CurrentWorld].ContainsKey(filename))
                    {
                        _textureId = KWEngine.CustomTextures[KWEngine.CurrentWorld][filename];
                    }
                    else
                    {
                        _textureId = isFile ? HelperTexture.LoadTextureForBackgroundExternal(filename) : HelperTexture.LoadTextureForBackgroundInternal(filename);
                        KWEngine.CustomTextures[KWEngine.CurrentWorld].Add(filename, _textureId);
                    }
                }
                _count = 1;
            }
            else
            {
                throw new Exception("Error: Is your HUD Type set to 'Image'? Or maybe the file " + filename + " does not exist?");
            }
        }

        /// <summary>
        /// Setzt die Textur
        /// </summary>
        /// <param name="filename">Bilddatei</param>
        /// <param name="isFile">false, wenn die Datei Teil der EXE ist (Eingebettete Ressource)</param>
        public void SetTexture(string filename, bool isFile = true)
        {
            if (GLWindow.CurrentWindow._multithreaded)
            {
                Action a = () => SetTextureInternal(filename, isFile);
                HelperGLLoader.AddCall(this, a);
            }
            else
            {
                SetTextureInternal(filename, isFile);
            }
        }

        /// <summary>
        /// Konstruktormethode
        /// </summary>
        /// <param name="type">Art des Objekts </param>
        /// <param name="x">Breitenposition</param>
        /// <param name="y">Höhenposition</param>
        public HUDObject(HUDObjectType type, float x, float y)
        {
            _type = type;
            Position = new Vector3(x - KWEngine.CurrentWindow.Width / 2, KWEngine.CurrentWindow.Height - y - KWEngine.CurrentWindow.Height / 2, 0);
            _absolute.X = x;
            _absolute.Y = y;
            
            UpdatePositions();
            
        }

        /// <summary>
        /// Setzt die Position
        /// </summary>
        /// <param name="x">Breite in Pixeln</param>
        /// <param name="y">Höhe in Pixeln</param>
        public void SetPosition(float x, float y)
        {
            Position = new Vector3(x - KWEngine.CurrentWindow.Width / 2, KWEngine.CurrentWindow.Height - y - KWEngine.CurrentWindow.Height / 2, 0);
            _absolute.X = x;
            _absolute.Y = y;
            UpdatePositions();
        }

        private void SetPosition(int index, Vector3 pos)
        {
            pos.X = pos.X + (CharacterSpreadFactor * index);
            lock (_positions)
            {
                _positions.Add(pos);
            }
            lock (_modelMatrices)
            {
                _modelMatrices.Add(_scaleMatrix * _rotationMatrix * Matrix4.CreateTranslation(pos));
            }
        }

        internal void UpdatePositions()
        {
            lock (_positions)
            {
                lock (_modelMatrices)
                {
                    _positions.Clear();
                    _modelMatrices.Clear();
                    for (int i = 0; i < _count; i++)
                    {
                        SetPosition(i, Position);
                    }
                }
            }
        }

        /// <summary>
        /// Prüft, ob der Mauszeiger auf dem HUD-Objekt ist
        /// </summary>
        /// <param name="ms">Mausinfo</param>
        /// <returns>true, wenn die Maus auf dem HUD-Objekt ist</returns>
        public bool IsMouseCursorOnMe(MouseState ms)
        {
            
            GLWindow w = KWEngine.CurrentWindow;

            if (w._windowRect.Contains(ms.X, ms.Y)){
                Vector2 coords = HelperGL.GetNormalizedMouseCoords(ms.X, ms.Y, w);
                float left, right, top, bottom;

                if(_type == HUDObjectType.Image)
                {
                    left = _absolute.X - _scale.X * 0.5f;
                    right = _absolute.X + _scale.X * 0.5f;

                    top = _absolute.Y - _scale.Y * 0.5f;
                    bottom = _absolute.Y + _scale.Y * 0.5f;
                }
                else
                {
                    left = _absolute.X - _scale.X * 0.5f;
                    right = _absolute.X + ((_count - 1) * _spread) + _scale.X * 0.5f;

                    top = _absolute.Y - _scale.Y * 0.5f;
                    bottom = _absolute.Y + _scale.Y * 0.5f;
                }

                if(coords.X >= left && coords.X <= right && coords.Y >= top && coords.Y <= bottom)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
                return false;
        }

        /// <summary>
        /// Setzt die Schriftart der Instanz
        /// </summary>
        /// <param name="fontFace">zu nutzende Schriftart</param>
        public void SetFont(FontFace fontFace)
        {
            Font = fontFace;
        }
    }
}
