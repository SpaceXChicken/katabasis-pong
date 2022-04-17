#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Katabasis;
using Katabasis.ImGui;

namespace Pong
{
	public class App : Game
	{
		// Declare variables
		SpriteBatch SpriteBatch;
		ImGuiRenderer ImGuiRenderer;
		public enum GameState
		{
			StartUpSequence = 0,
			Title = 1,
			Game = 2
		}
		public static GameState State;
		
		// Menu
		private double introTimer = 0f;
		private float introTextScale = 10f;
		private bool startIntro = false;
		private Vector2 WindowCentre;
		private Vector2 IntroText;
		private Rectangle centredRectTexture;
		private enum SelectScreen
		{
			None = 0,
			First = 1,
			Second = 2,
		}
		private SelectScreen selected = SelectScreen.None;
		
		// Game
		private bool twoPlayer = false;
		private bool disableMovement = false;
		private bool gamePaused = false;
		private bool inGameMenu = false;
		private bool gameOver = false;

		// AI
		private float aiNextPosY;
		private bool moveComplete;
		private Random random = new Random();
		private int randomOffset;

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
				return new Vector2(0, curSpeed);
			}
			public Rectangle Bounds()
			{
				return new Rectangle(X, Y, paddleWidth, paddleHeight);
			}
			public void Update(GameTime gameTime, GameWindow Window)
			{
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
				position += velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
			}
			// TODO: a priori calculations
		}
		private Ball ball = new Ball();
		private float collisionTimer = 0f;
		private float spawnTimer = 0f;
		private Tuple<Vector2, float>? nextPredictedPositionData;
		private Tuple<Vector2, float>? finalPredictedPositionData;
		private Vector2? positionData;
		private Vector2? velocityData;
		private int? verticalCollision;
		private bool isSpawned = false;

		// Input 
		private MouseState _currentMouseState;
		private MouseState _previousMouseState;
		private KeyboardState _currentKeyboardState;
		private KeyboardState _previousKeyboardState;
		
		// Events 
		private delegate void CollisionEvent(Rectangle body, Rectangle ball);
		private CollisionEvent OnHit;
		
		// Textures
		private Texture2D paddleTexture;
		private Texture2D ballTexture;
		private Texture2D dividerTexture;
		private Texture2D rectTexture;
		SpriteFont spriteFont;
		SpriteFont orangeFont;
		
		// Audio
		private Song OnHitSong;
		private Song OnScore;
		
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
			
			// Events
			// Window.ClientSizeChanged += (object? obj, EventArgs args) =>
			{
				WindowCentre = new Vector2(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);
				centredRectTexture.X = (int)WindowCentre.X - centredRectTexture.Width / 2;
				centredRectTexture.Y = (int)WindowCentre.Y - centredRectTexture.Height / 2;
			};
			// OnHit += BounceBall;
			// OnHit += OnHitAudio;

			PlayerOne = new Player(Window.ClientBounds.Width / 8, Window.ClientBounds.Height / 2 - paddleHeight / 2);
			PlayerTwo = new Player(Window.ClientBounds.Width * 7 / 8 - paddleWidth, Window.ClientBounds.Height / 2 - paddleHeight / 2);
			// Because of our eventual superior pre calculations the width of the border doesnt really matter as the ball won't clip through
			topBorder = new Border(0, 0, Window.ClientBounds.Width, 1);
			bottomBorder = new Border(0, Window.ClientBounds.Height - 1, Window.ClientBounds.Width, 1);
			leftBorder = new Border(0, 0, 1, Window.ClientBounds.Height);
			rightBorder = new Border(Window.ClientBounds.Width - 1, 0, 1, Window.ClientBounds.Height);
			ball.position = WindowCentre;
			ball.velocity = new Vector2(100, -100);
			base.Initialize();
		}

		protected override void LoadContent()
		{
			LoadSpriteFonts();
			LoadTextures();
			
			// Texture Related Variables
			centredRectTexture = new Rectangle((int)WindowCentre.X - rectTexture.Width * 5 / 2, (int)WindowCentre.Y - rectTexture.Height * 5 / 2,
			rectTexture.Width * 5, rectTexture.Height * 5);		
			IntroText = -spriteFont.MeasureString("Pong") * introTextScale / 2;
		}
		
		#region ContentHelpers
		private void LoadSpriteFonts()
		{
			spriteFont = Utility.SpriteFontReader.FromFile("Assets/Alfabeto.png",
				new Dictionary<char, Vector2>() { { 'm', new Vector2(7, 7) }, { 'w', new Vector2(7, 7) }, { 'q', new Vector2(6, 7) } },
				new Dictionary<char, Vector2>() { { '!', new Vector2(3, 7) }, { '?', new Vector2(5, 7) }, { ' ', new Vector2(5, 7) } },
				new Vector2(5, 7), new Vector2(1, 1), new Vector2(5, 14));
			orangeFont = Utility.SpriteFontReader.FromFile("Assets/Alfabeto0.png",
				new Dictionary<char, Vector2>() { { 'm', new Vector2(7, 7) }, { 'w', new Vector2(7, 7) }, { 'q', new Vector2(6, 7) } },
				new Dictionary<char, Vector2>() { { '!', new Vector2(3, 7) }, { '?', new Vector2(5, 7) }, { ' ', new Vector2(5, 7) } },
				new Vector2(5, 7), new Vector2(1, 1), new Vector2(5, 14));			
		}
		private void LoadTextures()
		{
			paddleTexture = Texture2D.FromFile("Assets/paddle.png");
			ballTexture = Texture2D.FromFile("Assets/ball.png");
			rectTexture = Texture2D.FromFile("Assets/rectangle.png");
			dividerTexture = Texture2D.FromFile("Assets/divider.png");
		}
		#endregion
		
		protected override void Update(GameTime gameTime)
		{
			HandleInput();
			
			if (State == GameState.Game)
			{ UpdateGame(gameTime); }
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);
			
			SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.Default, RasterizerState.CullNone);
			
			if (State == GameState.StartUpSequence)
			{ DrawIntro(gameTime); }
			else if (State == GameState.Title)
			{ DrawTitle(gameTime); }
			else if (State == GameState.Game)
			{ DrawGame(gameTime); }
			
			SpriteBatch.End();
		}

		private void BounceBall(Vector2 colliderVelocity, bool verticalCollision = false)
		{
			bool toPlayerTwo = Math.Sign(ball.velocity.X) == 1; // Is this more readable?
			// We'll add the players' velocity to the ball to make it more exciting.
			if (verticalCollision)
			{
				ball.velocity = new Vector2(ball.velocity.X, -ball.velocity.Y) + colliderVelocity;
			}
			else if ( (toPlayerTwo && InRange(ball.position.Y, PlayerTwo.Y, PlayerTwo.Y + paddleHeight)) || (!toPlayerTwo && InRange(ball.position.Y, PlayerOne.Y, PlayerOne.Y + paddleHeight)) )
			{
				ball.velocity = new Vector2(-ball.velocity.X, ball.velocity.Y) + colliderVelocity;
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
				UpdateScore(gameTime);
			}
			UpdateMenu();
		}
		private void UpdateMenu()
		{
			if (_previousKeyboardState.IsKeyDown(Keys.Escape) && _currentKeyboardState.IsKeyUp(Keys.Escape)) 
			{
				if (inGameMenu) { CloseMenu(); } else { OpenMenu(); }
			}

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
					ball.position = WindowCentre;
				}
				if (_currentKeyboardState.IsKeyDown(Keys.W))
				{
					if (PlayerOne.curSpeed > 0)
					{
						PlayerOne.curSpeed = 0;
					}
					PlayerOne.curSpeed -= accel * (float)gameTime.ElapsedGameTime.TotalSeconds;
				}
				else if (_currentKeyboardState.IsKeyDown(Keys.S))
				{
					if (PlayerOne.curSpeed < 0)
					{
						PlayerOne.curSpeed = 0;
					}
					PlayerOne.curSpeed += accel * (float)gameTime.ElapsedGameTime.TotalSeconds;
				}
				else
				{
					PlayerOne.curSpeed = 0;
				}
				if (twoPlayer)
				{
					if (_currentKeyboardState.IsKeyDown(Keys.Up))
					{
						if (PlayerTwo.curSpeed > 0)
						{
							PlayerTwo.curSpeed = 0;
						}
						PlayerTwo.curSpeed -= accel * (float)gameTime.ElapsedGameTime.TotalSeconds;
					}
					else if (_currentKeyboardState.IsKeyDown(Keys.Down))
					{
						if (PlayerTwo.curSpeed < 0)
						{
							PlayerTwo.curSpeed = 0;
						}
						PlayerTwo.curSpeed += accel * (float)gameTime.ElapsedGameTime.TotalSeconds;
					}
					else
					{
						PlayerTwo.curSpeed = 0;
					}
				}
				else
				{
					AIMove(gameTime, aiNextPosY);
				}
			}
		}

		private void AIMove(GameTime gameTime, float y)
		{
			// Yes, its not DRY.
			var aiPos = PlayerTwo.Y;
			y -= paddleHeight / 2;

			if (InRange(aiNextPosY, y + randomOffset - 8, y + randomOffset + 8) || InRange(aiNextPosY, y - randomOffset - 8, y - randomOffset + 8))
			{
				PlayerTwo.curSpeed = 0;
			}
			else if (aiPos < y)
			{
				if (PlayerTwo.curSpeed < 0)
				{
					PlayerTwo.curSpeed = 0;
				}
				PlayerTwo.curSpeed += accel * (float)gameTime.ElapsedGameTime.TotalSeconds;
			}
			else if (aiPos > y)
			{
				if (PlayerTwo.curSpeed > 0)
				{
					PlayerTwo.curSpeed = 0;
				}
				PlayerTwo.curSpeed -= accel * (float)gameTime.ElapsedGameTime.TotalSeconds;
			}
		}
		private void UpdateCollisions(GameTime gameTime)
		{
			HandleBallCollision(gameTime);
		}

		private void HandleBallCollision(GameTime gameTime)
		{
			nextPredictedPositionData ??= PredictBallPositionOnPlayerAxis();
			verticalCollision ??= 0;
			if (nextPredictedPositionData.Item1.Y < 0 || nextPredictedPositionData.Item1.Y > Window.ClientBounds.Height)
			{
				nextPredictedPositionData = PredictBallPositionOnBorderAxis();
				verticalCollision = 1;
			}
			collisionTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
			if(collisionTimer > nextPredictedPositionData.Item2)
			{
				if (ball.position.X < 0 || ball.position.X > Window.ClientBounds.Width)
				{
					ball.position = WindowCentre;
				}
				else if(nextPredictedPositionData.Item2 > 0) // Basically so it doesnt bounce after it has passed the paddle
				{
					BounceBall(verticalCollision == 1 ? Vector2.Zero : ball.velocity.X > 0 ? PlayerTwo.Velocity() : PlayerOne.Velocity(), verticalCollision == 1);
					if (ball.velocity.X > 0)
					{
						aiNextPosY = PredictFinalBallPositionOnPlayerAxis().Item1.Y;
						Console.WriteLine("Updated");
					}
					else
					{
						randomOffset = random.Next(32);
						Console.WriteLine(randomOffset);
					}
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

			if (InRange(borderPos.Item1.X, 0, Window.ClientBounds.Width))
			{
				finalPredictedPositionData ??= borderPos;
				positionData ??= position;
				velocityData ??= velocity;
			}
			else
			{
				return playerPos;
			}
			while (true)
			{
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
			ball.position = WindowCentre;
		}
		private void GameOver(GameTime gameTime)
		{
			disableMovement = true;
			introTimer += gameTime.ElapsedGameTime.TotalSeconds;
			if ((int)introTimer % 2 == 0)
			{
				SpriteBatch.DrawString(orangeFont, "Game Over", WindowCentre - new Vector2(orangeFont.MeasureString("Game Over").X, -orangeFont.MeasureString("Game Over").Y) * introTextScale / 2, Color.White, 0f, Vector2.Zero, introTextScale, SpriteEffects.None, 0f);
			}
			if (_previousKeyboardState.IsKeyDown(Keys.Space) && _currentKeyboardState.IsKeyUp(Keys.Space))
			{
				Exit();
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
			SpriteBatch.Draw(ballTexture, ball.position, Color.White);
			SpriteBatch.Draw(ballTexture, PredictBallPositionOnBorderAxis().Item1, Color.Red);
			SpriteBatch.Draw(ballTexture, PredictBallPositionOnPlayerAxis().Item1, Color.Blue);

			SpriteBatch.Draw(ballTexture, PredictFinalBallPositionOnPlayerAxis().Item1, Color.Purple);

			if (PlayerOne.score >= 15 || PlayerTwo.score >= 15)
			{
				GameOver(gameTime);
			}
			if (inGameMenu) { DrawInGameMenu(gameTime); }
		}
		private void DrawInGameMenu(GameTime gameTime)
		{
			// TODO: Height size of capitals.
			var text1 = "Restart Game";
			var text2 = "Quit to Title";
			var text3 = "Quit to Desktop";

			// Centre text2
			var textRect = centredRectTexture;
			SpriteBatch.Draw(rectTexture, textRect, selected.Equals(SelectScreen.First) ? Color.BurlyWood : Color.Wheat);
			var stringSize = spriteFont.MeasureString(text2);
			var scale = (centredRectTexture.Width - 20) / stringSize.X;
			SpriteBatch.DrawString(selected.Equals(SelectScreen.First) ? orangeFont : spriteFont, text2,
				new Vector2(textRect.X + 10, textRect.Y + textRect.Height / 2),
				Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			
			// Add text1 on top
			textRect = centredRectTexture;
			textRect.Y -= (int)(centredRectTexture.Height * 1.5);
			SpriteBatch.Draw(rectTexture, textRect, selected.Equals(SelectScreen.None) ? Color.BurlyWood : Color.Wheat);
			stringSize = spriteFont.MeasureString(text1);
			scale = (centredRectTexture.Width - 20) / stringSize.X;
			SpriteBatch.DrawString(selected.Equals(SelectScreen.None) ? orangeFont : spriteFont, text1,
				new Vector2(textRect.X + 10, textRect.Y + textRect.Height / 2),
				Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

			// Add text3 below
			textRect = centredRectTexture;
			textRect.Y += (int)(centredRectTexture.Height * 1.5);
			SpriteBatch.Draw(rectTexture, textRect, selected.Equals(SelectScreen.Second) ? Color.BurlyWood : Color.Wheat);
			stringSize = spriteFont.MeasureString(text3);
			scale = (centredRectTexture.Width - 20) / stringSize.X;
			SpriteBatch.DrawString(selected.Equals(SelectScreen.Second) ? orangeFont : spriteFont, text3,
				new Vector2(textRect.X + 10, textRect.Y + textRect.Height / 2),
				Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

			if (_previousKeyboardState.IsKeyDown(Keys.Up) && _currentKeyboardState.IsKeyUp(Keys.Up))
			{
				switch (selected)
				{
					case SelectScreen.None:
						selected = SelectScreen.Second;
						break;
					case SelectScreen.First:
						selected = SelectScreen.None;
						break;
					case SelectScreen.Second:
						selected = SelectScreen.First;
						break;
				};
			}
			else if (_previousKeyboardState.IsKeyDown(Keys.Down) && _currentKeyboardState.IsKeyUp(Keys.Down))
			{
				switch (selected)
				{
					case SelectScreen.None:
						selected = SelectScreen.First;
						break;
					case SelectScreen.First:
						selected = SelectScreen.Second;
						break;
					case SelectScreen.Second:
						selected = SelectScreen.None;
						break;
				};
			}
			if (_previousKeyboardState.IsKeyDown(Keys.Space) && _currentKeyboardState.IsKeyUp(Keys.Space))
			{
				switch (selected)
				{
					case SelectScreen.None:
						ResetGame();
						CloseMenu();
						break;
					case SelectScreen.First:
						selected = SelectScreen.None;
						IntroText = -spriteFont.MeasureString("Pong") * introTextScale / 2;
						startIntro = true;
						CloseMenu();
						break;
					case SelectScreen.Second:
						selected = SelectScreen.None;
						Exit();
						break;
				};
			}

			if(startIntro)
			{
				if (DrawTitleTransition(gameTime, true)) { State = GameState.Title; startIntro = false; };
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
			var stringSize = spriteFont.MeasureString(PlayerOne.score.ToString()) * introTextScale;
			SpriteBatch.DrawString(spriteFont, PlayerOne.score.ToString(), new Vector2(Window.ClientBounds.Width / 4 - stringSize.X / 2, stringSize.Y / 4), Color.LightBlue, 0f, Vector2.Zero, introTextScale, SpriteEffects.None, 0f);

			stringSize = spriteFont.MeasureString(PlayerTwo.score.ToString()) * introTextScale;
			SpriteBatch.DrawString(spriteFont, PlayerTwo.score.ToString(), new Vector2(Window.ClientBounds.Width / 4 * 3 - stringSize.X / 2, stringSize.Y / 4), Color.Tomato, 0f, Vector2.Zero, introTextScale, SpriteEffects.None, 0f);
		}
		private void DrawTitle(GameTime gameTime)
		{
			if (_previousKeyboardState.IsKeyDown(Keys.Up) && _currentKeyboardState.IsKeyUp(Keys.Up) || _previousKeyboardState.IsKeyDown(Keys.Down) && _currentKeyboardState.IsKeyUp(Keys.Down))
			{
				switch (selected)
				{
					case SelectScreen.None:
						selected = SelectScreen.First;
						break;
					case SelectScreen.First:
						selected = SelectScreen.Second;
						break;
					case SelectScreen.Second:
						selected = SelectScreen.First;
						break;
				};
			}
			if (_previousKeyboardState.IsKeyDown(Keys.Space) && _currentKeyboardState.IsKeyUp(Keys.Space))
			{
				switch (selected)
				{
					case SelectScreen.None:
						break;
					case SelectScreen.First:
						selected = SelectScreen.None;
						twoPlayer = false;
						startIntro = true;
						break;
					case SelectScreen.Second:
						selected = SelectScreen.None;
						twoPlayer = true;
						startIntro = true;
						break;
				};
			}

			if (!startIntro)
			{
				IntroText = -spriteFont.MeasureString("Pong") * introTextScale / 2;
				IntroText.Y = -WindowCentre.Y / 1.1f + 64;
				SpriteBatch.DrawString(spriteFont, "Pong", IntroText + WindowCentre, Color.White, 0f, Vector2.Zero, introTextScale, SpriteEffects.None, 0f);

				SpriteBatch.Draw(rectTexture, centredRectTexture, selected.Equals(SelectScreen.First) ? Color.BurlyWood : Color.Wheat);
				// Fit the text to the box.
				var scale = (centredRectTexture.Width - 20) / spriteFont.MeasureString("Player vs AI").X;
				var stringSize = spriteFont.MeasureString("Player vs AI");
				// The reason for the '+ 7 * scale' is because the lower text sprites are drawn down from the corner of the position coordinates, doing this makes the capital letters centred
				SpriteBatch.DrawString(selected.Equals(SelectScreen.First) ? orangeFont : spriteFont, "Player vs AI",
					new Vector2(centredRectTexture.X + centredRectTexture.Width / 2 - stringSize.X * scale / 2, centredRectTexture.Y + centredRectTexture.Height / 2 + 7 * scale - stringSize.Y * scale / 2),
					Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

				SpriteBatch.Draw(rectTexture, new Rectangle(centredRectTexture.X, centredRectTexture.Y + 100, centredRectTexture.Width, centredRectTexture.Height), selected.Equals(SelectScreen.Second) ? Color.BurlyWood : Color.Wheat);
				scale = (centredRectTexture.Width - 20) / spriteFont.MeasureString("Player vs Player").X;
				stringSize = spriteFont.MeasureString("Player vs Player");
				SpriteBatch.DrawString(selected.Equals(SelectScreen.Second) ? orangeFont : spriteFont, "Player vs Player",
					new Vector2(centredRectTexture.X + centredRectTexture.Width / 2 - stringSize.X * scale / 2, centredRectTexture.Y + 100 + centredRectTexture.Height / 2 + 7 * scale - stringSize.Y * scale / 2),
					Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			}
			else
			{
				if (DrawTitleTransition(gameTime, false)) { State = GameState.Game; startIntro = false; };
			}
		}
		private bool DrawTitleTransition(GameTime gameTime, bool inverse)
		{
			IntroText.Y = -WindowCentre.Y / 1.1f + 64;
			if (!inverse)
			{
				IntroText.X = Utility.Tween.Instance.TweenTo(IntroText.X, -spriteFont.MeasureString("Pong").X * introTextScale / 2, -Window.ClientBounds.Right, 1, gameTime);
			}
			else
			{
				IntroText.X = Utility.Tween.Instance.TweenTo(IntroText.X, -Window.ClientBounds.Right, - spriteFont.MeasureString("Pong").X * introTextScale / 2, 1, gameTime);
			}
			SpriteBatch.DrawString(spriteFont, "Pong", IntroText + WindowCentre, Color.White, 0f, Vector2.Zero, introTextScale, SpriteEffects.None, 0f);

			return IntroText.X == (-spriteFont.MeasureString("Pong") * introTextScale / 2).X;
		}
		private void DrawIntro(GameTime gameTime)
		{
			// Draw the intro
			introTimer += gameTime.ElapsedGameTime.TotalSeconds;


			if (_previousKeyboardState.IsKeyDown(Keys.Space) && _currentKeyboardState.IsKeyUp(Keys.Space))
			{
				startIntro = true;
			}
			if (startIntro)
			{
				IntroText.Y = (int)Utility.Tween.Instance.TweenTo(IntroText.Y, -spriteFont.MeasureString("Pong").Y * introTextScale / 2, (int)(-WindowCentre.Y / 1.1f + 64), 0.5f, gameTime);
				if (IntroText.Y == (int)(-WindowCentre.Y / 1.1f + 64))
				{ State = GameState.Title; startIntro = false; }
			}
			else if ((int)(introTimer) % 2 == 0)
			{
				SpriteBatch.DrawString(orangeFont, "Press spacebar to Start...", new Vector2(WindowCentre.X - orangeFont.MeasureString("Press spacebar to Start...").X * 3f / 2, WindowCentre.Y + 175), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);
				IntroText = -spriteFont.MeasureString("Pong") * introTextScale / 2;
			}
			SpriteBatch.DrawString(spriteFont, "Pong", IntroText + WindowCentre, Color.White, 0f, Vector2.Zero, introTextScale, SpriteEffects.None, 0f);
		}


		private void UpdateScore(GameTime gameTime)
		{

		}

		private void ResetGame()
		{
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

