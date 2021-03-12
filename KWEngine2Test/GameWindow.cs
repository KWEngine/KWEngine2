using KWEngine2;
using KWEngine2Test.Worlds;
using System;

namespace KWEngine2Test
{
    class GameWindow : GLWindow
    {
        public GameWindow()
            : base(
                  1280,                             // width
                  720,                              // height
                  OpenTK.GameWindowFlags.Default,   // window mode
                  1,                                // anti-aliasing (1 to 8)
                  false,                            // vsync?
                  false,                            // multithreading (experimental)
                  1)                                // anisotropic filtering (1 to 16)
        {
            KWEngine.PostProcessQuality = PostProcessingQuality.Standard;
            SetWorld(new GameWorldPBRTest());
        }
    }
}
