#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Katabasis;
using Katabasis.ImGui;

//Two paddles, one Player controlled and one AI controlled.
//A way to go back to the main menu from the gameplay screen
//A way to restart the gameplay screen from zero (ie: no points)
//A way to tell the users who won. 
//A way to play sounds to provide feedback.

namespace Pong
{
	public class App : Game
	{
		// Declare variables
		SpriteBatch SpriteBatch;
		ImGuiRenderer ImGuiRenderer;
		public enum GameState
		{
			Start = 0,
			Title = 1,
			Game = 2
		}
		public static GameState State;
		
		// Menu
		private Vector2 WindowCentre;
		private float titleTextScale = 10f;
		private int textBoxScale = 5;
		private Dictionary<string, Rectangle> titleTextBoxes = new Dictionary<string, Rectangle>();
		private Dictionary<string, Rectangle> gameTextBoxes = new Dictionary<string, Rectangle>();
		string[] gameMenu = { "Quit to Title", "Quit to Desktop" };
		string[] titleMenu = { "Single-player", "Two-player" };
		private int chosenOption = 0;
		private double introTimer = 0;
		private bool startIntro = false;
		private bool toStart = false;
		
		// Game
		private bool twoPlayer = true;
		private bool disableMovement = false;
		private bool gamePaused = false;
		private bool inGameMenu = false;
		private double timer = 0f;

		// AI
		private Tuple<float, double>? aiMovePositionData;
		private double aiTimer;
		private bool movingOnlyDown = false;
		private int bounceCounter = 0;
		private Random random = new Random();

		// Players
		// These variables aren't specific to the players
		private int accel = 200;
		// private int maxSpeed = 400; Don't really need this since the paddles have a short space where they can accelerate.
		// Static because we don't create an instance of the App class
		private static int paddleWidth = 6;
		private static int paddleHeight = 56;
		private class Player
		{
			public int score = 0;
			public float curSpeed = 0;

			private GameWindow winBounds;

			private float pX;
			public int X
			{
				get
				{
					return (int)pX;
				}
				set
				{
					pX = value;
				}
			}
			private float pY;
			public int Y
			{
				get
				{
					return (int)pY;
				}
				set
				{
					pY = value;
				}
			}
			public Vector2 Velocity()
			{
				return new Vector2(curSpeed - Math.Sign(curSpeed) * 50, curSpeed - Math.Sign(curSpeed) * 50);
			}
			public Rectangle Bounds()
			{
				return new Rectangle(X, Y, paddleWidth, paddleHeight);
			}

			public void AccelerateBy(int accel, GameTime gameTime)
			{
				if ((pY == 1 && accel < 0) || (pY == winBounds.ClientBounds.Height - 1 - paddleHeight && accel > 0))
				{
					accel = 0;
					curSpeed = 0;
				}
				if (Math.Sign(accel) != Math.Sign(curSpeed)) { curSpeed = Math.Sign(accel) * 50; }
				curSpeed += accel * (float)gameTime.ElapsedGameTime.TotalSeconds;
			}
			public void Update(GameTime gameTime, GameWindow Window)
			{
				winBounds = Window;

				if (pY + paddleHeight < Window.ClientBounds.Height - 1 && pY > 1)
				{
					pY += curSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
				}
				else if (pY + paddleHeight > Window.ClientBounds.Height - 1)
				{
					pY = Window.ClientBounds.Height - 1 - paddleHeight;
				}
				else if (Y < 1)
				{
					pY = 1;
				}
				else if (pY == Window.ClientBounds.Height - 1 - paddleHeight && curSpeed < 0)
				{
					pY += curSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
				}
				else if (pY == 1 && curSpeed > 0)
				{
					pY += curSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
				}
			}
			public Player(int x, int y)
			{
				pX = x;
				pY = y;
			}
		};
		private Player PlayerOne;
		private Player PlayerTwo;
		
		// Borders
		private class Border
		{
			public int borderWidth;
			public int borderHeight;
			public int X;
			public int Y;
			public Rectangle Bounds()
			{
				return new Rectangle(X, Y, borderWidth, borderHeight);
			}

			public Border(int x, int y, int bw, int bh)
			{
				X = x;
				Y = y;
				borderWidth = bw;
				borderHeight = bh;
			}
		}
		private Border topBorder;
		private Border bottomBorder;
		private Border leftBorder;
		private Border rightBorder;
		
		// Ball
		private static int ballLength = 4;
		private static int minSpeedX = 100;
		private class Ball
		{
			public Vector2 position = new Vector2(100, 100);
			public Vector2 velocity = Vector2.One;
			public Rectangle Bounds()
			{
				return new Rectangle((int)position.X, (int)position.Y, ballLength, ballLength);
			}
			public void Update(GameTime gameTime)
			{
				var minVelocity = minSpeedX * Math.Sign(velocity.X);
				if (Math.Abs(velocity.X) < minSpeedX)
				{
					velocity.X = minVelocity;
				}
				position += velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
			}
		}
		private Ball ball = new Ball();
		private float collisionTimer = 0f;
		private float spawnTimer = 0f;
		private bool ballSpawned = false;
		private Vector2? storedVelocity;
		private Tuple<Vector2, float>? nextPredictedPositionData;
		private Tuple<Vector2, float>? finalPredictedPositionData;
		private Vector2? positionData;
		private Vector2? velocityData;
		private int? verticalCollision;

		// Input 
		private MouseState _currentMouseState;
		private MouseState _previousMouseState;
		private KeyboardState _currentKeyboardState;
		private KeyboardState _previousKeyboardState;

		// Events 
		private delegate void BounceEvent(Vector2 ballVelocity, bool isVerticalCollision);
		private BounceEvent bounceHandler;

		// Textures
		private Texture2D paddleTexture;
		private Texture2D ballTexture;
		private Texture2D dividerTexture;
		private Texture2D rectTexture;
		SpriteFont spriteFont;
		SpriteFont orangeFont;
		
		// 2005 Audio - Credits to NoiseCollector from freesound.org
		private Song borderBounce;
		private Song playerBounce;
		private Song playerScores;
		
		public App()
		{
			// How the game should start
			Window.AllowUserResizing = true;
		}

		protected override void Initialize()
		{
			// Logic?
			SpriteBatch = new SpriteBatch();
			ImGuiRenderer = new ImGuiRenderer();
			State = GameState.Game;
			
			// Menu 
			WindowCentre = new Vector2(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);
			
			PlayerOne = new Player(Window.ClientBounds.Width / 8, Window.ClientBounds.Height / 2 - paddleHeight / 2);
			PlayerTwo = new Player(Window.ClientBounds.Width * 7 / 8 - paddleWidth, Window.ClientBounds.Height / 2 - paddleHeight / 2);
			// Because of our eventual superior pre calculations the width of the border doesnt really matter as the ball won't clip through
			topBorder = new Border(0, 0, Window.ClientBounds.Width, 1);
			bottomBorder = new Border(0, Window.ClientBounds.Height - 1, Window.ClientBounds.Width, 1);
			leftBorder = new Border(0, 0, 1, Window.ClientBounds.Height);
			rightBorder = new Border(Window.ClientBounds.Width - 1, 0, 1, Window.ClientBounds.Height);
			ball.position = WindowCentre;
			ball.velocity = new Vector2(200, -150);

			bounceHandler += (Vector2 ballVelocity, bool isVerticalCollision) =>
			{
				if (isVerticalCollision)
				{
					// Play border noise
					MediaPlayer.Play(borderBounce);
				}
				else
				{
					// Play paddle noise
					MediaPlayer.Play(playerBounce);
				}
			};
			base.Initialize();
		}

		protected override void LoadContent()
		{
			// Spritefonts
			spriteFont = Utility.SpriteFontReader.FromFile("Assets/Alfabeto.png",
				new Dictionary<char, Vector2>() { { 'm', new Vector2(7, 7) }, { 'w', new Vector2(7, 7) }, { 'q', new Vector2(6, 7) } },
				new Dictionary<char, Vector2>() { { '!', new Vector2(3, 7) }, { '?', new Vector2(5, 7) }, { ' ', new Vector2(5, 7) } },
				new Vector2(5, 7), new Vector2(1, 1), new Vector2(5, 14));
			orangeFont = Utility.SpriteFontReader.FromFile("Assets/Alfabeto0.png",
				new Dictionary<char, Vector2>() { { 'm', new Vector2(7, 7) }, { 'w', new Vector2(7, 7) }, { 'q', new Vector2(6, 7) } },
				new Dictionary<char, Vector2>() { { '!', new Vector2(3, 7) }, { '?', new Vector2(5, 7) }, { ' ', new Vector2(5, 7) } },
				new Vector2(5, 7), new Vector2(1, 1), new Vector2(5, 14));

			// Textures
			paddleTexture = Texture2D.FromFile("Assets/paddle.png");
			ballTexture = Texture2D.FromFile("Assets/ball.png");
			rectTexture = Texture2D.FromFile("Assets/rectangle.png");
			dividerTexture = Texture2D.FromFile("Assets/divider.png");

			// Audio
			borderBounce = Song.FromFile("borderBounce", "Assets/borderBounce.ogg");
			playerBounce = Song.FromFile("playerBounce", "Assets/playerBounce.ogg");
			playerScores = Song.FromFile("playerScores", "Assets/playerScores.ogg");
		}
		
		
		protected override void Update(GameTime gameTime)
		{
			HandleInput();

			switch (State)
			{
				case GameState.Game:
					UpdateGame(gameTime);
					break;
				case GameState.Title:
					UpdateTitle(gameTime);
					break;
				case GameState.Start:
					UpdateStart(gameTime);
					break;
			}
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);
			
			SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.Default, RasterizerState.CullNone);

			switch (State)
			{
				case GameState.Game:
					DrawGame(gameTime);
					break;
				case GameState.Title:
					DrawTitle();
					break;
				case GameState.Start:
					DrawStart();
					break;
			}

			SpriteBatch.End();
		}

		private void UpdateStart(GameTime gameTime)
		{
			if (KeyPressed(Keys.Space))
			{
				startIntro = true;
			}
			if (startIntro)
			{
				introTimer += gameTime.ElapsedGameTime.TotalSeconds;
			}
			if (introTimer > 0.5)
			{
				State = GameState.Title;
				startIntro = false;
				introTimer = 0;
			}
		}

		private void DrawStart()
		{
			if (!startIntro)
			{
				SpriteBatch.DrawString(orangeFont, "Pong", WindowCentre - new Vector2(orangeFont.MeasureString("Pong").X, orangeFont.MeasureString("Pong").Y - 7) * 5, Color.White, 0f, Vector2.Zero, 10f, 0, 0f);
			}
		}
		private void DrawTitle()
		{
			if (startIntro) { return; }

			var textBoxDimensions = new Rectangle(0, 0, rectTexture.Width * textBoxScale, rectTexture.Height * textBoxScale);

			int spacing = textBoxDimensions.Height / 2;

			float startHeight = WindowCentre.Y - textBoxDimensions.Height * titleMenu.Length / 2 - spacing * (titleMenu.Length - 1) / 2;

			foreach (var item in titleMenu)
			{
				titleTextBoxes[item] = new Rectangle((int)WindowCentre.X - textBoxDimensions.Width / 2, (int)startHeight, textBoxDimensions.Width, textBoxDimensions.Height);
				startHeight += textBoxDimensions.Height + spacing;
			}

			var selectCounter = 0;

			foreach (var item in titleTextBoxes)
			{
				var textBuffer = 20;
				var stringSize = orangeFont.MeasureString(item.Key);
				var textScale = (textBoxDimensions.Width - textBuffer * 2) / stringSize.X;

				if (selectCounter == chosenOption % titleMenu.Length)
				{
					textBuffer -= (int)(stringSize.X * textScale * 0.1f / 2);
					textScale *= 1.1f;
					var scaledDimensions = new Vector2((int)(item.Value.Width * 1.1f), (int)(item.Value.Height * 1.1f));
					SpriteBatch.Draw(rectTexture, new Rectangle(item.Value.X - ((int)scaledDimensions.X - item.Value.Width) / 2, item.Value.Y - ((int)scaledDimensions.Y - item.Value.Height) / 2, (int)scaledDimensions.X, (int)scaledDimensions.Y), Color.BurlyWood);
					SpriteBatch.DrawString(orangeFont, item.Key, new Vector2(item.Value.X + textBuffer, (item.Value.Y + item.Value.Height / 2 + 7 * textScale) - stringSize.Y * textScale / 2), Color.White, 0f, Vector2.Zero, textScale, 0, 0f);
				}
				else
				{
					SpriteBatch.Draw(rectTexture, item.Value, Color.Wheat);
					SpriteBatch.DrawString(spriteFont, item.Key, new Vector2(item.Value.X + textBuffer, (item.Value.Y + item.Value.Height / 2 + 7 * textScale) - stringSize.Y * textScale / 2), Color.White, 0f, Vector2.Zero, textScale, 0, 0f);
				}

				selectCounter++;
			}
		}
		private void UpdateTitle(GameTime gameTime)
		{
			if (KeyPressed(Keys.Up) || KeyPressed(Keys.Down))
			{
				chosenOption++;
			}

			if (KeyPressed(Keys.Escape))
			{
				startIntro = true;
				toStart = true;
			}

			if (KeyPressed(Keys.Space))
			{
				switch (chosenOption % gameMenu.Length)
				{
					case 0:
						twoPlayer = false;
						toStart = false;
						startIntro = true;
						break;
					case 1:
						twoPlayer = true;
						toStart = false;
						startIntro = true;
						break;
				}
			}
			if (startIntro)
			{
				introTimer += gameTime.ElapsedGameTime.TotalSeconds;
			}
			if (introTimer > 0.5)
			{
				ResetGame();
				State = toStart ? GameState.Start : GameState.Game;
				startIntro = false;
				introTimer = 0;
			}
		}

		private void ResetGame()
		{
			PlayerOne = new Player(Window.ClientBounds.Width / 8, Window.ClientBounds.Height / 2 - paddleHeight / 2);
			PlayerTwo = new Player(Window.ClientBounds.Width * 7 / 8 - paddleWidth, Window.ClientBounds.Height / 2 - paddleHeight / 2);
			ball.position = WindowCentre;
			ballSpawned = false;
			ball.velocity = new Vector2(200, -150);
			collisionTimer = 0f;
			spawnTimer = 0f;
			storedVelocity = null;
			nextPredictedPositionData = null;
			finalPredictedPositionData = null;
			positionData = null;
			velocityData = null;
			verticalCollision = null;
			aiTimer = 0f;
		}
		private void BounceBall(Vector2 colliderVelocity, bool verticalCollision = false)
		{
			bool toPlayerTwo = ball.velocity.X > 0; 
			// We'll add the players' velocity to the ball to make it more exciting.
			if (verticalCollision)
			{
				ball.velocity = new Vector2(ball.velocity.X, -ball.velocity.Y) + colliderVelocity;
				bounceHandler(ball.velocity, verticalCollision);
			}
			else if ( (toPlayerTwo && InRange(ball.position.Y, PlayerTwo.Y, PlayerTwo.Y + paddleHeight)) || (!toPlayerTwo && InRange(ball.position.Y, PlayerOne.Y, PlayerOne.Y + paddleHeight)) )
			{
				ball.velocity = new Vector2(-ball.velocity.X, ball.velocity.Y) + colliderVelocity;

				AIHitUpdate();
				bounceHandler(ball.velocity, verticalCollision);
			}
		}

		private bool InRange(float x, float a, float b)
		{
			// Concave up, a and b are x-intercepts
			return (x - a) * (x - b) <= 0;
		}
		private void UpdateGame(GameTime gameTime)
		{
			if (!gamePaused)
			{
				UpdateCollisions(gameTime);
				UpdateMovement(gameTime);
			}
			UpdateMenu(gameTime);
		}
		private void UpdateMenu(GameTime gameTime)
		{
			if (_previousKeyboardState.IsKeyDown(Keys.Escape) && _currentKeyboardState.IsKeyUp(Keys.Escape)) 
			{
				if (inGameMenu) { CloseMenu(); } else { OpenMenu(); }

				chosenOption = 0;
			}
			if (inGameMenu)
			{
				if (KeyPressed(Keys.Up) || KeyPressed(Keys.Down))
				{
					chosenOption++;
				}

				if (KeyPressed(Keys.Space))
				{
					switch (chosenOption % gameMenu.Length)
					{
						case 0:
							startIntro = true;
							CloseMenu();
							break;
						case 1:
							Exit();
							break;
					}
				}
			}
			if (startIntro)
			{
				introTimer += gameTime.ElapsedGameTime.TotalSeconds;
				gamePaused = true;
			}
			if (introTimer > 0.5)
			{
				gamePaused = false;
				State = GameState.Title;
				startIntro = false;
				introTimer = 0;
			}
		}

		private bool KeyPressed(Keys key)
		{
			return _previousKeyboardState.IsKeyDown(key) && _currentKeyboardState.IsKeyUp(key);
		}
		private void UpdateMovement(GameTime gameTime)
		{
			if (!disableMovement)
			{
				PlayerOne.Update(gameTime, Window);
				PlayerTwo.Update(gameTime, Window);
				ball.Update(gameTime);

				if (_previousKeyboardState.IsKeyDown(Keys.R) && _currentKeyboardState.IsKeyUp(Keys.R))
				{
					Console.WriteLine(ball.velocity);
				}
				if (_currentKeyboardState.IsKeyDown(Keys.W))
				{
					PlayerOne.AccelerateBy(-accel, gameTime);
				}
				else if (_currentKeyboardState.IsKeyDown(Keys.S))
				{
					PlayerOne.AccelerateBy(accel, gameTime);
				}
				else
				{
					PlayerOne.curSpeed = 0;
				}
				if (twoPlayer)
				{
					if (_currentKeyboardState.IsKeyDown(Keys.Up))
					{
						PlayerTwo.AccelerateBy(-accel, gameTime);
					}
					else if (_currentKeyboardState.IsKeyDown(Keys.Down))
					{
						PlayerTwo.AccelerateBy(accel, gameTime);
					}
					else
					{
						PlayerTwo.curSpeed = 0;
					}
				}
				else
				{
					UpdateAI(gameTime);
				}
			}
		}

		private void UpdateAI(GameTime gameTime)
		{
			if (aiMovePositionData == null || twoPlayer) { return; }
			var aiNextPosition = aiMovePositionData.Item1;
			var aiTimeToNextPos = aiMovePositionData.Item2;

			if (aiTimeToNextPos > aiTimer)
			{
				aiTimer += gameTime.ElapsedGameTime.TotalSeconds;

				if (!movingOnlyDown)
				{
					PlayerTwo.AccelerateBy(-accel, gameTime);
				}
				else
				{
					PlayerTwo.AccelerateBy(accel, gameTime);
				}
			}

			if (aiTimer > aiTimeToNextPos)
			{
				PlayerTwo.curSpeed = 0;
				PlayerTwo.Y = (int)aiNextPosition;
				aiTimer = 0;
				aiMovePositionData = null;
			}
		}
		private void SetAIMoveTo(float nextPosition)
		{
			Console.WriteLine("Set Move Called");
			if (nextPosition < 0)
			{
				nextPosition = 0;
			}
			else if (nextPosition > Window.ClientBounds.Height - paddleHeight)
			{
				nextPosition = Window.ClientBounds.Height - paddleHeight;
			}
			// s = ut + 0.5at^2  - > t = (-u + sqrt(u^2 + 2as) / a
			var displacement = nextPosition - PlayerTwo.Y;

			movingOnlyDown = displacement > 0;

			//var time = Math.Sqrt(Math.Abs(2 * (nextPosition - PlayerTwo.Y) / accel));
			var a = movingOnlyDown ? accel : -accel;
			var u = movingOnlyDown ? 50 : -50;

			var time = movingOnlyDown ? (-u + Math.Sqrt(u * u + 2 * a * displacement)) / a : (-u - Math.Sqrt(u * u + 2 * a * displacement)) / a;

			aiMovePositionData = new Tuple<float, double>(nextPosition, time);

		}

		private void AIHitUpdate()
		{
			if (twoPlayer) { return; }
			if (ball.velocity.X > 0)
			{
				SetAIMoveTo(PredictFinalBallPositionOnPlayerAxis().Item1.Y - paddleHeight / 2 + GenerateAIOffset(paddleHeight / 2));
			}
			else
			{
				SetAIMoveTo(PredictFinalBallPositionOnPlayerAxis(PredictFinalBallPositionOnPlayerAxis().Item1, new Vector2(-ball.velocity.X, ball.velocity.Y * (int)Math.Pow(-1, bounceCounter))).Item1.Y - paddleHeight / 2 + GenerateAIOffset(paddleHeight * 2));
			}
		}
		private int GenerateAIOffset(int maxDifferentiation)
		{

			return random.Next(2 * maxDifferentiation) - random.Next(2 * maxDifferentiation);
		}
		private void UpdateCollisions(GameTime gameTime)
		{
			HandleBallCollision(gameTime);
		}

		private void HandleBallCollision(GameTime gameTime)
		{
			if (!ballSpawned)
			{
				SpawnBall(gameTime);
			}

			if (!ballSpawned) { return; }

			nextPredictedPositionData ??= PredictBallPositionOnPlayerAxis();
			verticalCollision ??= 0;

			if (nextPredictedPositionData.Item1.Y < 0 || nextPredictedPositionData.Item1.Y > Window.ClientBounds.Height)
			{
				nextPredictedPositionData = PredictBallPositionOnBorderAxis();
				verticalCollision = 1;
			}

			if (ballSpawned && (ball.position.X < 0 || ball.position.X > Window.ClientBounds.Width))
			{
				if (spawnTimer == 0)
				{
					MediaPlayer.Play(playerScores);

					if (ball.velocity.X > 0)
					{
						PlayerOne.score++;
					}
					else
					{
						PlayerTwo.score++;
					}
				}
				ballSpawned = false;
			}

			collisionTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

			if(collisionTimer > nextPredictedPositionData.Item2)
			{
				if(nextPredictedPositionData.Item2 > 0) // Basically so it doesnt bounce after it has passed the paddle
				{
					BounceBall(verticalCollision == 1 ? Vector2.Zero : ball.velocity.X > 0 ? PlayerTwo.Velocity() : PlayerOne.Velocity(), verticalCollision == 1);
					ball.position = nextPredictedPositionData.Item1; // This is required to make the bounce appear as accurate as possible to the predicted/real data
				}
				nextPredictedPositionData = null;
				verticalCollision = null;
				collisionTimer = 0f;
			}
		}
		private Tuple<Vector2, float> PredictBallPositionOnPlayerAxis(Vector2 position = default, Vector2 velocity = default)
		{
			if (position == default) { position = ball.position; velocity = ball.velocity; }

			bool toPlayerTwo = Math.Sign(velocity.X) == 1;
			float dx;
			float dy;
			float time;
			Vector2 predictedPos;
			Tuple<Vector2, float> returnValue;

			if (toPlayerTwo)
			{
				dx = PlayerTwo.X - position.X - ballLength;
				time = dx / velocity.X;
				dy = velocity.Y * time;
				predictedPos = new Vector2(position.X + dx, position.Y + dy);
				returnValue = new Tuple<Vector2, float>(predictedPos, time);
			}
			else
			{
				dx = PlayerOne.X + paddleWidth - position.X;
				time = dx / velocity.X;
				dy = velocity.Y * time;
				predictedPos = new Vector2(position.X + dx, position.Y + dy);
				returnValue = new Tuple<Vector2, float>(predictedPos, time);
			}
			return returnValue;
		}

		private Tuple<Vector2, float> PredictBallPositionOnBorderAxis(Vector2 position = default, Vector2 velocity = default)
		{
			if (position == default) { position = ball.position; velocity = ball.velocity; }

			bool toBottomBorder = Math.Sign(velocity.Y) == 1;
			float dx;
			float dy;
			float time;
			Vector2 predictedPos;
			Tuple<Vector2, float> returnValue;

			if (toBottomBorder)
			{
				dy = bottomBorder.Y - ballLength - position.Y;
				time = dy / velocity.Y;
				dx = velocity.X * time;
				predictedPos = new Vector2(position.X + dx, position.Y + dy);
				returnValue = new Tuple<Vector2, float>(predictedPos, time);
			}
			else
			{
				dy = topBorder.Y + topBorder.borderHeight - position.Y;
				time = dy / velocity.Y;
				dx = velocity.X * time;
				predictedPos = new Vector2(position.X + dx, position.Y + dy);
				returnValue = new Tuple<Vector2, float>(predictedPos, time);
			}
			return returnValue;
		}

		private Tuple<Vector2, float> PredictFinalBallPositionOnPlayerAxis(Vector2 position = default, Vector2 velocity = default)
		{
			if (position == default) { position = ball.position; velocity = ball.velocity; }

			var borderPos = PredictBallPositionOnBorderAxis(position, velocity);
			var playerPos = PredictBallPositionOnPlayerAxis(position, velocity);

			bounceCounter = 0;

			if (InRange(borderPos.Item1.X, 0, Window.ClientBounds.Width))
			{
				finalPredictedPositionData ??= borderPos;
				positionData ??= position;
				velocityData ??= velocity;
				bounceCounter++;
			}
			else
			{
				return playerPos;
			}

			while (true)
			{
				bounceCounter++;
				var nextBounce = PredictBallPositionOnBorderAxis(positionData.Value, velocityData.Value);
				if (InRange(nextBounce.Item1.X, PlayerOne.X + paddleWidth, PlayerTwo.X))
				{
					positionData = nextBounce.Item1;
					velocityData = new Vector2(velocityData.Value.X, -velocityData.Value.Y);
					continue;
				}
				else
				{
					var copy1 = positionData.Value;
					var copy2 = velocityData.Value;
					finalPredictedPositionData = null;
					positionData = null;
					velocityData = null;
					return PredictBallPositionOnPlayerAxis(copy1, copy2);
				}

			}
		}
		private void SpawnBall(GameTime gameTime)
		{
			storedVelocity ??= ball.velocity;

			ball.position = WindowCentre;
			ball.velocity = Vector2.Zero;
			spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

			if (spawnTimer > 1)
			{
				spawnTimer = 0f;
				ball.velocity = storedVelocity.Value;
				storedVelocity = null;
				ballSpawned = true;
				AIHitUpdate();
			}
		}
		private void GameOver(GameTime gameTime)
		{
			disableMovement = true;
			timer += gameTime.ElapsedGameTime.TotalSeconds;
			ball.position = new Vector2(1000, 100);
			if ((int)timer % 2 == 0)
			{
				SpriteBatch.DrawString(orangeFont, "Game Over", WindowCentre - new Vector2(orangeFont.MeasureString("Game Over").X, -orangeFont.MeasureString("Game Over").Y) * titleTextScale / 2, Color.White, 0f, Vector2.Zero, titleTextScale, SpriteEffects.None, 0f);
			}
			if (_previousKeyboardState.IsKeyDown(Keys.Space) && _currentKeyboardState.IsKeyUp(Keys.Space))
			{
				Exit();
			}
			if (_previousKeyboardState.IsKeyDown(Keys.Escape) && _currentKeyboardState.IsKeyUp(Keys.Escape))
			{
				State = GameState.Title;
			}
		}
		private void DrawGame(GameTime gameTime)
		{
			DrawHud(gameTime);
			SpriteBatch.Draw(paddleTexture, new Vector2(PlayerOne.X, PlayerOne.Y), Color.White);
			SpriteBatch.Draw(paddleTexture, new Vector2(PlayerTwo.X, PlayerTwo.Y), Color.White);
			SpriteBatch.Draw(rectTexture, bottomBorder.Bounds(), Color.Black);
			SpriteBatch.Draw(rectTexture, topBorder.Bounds(), Color.Black);
			SpriteBatch.Draw(rectTexture, leftBorder.Bounds(), Color.Black);
			SpriteBatch.Draw(rectTexture, rightBorder.Bounds(), Color.Black);
			if (ballSpawned) { SpriteBatch.Draw(ballTexture, ball.position, Color.White); }

			SpriteBatch.Draw(ballTexture, PredictFinalBallPositionOnPlayerAxis().Item1, Color.Purple);
			SpriteBatch.Draw(ballTexture, PredictFinalBallPositionOnPlayerAxis(PredictFinalBallPositionOnPlayerAxis().Item1, new Vector2(-ball.velocity.X, ball.velocity.Y * (int)Math.Pow(-1, bounceCounter))).Item1, Color.FloralWhite);

			if (PlayerOne.score >= 15 || PlayerTwo.score >= 15)
			{
				GameOver(gameTime);
			}
			if (inGameMenu) { DrawInGameMenu(gameTime); }
		}
		private void DrawInGameMenu(GameTime gameTime)
		{
			var textBoxDimensions = new Rectangle(0, 0, rectTexture.Width * textBoxScale, rectTexture.Height * textBoxScale);

			int spacing = textBoxDimensions.Height / 2;

			float startHeight = WindowCentre.Y - textBoxDimensions.Height * gameMenu.Length / 2 - spacing * (gameMenu.Length - 1) / 2;

			foreach (var item in gameMenu)
			{
				gameTextBoxes[item] = new Rectangle((int)WindowCentre.X - textBoxDimensions.Width / 2, (int)startHeight, textBoxDimensions.Width, textBoxDimensions.Height);
				startHeight += textBoxDimensions.Height + spacing;
			}

			var selectCounter = 0;

			foreach (var item in gameTextBoxes)
			{
				var textBuffer = 20;
				var stringSize = orangeFont.MeasureString(item.Key);
				var textScale = (textBoxDimensions.Width - textBuffer * 2 ) / stringSize.X;

				if (selectCounter == chosenOption % gameMenu.Length)
				{
					textBuffer -= (int)(stringSize.X * textScale * 0.1f / 2);
					textScale *= 1.1f;
					var scaledDimensions = new Vector2((int)(item.Value.Width * 1.1f), (int)(item.Value.Height * 1.1f));
					SpriteBatch.Draw(rectTexture, new Rectangle(item.Value.X - ((int)scaledDimensions.X - item.Value.Width) / 2, item.Value.Y - ((int)scaledDimensions.Y - item.Value.Height) / 2, (int)scaledDimensions.X, (int)scaledDimensions.Y), Color.BurlyWood);
					SpriteBatch.DrawString(orangeFont, item.Key, new Vector2(item.Value.X + textBuffer, (item.Value.Y + item.Value.Height / 2 + 7 * textScale) - stringSize.Y * textScale / 2), Color.White, 0f, Vector2.Zero, textScale, 0, 0f);
				}
				else
				{
					SpriteBatch.Draw(rectTexture, item.Value, Color.Wheat);
					SpriteBatch.DrawString(spriteFont, item.Key, new Vector2(item.Value.X + textBuffer, (item.Value.Y + item.Value.Height / 2 + 7 * textScale) - stringSize.Y * textScale / 2), Color.White, 0f, Vector2.Zero, textScale, 0, 0f);
				}

				selectCounter++;
			}
		}

		private void DrawHud(GameTime gametime)
		{
			// Draw divider
			for (int i = 5 + dividerTexture.Height; i + dividerTexture.Height < Window.ClientBounds.Height;)
			{
				SpriteBatch.Draw(dividerTexture, new Vector2(Window.ClientBounds.Width / 2 - 1, i), Color.White);
				i += dividerTexture.Height * 2;
			}

			// Draw player scores
			var stringSize = spriteFont.MeasureString(PlayerOne.score.ToString()) * titleTextScale;
			SpriteBatch.DrawString(spriteFont, PlayerOne.score.ToString(), new Vector2(Window.ClientBounds.Width / 4 - stringSize.X / 2, stringSize.Y / 4), Color.LightBlue, 0f, Vector2.Zero, titleTextScale, SpriteEffects.None, 0f);

			stringSize = spriteFont.MeasureString(PlayerTwo.score.ToString()) * titleTextScale;
			SpriteBatch.DrawString(spriteFont, PlayerTwo.score.ToString(), new Vector2(Window.ClientBounds.Width / 4 * 3 - stringSize.X / 2, stringSize.Y / 4), Color.Tomato, 0f, Vector2.Zero, titleTextScale, SpriteEffects.None, 0f);
		}
		
		private void OpenMenu()
		{
			gamePaused = true;
			inGameMenu = true;
		}

		private void CloseMenu()
		{
			inGameMenu = false;
			gamePaused = false;
		}
		
		private void HandleInput()
		{
			_previousMouseState = _currentMouseState;
			_currentMouseState = Mouse.GetState();
			_previousKeyboardState = _currentKeyboardState;
			_currentKeyboardState = Keyboard.GetState();
		}
	}
}

