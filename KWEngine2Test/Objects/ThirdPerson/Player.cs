﻿using KWEngine2;
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
        private readonly Vector3 _offsetVertical = new Vector3(0, 1, 0);
        private readonly long _cooldown = 150;
        private long _lastShot = 0;

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

            Vector3 bodyCenter = new Vector3(Position.X, GetCenterPointForAllHitboxes().Y, Position.Z);


            Vector2 msMovement = GetMouseCursorMovement(ms);
            AddRotationY(-msMovement.X * 40);
            Vector3 camTargetWithOffset = DoCameraPosition(msMovement);
            Vector3 cameraLav = GetLookAtVector();
            Vector3 cameraLavRotated = HelperRotation.RotateVector(cameraLav, -90, Plane.Y);

            float currentSpeed = _speed * KWEngine.DeltaTimeFactor;

            
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

            
            if (ks[Key.ShiftLeft] || ms[MouseButton.Left])
            {
                DoShoot(ms, camTargetWithOffset);
                //_attacking = true;
                //_percentage = 0.25f;
            }
            //if (_attacking && _percentage >= 1)
            //{
            //    _attacking = false;
            //    _percentage = 0;
            //}
            


            DoStates();
            DoCollisionDetection();
            DoAnimation();

            /*
            List<GameObject> objects = PickGameObjectsFromLookAtVector(GetLookAtVector() + new Vector3(0, 1, 0), 0);
            foreach(GameObject g in objects)
            {
                Console.WriteLine(CurrentWorld.GetCurrentTimeInMilliseconds() + ": " + g.Name);
            }
            */
        }

        private void DoShoot(MouseState ms, Vector3 offset)
        {
           // Console.WriteLine(ms.X + " | " + ms.Y);
            if (CurrentWorld.GetCurrentTimeInMilliseconds() - _lastShot > _cooldown)
            {
                Shot s = new Shot();
                s.SetModel("KWSphere");
                s.SetScale(0.25f);
                s.SetColor(0, 0, 1);
                s.SetGlow(0, 0, 1, 0.25f);
                s.IsCollisionObject = true;
                s.SetPosition(GetCenterPointForAllHitboxes() + GetLookAtVector() * 1);
                
                Vector3 target = World.GetMouseIntersectionPoint(ms, Plane.Camera, -50, 35, 5);
                s.TurnTowardsXYZ(target);
                //s.AddRotationY(-2.5f, true);
                


                CurrentWorld.AddGameObject(s);

                _lastShot = CurrentWorld.GetCurrentTimeInMilliseconds();
            }
        }

        private Vector3 DoCameraPosition(Vector2 m)
        {
            _currentCameraRotationDegrees.X += m.X * 40;
            _currentCameraRotationDegrees.Y += m.Y * 40;
            if (_currentCameraRotationDegrees.Y < -75)
                _currentCameraRotationDegrees.Y = -75;
            if (_currentCameraRotationDegrees.Y > 5)
                _currentCameraRotationDegrees.Y = 5;

            float lav_factor = (0.00012f * (_currentCameraRotationDegrees.Y * _currentCameraRotationDegrees.Y) + 0.02099f * _currentCameraRotationDegrees.Y + 0.89190f);
            float lav_factor2 = _currentCameraRotationDegrees.Y >= -15 ? (_currentCameraRotationDegrees.Y + 15) / 20f : 0f;
            
            Vector3 offset1 = HelperRotation.RotateVector(GetLookAtVector(), -90, Plane.Y) * 1 + GetLookAtVector() * 5 * lav_factor;
            Vector3 offset2 = HelperRotation.RotateVector(GetLookAtVector(), -90, Plane.Y) * 1 + GetLookAtVector() * 2 + _offsetVertical * 2 * lav_factor2;
            Vector3 arcBallCenter = new Vector3(Position.X, GetCenterPointForAllHitboxes().Y, Position.Z);

            Vector3 newCamPos = HelperRotation.CalculateRotationForArcBallCamera(
                arcBallCenter, 
                10f, 
                _currentCameraRotationDegrees.X, 
                _currentCameraRotationDegrees.Y,
                false,
                false);
            CurrentWorld.SetCameraPosition(newCamPos + offset1);
            CurrentWorld.SetCameraTarget(new Vector3(Position.X, GetCenterPointForAllHitboxes().Y, Position.Z) + offset2);

            return offset2;
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
                if (i.Object is Shot)
                    continue;

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
