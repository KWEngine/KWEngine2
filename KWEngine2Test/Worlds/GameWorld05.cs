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
        private HUDObject _crosshair;
        //private long _lastspawn = 0;

        public override void Act(KeyboardState ks, MouseState ms)
        {
            if (ks[Key.Escape])
            {
                CurrentWindow.CursorVisible = true;
                CurrentWindow.CursorGrabbed = false;
                CurrentWindow.SetWorld(new GameWorldStart());
                return;
            }
        }

        public override void Prepare()
        {
            KWEngine.LoadModelFromFile("UBot", @".\Models\JumpAndRun\UBot.fbx");

            SetTextureSkybox(@".\Textures\skybox3.dds");
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
            //floor.SetTexture(@".\textures\mpanel_roughness.dds", TextureType.Roughness);
            //floor.SetTexture(@".\textures\mpanel_metalness.dds", TextureType.Metalness);
            floor.SetRoughness(0.25f);
            floor.SetMetalness(0.1f);
            floor.SetTextureRepeat(5, 5);
            floor.IsCollisionObject = true;
            floor.SetPosition(0, -1f, 0);
            floor.SetScale(50, 2, 50);
            AddGameObject(floor);
            
            _sun = new LightObject(LightType.Sun, true);
            _sun.SetColor(1, 1, 1, 0.75f);
            _sun.SetPosition(-100, 100, 10);
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

            // Place some obstacles:
            Immovable i1 = new Immovable();
            i1.SetModel("KWCube");
            i1.SetPosition(2.5f, 1f, -5);
            i1.SetScale(2);
            i1.IsPickable = true;
            i1.Name = "Obstacle Box #1";
            i1.SetRoughness(0.5f);
            i1.SetColor(1, 0.25f, 0.25f);
            i1.SetTexture(@".\textures\MetalPlates006_1K_ColorBright.jpg");
            i1.SetSpecularReflectionEnabled(true);
            i1.IsShadowCaster = true;
            i1.IsCollisionObject = true;
            AddGameObject(i1);

            // Place HUD crosshair:
            _crosshair = new HUDObject(HUDObjectType.Image, CurrentWindow.Width / 2, CurrentWindow.Height / 2);
            _crosshair.SetTexture(@".\textures\crosshair.dds");
            _crosshair.SetScale(64, 64);
            AddHUDObject(_crosshair);
        }
    }
}
