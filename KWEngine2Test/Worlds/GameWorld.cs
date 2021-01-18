using KWEngine2;
using KWEngine2.GameObjects;
using OpenTK;
using OpenTK.Input;
using KWEngine2Test.Objects;
using KWEngine2.Helper;
using KWEngine2Test.Objects.Main;

namespace KWEngine2Test.Worlds
{
    class GameWorld : World
    {
        private long _timeStamp = 0;
        private long _timeStampExp = 0;
        private long _timeStampExpDiff = 0;
        private Player p;

        public Player GetPlayer()
        {
            return p;
        }

        public override void Act(KeyboardState kb, MouseState ms, float deltaTimeFactor)
        {
            if(kb[Key.Escape])
            {
                CurrentWindow.SetWorld(new GameWorldStart());
                return;
            }

            long now = GetCurrentTimeInMilliseconds();
            if(now - _timeStamp > 3000)
            {
                ParticleObject smoke = new ParticleObject(new Vector3(-32.5f, 1, -22.5f), new Vector3(5, 5, 5), ParticleType.LoopSmoke1);
                smoke.SetColor(0.2f, 0.2f, 0.2f, 1);
                smoke.SetDuration(3.25f);
                AddParticleObject(smoke);
                _timeStamp = now;
            }

            if (now - _timeStampExp > _timeStampExpDiff)
            {
                Explosion ex = new Explosion(new Vector3(-35f, 4, -22.5f), 32, 1, 8, 2, ExplosionType.Cube, new Vector4(1, 1, 1, 1));
                //ex.SetAnimationAlgorithm(ExplosionAnimation.WindUp);
                AddGameObject(ex);

                _timeStampExp = now;
                _timeStampExpDiff = HelperRandom.GetRandomNumber(3000, 10000);
                //_timeStampExpDiff = HelperRandom.GetRandomNumber(2000, 4000);

            }

        }
     
        public override void Prepare()
        {
            KWEngine.LoadModelFromFile("Robot", @".\models\UBot\ubot.fbx");
            KWEngine.LoadModelFromFile("Lab", @".\models\labyrinth\walls.obj");
            KWEngine.LoadModelFromFile("Panel", @".\models\spacepanel\scifipanel.obj");
            KWEngine.LoadModelFromFile("Spaceship", @".\models\spaceship\spaceship4.obj");

            KWEngine.BuildTerrainModel("Terrain", @".\textures\heightmap.png", @".\textures\sand_diffuse.jpg", 100, 2, 100, 5, 5);
            KWEngine.ShadowMapCoefficient = 0.0005f;
            FOVShadow = 40f;
            SetSunPosition(250, 250, -250);
            SetSunColor(0.25f, 0.5f, 1, 0.7f);

            SunAmbientFactor = 0.3f;
            SetCameraPosition(100, 100, 100);
            WorldDistance = 1000;

            Immovable ship = new Immovable();
            ship.SetModel("Spaceship");
            ship.IsCollisionObject = true;
            ship.IsShadowCaster = true;
            ship.SetPosition(-35, 3.5f, -25);
            ship.SetScale(15);
            ship.AddRotationZ(25, true);
            ship.AddRotationX(40, true);
            AddGameObject(ship);

            Immovable floor = new Immovable();
            floor.SetModel("Terrain");
            floor.IsCollisionObject = true;
            floor.IsShadowCaster = true;
            floor.SetTexture(@".\textures\sand_normal.jpg", TextureType.Normal);
            AddGameObject(floor);
            
            Immovable wallLeft1 = new Immovable();
            wallLeft1.Name = "WallLeft";
            wallLeft1.SetModel("KWCube");
            wallLeft1.IsCollisionObject = true;
            wallLeft1.IsShadowCaster = true;
            wallLeft1.SetScale(2, 10, 100);
            wallLeft1.SetPosition(-49, 5, 0);
            AddGameObject(wallLeft1);

            Immovable wallRight = new Immovable();
            wallRight.Name = "WallRight";
            wallRight.SetModel("KWCube");
            wallRight.IsCollisionObject = true;
            wallRight.IsShadowCaster = true;
            wallRight.SetScale(2, 10, 100);
            wallRight.SetPosition(49, 5, 0);
            AddGameObject(wallRight);

            Immovable wallFront = new Immovable();
            wallFront.Name = "WallFront";
            wallFront.SetModel("KWCube");
            wallFront.IsCollisionObject = true;
            wallFront.IsShadowCaster = true;
            wallFront.SetScale(100, 10, 2);
            wallFront.SetPosition(0, 5, 49);
            AddGameObject(wallFront);

            Immovable wallBack = new Immovable();
            wallBack.Name = "WallBack";
            wallBack.SetModel("KWCube");
            wallBack.IsCollisionObject = true;
            wallBack.IsShadowCaster = true;
            wallBack.SetScale(100, 10, 2);
            wallBack.SetPosition(0, 5, -49);
            AddGameObject(wallBack);

            p = new Player();
            p.SetModel("Robot");
            p.Name = "Player";
            p.SetPosition(-5, 0f, -5);
            p.SetScale(4);
            p.AnimationID = 0;
            p.AnimationPercentage = 0;
            p.IsShadowCaster = true;
            p.IsCollisionObject = true;
            p.IsPickable = true;
            p.TurnTowardsXZ(new Vector3(0, 0, 0));
            //p.SetSpecularOverride(true, 8, 32);
            AddGameObject(p);
            //SetFirstPersonObject(p);

            p._flashlight = new Flashlight();
            p._flashlight.Type = LightType.DirectionalShadow;
            p._flashlight.SetDistanceMultiplier(2);
            p._flashlight.SetColor(1, 0.75f, 0, 5f);
            p._flashlight.SetFOVShadow(180);
            AddLightObject(p._flashlight);

            Immovable lab = new Immovable();
            lab.Name = "Labyrinth";
            lab.SetModel("Lab");
            lab.IsCollisionObject = true;
            lab.IsShadowCaster = true;
            //lab.SetSpecularOverride(true, 0, 2048);
            AddGameObject(lab);

            Panel panel = new Panel();
            panel.Name = "Panel";
            panel.SetModel("Panel");
            panel.SetPosition(10, 0.25f, -5);
            panel.SetScale(3);
            //panel.SetSpecularOverride(true, 2, 1024);
            //panel.SetTextureForMesh(0, @".\models\spacepanel\scifipanel2.png");
            panel.IsShadowCaster = true;
            panel.IsPickable = true;
            panel.IsCollisionObject = true;
            AddGameObject(panel);
            
            PanelLight pLight = new PanelLight();
            pLight.Type = LightType.Directional;
            pLight.SetColor(1, 1, 1, 1);
            pLight.SetPosition(10, 5, -5);
            pLight.SetTarget(10, 0, -5);
            pLight.SetDistanceMultiplier(2f);
            AddLightObject(pLight);
            

            HUDObject ho = new HUDObject(HUDObjectType.Text, 24, 24);
            ho.SetText("kwengine.de");
            ho.SetGlow(1, 0, 0, 1);
            ho.SetRotation(0, 0);
            AddHUDObject(ho);
           
            DebugShowHitboxes = false;
            if (IsFirstPersonMode)
            {
                FOV = 90;
            }
            else
            {
                FOV = 45;
            }
        }

    }
}
