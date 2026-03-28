using System;
using ComputerGameFinal.Engine.Managers;
using ComputerGameFinal.Game.Example;
using ComputerGameFinal.Game.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gum.Forms;
using Gum.Forms.Controls;
using MonoGameGum;
using Gum.Wireframe;

using ResourceManager = ComputerGameFinal.Engine.Managers.ResourceManager;

namespace ComputerGameFinal;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private int _nativeWidth = 960;
    private int _nativeHeight = 540;
    private RenderTarget2D _renderTarget;
    private Rectangle _renderDestination;
    private bool _isResizing = false;

    GumService GumUI => MonoGameGum.GumService.Default;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = _nativeWidth;
        _graphics.PreferredBackBufferHeight = _nativeHeight;
        _graphics.ApplyChanges();

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;
        
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Set up render target for native resolution rendering
        _renderTarget = new RenderTarget2D(GraphicsDevice, _nativeWidth, _nativeHeight);
        ScreenManager.Instance._graphics = _graphics;
        ScreenManager.Instance.nativeWidth = _nativeWidth;
        ScreenManager.Instance.nativeHeight = _nativeHeight;
        ScreenManager.Instance.previousHeight = _nativeHeight;
        ScreenManager.Instance.previousWidth = _nativeWidth;
        
        InitializeGum();

        CalculateRenderTargetSize();

        // Set up scenes
        SceneManager.Instance.AddScene<MainMenu>("main");
        SceneManager.Instance.AddScene<MainScene>("GameScene");
        SceneManager.Instance.AddScene<CollisionDemoScene>("collision_demo");
        
        base.Initialize();
    }

    private void InitializeGum()
    {
        GumUI.Initialize(this, DefaultVisualsVersion.Newest);
        GumService.Default.ContentLoader.XnaContentManager = Content;
        FrameworkElement.KeyboardsForUiControl.Add(GumUI.Keyboard);

        GumUI.CanvasWidth = _nativeWidth;  // 960
        GumUI.CanvasHeight = _nativeHeight; // 540
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        ResourceManager.Instance.LoadAll(Content);

        // 1×1 white pixel used by the collision demo for coloured box rendering
        var pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData([ Color.White ]);
        ResourceManager.Instance.LoadTexture("pixel", pixel);

        // Circle texture used by the collision demo
        int circleSize = 64;
        var circleTexture = new Texture2D(GraphicsDevice, circleSize, circleSize);
        Color[] circleData = new Color[circleSize * circleSize];
        for (int y = 0; y < circleSize; y++)
        {
            for (int x = 0; x < circleSize; x++)
            {
                int index = y * circleSize + x;
                Vector2 center = new Vector2(circleSize / 2f, circleSize / 2f);
                Vector2 pos = new Vector2(x, y);
                if (Vector2.Distance(pos, center) <= circleSize / 2f)
                {
                    circleData[index] = Color.White;
                }
                else                {
                    circleData[index] = Color.Transparent;
                }
            }
        }
        circleTexture.SetData(circleData);
        ResourceManager.Instance.LoadTexture("circle", circleTexture);
        
        SceneManager.Instance.LoadScene("main");
    }

    private void OnClientSizeChanged(object sender, EventArgs e)
    {
        if (!_isResizing && Window.ClientBounds.Width > 0 && Window.ClientBounds.Height > 0)
        {
            _isResizing = true;
            CalculateRenderTargetSize();
            _isResizing = false;
        }
    }

    private void CalculateRenderTargetSize()
    {
        Point size = GraphicsDevice.Viewport.Bounds.Size;

        float scaleX = (float)size.X / _renderTarget.Width;
        float scaleY = (float)size.Y / _renderTarget.Height;
        // use the smaller scale
        float scale = Math.Min(scaleX, scaleY);

        _renderDestination.Width = (int)(_renderTarget.Width * scale);
        _renderDestination.Height = (int)(_renderTarget.Height * scale);

        _renderDestination.X = (size.X - _renderDestination.Width) / 2;
        _renderDestination.Y = (size.Y - _renderDestination.Height) / 2;

        ScreenManager.Instance.previousWidth = size.X;
        ScreenManager.Instance.previousHeight = size.Y;

        GumUI.Cursor.TransformMatrix = Matrix.CreateTranslation(-_renderDestination.X, -_renderDestination.Y, 0) * Matrix.CreateScale(1f / scale);
    }

    protected override void Update(GameTime gameTime)
    {
        InputManager.Instance.Update();
        GumUI.Update(gameTime);
        foreach (var item in GumUI.Root.Children)
        {
            if(item is InteractiveGue asInteractiveGue)
                (asInteractiveGue.FormsControlAsObject as IUpdateable)?.Update(gameTime);
        }
        SceneManager.Instance.CurrentScene.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        GraphicsDevice.Clear(Color.Black);
        
        var currentScene = SceneManager.Instance.CurrentScene;
        Matrix cameraTransform = currentScene?.GetCameraTransform() ?? Matrix.Identity;
        
        // draw the current scene to the native resolution render target
        _graphics.GraphicsDevice.SetRenderTarget(_renderTarget);

        _spriteBatch.Begin(
            SpriteSortMode.FrontToBack,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullCounterClockwise,
            null,
            cameraTransform
        );
        currentScene?.Draw(_spriteBatch);
        _spriteBatch.End();

        GumUI.Draw();

        // draw the render target to the backbuffer
        _graphics.GraphicsDevice.SetRenderTarget(null);

        _spriteBatch.Begin(
            SpriteSortMode.FrontToBack,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullCounterClockwise
        );
        _spriteBatch.Draw(_renderTarget, _renderDestination, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }    
}
