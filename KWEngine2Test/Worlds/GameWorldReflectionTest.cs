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
    class GameWorldReflectionTest : World
    {

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
            KWEngine.LoadModelFromFile("Paddle", @".\models\paddle.obj");

            FOV = 90;

            SetAmbientLight(1, 1, 1, 0.2f);

            SetCameraPosition(0, 0, 25);
            SetCameraTarget(0, 0, 0);

            SetTextureSkybox(@".\textures\skybox1.dds");
            SetTextureBackgroundBrightnessMultiplier(4);

            
            LightObject sun = new LightObject(LightType.Sun, false);
            sun.SetPosition(30, 30, 30);
            sun.SetColor(1, 1, 1, 0.8f);
            AddLightObject(sun);

            DebugShowPerformanceInTitle = PerformanceUnit.FramesPerSecond;
            DebugShowCoordinateSystemGrid = GridType.GridXY;
            //DebugShowHitboxes = true;

            CreateTestScene();
        }

        private void CreateTestScene()
        {
            ReflectObject s = new ReflectObject();
            s.SetModel("KWSphere");
            s.SetPosition(0, -4, 0);
            s.SetScale(1,1,1);
            //s.AddRotationZ(0);
            s.Name = "DaBall!";
            s.IsShadowCaster = false;
            s.IsCollisionObject = true;
            s.UpdateLast = true;
            s.SetTexture(@".\textures\Metal022_1K_Color.jpg");
            s.SetTexture(@".\textures\Metal022_1K_Normal.jpg", TextureType.Normal);
            s.SetTexture(@".\textures\Metal022_1K_Metalness.jpg", TextureType.Metalness);
            s.SetTexture(@".\textures\Metal022_1K_Roughness.jpg", TextureType.Roughness);
            AddGameObject(s);

            Immovable sC = new Immovable();
            sC.SetModel("KWSphere");
            sC.SetPosition(0, 6, 0);
            sC.SetScale(4);
            sC.Name = "Obstacle Sphere";
            sC.IsCollisionObject = true;
            sC.SetColor(0, 1, 0);
            AddGameObject(sC);

            Cube sC2 = new Cube();
            sC2.SetModel("KWCube");
            sC2.Name = "Obstacle Cube";
            sC2.SetPosition(-5, 5f, 0);
            sC2.SetScale(2);
            sC2.AddRotationZ(-45);
            sC2.SetColor(1, 0, 0);
            sC2.IsCollisionObject = true;
            AddGameObject(sC2);

            Immovable convexHull = new Immovable();
            convexHull.SetModel("KWCube");
            convexHull.SetScale(2);
            convexHull.SetPosition(5, 5, 0);
            convexHull.IsCollisionObject = true;
            AddGameObject(convexHull);

            ReflectPaddle player = new ReflectPaddle(s);
            player.SetModel("Paddle");
            player.SetPosition(0, -6, 0);
            player.SetScale(2);
            player.IsCollisionObject = true;
            AddGameObject(player);

            Immovable wallLeft = new Immovable();
            wallLeft.SetModel("KWCube");
            wallLeft.SetPosition(-10.5f, 0, 0);
            wallLeft.SetScale(1, 20, 1);
            wallLeft.IsCollisionObject = true;
            AddGameObject(wallLeft);

            Immovable wallRight = new Immovable();
            wallRight.SetModel("KWCube");
            wallRight.SetPosition(10.5f, 0, 0);
            wallRight.SetScale(1, 20, 1);
            wallRight.IsCollisionObject = true;
            AddGameObject(wallRight);

            Immovable wallTop = new Immovable();
            wallTop.SetModel("KWCube");
            wallTop.SetPosition(0, 10.5f, 0);
            wallTop.SetScale(20, 1, 1);
            wallTop.IsCollisionObject = true;
            AddGameObject(wallTop);

            Immovable wallBottom = new Immovable();
            wallBottom.SetModel("KWCube");
            wallBottom.SetPosition(0, -10.5f, 0);
            wallBottom.SetScale(20, 1, 1);
            wallBottom.IsCollisionObject = true;
            AddGameObject(wallBottom);

            //CreateTerrainTestObject();   
        }

        private void CreateTerrainTestObject()
        {
            KWEngine.BuildTerrainModel("Terrain", @".\textures\heightmap2.png", @".\textures\sand_diffuse.dds", 20, 2, 20, 1, 1);
            Immovable floor2 = new Immovable();
            floor2.SetModel("Terrain");
            floor2.IsCollisionObject = true;
            floor2.SetPosition(0, -12, 0);
            floor2.SetTexture(@".\textures\sand_normal.dds", TextureType.Normal);
            AddGameObject(floor2);
        }
    }
}
