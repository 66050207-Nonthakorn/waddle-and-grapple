using ComputerGameFinal.Engine.Managers;
using ComputerGameFinal.Game.Example;
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
        SceneManager.Instance.AddScene<CollisionDemoScene>("collision_demo");
        base.Initialize();
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

    protected override void Update(GameTime gameTime)
    {
        InputManager.Instance.Update();
        SceneManager.Instance.CurrentScene.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(0, 20, 60)); // กรมท่า
        
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
