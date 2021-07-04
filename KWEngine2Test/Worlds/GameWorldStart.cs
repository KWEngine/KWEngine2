using KWEngine2;
using KWEngine2.GameObjects;
using KWEngine2Test.Objects;
using OpenTK;
using OpenTK.Input;

namespace KWEngine2Test.Worlds
{
    class GameWorldStart : World
    {
        private HUDObject[] _buttons = new HUDObject[5];
        private HUDObject[] _numbers = new HUDObject[5];
        private Vector4[] _colors = new Vector4[]
        {
            new Vector4(1f, 0.75f, 0f, 0.4f),
            new Vector4(0, 0, 1, 0.3f),
            new Vector4(0.25f, 1, 0.25f, 0.3f),
            new Vector4(0.0f, 1, 1.0f, 0.3f),
            new Vector4(1, 0.5f, 0.25f, 0.4f)
        };

        public override void Act(KeyboardState kb, MouseState ms)
        {
            if (kb[Key.Escape] && GetCurrentTimeInMilliseconds() > 500)
                CurrentWindow.Close();

            
            for(int i = 0; i < _buttons.Length; i++)
            {
                if(_buttons[i].IsMouseCursorOnMe(ms) || _numbers[i].IsMouseCursorOnMe(ms))
                {
                    _numbers[i].SetGlow(_colors[i].X, _colors[i].Y, _colors[i].Z, _colors[i].W);
                    _buttons[i].SetGlow(_colors[i].X, _colors[i].Y, _colors[i].Z, _colors[i].W);

                    if (ms.LeftButton == ButtonState.Pressed)
                    {
                        switch (i)
                        {
                            case 0:
                                CurrentWindow.SetWorld(new GameWorld01());
                                return;
                            case 1:
                                CurrentWindow.SetWorld(new GameWorld02());
                                return;
                            case 2:
                                CurrentWindow.SetWorld(new GameWorld03());
                                return;
                            case 3:
                                CurrentWindow.SetWorld(new GameWorld04());
                                return;
                            case 4:
                                CurrentWindow.SetWorld(new GameWorld05());
                                return;
                            default:
                                return;
                        }
                    }
                }
                else
                {
                    _numbers[i].SetGlow(_colors[i].X, _colors[i].Y, _colors[i].Z, 0);
                    _buttons[i].SetGlow(_colors[i].X, _colors[i].Y, _colors[i].Z, 0);
                }
            }
        }
     
        public override void Prepare()
        {
            int imageWidth = 128;
            int imageHeight = 128;
            int width = CurrentWindow.Width;
            int height = CurrentWindow.Height;
            int offset = -276;
            for(int i = 0; i < _buttons.Length; i++, offset += 128)
            {
                _buttons[i] = new HUDObject(HUDObjectType.Image, width / 2, height / 2 + offset);
                _buttons[i].SetTexture(@".\textures\button_gen.dds");
                _buttons[i].SetScale(imageWidth, imageHeight);
                _buttons[i].SetColor(_colors[i].X, _colors[i].Y, _colors[i].Z, 1);
                AddHUDObject(_buttons[i]);

                _numbers[i] = new HUDObject(HUDObjectType.Text, width / 2 - 11, height / 2 + offset + 2);
                _numbers[i].SetScale(24, 24);
                _numbers[i].CharacterSpreadFactor = 14;
                _numbers[i].SetText("0 " + (i + 1));
                AddHUDObject(_numbers[i]);
            }

            DebugShowPerformanceInTitle = PerformanceUnit.FramesPerSecond;
        }

    }
}
