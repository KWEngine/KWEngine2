﻿using KWEngine2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using KWEngine2Test.Objects.Arena;
using KWEngine2.Helper;

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
        }

        public override void Prepare()
        {
            FOV = 90;
            SetSunPosition(200, 200, 50);
            SetSunColor(1, 0.75f, 0.5f, 0.7f);
            SetSunAmbientFactor(0.25f);
            KWEngine.ShadowMapCoefficient = 0.00075f;
           
            
            KWEngine.LoadModelFromFile("ArenaOuter", @".\Models\ArenaOuter\ArenaOuter.fbx");
            KWEngine.LoadModelFromFile("ArenaPlatform", @".\Models\ArenaOuter\ArenaPlatform.obj");
            KWEngine.LoadModelFromFile("ArenaPlatforms", @".\Models\ArenaOuter\ArenaPlatforms.fbx");

            KWEngine.BuildTerrainModel("Arena", @".\textures\heightmapArena.png", @".\textures\sand_diffuse.jpg", 150, 10, 150, 7.5f, 7.5f);
            Immovable terra = new Immovable();
            terra.SetModel("Arena");
            terra.SetPosition(0, -0.5f, 0);
            terra.SetTexture(@".\textures\sand_normal.jpg", TextureType.Normal);
            AddGameObject(terra);


            Immovable floor = new Immovable();
            floor.SetModel("KWCube");
            floor.SetScale(80, 5, 80);
            floor.SetPosition(0, -2.5f, 0);
            floor.SetTextureRepeat(5, 5);
            floor.IsCollisionObject = true;
            floor.SetTexture(@".\textures\sand_diffuse.jpg");
            floor.SetTexture(@".\textures\sand_normal.jpg", TextureType.Normal);
            AddGameObject(floor);

            
            
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
            _player.SetPosition(25, 1f, 25);
            _player.FPSEyeOffset = 0.75f;
            _player.UpdateLast = true;
            AddGameObject(_player);
            SetFirstPersonObject(_player, 230);

            SetTextureSkybox(@".\textures\skybox1.jpg");
            DebugShowPerformanceInTitle = PerformanceUnit.FramesPerSecond;

        }
    }
}
