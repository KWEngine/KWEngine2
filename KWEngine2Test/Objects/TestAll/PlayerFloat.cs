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

namespace KWEngine2Test.Objects.TestAll
{
    public class PlayerFloat : GameObject
    {
        private float _speed = 0.1f;

        public override void Act(KeyboardState ks, MouseState ms, float deltaTimeFactor)
        {
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

                if (ks[Key.Q])
                {
                    MoveOffset(0, -_speed * deltaTimeFactor, 0);
                }
                if (ks[Key.E])
                {
                    MoveOffset(0, _speed * deltaTimeFactor, 0);
                }

                MoveAndStrafeFirstPersonXYZ(forward, strafe, _speed * deltaTimeFactor);
                MoveFPSCamera(ms);
            }
        }
    }
}
