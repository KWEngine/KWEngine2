using System.Runtime.CompilerServices;
using KWEngine2;
using KWEngine2.GameObjects;
using OpenTK;
using OpenTK.Input;
using KWEngine2Test.Objects;
using KWEngine2.Helper;
using KWEngine2Test.Objects.SpaceInvaders;

namespace KWEngine2Test.Worlds
{
    class GameWorldSpaceInvaders : World
    {
        private long _timestampLast = 0;
        private Player _p;
        private bool _test = false;

        public override void Act(KeyboardState kb, MouseState ms, float deltaTimeFactor)
        {
            if (kb[Key.Escape])
            {
                CurrentWindow.SetWorld(new GameWorldStart());
                return;
            }

            long now = GetCurrentTimeInMilliseconds();
            long diff = now - _timestampLast;
            
            if (!_test)
            {
                if(diff > 500)
                {
                    EnemySimple es = new EnemySimple();
                    es.SetModel("Spaceship6");
                    es.Name = "Enemy";
                    es.SetRotation(90, 0, 0);
                    es.SetPosition(HelperRandom.GetRandomNumber(-17f, 17f), 10.5f, 0);
                    es.IsCollisionObject = true;
                    AddGameObject(es);

                    _timestampLast = now;
                }
            }

        }
     
        public override void Prepare()
        {
            SunAmbientFactor = 0.9f;
            KWEngine.LoadModelFromFile("Spaceship4", @".\Models\Spaceship\spaceship4.obj");
            KWEngine.LoadModelFromFile("Spaceship5", @".\Models\Spaceship\spaceship5.obj");
            KWEngine.LoadModelFromFile("Spaceship2", @".\Models\Spaceship\spaceship2.obj");
            KWEngine.LoadModelFromFile("Spaceship6", @".\Models\Spaceship\spaceship6.obj");

            SoundPlay(@".\audio\dom.ogg", true, 0.4f);

            if (!_test)
            {
                _p = new Player();
                _p.SetModel("Spaceship4");
                _p.Name = "Player";
                _p.SetPositionY(-9);
                _p.SetRotation(90, 0, 180);
                _p.IsCollisionObject = true;
                AddGameObject(_p);
            }
            else
            {
                Box b1 = new Box();
                b1.SetModel("KWCube");
                b1.IsCollisionObject = true;
                b1.SetColor(1, 0, 0);
                b1.SetScale(10, 1, 1);
                b1.SetPosition(-5, 5, 0);
                b1.Name = "A";

                Box b2 = new Box();
                b2.SetModel("KWCube");
                b2.IsCollisionObject = true;
                b2.SetColor(0, 1, 0);
                b2.SetScale(1, 1, 1);
                b2.SetPosition(-8, 4, 0);
                b2.Name = "B";

                Box b3 = new Box();
                b3.SetModel("KWCube");
                b3.IsCollisionObject = true;
                b3.SetColor(1, 1, 0);
                b3.SetScale(3, 1, 1);
                b3.SetPosition(-5, 3, 0);
                b3.Name = "C";

                Box b4 = new Box();
                b4.SetModel("KWCube");
                b4.IsCollisionObject = true;
                b4.SetColor(0, 0, 1);
                b4.SetPosition(-3.5f, 2, 0);
                b4.SetScale(3, 1, 1);
                b4.Name = "D";

                Box b5 = new Box();
                b5.SetModel("KWCube");
                b5.IsCollisionObject = true;
                b5.SetColor(0, 1, 1);
                b5.SetScale(2, 1, 1);
                b5.SetPosition(3, 1, 0);
                b5.Name = "E";

                Box b6 = new Box();
                b6.SetModel("KWCube");
                b6.IsCollisionObject = true;
                b6.SetColor(1, 1, 1);
                b6.SetScale(2, 1, 1);
                b6.SetPosition(6, 0, 0);
                b6.Name = "F";

                Box b7 = new Box();
                b7.SetModel("KWCube");
                b7.IsCollisionObject = true;
                b7.SetColor(0.5f, 1, 0.5f);
                b7.SetScale(2, 1, 1);
                b7.SetPosition(7, -1, 0);
                b7.Name = "G";


                AddGameObject(b1);
                AddGameObject(b2);
                AddGameObject(b3);
                AddGameObject(b4);
                AddGameObject(b5);
                AddGameObject(b6);
                AddGameObject(b7);
            }

            FOV = 90;
            SetTextureBackground(@".\textures\spacebackground.jpg", 2, 2);
            KWEngine.DebugShowPerformanceInTitle = PerformanceUnit.FramesPerSecond;
        }

    }
}
