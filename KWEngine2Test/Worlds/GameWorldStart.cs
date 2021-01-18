using KWEngine2;
using KWEngine2.GameObjects;
using KWEngine2Test.Objects;
using OpenTK.Input;

namespace KWEngine2Test.Worlds
{
    class GameWorldStart : World
    {
        private HUDObject _button = null;
        private HUDObject _button2 = null;
        private HUDObject _button3 = null;
        private HUDObject _button4 = null;

        public override void Act(KeyboardState kb, MouseState ms, float deltaTimeFactor)
        {
            if (_button == null)
                return;

            if (_button.IsMouseCursorOnMe(ms))
            {
                _button.SetGlow(0.25f, 0.5f, 1f, 0.4f);

                if (ms.LeftButton == ButtonState.Pressed)
                {
                    CurrentWindow.SetWorld(new GameWorld());
                    return;
                }
            }
            else
            {
                _button.SetGlow(1, 0, 0, 0);
            }


            if (_button2.IsMouseCursorOnMe(ms))
            {
                _button2.SetGlow(0, 0, 1, 0.7f);

                if (ms.LeftButton == ButtonState.Pressed)
                {
                    CurrentWindow.SetWorld(new GameWorldArena());
                    return;
                }
            }
            else
            {
                _button2.SetGlow(1, 0, 0, 0);
            }

            if (_button3.IsMouseCursorOnMe(ms))
            {
                _button3.SetGlow(0.5f, 1, 0.5f, 0.3f);

                if (ms.LeftButton == ButtonState.Pressed)
                {
                    CurrentWindow.SetWorld(new GameWorldSpaceInvaders());
                    return;
                }
            }
            else
            {
                _button3.SetGlow(1, 0, 0, 0);
            }

            if (_button4.IsMouseCursorOnMe(ms))
            {
                _button4.SetGlow(0.5f, 1, 0.5f, 0.3f);

                if (ms.LeftButton == ButtonState.Pressed)
                {
                    CurrentWindow.SetWorld(new GameWorldJumpAndRun());
                    return;
                }
            }
            else
            {
                _button4.SetGlow(1, 0, 0, 0);
            }
        }
     
        public override void Prepare()
        {
            //KWEngine.PostProcessQuality = KWEngine.PostProcessingQuality.Standard;
            //KWEngine.MouseSensitivity = -0.001f;

            int imageWidth = 190;
            int imageHeight = 190;
            int width = CurrentWindow.Width;
            int height = CurrentWindow.Height;

            Cube c = new Cube();
            c.SetModel("KWCube");
            c.SetGlow(1, 1, 1, 0.5f);
            c.SetPosition(0, 4, 0);
            //AddGameObject(c);

            _button = new HUDObject(HUDObjectType.Image, width / 2, height / 2 - 276);
            _button.SetTexture(@".\textures\button01.png");
            _button.SetScale(imageWidth, imageHeight);
            AddHUDObject(_button);

            _button2 = new HUDObject(HUDObjectType.Image, width / 2, height / 2 - 100);
            _button2.SetTexture(@".\textures\button02.png");
            _button2.SetScale(imageWidth, imageHeight);
            _button2.SetColor(1, 0.75f, 1, 1);
            AddHUDObject(_button2);

            _button3 = new HUDObject(HUDObjectType.Image, width / 2, height / 2 + 76);
            _button3.SetTexture(@".\textures\button03.png");
            _button3.SetScale(imageWidth, imageHeight);
            _button3.SetColor(1, 1, 0.5f, 1);
            AddHUDObject(_button3);

            _button4 = new HUDObject(HUDObjectType.Image, width / 2, height / 2 + 252);
            _button4.SetTexture(@".\textures\button04.png");
            _button4.SetScale(imageWidth, imageHeight);
            _button4.SetColor(0, 1f, 1f, 1);
            AddHUDObject(_button4);

            DebugShowPerformanceInTitle = PerformanceUnit.FramesPerSecond;
        }

    }
}
