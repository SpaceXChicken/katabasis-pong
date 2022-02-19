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
		SpriteBatch SpriteBatch;
		ImGuiRenderer ImGuiRenderer;
		SpriteFont spriteFont;
		SpriteFont orangeFont;
		public enum GameState
		{
			StartUpSequence = 0,
			Title = 1,
			Game = 2
		}
		public static GameState State;

		private Vector2 WindowCentre;
		private Vector2 IntroText;
		private float introTextScale;
		private bool startIntro = false;
		private double introTimer;
		private Texture2D rectTexture;
		private Rectangle centredBounds;
		private Texture2D dividerTexture;
		private int playerOneScore;
		private int playerTwoScore;
		private Vector2 playerOnePos;
		private Vector2 playerTwoPos;
		private int playerOneSpeed;
		private int playerTwoSpeed;
		private int paddleHeight;
		private int paddleWidth;
		private Vector2 ballPos;
		private Vector2 ballAngle;
		private int ballSpeed;
		private Texture2D paddleTexture;
		private Texture2D ballTexture;
		private Rectangle bottomBorder;
		private Rectangle topBorder;
		private delegate void CollisionEvent(Rectangle body, Rectangle ball);
		private CollisionEvent OnHit;
		private bool toPlayerOne = false;
		private float spawnTimer;
		private bool startSpawn = false;
		private bool twoPlayer = false;
		private bool disableMovement = false;
		private enum SelectScreen
		{
			None = 0,
			First = 1,
			Second = 2,
		}
		private SelectScreen selected;

		private MouseState _currentMouseState;
		private MouseState _previousMouseState;
		private KeyboardState _currentKeyboardState;
		private KeyboardState _previousKeyboardState;

		public App()
		{
			Window.AllowUserResizing = true;
		}

		protected override void Initialize()
		{
			SpriteBatch = new SpriteBatch();
			ImGuiRenderer = new ImGuiRenderer();

			State = GameState.Game;
			playerOneScore = 0;
			playerTwoScore = 0;

			WindowCentre = new Vector2(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);
			introTextScale = 10f;
			introTimer = 0f;
			selected = SelectScreen.First;
			rectTexture = Texture2D.FromFile("Assets/rectangle.png");
			dividerTexture = Texture2D.FromFile("Assets/divider.png");
			centredBounds = new Rectangle((int)WindowCentre.X - rectTexture.Width * 5 / 2, (int)WindowCentre.Y - rectTexture.Height * 5 / 2, rectTexture.Width * 5, rectTexture.Height * 5);
			Window.ClientSizeChanged += (object? obj, EventArgs args) =>
			{
				WindowCentre = new Vector2(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);
				centredBounds.X = (int)WindowCentre.X - centredBounds.Width / 2;
				centredBounds.Y = (int)WindowCentre.Y - centredBounds.Height / 2;
				Console.WriteLine(spriteFont.MeasureString("Player vs AI"));
			};
			paddleWidth = 6;
			paddleHeight = 56;
			playerOnePos = new Vector2(Window.ClientBounds.Width / 8, Window.ClientBounds.Height / 2 - paddleHeight / 2);
			playerTwoPos = new Vector2(Window.ClientBounds.Width * 7 / 8 - paddleWidth, Window.ClientBounds.Height / 2 - paddleHeight / 2);
			playerOneSpeed = 400;
			playerTwoSpeed = 400;
			ballPos = WindowCentre;
			ballSpeed = 100;
			ballAngle = new Vector2(1, 0);
			topBorder = new Rectangle((int)playerOnePos.X + paddleWidth, - paddleWidth, (int)(playerTwoPos.X - playerOnePos.X - paddleWidth), paddleWidth);
			bottomBorder = new Rectangle((int)playerOnePos.X + paddleWidth, Window.ClientBounds.Height, (int)(playerTwoPos.X - playerOnePos.X - paddleWidth), paddleWidth);
			OnHit += BounceBall;
			spawnTimer = 0f;

			base.Initialize();
		}

		protected override void LoadContent()
		{
			spriteFont = Utility.SpriteFontReader.FromFile("Assets/Alfabeto.png",
				new Dictionary<char, Vector2>() { { 'm', new Vector2(7, 7) }, { 'w', new Vector2(7, 7) }, { 'q', new Vector2(6, 7) } },
				new Dictionary<char, Vector2>() { { '!', new Vector2(3, 7) }, { '?', new Vector2(5, 7) }, { ' ', new Vector2(5, 7) } },
				new Vector2(5, 7), new Vector2(1, 1), new Vector2(5, 14));
			orangeFont = Utility.SpriteFontReader.FromFile("Assets/Alfabeto0.png",
				new Dictionary<char, Vector2>() { { 'm', new Vector2(7, 7) }, { 'w', new Vector2(7, 7) }, { 'q', new Vector2(6, 7) } },
				new Dictionary<char, Vector2>() { { '!', new Vector2(3, 7) }, { '?', new Vector2(5, 7) }, { ' ', new Vector2(5, 7) } },
				new Vector2(5, 7), new Vector2(1, 1), new Vector2(5, 14));

			paddleTexture = Texture2D.FromFile("Assets/paddle.png");
			ballTexture = Texture2D.FromFile("Assets/ball.png");

			IntroText = -spriteFont.MeasureString("Pong") * introTextScale / 2;
		}
		protected override void Update(GameTime gameTime)
        {
			HandleInput();
			if (State == GameState.Game)
			{ UpdateGame(gameTime); }
		}

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

			//ImGuiRenderer.Begin(gameTime);
			//DrawUserInterface();
			//ImGuiRenderer.End();
			if (State == GameState.StartUpSequence)
			{ DrawIntro(gameTime); }
			else if (State == GameState.Title)
			{ DrawTitle(gameTime); }
			else if (State == GameState.Game)
			{ DrawGame(gameTime); }
		}

		private void BounceBall(Rectangle body, Rectangle ball)
		{
			var vector = PenetrationVector(body, ball, true);
			var greaterX = vector.X > vector.Y;
			if (body.Height < 10)
			{
				ballAngle = new Vector2(ballAngle.X, -ballAngle.Y);
			}
			else
			{
				if (greaterX) { vector.X = Math.Sign(vector.X); }
				if (vector.Y > 3) { vector.Y = -Math.Sign(vector.Y); } else { vector.Y *= -1; }
				ballSpeed += 2 * Math.Abs((int)ballAngle.X);
				ballAngle = vector;
				Console.WriteLine(ballSpeed);
			}
		}

		private void SpawnBall()
		{
			ballPos = WindowCentre; ballSpeed = 100;
			var newAngle = new Vector2((ballAngle.X / 2), (ballAngle.Y / 2));
			Console.WriteLine(newAngle.ToString());
			newAngle.X = (int)Math.Round(newAngle.X, MidpointRounding.AwayFromZero);
			newAngle.Y = (int)Math.Round(newAngle.Y, MidpointRounding.AwayFromZero);
			Console.WriteLine(newAngle.ToString());

			ballAngle = newAngle;
		} 
		private void UpdateGame(GameTime gameTime)
		{
			#region Bounds
			var ballBounds = new Rectangle((int)ballPos.X, (int)ballPos.Y, 4, 4);
			var playerOneBounds = new Rectangle((int)playerOnePos.X, (int)playerOnePos.Y, paddleWidth, paddleHeight);
			var playerTwoBounds = new Rectangle((int)playerTwoPos.X, (int)playerTwoPos.Y, paddleWidth, paddleHeight);
			#endregion
			#region BounceBall
			if (ballBounds.Intersects(playerOneBounds))
			{
				OnHit?.Invoke(playerOneBounds, ballBounds);
				toPlayerOne = false;
			}
			else if (ballBounds.Intersects(playerTwoBounds))
			{
				OnHit?.Invoke(playerTwoBounds, ballBounds);
				toPlayerOne = true;
			}
			else if (ballBounds.Intersects(topBorder))
			{
				OnHit?.Invoke(topBorder, ballBounds);
			}
			else if (ballBounds.Intersects(bottomBorder))
			{
				OnHit?.Invoke(bottomBorder, ballBounds);
			}
			if (toPlayerOne) { ballAngle.X = -Math.Abs(ballAngle.X); }
			else { ballAngle.X = Math.Abs(ballAngle.X); }
			if (!startSpawn) { ballPos += ballAngle * (float)gameTime.ElapsedGameTime.TotalSeconds * ballSpeed; }
			#endregion
			if (!disableMovement)
			{
				#region Player Movement
				if (_currentKeyboardState.IsKeyDown(Keys.W) && playerOnePos.Y > 0)
				{
					playerOnePos.Y -= playerOneSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
				}
				else if (playerOnePos.Y < 0)
				{
					playerOnePos.Y = 0;
				}
				if (_currentKeyboardState.IsKeyDown(Keys.S) && playerOnePos.Y < Window.ClientBounds.Height - paddleHeight)
				{
					playerOnePos.Y += playerOneSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
				}
				else if (playerOnePos.Y > Window.ClientBounds.Height - paddleHeight)
				{
					playerOnePos.Y = Window.ClientBounds.Height - paddleHeight;
				}
				if (twoPlayer)
				{
					if (_currentKeyboardState.IsKeyDown(Keys.Up) && playerTwoPos.Y > 0)
					{
						playerTwoPos.Y -= playerOneSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
					}
					else if (playerTwoPos.Y < 0)
					{
						playerTwoPos.Y = 0;
					}
					if (_currentKeyboardState.IsKeyDown(Keys.Down) && playerTwoPos.Y < Window.ClientBounds.Height - paddleHeight)
					{
						playerTwoPos.Y += playerTwoSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
					}
					else if (playerTwoPos.Y > Window.ClientBounds.Height - paddleHeight)
					{
						playerTwoPos.Y = Window.ClientBounds.Height - paddleHeight;
					}
				}
				else
				{
					if(ballPos.Y - paddleHeight / 2 > 0 && ballPos.Y + paddleHeight / 2 < Window.ClientBounds.Height)
					{
						playerTwoPos.Y = ballPos.Y - paddleHeight / 2;
					}
				}
				#endregion
			}
			#region Score
			var hidden = new Vector2(1, -20);
			if (!disableMovement)
			{
				if (_previousKeyboardState.IsKeyDown(Keys.R) && _currentKeyboardState.IsKeyUp(Keys.R))
				{
					startSpawn = true;
					ballPos = hidden;
				};
				if (ballPos.X < 0)
				{
					playerTwoScore += 1;
					startSpawn = true;
					ballPos = hidden;
				}
				else if (ballPos.X > Window.ClientBounds.Width)
				{
					playerOneScore += 1;
					startSpawn = true;
					ballPos = hidden;
				}
			}
			if (startSpawn)
			{
				spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
				if(spawnTimer > 1)
				{
					spawnTimer = 0;
					startSpawn = false;
					SpawnBall();
				}
			}
			#endregion
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
			SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
			DrawHud(gameTime);
			SpriteBatch.Draw(paddleTexture, playerOnePos, Color.White);
			SpriteBatch.Draw(paddleTexture, playerTwoPos, Color.White);
			SpriteBatch.Draw(rectTexture, bottomBorder, Color.Red);
			SpriteBatch.Draw(rectTexture, topBorder, Color.Red);
			SpriteBatch.Draw(ballTexture, ballPos, Color.White);
			if (playerOneScore >= 15 || playerTwoScore >= 15)
			{
				GameOver(gameTime);
			}
			SpriteBatch.End();
		}
		private void DrawHud(GameTime gametime)
		{
			if (_previousKeyboardState.IsKeyDown(Keys.Up) && _currentKeyboardState.IsKeyUp(Keys.Up))
			{
				//playerOneScore++;
				//playerTwoScore++;
			}
			else if (_previousKeyboardState.IsKeyDown(Keys.Down) && _currentKeyboardState.IsKeyUp(Keys.Down))
			{
				//playerOneScore--;
			}

			// Draw divider
			for (int i = 5 + dividerTexture.Height; i + dividerTexture.Height < Window.ClientBounds.Height;)
			{
				SpriteBatch.Draw(dividerTexture, new Vector2(Window.ClientBounds.Width / 2 - 1, i), Color.White);
				i += dividerTexture.Height * 2;
			}

			// Draw player scores
			var stringSize = spriteFont.MeasureString(playerOneScore.ToString()) * introTextScale;
			SpriteBatch.DrawString(spriteFont, playerOneScore.ToString(), new Vector2(Window.ClientBounds.Width / 4 - stringSize.X / 2, stringSize.Y / 4) , Color.LightBlue, 0f, Vector2.Zero, introTextScale, SpriteEffects.None, 0f);

			stringSize = spriteFont.MeasureString(playerTwoScore.ToString()) * introTextScale;
			SpriteBatch.DrawString(spriteFont, playerTwoScore.ToString(), new Vector2(Window.ClientBounds.Width / 4 * 3 - stringSize.X / 2, stringSize.Y / 4), Color.Tomato, 0f, Vector2.Zero, introTextScale, SpriteEffects.None, 0f);
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
				IntroText.Y = -WindowCentre.Y / 1.1f + 64; SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
				SpriteBatch.DrawString(spriteFont, "Pong", IntroText + WindowCentre, Color.White, 0f, Vector2.Zero, introTextScale, SpriteEffects.None, 0f);

				SpriteBatch.Draw(rectTexture, centredBounds, selected.Equals(SelectScreen.First) ? Color.BurlyWood : Color.Wheat);
				// Fit the text to the box.
				var scale = (centredBounds.Width - 20) / spriteFont.MeasureString("Player vs AI").X;
				var stringSize = spriteFont.MeasureString("Player vs AI");
				// The reason for the '+ 7 * scale' is because the lower text sprites are drawn down from the corner of the position coordinates, doing this makes the capital letters centred
				SpriteBatch.DrawString(selected.Equals(SelectScreen.First) ? orangeFont : spriteFont, "Player vs AI",
					new Vector2(centredBounds.X + centredBounds.Width / 2 - stringSize.X * scale / 2, centredBounds.Y + centredBounds.Height / 2 + 7 * scale - stringSize.Y * scale / 2),
					Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

				SpriteBatch.Draw(rectTexture, new Rectangle(centredBounds.X, centredBounds.Y + 100, centredBounds.Width, centredBounds.Height), selected.Equals(SelectScreen.Second) ? Color.BurlyWood : Color.Wheat);
				scale = (centredBounds.Width - 20) / spriteFont.MeasureString("Player vs Player").X;
				stringSize = spriteFont.MeasureString("Player vs Player");
				SpriteBatch.DrawString(selected.Equals(SelectScreen.Second) ? orangeFont : spriteFont, "Player vs Player",
					new Vector2(centredBounds.X + centredBounds.Width / 2 - stringSize.X * scale / 2, centredBounds.Y + 100 + centredBounds.Height / 2 + 7 * scale - stringSize.Y * scale / 2),
					Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
				SpriteBatch.End();
			}
			else
			{
				if (DrawTitleTransition(gameTime)) { State = GameState.Game; };
			}
		}
		private bool DrawTitleTransition(GameTime gameTime)
		{
			IntroText.Y = -WindowCentre.Y / 1.1f + 64;
			IntroText.X = Utility.Tween.Instance.TweenTo(IntroText.X, -spriteFont.MeasureString("Pong").X * introTextScale / 2, -Window.ClientBounds.Right, 1, gameTime);
			SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
			SpriteBatch.DrawString(spriteFont, "Pong", IntroText + WindowCentre, Color.White, 0f, Vector2.Zero, introTextScale, SpriteEffects.None, 0f);
			SpriteBatch.End();

			return IntroText.X == -Window.ClientBounds.Right;
		}
		private void DrawIntro(GameTime gameTime)
		{
			// Draw the intro
			introTimer += gameTime.ElapsedGameTime.TotalSeconds;

			SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);

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
			else if ((int)(introTimer)%2 == 0)
			{
				SpriteBatch.DrawString(orangeFont, "Press spacebar to Start...", new Vector2(WindowCentre.X - orangeFont.MeasureString("Press spacebar to Start...").X * 3f / 2, WindowCentre.Y + 175), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);
				IntroText = -spriteFont.MeasureString("Pong") * introTextScale / 2;
			}
			SpriteBatch.DrawString(spriteFont, "Pong", IntroText + WindowCentre, Color.White, 0f, Vector2.Zero, introTextScale, SpriteEffects.None, 0f);
			SpriteBatch.End();
		}
		private Vector2 PenetrationVector(Rectangle stationary, Rectangle collider, bool returnBoth = false, bool straightOnX = false)
		{
			Vector2 penetrationVector = Vector2.Zero;
			bool colliderLeft = collider.X < stationary.X;
			bool colliderUp = collider.Y < stationary.Y;

			var P4 = new Vector2(stationary.X + stationary.Width, stationary.Y + stationary.Height);
			var B4 = new Vector2(collider.X + collider.Width, collider.Y + collider.Height);

			if (collider.Intersects(stationary))
			{
				if (colliderLeft)
				{
					penetrationVector.X = stationary.X - B4.X;
					straightOnX = collider.Height < Math.Abs(P4.Y - collider.Y);
				}
				else
				{
					penetrationVector.X = P4.X - collider.X;
					straightOnX = collider.Height < Math.Abs(collider.Y - stationary.Y);
				}
				if (colliderUp)
				{
					penetrationVector.Y = stationary.Y - B4.Y;
				}
				else
				{
					penetrationVector.Y = P4.Y - collider.Y;
				}
			}

			bool returnX = penetrationVector.X < penetrationVector.Y;
			var vectorX = new Vector2(penetrationVector.X, 0);
			var vectorY = new Vector2(0, penetrationVector.Y);
			var angleX = new Vector2(penetrationVector.X, penetrationVector.X);
			var angleY = new Vector2(penetrationVector.Y, penetrationVector.Y);

			// if (returnBoth && straightOnX) { return vectorX; }
			return returnBoth ? penetrationVector : returnX ? vectorX : vectorY;
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
