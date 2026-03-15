using System;
using System.Diagnostics;
using ComputerGameFinal.Engine.Managers;
using ComputerGameFinal.Game;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ResourceManager = ComputerGameFinal.Engine.Managers.ResourceManager;

namespace ComputerGameFinal;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        SceneManager.Instance.AddScene<MainScene>("main");
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        ResourceManager.Instance.LoadAll(Content);

        // สร้าง 1×1 pixel texture สำหรับ debug drawing (PickaxeRenderer, rope line)
        var pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        ResourceManager.Instance.LoadTexture("pixel", pixel);

        SceneManager.Instance.LoadScene("main");
    }

    protected override void Update(GameTime gameTime)
    {
        InputManager.Instance.Update();
        SceneManager.Instance.CurrentScene.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);
        
        var currentScene = SceneManager.Instance.CurrentScene;
        Matrix cameraTransform = currentScene?.GetCameraTransform() ?? Matrix.Identity;
        
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

        base.Draw(gameTime);
    }
}
