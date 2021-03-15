using KWEngine2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using KWEngine2Test.Objects.TestAll;
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

        public override void Act(KeyboardState kb, MouseState ms)
        {
            if (kb[Key.Escape])
            {
                CurrentWindow.SetWorld(new GameWorldStart());
                return;
            }
            /*
            if(kb[Key.U])
            {
                float i = GetSunColor().W;
                i -= 0.05f * KWEngine.DeltaTimeFactor;
                i = HelperGL.Clamp(i, 0, 2);
                SetSunColor(GetSunColor().X, GetSunColor().Y, GetSunColor().Z, i);
                Console.WriteLine("SunIntensity: " + i);
            }
            else if (kb[Key.I])
            {
                float i = GetSunColor().W;
                i += 0.05f * KWEngine.DeltaTimeFactor;
                i = HelperGL.Clamp(i, 0, 2);
                SetSunColor(GetSunColor().X, GetSunColor().Y, GetSunColor().Z, i);
                Console.WriteLine("SunIntensity: " + i);
            }

            if (kb[Key.J])
            {
                float i = SunAmbientFactor;
                i -= 0.01f * KWEngine.DeltaTimeFactor;
                i = HelperGL.Clamp(i, 0, 1);
                SetSunAmbientFactor(i);
                Console.WriteLine("Ambient: " + i);
            }
            else if (kb[Key.K])
            {
                float i = SunAmbientFactor;
                i += 0.01f * KWEngine.DeltaTimeFactor;
                i = HelperGL.Clamp(i, 0, 1);
                SetSunAmbientFactor(i);
                Console.WriteLine("Ambient: " + i);
            }
            */

            //SpawnParticles();
        }
        
        private void SpawnParticles()
        {
            long now = GetCurrentTimeInMilliseconds();
            if (now - timestamp > 3000)
            {
                ParticleObject p = new ParticleObject(new Vector3(7, 2.5f, -5), new Vector3(5, 5, 5), ParticleType.BurstFirework2);
                AddParticleObject(p);

                Explosion e = new Explosion(new Vector3(3, 0, -5), 512, 0.5f, 5f, 3, ExplosionType.Dollar, new Vector4(1, 1, 0, 1));
                e.SetAnimationAlgorithm(ExplosionAnimation.WhirlwindUp);
                AddGameObject(e);

                timestamp = now;
            }
        }


        public override void Prepare()
        {
            KWEngine.LoadModelFromFile("MatTest", @".\models\gltftestcube.glb");
            KWEngine.LoadModelFromFile("MatTest255", @".\models\gltftestcube255.glb");

            FOV = 45;

            //SetSunPosition(100, 100, 100);
            //SetSunTarget(0, 0, 0);
            //SetSunAmbientFactor(0.5f);
            //SetSunColor(1, 1, 1, 0.5f);
            SetAmbientLight(1, 1, 1, 0.25f);

            SetCameraPosition(0, 25, 50);
            SetCameraTarget(0, 0, 0);

            //SetTextureSkybox(@".\textures\skybox1.jpg");
            SetTextureBackground(@".\textures\bg_greenmountains.png");
            SetTextureBackgroundBrightnessMultiplier(4);

            //CreateMetalSphereTestObject();
            //CreateCubeTestObject();
            //CreateGLBTestObject();
            //CreateTerrainTestObject();

            CreateRoomForLightTest();


            CreateFreeFloatPlayer();


            DebugShowPerformanceInTitle = PerformanceUnit.FramesPerSecond;
            DebugShowCoordinateSystemGrid = GridType.GridXZ;
        }

        private void CreateFreeFloatPlayer()
        {
            PlayerFloat pf = new PlayerFloat();
            pf.SetModel("KWCube");
            pf.SetPosition(0, 5, 15);
            AddGameObject(pf);
            SetFirstPersonObject(pf, 180);
        }

        private void CreateRoomForLightTest()
        {
            SetAmbientLight(1, 1, 1, 0.25f);

            Cube c1 = new Cube();
            c1.SetModel("KWCube");
            c1.SetScale(20, 10, 2);
            c1.SetPosition(0, 5, -11);
            c1.IsShadowCaster = true;
            AddGameObject(c1);

            Cube c2 = new Cube();
            c2.SetModel("KWCube");
            c2.SetScale(20, 2, 20);
            c2.SetPosition(0, -1, 0);
            c2.IsShadowCaster = true;
            AddGameObject(c2);

            Cube c3 = new Cube();
            c3.SetModel("KWCube");
            c3.SetScale(2, 10, 20);
            c3.SetPosition(-11, 5, 0);
            c3.IsShadowCaster = true;
            AddGameObject(c3);

            Cube c4 = new Cube();
            c4.SetModel("KWCube");
            c4.SetScale(2, 10, 20);
            c4.SetPosition(+11, 5, 0);
            c4.IsShadowCaster = true;
            AddGameObject(c4);

            // Testing Cubes and Spheres
            Cube box1 = new Cube();
            box1.SetModel("KWCube");
            box1.SetPosition(-5, 1, 0);
            box1.SetScale(2);
            box1.IsShadowCaster = true;
            AddGameObject(box1);

            Cube box2 = new Cube();
            box2.SetModel("KWCube");
            box2.SetPosition(0, 0.5f, -3);
            box2.SetScale(1);
            box2.IsShadowCaster = true;
            //box2.SetMetalness(0.9f);
            box2.Opacity = 0.1f;
            AddGameObject(box2);

            Cube box3 = new Cube();
            box3.SetModel("KWCube");
            box3.SetPosition(0, 3, -7.5f);
            box3.SetScale(3);
            box3.IsShadowCaster = true;
            AddGameObject(box3);

            LightObject pointlightWithShadow = new LightObject(LightType.Point, true);
            pointlightWithShadow.SetPosition(0, 5, 0);
            pointlightWithShadow.SetDistanceMultiplier(20);
            pointlightWithShadow.SetColor(1, 0, 0, 1);
            AddLightObject(pointlightWithShadow);

            LightObject sunlight = new LightObject(LightType.Sun, true);
            sunlight.SetPosition(50, 50, 50);
            sunlight.SetTarget(0, 0, 0);
            sunlight.SetColor(1, 1, 0, 0.25f);
            sunlight.SetFOVBiasCoefficient(0.0001f);
            AddLightObject(sunlight);

            //DebugShadowLight = sunlight;
        }

        private void CreateMetalSphereTestObject()
        {
            Sphere s = new Sphere();
            s.SetModel("KWSphere");
            s.SetPosition(-5, 1, -5);
            s.SetScale(2);
            s.IsShadowCaster = true;
            s.SetTexture(@".\textures\Metal022_1K_Color.jpg");
            s.SetTexture(@".\textures\Metal022_1K_Normal.jpg", TextureType.Normal);
            s.SetTexture(@".\textures\Metal022_1K_Metalness.jpg", TextureType.Metalness);
            s.SetTexture(@".\textures\Metal022_1K_Roughness.jpg", TextureType.Roughness);
            AddGameObject(s);

            Cube c = new Cube();
            c.SetModel("KWCube");
            c.SetScale(1);
            c.IsShadowCaster = true;
            c.SetPosition(-2.5f, 0.5f, -2.5f);
            AddGameObject(c);

            CubeRoughnessTest floor = new CubeRoughnessTest();
            floor.SetModel("KWCube");
            floor.Name = "Test";
            floor.SetPosition(-5, -0.5f, -5);
            floor.SetScale(10, 1, 10);
            floor.SetColor(1, 0, 0);
            floor.SetRoughness(0.6f);
            floor.SetTexture(@".\textures\MetalPlates006_1K_Normal.jpg", TextureType.Normal);
            floor.SetTextureRepeat(2, 2);
            floor.IsShadowCaster = true;
            floor.SetSpecularReflectionEnabled(true);
            AddGameObject(floor);
        }

        private void CreateCubeTestObject()
        {
            Cube floor = new Cube();
            floor.SetModel("KWCube");
            floor.SetPosition(5, -0.5f, -5);
            floor.SetScale(10, 1, 10);
            //floor.SetColor(0, 0, 0);
            floor.SetTexture(@".\textures\MetalPlates006_1K_ColorBright.jpg");
            floor.SetTexture(@".\textures\MetalPlates006_1K_Normal.jpg", TextureType.Normal);
            floor.SetTexture(@".\textures\MetalPlates006_1K_Metalness.jpg", TextureType.Metalness);
            floor.SetTexture(@".\textures\MetalPlates006_1K_Roughness.jpg", TextureType.Roughness);
            floor.SetTextureRepeat(2, 2);
            floor.IsShadowCaster = true;
            AddGameObject(floor);
        }

        private void CreateGLBTestObject()
        {
            Immovable mattest = new Immovable();
            mattest.SetModel("MatTest");
            mattest.SetPosition(-3, 1, 5);
            mattest.IsShadowCaster = true;
            mattest.SetScale(1f);
            mattest.SetSpecularReflectionEnabled(true);
            AddGameObject(mattest);

            Immovable mattest255 = new Immovable();
            mattest255.SetModel("MatTest255");
            mattest255.SetPosition(-7, 1, 5);
            mattest255.IsShadowCaster = true;
            mattest255.SetScale(1f);
            mattest255.SetSpecularReflectionEnabled(true);
            AddGameObject(mattest255);

            Immovable kwcube6test = new Immovable();
            kwcube6test.SetModel("KWCube6");
            kwcube6test.SetPosition(-5, -0.5f, 5);
            kwcube6test.SetScale(10, 1, 10);
            kwcube6test.SetTexture(@".\textures\pavement01.jpg", TextureType.Albedo, CubeSide.Top);
            kwcube6test.SetTexture(@".\textures\pavement01_normal.jpg", TextureType.Normal, CubeSide.Front);
            kwcube6test.SetTextureRepeat(5, 1, CubeSide.Front);
            AddGameObject(kwcube6test);
        }

        private void CreateTerrainTestObject()
        {
            KWEngine.BuildTerrainModel("Terrain", @".\textures\heightmap.png", @".\textures\sand_diffuse.jpg", 10, 1, 10, 1, 1);
            Immovable floor2 = new Immovable();
            floor2.SetModel("Terrain");
            floor2.IsCollisionObject = true;
            floor2.IsShadowCaster = true;
            floor2.SetPosition(5, 0, 5);
            floor2.SetRoughness(0.9f);
            floor2.SetTexture(@".\textures\sand_normal.jpg", TextureType.Normal);
            floor2.SetTextureTerrainBlendMapping(
                @".\textures\blendmap.png",
                @".\textures\metal022_1K_Color.jpg",
                @".\textures\bg_greenmountains.png",
                @".\textures\metalplates006_1k_color.jpg");
            AddGameObject(floor2);
        }
    }
}
