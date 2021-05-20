using KWEngine2.GameObjects;
using KWEngine2;
using KWEngine2.Collision;
using OpenTK.Input;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects.TestAll
{
    class PlayerSphere : GameObject
    {
        private float _speed = 0.05f;

        public override void Act(KeyboardState ks, MouseState ms)
        {
            if (ks[Key.W])
                MoveOffset(0, 0, -_speed);
            if (ks[Key.S])
                MoveOffset(0, 0, +_speed);
            if (ks[Key.A])
                MoveOffset(-_speed, 0, 0);
            if (ks[Key.D])
                MoveOffset(+_speed, 0, 0);

            if (ks[Key.Q])
                MoveOffset(0, -_speed, 0);
            if (ks[Key.E])
                MoveOffset(0, +_speed, 0);

            if (ks[Key.F])
                AddRotationY(1, true);
            if (ks[Key.G])
                AddRotationZ(1, true);

            Intersection i = GetIntersection();
            if(i != null)
            {
                // MoveOffset(i.MTVUp);
                MoveOffset(i.MTV);
            }
        }
    }
}
