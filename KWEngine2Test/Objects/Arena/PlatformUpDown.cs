using KWEngine2;
using KWEngine2.Collision;
using KWEngine2.GameObjects;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects.Arena
{
    enum Direction { Up, Down, WaitUp, WaitDown }
    class PlatformUpDown : GameObject
    {
        private Direction _dir = Direction.WaitUp;
        private float _stateTime = 0;
        private float _limitLow = 1.5f;
        private float _limitUp = 9f;

        public override void Act(KeyboardState ks, MouseState ms)
        {
            long timeStamp = GetCurrentTimeInMilliseconds();
            if((int)_dir > 1 && timeStamp - _stateTime > 3000)
            {
                ChangeState(timeStamp);
            }

            if (_dir == Direction.WaitUp)
            {
                
            }
            else if (_dir == Direction.WaitDown)
            {

            }
            else if (_dir == Direction.Up)
            {
                MoveOffset(0, 0.1f * KWEngine.DeltaTimeFactor, 0);
                if(Position.Y > _limitUp)
                {
                    ChangeState(timeStamp);
                }
            }
            else if (_dir == Direction.Down)
            {
                MoveOffset(0, -0.1f * KWEngine.DeltaTimeFactor, 0);
                if (Position.Y <= _limitLow)
                {
                    ChangeState(timeStamp);
                }
            }

            Intersection i = GetIntersection();
            if(i != null && i.Object is Player)
            {
                if(i.MTV.Y > 0)
                {
                    MoveOffset(i.MTV);
                    ChangeState(timeStamp, true);
                }
            }
        }

        private void ChangeState(long timeStamp, bool collision = false)
        {
            if (collision)
            {
                if (_dir == Direction.Up)
                {
                    _dir = Direction.Down;
                }
                else if (_dir == Direction.Down)
                {
                    _dir = Direction.Up;
                }
            }
            else
            {
                if (_dir == Direction.WaitUp)
                {
                    _dir = Direction.Up;
                }
                else if (_dir == Direction.WaitDown)
                {
                    _dir = Direction.Down;
                }
                else if (_dir == Direction.Up)
                {
                    _dir = Direction.WaitDown;
                }
                else
                {
                    _dir = Direction.WaitUp;
                }
            }
            _stateTime = timeStamp;
        }
    }
}
