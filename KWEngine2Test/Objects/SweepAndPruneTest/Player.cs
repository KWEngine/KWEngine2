using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KWEngine2;
using KWEngine2.Collision;
using KWEngine2.GameObjects;
using OpenTK;
using OpenTK.Input;

namespace KWEngine2Test.Objects.SweepAndPruneTest
{
    public class Player : GameObject
    {
        private float _movementSpeed = 0.125f;
        private long _timestampLastShot = 0;
        private long _cooldown = 100;

        public override void Act(KeyboardState ks, MouseState ms)
        {
            Vector3 mousePoint = GetMouseIntersectionPoint(ms, Plane.Z);
            TurnTowardsXY(mousePoint);

            if (ks[Key.A] && Position.X > -17)
                MoveOffset(-_movementSpeed * KWEngine.DeltaTimeFactor, 0, 0);
            if (ks[Key.D] && Position.X < 17)
                MoveOffset(+_movementSpeed * KWEngine.DeltaTimeFactor, 0, 0);
            if (ks[Key.W] && Position.Y < 10)
                MoveOffset(0, +_movementSpeed * KWEngine.DeltaTimeFactor, 0);
            if (ks[Key.S] && Position.Y > -10)
                MoveOffset(0, -_movementSpeed * KWEngine.DeltaTimeFactor, 0);

            if (ms.LeftButton == ButtonState.Pressed || ks[Key.Space])
            {
                long timestampNow = GetCurrentTimeInMilliseconds();

                if (timestampNow - _timestampLastShot > _cooldown)
                {
                    Vector3 lav = GetLookAtVector();

                    Shot s = new Shot(this);
                    s.SetModel("KWCube");
                    s.Name = "PlayerShot";
                    s.SetRotation(this.Rotation);
                    s.SetPosition(this.Position + lav);
                    s.SetScale(0.075f, 0.075f, 0.5f);
                    s.IsCollisionObject = true;
                    s.SetGlow(0, 1, 0, 1);
                    CurrentWorld.AddGameObject(s);

                    _timestampLastShot = timestampNow;
                }
            }

            Intersection i = GetIntersection(0, 0, 0, typeof(Enemy));
            if (i != null)
            {
                CurrentWorld.RemoveGameObject(this);
                Explosion ex = new Explosion(Position, 512, 5, ExplosionType.SkullRingZ);
                CurrentWorld.AddGameObject(ex);
            }
        }
    }
}
