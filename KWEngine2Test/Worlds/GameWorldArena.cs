using KWEngine2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using KWEngine2Test.Objects.Arena;

namespace KWEngine2Test.Worlds
{
    class GameWorldArena : World
    {
        private Player _player = new Player();

        public override void Act(KeyboardState kb, MouseState ms, float deltaTimeFactor)
        {
            if (kb[Key.Escape])
            {
                CurrentWindow.SetWorld(new GameWorldStart());
                return;
            }

            if (kb[Key.P])
            {
                KWEngine.GlowRadius += 0.01f;
                Console.WriteLine(KWEngine.GlowRadius);
            }
            if (kb[Key.O])
            {
                KWEngine.GlowRadius -= 0.01f;
                Console.WriteLine(KWEngine.GlowRadius);
            }

            if (kb[Key.J])
            {
                KWEngine.PostProcessQuality = KWEngine.PostProcessingQuality.Low;
                Console.WriteLine("Low");
            }
            if (kb[Key.K])
            {
                KWEngine.PostProcessQuality = KWEngine.PostProcessingQuality.Standard;
                Console.WriteLine("Medium");
            }
            if (kb[Key.L])
            {
                KWEngine.PostProcessQuality = KWEngine.PostProcessingQuality.High;
                Console.WriteLine("High");
            }
        }

        public override void Prepare()
        {
            FOV = 90;
            //SetSunPosition(200, 200, 50);
            //SetSunColor(1, 0.75f, 0.5f, 1);
            SunAmbientFactor = 0.8f;
            SetSunPosition(25, 50, 100);
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
            

            /*
            KWEngine.LoadModelFromFile("ArenaOuter", @".\Models\ArenaOuter\ArenaOuter.fbx");
            KWEngine.LoadModelFromFile("ArenaPlatform", @".\Models\ArenaOuter\ArenaPlatform.obj");
            KWEngine.LoadModelFromFile("ArenaPlatforms", @".\Models\ArenaOuter\ArenaPlatforms.fbx");

            KWEngine.BuildTerrainModel("Arena", @".\textures\heightmapArena.png", @".\textures\sand_diffuse.png", 150, 10, 150, 7.5f, 7.5f);
            Immovable terra = new Immovable();
            terra.SetModel("Arena");
            terra.SetPosition(0, -0.5f, 0);
            terra.SetTexture(@".\textures\sand_normal.png", KWEngine.TextureType.Normal);
            AddGameObject(terra);

            Immovable floor = new Immovable();
            floor.SetModel("KWCube");
            floor.SetScale(80, 5, 80);
            floor.SetPosition(0, -2.5f, 0);
            floor.SetTextureRepeat(5, 5);
            floor.IsCollisionObject = true;
            floor.SetTexture(@".\textures\sand_diffuse.png");
            floor.SetTexture(@".\textures\sand_normal.png", KWEngine.TextureType.Normal);
            AddGameObject(floor);

            Immovable arenaOuter = new Immovable();
            arenaOuter.SetModel("ArenaOuter");
            arenaOuter.IsCollisionObject = true;
            arenaOuter.IsShadowCaster = true;
            AddGameObject(arenaOuter);

            Immovable arenaPlatforms = new Immovable();
            arenaPlatforms.SetModel("ArenaPlatforms");
            arenaPlatforms.IsCollisionObject = true;
            arenaPlatforms.IsShadowCaster = true;
            AddGameObject(arenaPlatforms);

            PlatformUpDown testPlatform = new PlatformUpDown();
            testPlatform.SetModel("ArenaPlatform");
            testPlatform.SetScale(1.5f);
            testPlatform.SetPosition(15, 1.5f, 0);
            testPlatform.IsCollisionObject = true;
            testPlatform.IsShadowCaster = true;
            //testPlatform.SetGlow(1, 0, 0, 1);
            AddGameObject(testPlatform);
            */

            _player = new Player();
            _player.SetModel("KWCube");
            _player.SetScale(0.5f, 1, 0.5f);
            _player.IsShadowCaster = false;
            _player.IsCollisionObject = true;
            _player.SetPosition(0, 0.5f, 0);
            _player.FPSEyeOffset = 1.25f;
            _player.UpdateLast = true;
            AddGameObject(_player);
            SetFirstPersonObject(_player, 0);

            SetTextureSkybox(@".\textures\skybox1.jpg");
            //SetTextureSkyboxRotation(90);
            DebugShowPerformanceInTitle = KWEngine.PerformanceUnit.FramesPerSecond;

        }
    }
}
