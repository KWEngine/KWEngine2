﻿using KWEngine2;
using KWEngine2.Collision;
using KWEngine2.GameObjects;
using KWEngine2.Model;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KWEngine2Test.GameObjects
{
    public class Ship : GameObject
    {
        private long last = 0;
        private float p = 0;
        public override void Act(KeyboardState ks, MouseState ms, float deltaTimeFactor)
        {
            if (!CurrentWindow.Focused) // || !CurrentWindow.IsMouseInWindow)
                return;
            if (CurrentWorld.IsFirstPersonMode)
            {
                float forward = 0;
                float strafe = 0;
                if (ks[Key.W])
                    forward += 1 * deltaTimeFactor;
                if (ks[Key.S])
                    forward -= 1 * deltaTimeFactor;
                if (ks[Key.D])
                    strafe += 1 * deltaTimeFactor;
                if (ks[Key.A])
                    strafe -= 1 * deltaTimeFactor;

                MoveAndStrafeFirstPerson(forward, strafe, 0.1f);
                MoveFPSCamera(ms);

                if (ks[Key.Q])
                    this.MoveOffset(0, -0.1f * deltaTimeFactor, 0);
                if (ks[Key.E])
                    this.MoveOffset(0, +0.1f * deltaTimeFactor, 0);
            }
            else
            {
                if (ks[Key.D])
                    this.MoveOffset(0.1f * deltaTimeFactor, 0, 0);
                if (ks[Key.S])
                    this.MoveOffset(0, 0, 0.1f * deltaTimeFactor);
                if (ks[Key.A])
                    this.MoveOffset(-0.1f * deltaTimeFactor, 0, 0);
                if (ks[Key.W])
                    this.MoveOffset(0, 0, -0.1f * deltaTimeFactor);

                if (ks[Key.Q])
                    this.MoveOffset(0, -0.1f * deltaTimeFactor, 0);
                if (ks[Key.E])
                    this.MoveOffset(0, +0.1f * deltaTimeFactor, 0);

                if (ks[Key.T])
                    this.AddRotationZ(1, true);
            }
            /*
            List<Intersection> intersections = GetIntersections();
            foreach (Intersection i in intersections)
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
            */

            //Vector3 pos = GetMouseIntersectionPoint(ms, Plane.Z);
            //TurnTowardsXY(pos);

            if (ms.LeftButton == ButtonState.Pressed)
            {
                long now = GetCurrentTimeInMilliseconds();
                if (now - last > 500)
                {


                    Shot s = new Shot();
                    s.SetModel(KWEngine.GetModel("KWCube"));
                    s.SetScale(0.1f,0.1f,1);
                    s.SetRotation(RotationFirstPersonObject);
                    s.SetPosition(Position);
                    s.ColorEmissive = new Vector4(0.5f, 0.1f, 0.1f, 1);
                    s.IsAffectedByLight = false;
                    s.IsAffectedBySun = false;
                    CurrentWorld.AddGameObject(s);

                    last = now;
                }
                
            }

            if (HasAnimations)
            {
                AnimationID = 0;
                AnimationPercentage = p;
                p = (p + 0.01f) % 1f;
            }
        }
    }
}
