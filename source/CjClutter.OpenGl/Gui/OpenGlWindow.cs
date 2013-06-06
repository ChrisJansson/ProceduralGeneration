using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Awesomium.Core;
using CjClutter.OpenGl.Camera;
using CjClutter.OpenGl.CoordinateSystems;
using CjClutter.OpenGl.Input;
using CjClutter.OpenGl.Input.Keboard;
using CjClutter.OpenGl.Input.Mouse;
using CjClutter.OpenGl.SceneGraph;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using FrameEventArgs = OpenTK.FrameEventArgs;

namespace CjClutter.OpenGl.Gui
{
    public class OpenGlWindow : GameWindow
    {
        private readonly FrameTimeCounter _frameTimeCounter = new FrameTimeCounter();
        private Stopwatch _stopwatch;
        private readonly MouseInputProcessor _mouseInputProcessor;
        private readonly MouseInputObservable _mouseInputObservable;
        private readonly KeyboardInputProcessor _keyboardInputProcessor = new KeyboardInputProcessor();
        private readonly KeyboardInputObservable _keyboardInputObservable;
        private readonly OpenTkCamera _openTkCamera;
        private readonly Scene _scene;
        private Func<Matrix4d> _createaProjectionMatrix;
        private Hud _hud;

        public OpenGlWindow(int width, int height, string title, OpenGlVersion openGlVersion)
            : base(
            width,
            height,
            GraphicsMode.Default,
            title,
            GameWindowFlags.Default,
            DisplayDevice.Default,
            openGlVersion.Major,
            openGlVersion.Minor,
            GraphicsContextFlags.Default)
        {
            VSync = VSyncMode.On;

            _mouseInputProcessor = new MouseInputProcessor(this, new GuiToRelativeCoordinateTransformer());

            var buttonUpEventEvaluator = new ButtonUpActionEvaluator(_mouseInputProcessor);
            _mouseInputObservable = new MouseInputObservable(buttonUpEventEvaluator);

            _keyboardInputObservable = new KeyboardInputObservable(_keyboardInputProcessor);

            var trackballCameraRotationCalculator = new TrackballCameraRotationCalculator();
            var trackballCamera = new TrackballCamera(trackballCameraRotationCalculator);
            _openTkCamera = new OpenTkCamera(_mouseInputProcessor, trackballCamera);

            _scene = new Scene();

            _hud = new Hud();
            using (var webView = WebCore.CreateWebView(1024, 768))
            {
                webView.Source = new Uri("http://www.google.com");

                while (webView.IsLoading)
                {
                    WebCore.Update();
                }

                var surface = (BitmapSurface)webView.Surface;
                var bytes = new byte[surface.Width * surface.Height * 4];

                var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                IntPtr addrOfPinnedObject = handle.AddrOfPinnedObject();
                surface.CopyTo(addrOfPinnedObject, surface.Width * 4, 4, true, false);
                handle.Free();
                _hud.SetTexture(bytes);
            }

            //var bmp = new Bitmap("result.png");
            //var bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            //int texture;
            //GL.GenTextures(1, out texture);
            //GL.BindTexture(TextureTarget.Texture2D, texture);

            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);

            //bmp.UnlockBits(bmp_data);
        }

        protected override void OnLoad(EventArgs e)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            Func<Matrix4d> perspectiveMatrixFactory = () => Matrix4d.CreatePerspectiveFieldOfView(Math.PI/4, (double)Width/Height, 1, 100);
            Func<Matrix4d> orthoGraphicMatrixFactory = () => Matrix4d.CreateOrthographic(2, 2, 1, 100);

            _createaProjectionMatrix = perspectiveMatrixFactory;

            _keyboardInputObservable.SubscribeKey(KeyCombination.Esc, CombinationDirection.Down, Exit);
            _keyboardInputObservable.SubscribeKey(KeyCombination.LeftAlt && KeyCombination.Enter, CombinationDirection.Down, ToggleFullScren);
            _keyboardInputObservable.SubscribeKey(KeyCombination.O, CombinationDirection.Down, () => SwitchProjectionMatrix(perspectiveMatrixFactory));
            _keyboardInputObservable.SubscribeKey(KeyCombination.P, CombinationDirection.Down, () => SwitchProjectionMatrix(orthoGraphicMatrixFactory));

            _scene.Load();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _scene.Unload();
        }

        private void ToggleFullScren()
        {
            if (WindowState == WindowState.Fullscreen)
            {
                WindowState = WindowState.Normal;
            }
            else if(WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Fullscreen;
            }
        }

        protected override void OnUnload(EventArgs e)
        {
            _scene.Unload();
        }

        private void SwitchProjectionMatrix(Func<Matrix4d> factory)
        {
            _createaProjectionMatrix = factory;
            SetProjectionMatrix();
        }

        private void SetProjectionMatrix()
        {
            _scene.ProjectionMatrix = _createaProjectionMatrix();
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            SetProjectionMatrix();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            //ProcessMouseInput();
            //ProcessKeyboardInput();

            //_frameTimeCounter.UpdateFrameTime(e.Time);

            //GL.ClearColor(Color4.White);
            //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //_scene.ViewMatrix = _openTkCamera.GetCameraMatrix();
            
            //_scene.Update(ElapsedTime.TotalSeconds);
            //_scene.Draw();

            _hud.Draw();
            SwapBuffers();
        }

        private void ProcessKeyboardInput()
        {
            if (!Focused)
            {
                return;
            }

            var keyboardState = OpenTK.Input.Keyboard.GetState();

            _keyboardInputProcessor.Update(keyboardState);
            _keyboardInputObservable.ProcessKeys();
        }

        private void ProcessMouseInput()
        {
            if (!Focused)
            {
                return;
            }

            var mouseState = OpenTK.Input.Mouse.GetState();

            _mouseInputProcessor.Update(mouseState);
            _mouseInputObservable.ProcessMouseButtons();

            _openTkCamera.Update();
        }

        public TimeSpan ElapsedTime
        {
            get
            {
                return _stopwatch.Elapsed;
            }
        }
    }
}