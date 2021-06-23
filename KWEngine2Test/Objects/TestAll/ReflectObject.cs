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
        private Vector3 _currentDirection = Vector3.Normalize(KWEngine.WorldUp + new Vector3(0.1f, 0, 0));
        private float _speed = 0.25f;
        private bool _moving = false;
        private const float COOLDOWN = 5f;
        private float currentLengthAfterCollision = 0;

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
                currentLengthAfterCollision += (_currentDirection.LengthFast * _speed * KWEngine.DeltaTimeFactor);
                Console.WriteLine(currentLengthAfterCollision);
            }
            else
            {
                if (ks[Key.Space])
                    _moving = true;
            }

            Intersection intersection = GetIntersection();
            if(intersection != null )
            {
                if(intersection.Object is ReflectPaddle)
                {
                    if (currentLengthAfterCollision >= COOLDOWN)
                    {
                        MoveOffset(intersection.MTV);
                        SetPositionZ(0);
                        _currentDirection = ReflectVector(_currentDirection, intersection.ColliderSurfaceNormal);
                        _currentDirection.Z = 0;

                        // reset collision cooldown:
                        currentLengthAfterCollision = 0;
                        Console.WriteLine("-------------------------------------");
                    }
                }
                else
                {
                    MoveOffset(intersection.MTV);
                    SetPositionZ(0);
                    _currentDirection = ReflectVector(_currentDirection, intersection.ColliderSurfaceNormal);
                    _currentDirection.Z = 0;
                }
                

                
            }
        }
    }
}
