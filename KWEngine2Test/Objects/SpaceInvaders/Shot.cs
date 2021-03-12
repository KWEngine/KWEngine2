using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KWEngine2;
using KWEngine2.Collision;
using KWEngine2.GameObjects;
using OpenTK.Input;

namespace KWEngine2Test.Objects.SpaceInvaders
{
    class Shot : GameObject
    {
        private float _movementSpeed = 0.3f;
        private GameObject _parent = null;

        public Shot(GameObject parent)
        {
            _parent = parent;
        }

        public override void Act(KeyboardState ks, MouseState ms)
        {
            if (!IsInsideScreenSpace)
            {
                CurrentWorld.RemoveGameObject(this);
                return;
            }

            Move(_movementSpeed * KWEngine.DeltaTimeFactor);

            Intersection intersection;
            if(_parent is Player)
                intersection = GetIntersection(0, 0, 0, typeof(Enemy));
            else
                intersection = GetIntersection(0, 0, 0, typeof(Player));

            if (intersection != null && !intersection.Object.Equals(_parent))
                {
                    if (_parent is Player)
                    {
                        if (intersection.Object is Enemy)
                        {
                            ((Enemy)intersection.Object).ReduceHealth(100);
                            CurrentWorld.RemoveGameObject(this);
                            Explosion ex = new Explosion(Position, 16);
                            ex.SetColor(1, 1, 1);
                            ex.SetGlow(1, 1, 0.5f, 0.25f);
                            CurrentWorld.AddGameObject(ex);
                        }
                    }
                    else if (_parent is Enemy)
                    {
                        if (intersection.Object is Player)
                        {
                            CurrentWorld.RemoveGameObject(intersection.Object);
                            Explosion ex1 = new Explosion(intersection.Object.Position, 128);
                            ex1.SetColor(0, 1, 0);
                            ex1.SetGlow(0, 1, 0.5f, 1);
                            CurrentWorld.AddGameObject(ex1);

                            CurrentWorld.RemoveGameObject(this);
                            Explosion ex = new Explosion(Position, 16);
                            ex.SetColor(1, 1, 1);
                            ex.SetGlow(1, 1, 0.5f, 0.25f);
                            CurrentWorld.AddGameObject(ex);
                        }
                    }

                }
            
            
        }
    }
}
