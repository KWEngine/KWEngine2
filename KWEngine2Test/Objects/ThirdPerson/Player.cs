using KWEngine2;
using KWEngine2.Collision;
using KWEngine2.GameObjects;
using KWEngine2.Helper;
using KWEngine2.Model;
using KWEngine2Test.Worlds;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;

namespace KWEngine2Test.Objects.ThirdPerson
{
    public class Player : GameObject
    {
        private enum PlayerState
        {
            OnFloor = 0,
            Jump = 2,
            Fall = 3
        }

        private float _percentage = 0.0f;
        private PlayerState _state = PlayerState.Fall;
        private bool _running = false;
        private bool _upKeyPressed = false;
        private bool _attacking = false;

        private float _momentum = 0;
        private float _gravity = 0.02f;
        private float _speed = 0.1f;
        private float _bgOffset = 0;
        private Vector2 _currentCameraRotationDegrees = new Vector2(0, 20);

        private void MoveBackground(float offset)
        {
            _bgOffset += offset / 100;
            CurrentWorld.SetTextureBackgroundOffset(_bgOffset, 0);
        }

        public override void Act(KeyboardState ks, MouseState ms)
        {
            if (Position.Y < -25)
            {
                SetPosition(0, 0, 0);
                return;
            }
            
            Vector2 msMovement = GetMouseCursorMovement(ms);
            DoCameraPosition(msMovement);

            Vector3 cameraLav = GetCameraLookAtVector();
            cameraLav.Y = 0;
            cameraLav.NormalizeFast();
            Vector3 cameraLavRotated = HelperRotation.RotateVector(cameraLav, -90, Plane.Y);
            float currentSpeed = _speed * KWEngine.DeltaTimeFactor;

            TurnTowardsXZ(Position + cameraLav);
            if (ks[Key.A] || ks[Key.D] || ks[Key.W] || ks[Key.S])
            {
                if (ks[Key.W])
                {
                    MoveAlongVector(cameraLav, currentSpeed);
                }
                if (ks[Key.S])
                {
                    MoveAlongVector(cameraLav, -currentSpeed);
                }
                if(ks[Key.A])
                {
                    MoveAlongVector(-cameraLavRotated, currentSpeed);
                }
                if (ks[Key.D])
                {
                    MoveAlongVector(cameraLavRotated, currentSpeed);
                }

                _running = true;
                _attacking = false;
            }
            else
            {
                if (_running)
                {
                    _percentage = 0;
                    _running = false;
                }

            }

            if (_state == PlayerState.OnFloor && (ks[Key.Space] || ms[MouseButton.Right]))
            {
                if (!_upKeyPressed)
                {
                    _state = PlayerState.Jump;
                    _percentage = 0;
                    _momentum = 0.35f;
                    _upKeyPressed = true;
                    _attacking = false;

                }
            }
            else if (!(ks[Key.Space] || ms[MouseButton.Right]))
            {
                _upKeyPressed = false;
            }

            /*
            if (ks[Key.Space] && _state == PlayerState.OnFloor && !_running)
            {
                _attacking = true;
                _percentage = 0.25f;
            }
            if (_attacking && _percentage >= 1)
            {
                _attacking = false;
                _percentage = 0;
            }
            */


            DoStates();
            DoCollisionDetection();
            DoAnimation();
            
        }

        private void DoCameraPosition(Vector2 m)
        {
            _currentCameraRotationDegrees.X += m.X * 40;
            _currentCameraRotationDegrees.Y += m.Y * 40;
            if (_currentCameraRotationDegrees.Y < -85)
                _currentCameraRotationDegrees.Y = -85;
            if (_currentCameraRotationDegrees.Y > 2)
                _currentCameraRotationDegrees.Y = 2;

            //Vector3 offset = HelperRotation.RotateVector(GetLookAtVector(), -90, Plane.Y) * 2;
            Vector3 arcBallCenter = new Vector3(Position.X, GetCenterPointForAllHitboxes().Y, Position.Z);

            Vector3 newCamPos = HelperRotation.CalculateRotationForArcBallCamera(
                arcBallCenter, 
                10f, 
                _currentCameraRotationDegrees.X, 
                _currentCameraRotationDegrees.Y,
                false,
                false);
            CurrentWorld.SetCameraPosition(newCamPos);
            CurrentWorld.SetCameraTarget(new Vector3(Position.X, GetCenterPointForAllHitboxes().Y, Position.Z));
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
                        _percentage = 0.5f;
                    }
                }
            }
            MoveOffset(0, maxYUpCorrection, 0);

            if (_state == PlayerState.OnFloor && !upCorrection)
            {
                _state = PlayerState.Fall;
                _attacking = false;
                _momentum = 0;
                _percentage = 0.5f;
            }
        }
        private void DoAnimation()
        {
            if (this.HasAnimations)
            {
                if (_state == PlayerState.OnFloor)
                {
                    if (_running)
                    {
                        this.AnimationID = 2;
                        _percentage = (_percentage + 0.040f * KWEngine.DeltaTimeFactor) % 1.0f;
                    }
                    else if (_attacking)
                    {
                        this.AnimationID = 1;
                        _percentage = (_percentage + 0.045f * KWEngine.DeltaTimeFactor);
                    }
                    else
                    {
                        this.AnimationID = 0;
                        _percentage = (_percentage + 0.0025f * KWEngine.DeltaTimeFactor) % 1.0f;
                    }
                }
                else if (_state == PlayerState.Jump || _state == PlayerState.Fall)
                {
                    this.AnimationID = 3;
                    _percentage = (_percentage + 0.025f * KWEngine.DeltaTimeFactor) % 1.0f;
                }

                AnimationPercentage = _percentage;

            }
        }
    }
}
