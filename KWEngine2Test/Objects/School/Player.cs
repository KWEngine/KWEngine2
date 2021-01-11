using KWEngine2;
using KWEngine2.Collision;
using KWEngine2.GameObjects;
using KWEngine2.Helper;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects.School
{
    public class Player : GameObject
    {
        private enum PlayerState
        {
            OnFloor = 0,
            Jump = 2,
            Fall = 3
        }

        private PlayerState _state = PlayerState.Fall;
        private bool _jumpButtonPressed = false;

        private float _movementSpeed = 0.1f;
        private float _momentum = 0;
        private float _gravity = 0.02f;

        public override void Act(KeyboardState ks, MouseState ms, float deltaTimeFactor)
        {
            if (!CurrentWindow.Focused)
                return;

            // Basic controls:
            if (CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
            {
                float forward = 0;
                float strafe = 0;
                if (ks[Key.A])
                {
                    strafe -= 1;
                }
                if (ks[Key.D])
                {
                    strafe += 1;
                }
                if (ks[Key.W])
                {
                    forward += 1;
                }
                if (ks[Key.S])
                {
                    forward -= 1;
                }

                MoveAndStrafeFirstPerson(forward, strafe, _movementSpeed * deltaTimeFactor);
            }

            if (_state == PlayerState.OnFloor && ms.RightButton == ButtonState.Pressed && !_jumpButtonPressed)
            {
                _state = PlayerState.Jump;
                _momentum = 0.15f;
                _jumpButtonPressed = true;
            }
            if(ms.RightButton == ButtonState.Released)
            {
                _jumpButtonPressed = false;
            }

            DoStates();
            DoCollisionDetection();

            if (Position.Y < -5)
            {
                SetPosition(0, 0.9f, 0);
                _momentum = 0;
            }

            MoveFPSCamera(ms);
        }

        private void DoStates()
        {
            if (_state == PlayerState.Jump)
            {
                MoveOffset(0, _momentum * KWEngine.DeltaTimeFactor, 0);
                _momentum -= _gravity * KWEngine.DeltaTimeFactor;
                if (_momentum < 0)
                {
                    _momentum = 0;
                    _state = PlayerState.Fall;
                }
            }
            else if (_state == PlayerState.Fall)
            {
                MoveOffset(0, _momentum * KWEngine.DeltaTimeFactor, 0);
                _momentum -= _gravity * KWEngine.DeltaTimeFactor;
            }
            else if (_state == PlayerState.OnFloor)
            {
                MoveOffset(0, -0.0001f, 0);
            }
        }

        private void DoCollisionDetection()
        {
            List<Intersection> collisionlist = GetIntersections();
            bool upCorrection = false;
            float maxYUpCorrection = 0;
            foreach (Intersection i in collisionlist)
            {
                if (i.MTV.Y > maxYUpCorrection)
                    maxYUpCorrection = i.MTV.Y;

                MoveOffset(new Vector3(i.MTV.X, 0, i.MTV.Z));
                if (i.MTV.Y > 0)
                {
                    if (_state == PlayerState.OnFloor)
                    {
                        upCorrection = true;
                    }
                    else if (_state == PlayerState.Fall)
                    {
                        upCorrection = true;
                        _state = PlayerState.OnFloor;
                    }
                }
                else if (i.MTV.Y < 0 && Math.Abs(i.MTV.Y) > Math.Abs(i.MTV.X) && Math.Abs(i.MTV.Y) > Math.Abs(i.MTV.Z))
                {
                    if (_state == PlayerState.Jump)
                    {
                        _state = PlayerState.Fall;
                        _momentum = 0;
                    }
                }
            }
            MoveOffset(0, maxYUpCorrection, 0);

            if (_state == PlayerState.OnFloor && !upCorrection)
            {
                _state = PlayerState.Fall;
                _momentum = 0;
            }
        }
    }
}