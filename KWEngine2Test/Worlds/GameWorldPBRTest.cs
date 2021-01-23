﻿using KWEngine2;
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
                i -= 0.05f * deltaTimeFactor;
                i = HelperGL.Clamp(i, 0, 2);
                SetSunColor(GetSunColor().X, GetSunColor().Y, GetSunColor().Z, i);
                Console.WriteLine("SunIntensity: " + i);
            }
            else if (kb[Key.I])
            {
                float i = GetSunColor().W;
                i += 0.05f * deltaTimeFactor;
                i = HelperGL.Clamp(i, 0, 2);
                SetSunColor(GetSunColor().X, GetSunColor().Y, GetSunColor().Z, i);
                Console.WriteLine("SunIntensity: " + i);
            }

            if (kb[Key.J])
            {
                float i = SunAmbientFactor;
                i -= 0.01f * deltaTimeFactor;
                i = HelperGL.Clamp(i, 0, 1);
                SetSunAmbientFactor(i);
                Console.WriteLine("Ambient: " + i);
            }
            else if (kb[Key.K])
            {
                float i = SunAmbientFactor;
                i += 0.01f * deltaTimeFactor;
                i = HelperGL.Clamp(i, 0, 1);
                SetSunAmbientFactor(i);
                Console.WriteLine("Ambient: " + i);
            }

            SpawnParticles();
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
            KWEngine.LoadModelFromFile("MatTest", @".\models\materialtest.glb");

            FOV = 45;
            FOVShadow = 30f;

            SetSunPosition(100, 100, 100);
            SetSunTarget(0, 0, 0);
            SetSunAmbientFactor(0.5f);
            SetSunColor(1, 1, 1, 0.5f);

            SetCameraPosition(0, 25, 50);
            SetCameraTarget(0, 0, 0);

            SetTextureSkybox(@".\textures\skybox1.jpg");

            CreateMetalSphereTestObject();
            CreateCubeTestObject();
            CreateGLBTestObject();
            CreateTerrainTestObject();


            CreateFreeFloatPlayer();

            

            
            PointLight p = new PointLight();
            p.Type = LightType.Point;
            p.SetPosition(-2, 2, 4);
            p.SetColor(1, 0, 0, 1f);
            p.SetDistanceMultiplier(3);
            //AddLightObject(p);

            DirectionalLight p2 = new DirectionalLight();
            p2.Type = LightType.DirectionalShadow;
            p2.SetPosition(-10, 5, -10);
            p2.SetTarget(0, 2.5f, 0);
            p2.SetColor(0, 1, 0, 10f);
            p2.SetDistanceMultiplier(10);
            //AddLightObject(p2);

            DebugShowPerformanceInTitle = PerformanceUnit.FramesPerSecond;
            DebugShowCoordinateSystemGrid = GridType.GridXZ;
            DebugShadowCaster = false;
        }

        private void CreateFreeFloatPlayer()
        {
            PlayerFloat pf = new PlayerFloat();
            pf.SetModel("KWCube");
            pf.SetPosition(0, 5, 20);
            AddGameObject(pf);
            SetFirstPersonObject(pf, 180);
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

            CubeRoughnessTest floor = new CubeRoughnessTest();
            floor.SetModel("KWCube");
            floor.SetPosition(-5, -0.5f, -5);
            floor.SetScale(10, 1, 10);
            floor.SetColor(1, 0, 0);
            floor.SetRoughness(0.6f);
            floor.SetTexture(@".\textures\MetalPlates006_1K_Normal.jpg", TextureType.Normal);
            floor.SetTextureRepeat(2, 2);
            floor.IsShadowCaster = true;
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
            mattest.SetPosition(-5, 0, 5);
            mattest.IsShadowCaster = true;
            mattest.SetScale(0.5f);
            AddGameObject(mattest);
        }

        private void CreateTerrainTestObject()
        {
            KWEngine.BuildTerrainModel("Terrain", @".\textures\heightmap.png", @".\textures\sand_diffuse.jpg", 10, 1, 10, 1, 1);
            Immovable floor2 = new Immovable();
            floor2.SetModel("Terrain");
            floor2.IsCollisionObject = true;
            floor2.IsShadowCaster = true;
            floor2.SetPosition(5, 0, 5);
            floor2.SetRoughness(0);
            floor2.SetTexture(@".\textures\sand_normal.jpg", TextureType.Normal);
            AddGameObject(floor2);
        }
    }
}
