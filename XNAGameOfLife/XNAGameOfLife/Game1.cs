using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace XNAGameOfLife
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // Resources
        // Dynamic
        SpriteFont _spriteFont;
        Texture2D _pixel;
        Texture2D _cellTextureBlack;
        Texture2D _cellTextureRed;
        Texture2D _cellTextureGrey;
        // Static
        Texture2D _frame;
        Texture2D _pauseButton;
        Texture2D _pauseButtonOver;
        Texture2D _playButton;
        Texture2D _playButtonOver;
        Texture2D _zoomOutOver;
        Texture2D _zoomInOver;
        Texture2D _speedDownOver;
        Texture2D _speedUpOver;
        Texture2D _newOver;

        // State
        KeyboardState _keyboardStatePrevious;
        MouseState _mouseStatePrevious;
        PlayerIndex _currentPlayer;
        int _minUpdate = 150;
        double _lastUpdate = 0;

        bool _paused = true;

        // Data
        Vector2 gameSize = new Vector2(800, 480);
        // The number of times each pixel in the game is larger than a pixel on the screen
        // Make sure this is always divisible by both the window width and height or the game will refuse to start
        int pixelScale = 8;
        // A pixelScale^2 2D array to hold the current game of life
        Queue<int[,]> currentGame;
        int setValue;
        int blackBlocks = 0;
        int redBlocks = 0;
        int maxBlocks = 151;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 480;
            Content.RootDirectory = "Content";
            TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 10);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _currentPlayer = PlayerIndex.One;

            if ((int)gameSize.X % pixelScale != 0 || (int)gameSize.Y % pixelScale != 0)
                this.Exit();

            this.IsMouseVisible = true;

            currentGame = new Queue<int[,]>();
            for (int i = 0; i < 10; i++)
                currentGame.Enqueue(new int[(int)gameSize.X / pixelScale, (int)gameSize.Y / pixelScale]);

            //currentGame = new int[((int)gameSize.X * (int)gameSize.Y) / (pixelScale * pixelScale)];
            //oldGame = new int[(int)gameSize.X / pixelScale, (int)gameSize.Y / pixelScale];

            //for (int y = 0; y < currentGame.GetLength(1); y++)
            //{
            //    for (int x = 0; x < currentGame.GetLength(0); x++)
            //    {
            //        currentGame[x, y] = 1;
            //    }
            //}

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            _spriteFont = Content.Load<SpriteFont>("SpriteFont1");

            // Create black, singe pixel texture for drawing and such
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData<int>(new int[] { Color.Black.GetHashCode() });

            // Generate our pixelScale * pixelScale cell texture
            _cellTextureBlack = CreateTexture(Color.Black);
            _cellTextureRed = CreateTexture(Color.Red);
            _cellTextureGrey = CreateTexture(Color.LightGray);

            // TODO: use this.Content to load your game content here
            _frame = Content.Load<Texture2D>("frame");
            _pauseButton = Content.Load<Texture2D>("pausebutton");
            _pauseButtonOver = Content.Load<Texture2D>("pausebuttonover");
            _playButton = Content.Load<Texture2D>("playbutton");
            _playButtonOver = Content.Load<Texture2D>("playbuttonover");
            _zoomOutOver = Content.Load<Texture2D>("zoomoutover");
            _zoomInOver = Content.Load<Texture2D>("zoominover");
            _speedDownOver = Content.Load<Texture2D>("speeddownover");
            _speedUpOver = Content.Load<Texture2D>("speedupover");
            _newOver = Content.Load<Texture2D>("newover");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            _lastUpdate += gameTime.ElapsedGameTime.TotalMilliseconds;
            // Allows the game to exit
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                this.Exit();
            }
            else if (Mouse.GetState().LeftButton != ButtonState.Pressed)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Left))
                    _currentPlayer = PlayerIndex.One;
                else if (Keyboard.GetState().IsKeyDown(Keys.Right))
                    _currentPlayer = PlayerIndex.Two;
            }

            if (this.IsActive)
            {
                if ((Keyboard.GetState().IsKeyDown(Keys.P) && !_keyboardStatePrevious.IsKeyDown(Keys.P))
                    || ((Mouse.GetState().LeftButton == ButtonState.Released && _mouseStatePrevious.LeftButton == ButtonState.Pressed)
                    && Mouse.GetState().X > gameSize.X && Mouse.GetState().Y > 0 && Mouse.GetState().Y <= 100))
                    _paused = !_paused;

                int mouseX = Mouse.GetState().X;
                int mouseY = Mouse.GetState().Y;

                if (_paused && 
                    ((Keyboard.GetState().IsKeyDown(Keys.OemPlus) && !_keyboardStatePrevious.IsKeyDown(Keys.OemPlus))
                    || (Keyboard.GetState().IsKeyDown(Keys.OemMinus) && !_keyboardStatePrevious.IsKeyDown(Keys.OemMinus))
                    || ((Mouse.GetState().LeftButton == ButtonState.Released && _mouseStatePrevious.LeftButton == ButtonState.Pressed)
                    && Mouse.GetState().X > gameSize.X && Mouse.GetState().Y > 100 && Mouse.GetState().Y <= 200)))
                {
                    int oldPixelScale = pixelScale;

                    int modifier = 1;
                    if (Mouse.GetState().LeftButton == ButtonState.Released && _mouseStatePrevious.LeftButton == ButtonState.Pressed)
                    {
                        if (Mouse.GetState().X < gameSize.X + 112)
                        {
                            modifier = -1;
                        }
                    }
                    else if (!Keyboard.GetState().IsKeyDown(Keys.OemPlus))
                    {
                        modifier = -1;
                    }

                    // This is the most confusing for loop that I've ever written
                    // Future me: Consider throwing yourself off a bridge if you need to understand this
                    for (int i = pixelScale + modifier; i > 0 && i * 2 < (int)gameSize.X && i * 2 < (int)gameSize.Y; i += modifier)
                    {
                        if ((int)gameSize.X % i == 0 && (int)gameSize.Y % i == 0)
                        {
                            pixelScale = i;
                            break;
                        }
                    }

                    if (pixelScale != oldPixelScale)
                    {
                        Rectangle newRect = new Rectangle();
                        newRect.X = 0;
                        newRect.Y = 0;
                        // If the number of cells on screen has gone down
                        if (pixelScale > oldPixelScale)
                        {
                            // Only copy the new amount of cells
                            newRect.Width = (int)gameSize.X / pixelScale;
                            newRect.Height = (int)gameSize.Y / pixelScale;
                        }
                        else
                        {
                            // Copy all cells
                            newRect.Width = currentGame.Last().GetLength(0);
                            newRect.Height = currentGame.Last().GetLength(1);
                        }

                        for (int i = 0; i < currentGame.Count; i++)
                        {
                            currentGame.Enqueue(ResizeArray(currentGame.Dequeue(), newRect, new Rectangle(0, 0, (int)gameSize.X / pixelScale, (int)gameSize.Y / pixelScale)));
                        }
                        _cellTextureBlack = CreateTexture(Color.Black);
                        _cellTextureRed = CreateTexture(Color.Red);
                        _cellTextureGrey = CreateTexture(Color.LightGray);
                    }
                }
                else if (Mouse.GetState().LeftButton == ButtonState.Pressed && _paused)
                {
                    if ((_currentPlayer == PlayerIndex.One && mouseX < (gameSize.X / 2))
                        || (_currentPlayer == PlayerIndex.Two && mouseX >= (gameSize.X / 2)))
                    {
                        if (mouseX > 0 && mouseX < (int)gameSize.X && mouseY > 0 && mouseY < (int)gameSize.Y)
                        {
                            int currentCellX = (int)(Mouse.GetState().X / pixelScale);
                            int currentCellY = (int)(Mouse.GetState().Y / pixelScale);

                            if (Mouse.GetState().LeftButton != _mouseStatePrevious.LeftButton)
                            {
                                setValue = (currentGame.Last()[currentCellX, currentCellY] == 0 ? (_currentPlayer == PlayerIndex.One ? 1 : 2) : 0);
                            }

                            if (currentGame.Last()[currentCellX, currentCellY] != setValue)
                            {
                                if (currentGame.Last()[currentCellX, currentCellY] == 0 || (_currentPlayer == PlayerIndex.One ? 1 : 2) == currentGame.Last()[currentCellX, currentCellY])
                                {
                                    if (setValue == 0)
                                    {
                                        if (currentGame.Last()[currentCellX, currentCellY] == 1)
                                            blackBlocks--;
                                        else
                                            redBlocks--;
                                    }
                                    else
                                    {
                                        if (setValue == 1)
                                            blackBlocks++;
                                        else
                                            redBlocks++;
                                    }

                                    if (setValue == 0)
                                    {
                                        currentGame.Last()[currentCellX, currentCellY] = setValue;
                                    }
                                    else
                                    {
                                        if (setValue == 1)
                                        {
                                            if (blackBlocks < maxBlocks)
                                                currentGame.Last()[currentCellX, currentCellY] = setValue;
                                            else
                                                blackBlocks--;
                                        }
                                        else
                                        {
                                            if (redBlocks < maxBlocks)
                                                currentGame.Last()[currentCellX, currentCellY] = setValue;
                                            else
                                                redBlocks--;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (!_paused && _lastUpdate > _minUpdate)
                {
                    _lastUpdate = 0;
                    blackBlocks = 0;
                    redBlocks = 0;
                    int width = currentGame.Last().GetLength(0);
                    int height = currentGame.Last().GetLength(1);
                    int[,] nextFrame = currentGame.Dequeue();
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            nextFrame[x, y] = GetColour(currentGame.Last(), x, y);
                            if (nextFrame[x, y] == 1)
                                blackBlocks++;
                            else if (nextFrame[x, y] == 2)
                                redBlocks++;
                        }
                    }
                    currentGame.Enqueue(nextFrame);
                    //if (_lastTime + 10 < gameTime.TotalGameTime.TotalSeconds)
                    //{
                    //    if (_lastValues.X == blackBlocks && _lastValues.Y == redBlocks)
                    //    {
                    //        this.Exit();
                    //    }
                    //    else
                    //    {
                    //        _lastValues.X = blackBlocks;
                    //        _lastValues.Y = redBlocks;
                    //        _lastTime = (int)gameTime.TotalGameTime.TotalSeconds;
                    //    }
                    //}
                }

                if (Mouse.GetState().LeftButton == ButtonState.Released && _mouseStatePrevious.LeftButton == ButtonState.Pressed)
                {
                    if (Mouse.GetState().X > gameSize.X && Mouse.GetState().X < graphics.PreferredBackBufferWidth)
                    {
                        if (Mouse.GetState().Y > 300 && Mouse.GetState().Y <= 400)
                        {
                            _paused = true;
                            redBlocks = 0;
                            blackBlocks = 0;
                            foreach (int[,] game in currentGame)
                            {
                                for (int y = 0; y < game.GetLength(1); y++)
                                {
                                    for (int x = 0; x < game.GetLength(0); x++)
                                    {
                                        game[x, y] = 0;
                                    }
                                }
                            }
                        }
                        else if (Mouse.GetState().Y > 200 && Mouse.GetState().Y <= 300)
                        {
                            if (Mouse.GetState().X < gameSize.X + 112)
                            {
                                _minUpdate += 50;
                                if (_minUpdate > 500)
                                    _minUpdate = 500;
                            }
                            else
                            {
                                _minUpdate -= 50;
                                if (_minUpdate < 50)
                                    _minUpdate = 50;
                            }
                        }
                        else if (Mouse.GetState().Y > 400 && Mouse.GetState().Y <= 480)
                        {
                            if (Mouse.GetState().X < gameSize.X + 112)
                            {
                                _currentPlayer = PlayerIndex.One;
                            }
                            else
                            {
                                _currentPlayer = PlayerIndex.Two;
                            }
                        }
                    }
                }

                _keyboardStatePrevious = Keyboard.GetState();
                _mouseStatePrevious = Mouse.GetState();
            }

            for (int i = 0; i < currentGame.Count; i++)
            {
                for (int j = 0; j < currentGame.Count; j++)
                {
                    if (i != j)
                    {
                        if (currentGame.ElementAt(i).Equals(currentGame.ElementAt(j)))
                            this.Exit();
                    }
                }
            }


            // TODO: Add your update logic here
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            if (!_paused && Mouse.GetState().X > 0 && Mouse.GetState().X < gameSize.X && Mouse.GetState().Y > 0
                && Mouse.GetState().Y < gameSize.Y && Mouse.GetState().LeftButton == ButtonState.Pressed)
                GraphicsDevice.Clear(Color.Orange);

            spriteBatch.Begin();
            // Draw interface

            // Draw GOL
            for (int y = 0; y < currentGame.Last().GetLength(1); y++)
            {
                for (int x = 0; x < currentGame.Last().GetLength(0); x++)
                {
                    if (currentGame.Last()[x, y] == 0 && (x % 2) == y % 2)
                        spriteBatch.Draw(_cellTextureGrey, new Vector2(x * pixelScale, y * pixelScale), Color.White);
                    if (currentGame.Last()[x, y] == 1)
                        spriteBatch.Draw(_cellTextureBlack, new Vector2(x * pixelScale, y * pixelScale), Color.White);
                    else if (currentGame.Last()[x, y] != 0)
                        spriteBatch.Draw(_cellTextureRed, new Vector2(x * pixelScale, y * pixelScale), Color.White);
                }
            }

            spriteBatch.Draw(_frame, new Vector2(0, 0), Color.White);

            if (_paused)
            {
                spriteBatch.Draw(_playButton, new Vector2(gameSize.X, 0), Color.White);
            }
            else
            {
                spriteBatch.Draw(_pauseButton, new Vector2(gameSize.X, 0), Color.White);
            }

            if (_mouseStatePrevious.LeftButton == ButtonState.Pressed)
            {
                if (_mouseStatePrevious.X > gameSize.X && _mouseStatePrevious.X < graphics.PreferredBackBufferWidth)
                {
                    if (_mouseStatePrevious.Y > 0 && _mouseStatePrevious.Y <= 100)
                    {
                        if (_paused)
                        {
                            spriteBatch.Draw(_playButtonOver, new Vector2(gameSize.X, 0), Color.White);
                        }
                        else
                        {
                            spriteBatch.Draw(_pauseButtonOver, new Vector2(gameSize.X, 0), Color.White);
                        }
                    }
                    else if (_mouseStatePrevious.Y > 100 && _mouseStatePrevious.Y < 200)
                    {
                        if (_mouseStatePrevious.X - gameSize.X < 112)
                            spriteBatch.Draw(_zoomOutOver, new Vector2(gameSize.X, 100), Color.White);
                        else
                            spriteBatch.Draw(_zoomInOver, new Vector2(gameSize.X + 112, 100), Color.White);
                    }
                    else if (_mouseStatePrevious.Y > 200 && _mouseStatePrevious.Y < 300)
                    {
                        if (_mouseStatePrevious.X - gameSize.X < 112)
                            spriteBatch.Draw(_speedDownOver, new Vector2(gameSize.X, 200), Color.White);
                        else
                            spriteBatch.Draw(_speedUpOver, new Vector2(gameSize.X + 112, 200), Color.White);
                    }
                    else if (_mouseStatePrevious.Y > 300 && _mouseStatePrevious.Y < 400)
                    {
                        spriteBatch.Draw(_newOver, new Vector2(gameSize.X, 300), Color.White);
                    }
                    else if (_mouseStatePrevious.Y > 400 && _mouseStatePrevious.Y < 480 && _paused)
                    {
                        if (_mouseStatePrevious.X - gameSize.X < 112)
                            GraphicsDevice.Clear(Color.Black);
                        else
                            GraphicsDevice.Clear(Color.Red);
                    }
                }
            }

            if (_paused)
            {
                spriteBatch.Draw(_cellTextureGrey, new Rectangle((int)gameSize.X + (_currentPlayer == PlayerIndex.One ? 0 : 112), 400, 112, 80), new Color(0, 128, 0, 50));
                spriteBatch.Draw(_cellTextureGrey, new Rectangle((_currentPlayer == PlayerIndex.One ? 400 : 0), 0, 400, 480), new Color(10, 10, 10, 100));

                if (Mouse.GetState().Y > 400 && Mouse.GetState().Y <= 480)
                {
                    if (Mouse.GetState().X < gameSize.X + 112)
                        spriteBatch.Draw(_cellTextureGrey, new Rectangle((int)gameSize.X, 400, 112, 80), new Color(0, 128, 0, 70));
                    else
                        spriteBatch.Draw(_cellTextureGrey, new Rectangle((int)gameSize.X + 112, 400, 112, 80), new Color(0, 128, 0, 70));
                }
            }

            spriteBatch.DrawString(_spriteFont, blackBlocks.ToString(), new Vector2(gameSize.X + 50, 425), Color.Black);
            spriteBatch.DrawString(_spriteFont, redBlocks.ToString(), new Vector2(gameSize.X + 162, 425), Color.Red);

            spriteBatch.End();

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }

        /*
         * GetColour
         * Previously GetNeighbours
         * Calculates the number of neighbours for a cell
         * and returns the new colour for it
         */
        private int GetColour(int[,] array, int x, int y)
        {
            int cellNeighbours = 0;
            int blackNeighbours = 0;
            int redNeighbours = 0;

            if (x > 0 && array[x - 1, y] != 0)
            {
                if (array[x - 1, y] == 1)
                    blackNeighbours++;
                else
                    redNeighbours++;
            }
            if (x < array.GetLength(0) - 1 && array[x + 1, y] != 0)
            {
                if (array[x + 1, y] == 1)
                    blackNeighbours++;
                else
                    redNeighbours++;
            }

            if (y > 0)
            {
                if (array[x, y - 1] != 0)
                {
                    if (array[x, y - 1] == 1)
                        blackNeighbours++;
                    else
                        redNeighbours++;
                }
                if (x > 0 && array[x - 1, y - 1] != 0)
                {
                    if (array[x - 1, y - 1] == 1)
                        blackNeighbours++;
                    else
                        redNeighbours++;
                }
                if (x < array.GetLength(0) - 1 && array[x + 1, y - 1] != 0)
                {
                    if (array[x + 1, y - 1] == 1)
                        blackNeighbours++;
                    else
                        redNeighbours++;
                }
            }

            if (y < array.GetLength(1) - 1)
            {
                if (array[x, y + 1] != 0)
                {
                    if (array[x, y + 1] == 1)
                        blackNeighbours++;
                    else
                        redNeighbours++;
                }
                if (x > 0 && array[x - 1, y + 1] != 0)
                {
                    if (array[x - 1, y + 1] == 1)
                        blackNeighbours++;
                    else
                        redNeighbours++;
                }
                if (x < array.GetLength(0) - 1 && array[x + 1, y + 1] != 0)
                {
                    if (array[x + 1, y + 1] == 1)
                        blackNeighbours++;
                    else
                        redNeighbours++;
                }
            }

            cellNeighbours = redNeighbours + blackNeighbours;

            //if (current == 0)
            //{
            //    if (cellNeighbours == 3)
            //        return 1;
            //    else
            //        return 0;
            //}
            //else
            //{
            //    if (cellNeighbours < 2)
            //        return 0;
            //    else if (cellNeighbours < 4)
            //        return 1;
            //    else
            //        return 0;
            //}

            if (array[x, y] == 0)
            {
                if (cellNeighbours == 3)
                {
                    return (blackNeighbours == redNeighbours ? 0 : (blackNeighbours > redNeighbours ? 1 : 2));
                }
                //else
                //{
                //    return 0;
                //}
            }
            else
            {
                if (cellNeighbours < 2)
                {
                    return 0;
                }
                else if (cellNeighbours < 4)
                {
                    return (blackNeighbours == redNeighbours ? 0 : (blackNeighbours > redNeighbours ? 1 : 2));
                }
                //else
                //{
                //    return 0;
                //}
            }

            return 0;
        }
        
        /*
         * CreateTexture
         * Returns the texture2D to use for each GoL cell
         * can be any size, but is pixelScale * pixelScale by default
         */
        private Texture2D CreateTexture(Color colour)
        {
            Texture2D newTexture = new Texture2D(GraphicsDevice, pixelScale, pixelScale);

            int[] pixelTexture = new int[pixelScale * pixelScale];

            int whiteColour = colour.GetHashCode();
            for (int i = 0; i < pixelScale * pixelScale; i++)
            {
                pixelTexture[i] = whiteColour;
            }

            newTexture.SetData<int>(pixelTexture);
            return newTexture;
        }

        /*
         * DrawLine
         * Scales a pixel square texture to create a line between point a and point b
         */
        private void DrawLine(Vector2 a, Vector2 b)
        {
            
        }

        /*
         * ResizeArray
         * Resizes a 2D int array
         */
        private int[,] ResizeArray(int[,] source, Rectangle sourceRect, Rectangle destinationRect)
        {
            int[,] destination = new int[destinationRect.Width, destinationRect.Height];

            for (int y = 0; y < sourceRect.Height; y++)
            {
                for (int x = 0; x < sourceRect.Width; x++)
                {
                    destination[x, y] = source[x + sourceRect.X, y + sourceRect.Y];
                }
            }

            return destination;
        }
    }
}
