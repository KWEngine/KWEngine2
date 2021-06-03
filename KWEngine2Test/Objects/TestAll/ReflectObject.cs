using KWEngine2.GameObjects;
using KWEngine2;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Input;
using KWEngine2.Collision;

namespace KWEngine2Test.Objects.TestAll
{
    class ReflectObject : GameObject
    {
        private Vector3 _currentDirection = KWEngine.WorldUp;
        private float _speed = 0.25f;
        private bool _moving = false;

        public bool IsInMotion
        {
            get
            {
                return _moving;
            }
        }

        public override void Act(KeyboardState ks, MouseState ms)
        {
            if (_moving)
            {
                MoveAlongVector(_currentDirection, _speed * KWEngine.DeltaTimeFactor);
            }
            else
            {
                if (ks[Key.Space])
                    _moving = true;
            }

            Intersection intersection = GetIntersection();
            if(intersection != null)
            {
                MoveOffset(intersection.MTV);
                _currentDirection = ReflectVector(_currentDirection, intersection.ColliderSurfaceNormal);

                //MoveAlongVector(_currentDirection, intersection.MTV.Length);
            }
        }
    }
}
