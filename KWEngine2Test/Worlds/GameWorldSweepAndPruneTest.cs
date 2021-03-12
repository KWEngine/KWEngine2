using KWEngine2;
using KWEngine2.Helper;
using OpenTK.Input;
using KWEngine2Test.Objects.SweepAndPruneTest;

namespace KWEngine2Test.Worlds
{
    class GameWorldSweepAndPruneTest : World
    {
        
        public override void Act(KeyboardState kb, MouseState ms)
        {
           
        }
     
        public override void Prepare()
        {
            KWEngine.DebugShowPerformanceInTitle = PerformanceUnit.FrameTimeInMilliseconds;

            FOV = 90;
            Player p = new Player();
            p.SetModel("KWCube");
            p.SetPosition(0, -8, 0);
            p.IsCollisionObject = true;
            AddGameObject(p);

            SpawnEnemies();

            /*
            SweepCube c1 = new SweepCube();
            c1.Name = "c1";
            c1.SetModel("KWCube");
            c1.SetPosition(-12, 15, 0);
            c1.SetScale(5, 1, 1);
            c1.IsCollisionObject = true;
            AddGameObject(c1);

            SweepCube c2 = new SweepCube();
            c2.Name = "c2";
            c2.SetModel("KWCube");
            c2.SetPosition(-5, 5, 0);
            c2.SetScale(6, 1, 1);
            c2.IsCollisionObject = true;
            AddGameObject(c2);

            SweepCube c3 = new SweepCube();
            c3.Name = "c3";
            c3.SetModel("KWCube");
            c3.SetPosition(-7, 0, 0);
            c3.SetScale(1, 1, 1);
            c3.IsCollisionObject = true;
            AddGameObject(c3);

            SweepCube c4 = new SweepCube();
            c4.Name = "c4";
            c4.SetModel("KWCube");
            c4.SetPosition(-5, -5, 0);
            c4.SetScale(1, 1, 1);
            c4.IsCollisionObject = true;
            AddGameObject(c4);

            SweepCube c5 = new SweepCube();
            c5.Name = "c5";
            c5.SetModel("KWCube");
            c5.SetPosition(2, -15, 0);
            c5.SetScale(5, 1, 1);
            c5.IsCollisionObject = true;
            AddGameObject(c5);
            */
        }

        private void SpawnEnemies()
        {
            for(int i = 0; i < 100; i++)
            {
                KWEngine2Test.Objects.SweepAndPruneTest.Enemy e = new Enemy();
                e.SetModel("KWSphere");
                e.SetPosition((i - 50) / 2f, HelperRandom.GetRandomNumber(-7f, 10f), 0);
                e.IsCollisionObject = true;
                AddGameObject(e);
            }
        }

    }
}
