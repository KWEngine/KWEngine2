using KWEngine2;
using KWEngine2.GameObjects;
using KWEngine2.Collision;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects.ThirdPerson
{
    class Shot : GameObject
    {
        private float _speed = 0.25f;
        private float _distance = 0;
        public override void Act(KeyboardState ks, MouseState ms)
        {
            float currentSpeed = _speed * KWEngine.DeltaTimeFactor;
            _distance += currentSpeed;
            Move(currentSpeed);

            Intersection i = GetIntersection();
            if(i != null && !(i.Object is Player))
            {
                Explosion ex = new Explosion(Position, ExplosionType.Sphere);
                CurrentWorld.AddGameObject(ex);
                CurrentWorld.RemoveGameObject(this);
            }
            else if (_distance > 50)
            {
                CurrentWorld.RemoveGameObject(this);
            }
        }
    }
}
