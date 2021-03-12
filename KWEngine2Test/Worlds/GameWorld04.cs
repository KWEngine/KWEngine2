using KWEngine2;
using KWEngine2.GameObjects;
using KWEngine2.Helper;
using KWEngine2Test.Objects.JumpAndRun.Actors;
using KWEngine2Test.Objects.JumpAndRun.Platforms;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Worlds
{
    class GameWorld04 : World
    {
        private Player _player;

        public override void Act(KeyboardState ks, MouseState ms)
        {
            if (ks[Key.Escape])
            {
                CurrentWindow.SetWorld(new GameWorldStart());
                return;
            }

            if (_player != null)
            {
                SetCameraPosition(_player.Position.X, 0, 50);
                SetCameraTarget(_player.Position.X, 0, 0);
            }
        }

        public override void Prepare()
        {
            KWEngine.LoadModelFromFile("UBot", @".\Models\JumpAndRun\UBot.fbx");
            KWEngine.LoadModelFromFile("Platform10", @".\Models\JumpAndRun\Platform10.obj");
            KWEngine.LoadModelFromFile("Platform02", @".\Models\JumpAndRun\Platform02.obj");

            SetTextureBackground(@".\Textures\bg_greenmountains.png", 1, 1);
            SetTextureBackgroundBrightnessMultiplier(0.75f);
            //SetSunColor(1, 1, 1, 1);
            WorldDistance = 1000;

            SetCameraPosition(0, 0, 50);
            SetCameraTarget(0, 0, 0);
            FOV = 30;

            //GeneratePlatforms();

            // Player object:
            _player = new Player();
            _player.SetModel("UBot");
            _player.SetScale(2);
            _player.SetColor(0.9f, 0.9f, 0.9f);
            _player.SetRotation(0, 90, 0);
            _player.Name = "Heinz";
            _player.SetPosition(0, 4f, 0);
            _player.IsCollisionObject = true;
            _player.UpdateLast = true;
            //AddGameObject(_player);

            Platform floor = new Platform();
            floor.SetModel("Platform02");
            floor.IsCollisionObject = true;
            floor.SetPosition(0, 3f, 0);
            AddGameObject(floor);

            //LightObject l1 = new LightObject(LightType.Directional, true);
            //AddLightObject(l1);
        }

        private void GeneratePlatforms()
        {
            int i = -10;
            int lastWidth = 0;
            while (i < 200)
            {
                i += lastWidth;
                Platform floor = new Platform();
                floor.SetModel(HelperRandom.GetRandomNumber(0, 1) == 0 ? "Platform02" : "Platform10");
                float width = floor.GetMaxDimensions().X;
                if ((int)width > lastWidth)
                {
                    i += (int)(width / 2) + 1;
                }
                else if ((int)width == lastWidth)
                {
                    i += 3;
                }
                else
                {
                    i += 0;
                }
                floor.IsCollisionObject = true;
                floor.SetPosition(i, HelperRandom.GetRandomNumber(-3, -0.5f), 0);
                AddGameObject(floor);

                lastWidth = (int)width;
            }
        }

        public void Reset()
        {
            _player.SetPosition(0, 4, 0);
        }
    }
}
