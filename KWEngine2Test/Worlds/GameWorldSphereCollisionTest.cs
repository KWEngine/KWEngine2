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
    class GameWorldSphereCollisionTest : World
    {
        //private long timestamp = 0;
        private HUDObject ho;
        private HUDObject ho2;

        public override void Act(KeyboardState kb, MouseState ms)
        {
            if (kb[Key.Escape])
            {
                CurrentWindow.SetWorld(new GameWorldStart());
                return;
            }
        }

        public override void Prepare()
        {
            KWEngine.LoadModelFromFile("ConvexHull", @".\models\convexhull.glb");

            FOV = 45;

            SetAmbientLight(1, 1, 1, 0.2f);

            SetCameraPosition(0, 25, 0);
            SetCameraTarget(0, 0, 0);

            SetTextureSkybox(@".\textures\skybox1.dds");
            SetTextureBackgroundBrightnessMultiplier(4);

            /*
            LightObject sun = new LightObject(LightType.Sun, true);
            sun.SetPosition(30, 30, 30);
            sun.SetColor(1, 1, 1, 0.8f);
            sun.SetFOVBiasCoefficient(0.00009f);
            AddLightObject(sun);
            */

            DebugShowPerformanceInTitle = PerformanceUnit.FramesPerSecond;
            DebugShowCoordinateSystemGrid = GridType.GridXZ;

            CreateTestScene();
        }

        private void CreateTestScene()
        {
            PlayerSphere s = new PlayerSphere();
            s.SetModel("KWSphere");
            s.SetPosition(4, 1, 0);
            s.SetScale(4, 2, 2);
            //s.SetScale(2);
            s.Name = "Sphere #1";
            s.IsShadowCaster = true;
            s.IsCollisionObject = true;
            s.ColorEmissive = new Vector4(1, 1, 1, 0.1f);
            AddGameObject(s);

            Immovable sC = new Immovable();
            sC.SetModel("KWSphere");
            sC.SetPosition(-5, 1, 0);
            sC.SetScale(2);
            sC.Name = "Sphere #2";
            sC.IsShadowCaster = true;
            sC.IsCollisionObject = true;
            sC.SetTexture(@".\textures\Metal022_1K_Color.jpg");
            sC.SetTexture(@".\textures\Metal022_1K_Normal.jpg", TextureType.Normal);
            sC.SetTexture(@".\textures\Metal022_1K_Metalness.jpg", TextureType.Metalness);
            sC.SetTexture(@".\textures\Metal022_1K_Roughness.jpg", TextureType.Roughness);
            //AddGameObject(sC);

            CubeRoughnessTest floor = new CubeRoughnessTest();
            floor.SetModel("KWCube");
            floor.Name = "Floor";
            floor.SetPosition(0, -0.5f, 0);
            floor.SetScale(10, 1, 10);
            floor.SetColor(1, 0, 0);
            floor.IsShadowCaster = true;
            floor.IsCollisionObject = true;
            //AddGameObject(floor);

            Immovable convexHull = new Immovable();
            convexHull.SetModel("ConvexHull");
            convexHull.SetScale(2);
            convexHull.SetPosition(0, 1, 0);
            convexHull.IsCollisionObject = true;
            convexHull.IsShadowCaster = true;
            AddGameObject(convexHull);

            DebugShowHitboxes = true;
        }

        private void CreateTerrainTestObject()
        {
            KWEngine.BuildTerrainModel("Terrain", @".\textures\heightmap.png", @".\textures\sand_diffuse.dds", 10, 1, 10, 1, 1);
            Immovable floor2 = new Immovable();
            floor2.SetModel("Terrain");
            floor2.IsCollisionObject = true;
            floor2.IsShadowCaster = true;
            floor2.SetPosition(5, 0, 5);
            floor2.SetRoughness(0.9f);
            //floor2.SetMetalness(0.9f);
            floor2.SetTexture(@".\textures\sand_normal.dds", TextureType.Normal);
            floor2.SetTextureTerrainBlendMapping(
                @".\textures\blendmap.png",
                @".\textures\metal022_1K_Color.jpg",
                @".\textures\bg_greenmountains.dds",
                @".\textures\metalplates006_1k_color.jpg");
            AddGameObject(floor2);
        }
    }
}
