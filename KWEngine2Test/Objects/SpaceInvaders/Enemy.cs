using KWEngine2.GameObjects;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects.SpaceInvaders
{
    class Enemy : GameObject
    {
        protected float _movementSpeed = 0.1f;
        protected long _spawnTime = 0;
        protected int _health = 100;

        public void SetHealth(int amount)
        {
            _health = amount;
        }

        public int GetHealth()
        {
            return _health;
        }

        public void ReduceHealth(int amount)
        {
            _health -= amount;
        }

        public override void Act(KeyboardState ks, MouseState ms)
        {
            if(_health <= 0)
            {
                Explosion ex = new Explosion(Position, 16);
                ex.SetColor(1, 1, 1);
                ex.SetGlow(1, 0, 1f, 0.5f);
                CurrentWorld.AddGameObject(ex);

                CurrentWorld.RemoveGameObject(this);
            }
        }
    }
}
