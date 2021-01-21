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
        }

        public override void Prepare()
        {
            KWEngine.LoadModelFromFile("MatTest", @".\models\materialtest.glb");

            FOV = 45;
            FOVShadow = 30f;
            SetSunPosition(25, 25, 0);
            SetSunTarget(0, 0, 0);
            SetSunAmbientFactor(0.5f);
            SetSunColor(1, 1, 1, 0.5f);

            SetCameraPosition(0, 10f, 15f);

            SetTextureSkybox(@".\textures\skybox1.jpg");
            
            Sphere s = new Sphere();
            s.SetModel("KWSphere");
            s.SetPosition(0, 1, 0);
            s.SetScale(2);
            s.IsShadowCaster = true;
            //s.SetTexture(@".\textures\Metal022_1K_Color.jpg");
            s.SetTexture(@".\textures\Metal022_1K_Normal.jpg", TextureType.Normal);
            //s.SetTexture(@".\textures\Metal022_1K_Metalness.jpg", TextureType.Metalness);
            //s.SetTexture(@".\textures\Metal022_1K_Roughness.jpg", TextureType.Roughness);
            AddGameObject(s);
            /*
            Sphere s2 = new Sphere();
            s2.SetModel("KWSphere");
            s2.SetPosition(6, 1, 0);
            s2.IsShadowCaster = true;
            s2.SetTexture(@".\textures\Metal022_1K_Color.jpg");
            s2.SetTexture(@".\textures\Metal022_1K_Normal.jpg", TextureType.Normal);
            s2.SetTexture(@".\textures\Metal022_1K_Metalness.jpg", TextureType.Metalness);
            s2.SetTexture(@".\textures\Metal022_1K_Roughness.jpg", TextureType.Roughness);
            //AddGameObject(s2);

            Sphere s3 = new Sphere();
            s3.SetModel("KWSphere");
            s3.SetPosition(-5.5f, 1, 0);
            s3.IsShadowCaster = true;
            s3.SetTexture(@".\textures\Metal022_1K_Color.jpg");
            s3.SetTexture(@".\textures\Metal022_1K_Normal.jpg", TextureType.Normal);
            s3.SetTexture(@".\textures\Metal022_1K_Metalness.jpg", TextureType.Metalness);
            s3.SetTexture(@".\textures\Metal022_1K_Roughness.jpg", TextureType.Roughness);
           // AddGameObject(s3);

            Sphere s4 = new Sphere();
            s4.SetModel("KWSphere");
            s4.SetPosition(0, 1, 5);
            s4.IsShadowCaster = true;
            s4.SetTexture(@".\textures\Metal022_1K_Color.jpg");
            s4.SetTexture(@".\textures\Metal022_1K_Normal.jpg", TextureType.Normal);
            s4.SetTexture(@".\textures\Metal022_1K_Metalness.jpg", TextureType.Metalness);
            s4.SetTexture(@".\textures\Metal022_1K_Roughness.jpg", TextureType.Roughness);
            //AddGameObject(s4);
            */
            Immovable mattest = new Immovable();
            mattest.SetModel("MatTest");
            mattest.IsShadowCaster = true;
            mattest.SetScale(0.5f);
            //AddGameObject(mattest);

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
            p.SetColor(1, 0, 0, 1f);
            p.SetDistanceMultiplier(3);
            AddLightObject(p);

            PointLight p2 = new PointLight();
            p2.Type = LightType.DirectionalShadow;
            p2.SetPosition(3, 2, 3);
            p2.SetTarget(0, 1, 0);
            p2.SetColor(0, 1, 0, 1f);
            p2.SetDistanceMultiplier(10);
            AddLightObject(p2);

            DebugShowPerformanceInTitle = PerformanceUnit.FramesPerSecond;
            //DebugShowCoordinateSystemGrid = GridType.GridXY;
            //DebugShadowCaster = true;
        }
    }
}
