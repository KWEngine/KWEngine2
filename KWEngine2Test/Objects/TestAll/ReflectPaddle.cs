using KWEngine2.GameObjects;
using KWEngine2;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects.TestAll
{
    class ReflectPaddle : GameObject
    {
        private float _speed = 0.2f;
        private ReflectObject _theBall;

        public ReflectPaddle(ReflectObject ball)
        {
            _theBall = ball;
        }
        public override void Act(KeyboardState ks, MouseState ms)
        {
            if (ks[Key.A] || ks[Key.Left])
                MoveOffset(-_speed * KWEngine.DeltaTimeFactor, 0, 0);
            if (ks[Key.D] || ks[Key.Right])
                MoveOffset(_speed * KWEngine.DeltaTimeFactor, 0, 0);

            if(_theBall != null && !_theBall.IsInMotion)
            {
                _theBall.SetPosition(Position.X, Position.Y + 2, Position.Z);
            }
        }
    }
}
