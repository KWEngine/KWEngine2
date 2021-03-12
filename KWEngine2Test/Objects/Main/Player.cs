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

namespace KWEngine2Test.Objects.Main
{
    class Player : GameObject
    {
        public LightObject _flashlight { get; set; } = new LightObject(LightType.Directional, true);
        private float _animationPercentage = 0;
        private float _height = 50;

        public override void Act(KeyboardState ks, MouseState ms)
        {
            bool runs = false;
            if (CurrentWorld.IsFirstPersonMode && CurrentWorld.GetFirstPersonObject().Equals(this))
            {
                float forward = 0;
                float strafe = 0;
                if (ks[Key.A])
                {
                    strafe -= 1;
                    runs = true;
                }
                if (ks[Key.D])
                {
                    strafe += 1;
                    runs = true;
                }
                if (ks[Key.W])
                {
                    forward += 1;
                    runs = true;
                }
                if (ks[Key.S])
                {
                    forward -= 1;
                    runs = true;
                }
                MoveFPSCamera(ms);
                MoveAndStrafeFirstPerson(forward, strafe, 0.1f * KWEngine.DeltaTimeFactor);
                FPSEyeOffset = 5;
                if (ks[Key.Q])
                {
                    MoveOffset(0, -0.2f, 0);
                }
                if (ks[Key.E])
                {
                    MoveOffset(0, +0.2f, 0);
                }
            }
            else
            {
                TurnTowardsXZ(GetMouseIntersectionPoint(ms, Plane.Y));
                Vector3 cameraLookAt = GetCameraLookAtVector();
                cameraLookAt.Y = 0;
                cameraLookAt.NormalizeFast();

                Vector3 strafe = HelperRotation.RotateVector(cameraLookAt, 90, Plane.Y);

                if (ks[Key.A])
                {
                    MoveAlongVector(strafe, 0.1f * KWEngine.DeltaTimeFactor);
                    runs = true;
                }
                if (ks[Key.D])
                {
                    MoveAlongVector(strafe, -0.1f * KWEngine.DeltaTimeFactor);
                    runs = true;
                }
                if (ks[Key.W])
                {
                    MoveAlongVector(cameraLookAt, 0.1f * KWEngine.DeltaTimeFactor);
                    runs = true;
                }
                if (ks[Key.S])
                {
                    MoveAlongVector(cameraLookAt, -0.1f * KWEngine.DeltaTimeFactor);
                    runs = true;
                }

                if (ks[Key.T])
                    MoveOffset(0, 0.2f * KWEngine.DeltaTimeFactor, 0);
                
                if (ks[Key.Q])
                {
                    _height += 0.5f;
                }
                if (ks[Key.E])
                {
                    _height -= 0.5f;
                }
                
                
            }

            if (IsMouseCursorInsideMyHitbox(ms))
            {
                SetColorOutline(0, 1, 0, 0.2f);
                
            }
            else
            {
                SetColorOutline(0, 1, 0, 0);
            }

            /*
            if (ms.LeftButton == ButtonState.Pressed)
            {
                GameObject o = PickGameObject(ms);
                Console.WriteLine(o);
            }
            */

            MoveOffset(0, -0.1f * KWEngine.DeltaTimeFactor, 0);
            List<Intersection> intersections = GetIntersections();
            foreach(Intersection i in intersections)
            {
                if (i.IsTerrain)
                {
                    SetPosition(Position.X, i.HeightOnTerrainSuggested, Position.Z);
                }
                else
                {
                    Position += i.MTV;
                }
            }

            AdjustFlashlight();
            AdjustAnimation(runs, KWEngine.DeltaTimeFactor);

            Vector3 camPos = this.Position + new Vector3(50, _height, 50);
            camPos.Y = _height;
            CurrentWorld.SetCameraPosition(camPos);
            CurrentWorld.SetCameraTarget(Position.X, 0, Position.Z);

        }

        private void AdjustAnimation(bool runs, float dt)
        {
            if (Model.Animations != null && Model.Animations.Count > 0)
            {
                AnimationID = runs ? 2 : 0;
                _animationPercentage = (_animationPercentage + 0.02f * dt) % 1;
                AnimationPercentage = _animationPercentage;
            }
        }

        private void AdjustFlashlight()
        {
            if (_flashlight.CurrentWorld != null)
            {
                if (CurrentWorld.IsFirstPersonMode)
                {
                    Vector3 lookAt = GetLookAtVector();
                    Vector3 middle = this.GetCenterPointForAllHitboxes();
                    Vector3 lookAtRot = HelperRotation.RotateVector(lookAt, -70, Plane.Y);
                    middle.Y += Scale.Y / 3f;
                    Vector3 source = middle + lookAtRot * 0.8f;
                    
                    _flashlight.SetPosition(source);
                    if (_flashlight.Type == LightType.Directional)
                    {
                        lookAt.Y = lookAt.Y - 0.02f;
                        _flashlight.SetTarget(middle + lookAt * 2f);
                    }
                }
                else
                {
                    Vector3 lookAt = GetLookAtVector();
                    Vector3 middle = this.GetCenterPointForAllHitboxes();
                    middle.Y += Scale.Y / 3f;
                    Vector3 source = middle + lookAt * 0.7f;
                    _flashlight.SetPosition(source);
                    if (_flashlight.Type == LightType.Directional)
                    {
                        lookAt.Y = lookAt.Y - 0.075f;
                        _flashlight.SetTarget(middle + lookAt * 0.8f);
                    }

                }

            }
        }
    }
}
