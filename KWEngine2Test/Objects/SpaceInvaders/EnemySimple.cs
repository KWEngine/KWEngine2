using KWEngine2;
using KWEngine2.Helper;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects.SpaceInvaders
{
    class EnemySimple : Enemy
    {
        private long _timestampLastShot;

        public EnemySimple()
        {
            _spawnTime = GetCurrentTimeInMilliseconds();
            _timestampLastShot = _spawnTime + HelperRandom.GetRandomNumber(0, 1000);
        }

        public override void Act(KeyboardState ks, MouseState ms)
        {

            long now = GetCurrentTimeInMilliseconds();

            float z = (float)Math.Sin(_spawnTime + now / 75.0);
            //float y = -0.001f * (now * now * now) + 0.1f * (now * now);
            MoveOffset(z * KWEngine.DeltaTimeFactor * _movementSpeed, -_movementSpeed * KWEngine.DeltaTimeFactor, 0);

            if(now - _timestampLastShot > 200)
            {
                Vector3 lav = GetLookAtVector();

                Shot s = new Shot(this);
                s.SetModel("KWCube");
                s.SetRotation(this.Rotation);
                s.AddRotationZ(10, true);
                s.SetPosition(this.Position + 0.5f * lav);
                s.SetScale(0.075f, 0.075f, 0.5f);
                s.IsCollisionObject = true;
                s.SetGlow(1, 0.5f, 0, 1);
                CurrentWorld.AddGameObject(s);

                Shot s2 = new Shot(this);
                s2.SetModel("KWCube");
                s2.SetRotation(this.Rotation);
                s2.AddRotationZ(-10, true);
                s2.SetPosition(this.Position + 0.5f * lav);
                s2.SetScale(0.075f, 0.075f, 0.5f);
                s2.IsCollisionObject = true;
                s2.SetGlow(1, 0.5f, 0, 1);
                CurrentWorld.AddGameObject(s2);


                _timestampLastShot = now + HelperRandom.GetRandomNumber(0, 500);
            }

            if (!IsInsideScreenSpace)
                CurrentWorld.RemoveGameObject(this);

            base.Act(ks, ms);
        }
    }
}
