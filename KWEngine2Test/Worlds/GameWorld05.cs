using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KWEngine2;
using KWEngine2.GameObjects;
using KWEngine2.Helper;
using KWEngine2Test.Objects.ThirdPerson;
using OpenTK.Input;

namespace KWEngine2Test.Worlds
{
    class GameWorld05 : World
    {
        private Player _player;
        private LightObject _sun;
        //private long _lastspawn = 0;

        public override void Act(KeyboardState ks, MouseState ms)
        {
            if (ks[Key.Escape])
            {
                CurrentWindow.SetWorld(new GameWorldStart());
                return;
            }
        }

        public override void Prepare()
        {
            KWEngine.LoadModelFromFile("UBot", @".\Models\JumpAndRun\UBot.fbx");

            SetTextureSkybox(@".\Textures\skybox2.dds");
            SetTextureBackgroundBrightnessMultiplier(3);
            SetAmbientLight(1, 1, 1, 0.25f);
            WorldDistance = 50;

            // Player object:
            _player = new Player();
            _player.SetModel("UBot");
            _player.SetScale(1.8f);
            _player.SetRotation(0, 180, 0);
            _player.SetMetalness(0.1f);
            _player.Name = "Player";
            _player.SetPosition(0, 0, 0);
            _player.IsCollisionObject = true;
            _player.IsShadowCaster = true;
            _player.UpdateLast = true;
            AddGameObject(_player);

            Platform floor = new Platform();
            floor.SetModel("KWCube");
            floor.SetTexture(@".\textures\mpanel_diffuse.dds");
            floor.SetTexture(@".\textures\mpanel_normal.dds", TextureType.Normal);
            floor.SetTexture(@".\textures\mpanel_roughness.dds", TextureType.Roughness);
            floor.SetTexture(@".\textures\mpanel_metalness.dds", TextureType.Metalness);
            floor.SetTextureRepeat(5, 5);
            floor.IsCollisionObject = true;
            floor.SetPosition(0, -1f, 0);
            floor.SetScale(50, 2, 50);
            AddGameObject(floor);
            
            _sun = new LightObject(LightType.Sun, true);
            _sun.SetColor(1, 1, 1, 0.75f);
            _sun.SetPosition(100, 100, 50);
            _sun.SetTarget(0, 0, 0);
            _sun.SetFOV(45);
            _sun.SetFOVBiasCoefficient(0.0001f);
            AddLightObject(_sun);
            //DebugShadowLight = _sun;

            SetCameraPosition(0, 0, 0);
            SetCameraTarget(0, 0, 0);
            FOV = 90;

            CurrentWindow.CursorVisible = false;
            CurrentWindow.CursorGrabbed = true;
        }
    }
}
