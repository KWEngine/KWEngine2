using KWEngine2.GameObjects;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects.Main
{
    class Panel : GameObject
    {
        private Vector4 _glow = new Vector4(1, 0, 0, 0);
        private float _intensity = 0;
        private bool _gain = true;
        public override void Act(KeyboardState ks, MouseState ms, float deltaTimeFactor)
        {
            
            _intensity = _gain ? _intensity + 0.02f * deltaTimeFactor : _intensity - 0.02f * deltaTimeFactor;
            if(_intensity > 1f)
            {
                _gain = false;
                _intensity = 1f;
            }
            if(_intensity < 0)
            {
                _gain = true;
                _intensity = 0;
            }

            _glow.W = _intensity;
            this.Glow = _glow;
            
        }
    }
}
