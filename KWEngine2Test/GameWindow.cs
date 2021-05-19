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
                  true,                             // vsync?
                  false,                            // multithreading (experimental)
                  4,                                // anisotropic filtering (1 to 16)
                  1024                              // shadow map resolution (256 to 4096 at 2^n values)
                  )
        {
            SetWorld(new GameWorldSphereCollisionTest());
        }
    }
}
