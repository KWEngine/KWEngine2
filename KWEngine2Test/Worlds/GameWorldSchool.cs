using KWEngine2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using KWEngine2Test.Objects.School;

namespace KWEngine2Test.Worlds
{
    class GameWorldSchool : World
    {
        private Player _player = new Player();

        public override void Act(KeyboardState kb, MouseState ms, float deltaTimeFactor)
        {
            if (kb[Key.Escape])
            {
                CurrentWindow.SetWorld(new GameWorldStart());
                return;
            }
        }

        public override void Prepare()
        {
            FOV = 90;
            SunAmbientFactor = 0.8f;
            SetSunPosition(25, 20, 50);
            SetSunTarget(10, 0, 0);
            //KWEngine.ShadowMapCoefficient = 0.00075f;
            //DebugShowHitboxes = true;
            //DebugShadowCaster = true;

            KWEngine.LoadModelFromFile("Main", @".\models\forum\forum_main.fbx");
            KWEngine.LoadModelFromFile("Upper", @".\models\forum\forum_upper.fbx");
            KWEngine.LoadModelFromFile("Stairs", @".\models\forum\forum_stairs.fbx");
            KWEngine.LoadModelFromFile("Tables", @".\models\forum\forum_tables.fbx");
            KWEngine.LoadModelFromFile("HBMain", @".\models\forum\forum_hitbox_main.fbx");
            KWEngine.LoadModelFromFile("HBUpper", @".\models\forum\forum_hitbox_upper.fbx");
            KWEngine.LoadModelFromFile("HBStairs", @".\models\forum\forum_hitbox_stairs.fbx");

            Immovable main = new Immovable();
            main.SetModel("Main");
            main.IsShadowCaster = true;
            AddGameObject(main);
            
            Immovable upper = new Immovable();
            upper.SetModel("Upper");
            upper.IsShadowCaster = true;
            AddGameObject(upper);

            Immovable stairs = new Immovable();
            stairs.SetModel("Stairs");
            stairs.IsShadowCaster = true;
            AddGameObject(stairs);

            Immovable tables = new Immovable();
            tables.SetModel("Tables");
            tables.IsCollisionObject = true;
            tables.IsShadowCaster = true;
            AddGameObject(tables);
            
            Immovable hbMain = new Immovable();
            hbMain.SetModel("HBMain");
            hbMain.IsCollisionObject = true;
            hbMain.Opacity = 0;
            AddGameObject(hbMain);
            
            Immovable hbUpper = new Immovable();
            hbUpper.SetModel("HBUpper");
            hbUpper.IsCollisionObject = true;
            hbUpper.Opacity = 0;
            AddGameObject(hbUpper);

            Immovable hbStairs = new Immovable();
            hbStairs.SetModel("HBStairs");
            hbStairs.IsCollisionObject = true;
            hbStairs.Opacity = 0;
            AddGameObject(hbStairs);
            
            _player = new Player();
            _player.SetModel("KWCube");
            _player.SetScale(0.5f, 1.8f, 0.5f);
            _player.IsShadowCaster = false;
            _player.IsCollisionObject = true;
            _player.SetPosition(0, 0.9f, 0);
            _player.FPSEyeOffset = 0.75f;
            _player.UpdateLast = true;
            AddGameObject(_player);
            SetFirstPersonObject(_player, 0);

            SetTextureSkybox(@".\textures\skybox1.jpg");
            //SetTextureSkyboxRotation(90);
            DebugShowPerformanceInTitle = KWEngine.PerformanceUnit.FramesPerSecond;

        }
    }
}
