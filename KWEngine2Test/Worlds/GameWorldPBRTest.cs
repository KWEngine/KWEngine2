using KWEngine2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using KWEngine2Test.Objects;
using KWEngine2.GameObjects;
using KWEngine2.Helper;
using KWEngine2Test.Objects.Main;

namespace KWEngine2Test.Worlds
{
    class GameWorldPBRTest : World
    {
        private long timestamp = 0;
        private HUDObject ho;
        private HUDObject ho2;

        public override void Act(KeyboardState kb, MouseState ms, float deltaTimeFactor)
        {
            if (kb[Key.Escape])
            {
                CurrentWindow.SetWorld(new GameWorldStart());
                return;
            }
            
            if(kb[Key.U])
            {
                float i = GetSunColor().W;
                i -= 0.05f;
                i = HelperGL.Clamp(i, 0, 2);
                SetSunColor(GetSunColor().X, GetSunColor().Y, GetSunColor().Z, i);
                Console.WriteLine("SunIntensity: " + i);
            }
            if (kb[Key.I])
            {
                float i = GetSunColor().W;
                i += 0.05f;
                i = HelperGL.Clamp(i, 0, 2);
                SetSunColor(GetSunColor().X, GetSunColor().Y, GetSunColor().Z, i);
                Console.WriteLine("SunIntensity: " + i);
            }

            if (kb[Key.J])
            {
                float i = SunAmbientFactor;
                i -= 0.01f;
                i = HelperGL.Clamp(i, 0, 1);
                SetSunAmbientFactor(i);
                Console.WriteLine("Ambient: " + i);
            }
            if (kb[Key.K])
            {
                float i = SunAmbientFactor;
                i += 0.01f;
                i = HelperGL.Clamp(i, 0, 1);
                SetSunAmbientFactor(i);
                Console.WriteLine("Ambient: " + i);
            }

            long now = GetCurrentTimeInMilliseconds();
            if(now - timestamp > 3000)
            {
                ParticleObject p = new ParticleObject(new Vector3(-2, -1, 0), new Vector3(1, 1, 1), ParticleType.BurstFirework2);
                AddParticleObject(p);
                timestamp = now;
            }
            

            if(ho != null)
            {
                
                if(ho.IsMouseCursorOnMe(ms))
                {
                    ho.SetGlow(1, 1, 1, 1);
                }
                else
                {
                    ho.SetGlow(0, 0, 0, 0);
                }
            }
        }

        public override void Prepare()
        {
            FOV = 45;
            FOVShadow = 30f;
            SetSunPosition(25, 25, 0);
            SetSunTarget(0, 0, 0);
            SetSunAmbientFactor(0.5f);
            SetSunColor(1, 1, 1, 0.0f);

            SetCameraPosition(0, 3f, 3.5f);

            SetTextureSkybox(@".\textures\skybox1.jpg");

            Sphere s = new Sphere();
            s.SetModel("KWSphere");
            s.SetPosition(0, 1, 0);
            s.IsShadowCaster = true;
            s.SetTexture(@".\textures\Metal022_1K_Color.jpg");
            //s.SetTexture(@".\textures\bg_greenmountains.png");
            s.SetTexture(@".\textures\Metal022_1K_Normal.jpg", TextureType.Normal);
            s.SetTexture(@".\textures\Metal022_1K_Metalness.jpg", TextureType.Metalness);
            s.SetTexture(@".\textures\Metal022_1K_Roughness.jpg", TextureType.Roughness);
            //s.SetRoughness(0);
           // AddGameObject(s);

            KWEngine.LoadModelFromFile("MatTest", @".\Models\ArenaOuter\materialtest.glb");
            //KWEngine.LoadModelFromFile("ArenaPlatform", @".\Models\ArenaOuter\ArenaPlatform.obj");
            //KWEngine.LoadModelFromFile("ArenaPlatforms", @".\Models\ArenaOuter\ArenaPlatforms.fbx");

            Immovable arenaOuter = new Immovable();
            arenaOuter.SetModel("MatTest");
            arenaOuter.IsCollisionObject = true;
            arenaOuter.IsShadowCaster = true;
            arenaOuter.AddRotationY(45);
            arenaOuter.SetScale(0.25f);
            AddGameObject(arenaOuter);


            Cube floor = new Cube();
            floor.SetModel("KWCube");
            floor.SetPosition(0f, -0.5f, 0);
            floor.SetScale(25, 1, 25);
            //floor.SetColor(0, 0, 0);
            floor.SetTexture(@".\textures\sand_diffuse.jpg");
            floor.SetTexture(@".\textures\sand_normal.jpg", TextureType.Normal);
            floor.SetTextureRepeat(2, 2);
            floor.IsShadowCaster = true;
            AddGameObject(floor);

            KWEngine.BuildTerrainModel("Terrain", @".\textures\heightmap.png", @".\textures\sand_diffuse.jpg", 25, 0.1f, 25, 2, 2);
            Immovable floor2 = new Immovable();
            floor2.SetModel("Terrain");
            floor2.IsCollisionObject = true;
            floor2.IsShadowCaster = true;
            floor2.SetPosition(12.5f, 0, 0);
            floor2.SetTexture(@".\textures\sand_normal.jpg", TextureType.Normal);
            //AddGameObject(floor2);

            
            PointLight p = new PointLight();
            p.Type = LightType.Point;
            p.SetPosition(-2, 2, 4);
            p.SetColor(1, 1, 1, 1f);
            p.SetDistanceMultiplier(3);
            AddLightObject(p);

            PointLight p2 = new PointLight();
            p2.Type = LightType.DirectionalShadow;
            p2.SetPosition(3, 2, 3);
            p2.SetTarget(0, 1, 0);
            p2.SetColor(0, 1, 0, 1f);
            p2.SetDistanceMultiplier(10);
            //AddLightObject(p2);


            /*
            ho = new HUDObject(HUDObjectType.Text, 64, 64);
            ho.SetText("Hello World ÖÄÜäüöß");
            //ho.SetScale(64, 64);
            //ho.CharacterSpreadFactor = 48;
            //AddHUDObject(ho);

            ho2 = new HUDObject(HUDObjectType.Image, 1024, 256);
            ho2.SetTexture(@".\textures\bg_greenmountains.png");
            ho2.SetScale(320, 240);
            //AddHUDObject(ho2);


            KWEngine.SetFont(0);
            */

            //DebugShadowCaster = true;
        }
    }
}
