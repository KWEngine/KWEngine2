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
            SetSunPosition(200, 200, 50);
            SetSunColor(1, 0.75f, 0.5f, 1);
            KWEngine.ShadowMapCoefficient = 0.00075f;
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

            _player = new Player();
            _player.SetModel("KWCube");
            _player.SetScale(1, 2, 1);
            _player.IsShadowCaster = false;
            _player.IsCollisionObject = true;
            _player.SetPosition(25, 0, 15);
            _player.FPSEyeOffset = 0.75f;
            _player.UpdateLast = true;
            AddGameObject(_player);
            SetFirstPersonObject(_player, -230);

            SetTextureSkybox(@".\textures\skybox1.jpg", 1, 0.75f, 0.5f);
            //SetTextureSkyboxRotation(90);
            DebugShowPerformanceInTitle = KWEngine.PerformanceUnit.FramesPerSecond;

        }
    }
}
