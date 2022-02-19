using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Katabasis;
#region
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
#endregion

namespace Pong.Utility
{
	public class SpriteFontReader
	{
		public static T CreateInstance<T>(params object[] args)
		{
			var type = typeof(T);
			var instance = type.Assembly.CreateInstance(
				type.FullName, false,
				BindingFlags.Instance | BindingFlags.NonPublic,
				null, args, null, null);
			return (T)instance;
		}
		public static SpriteFont FromFile(string filePath, Dictionary<char, Vector2> customChars, Dictionary<char, Vector2> extraChars, Vector2 glyphBounds, Vector2 buffers, Vector2 spacing)
	
		{
			var chars = new List<char>();
			var underChars = new List<char>() { 'g', 'j', 'y' };
			var GlyphBounds = glyphBounds;
			var glyph = new List<Rectangle>();
			var crop = new List<Rectangle>();
			var vect = new List<Vector3>();

			var lowerCase = Enumerable.Range(97, 26);
			var numbers = Enumerable.Range(49, 9); // 0 is (char)48

			var nextPos = buffers;

			Func<Vector2, Vector2, Rectangle> glyphRect = (nPos, GBounds) => new Rectangle((int)nPos.X, (int)nPos.Y, (int)GBounds.X, (int)GBounds.Y);
			Func<Vector2, Vector2, Vector3> vectRect = (gBounds, GBounds) => new Vector3(0, GBounds.X, -gBounds.X);
			Func<Vector2, Vector2, Rectangle> cropRect = (vec, bounds) => new Rectangle((int)vec.X, (int)vec.Y,  (int)bounds.X, (int)bounds.Y);

			#region Homemade
			chars.Add('.');
			crop.Add(cropRect(new Vector2(0, glyphBounds.Y - 1), Vector2.One));
			vect.Add(vectRect(glyphBounds, Vector2.One));
			glyph.Add(glyphRect(nextPos + Vector2.One, Vector2.One));
			chars.Add(',');
			crop.Add(cropRect(new Vector2(0, glyphBounds.Y - 1), Vector2.One + Vector2.UnitY));
			vect.Add(vectRect(glyphBounds, Vector2.One + Vector2.UnitY));
			glyph.Add(glyphRect(nextPos + Vector2.One, Vector2.One + Vector2.UnitY));
			#endregion

			foreach (var i in lowerCase)
			{
				if (underChars.Contains((char)i))
				{
					chars.Add((char)i);
					crop.Add(cropRect(Vector2.Zero, GlyphBounds));
					vect.Add(vectRect(glyphBounds, GlyphBounds));
					glyph.Add(glyphRect(nextPos, GlyphBounds));

					//Capitalise
					chars.Add((char)(i - 32));
					crop.Add(cropRect(new Vector2(0, -GlyphBounds.Y), GlyphBounds * 2));
					vect.Add(vectRect(glyphBounds, GlyphBounds * 2));
					glyph.Add(glyphRect(nextPos, GlyphBounds));
				}
				else if (customChars.ContainsKey((char)i)) 
				{
					chars.Add((char)i);
					GlyphBounds = customChars[(char)i];
					crop.Add(cropRect(Vector2.Zero, GlyphBounds));
					vect.Add(vectRect(glyphBounds, GlyphBounds));
					glyph.Add(glyphRect(nextPos, GlyphBounds));

					chars.Add((char)(i - 32));
					crop.Add(cropRect(new Vector2(0, -GlyphBounds.Y), GlyphBounds * 2));
					vect.Add(vectRect(glyphBounds, GlyphBounds * 2));
					glyph.Add(glyphRect(nextPos, GlyphBounds));
				}
				else
				{
					chars.Add((char)i);
					GlyphBounds = glyphBounds;
					crop.Add(cropRect(Vector2.Zero, GlyphBounds));
					vect.Add(vectRect(glyphBounds, GlyphBounds));
					glyph.Add(glyphRect(nextPos, GlyphBounds));

					chars.Add((char)(i - 32));
					crop.Add(cropRect(new Vector2(0, -GlyphBounds.Y), GlyphBounds * 2));
					vect.Add(vectRect(glyphBounds, GlyphBounds * 2));
					glyph.Add(glyphRect(nextPos, GlyphBounds));
				}

				nextPos.X += GlyphBounds.X + buffers.X;
				GlyphBounds = glyphBounds;

			};

			foreach (var i in numbers)
			{
				chars.Add((char)i);
				GlyphBounds = glyphBounds;

				crop.Add(cropRect(Vector2.Zero, glyphBounds));
				vect.Add(vectRect(glyphBounds, GlyphBounds));
				glyph.Add(glyphRect(nextPos, GlyphBounds));
				nextPos.X += GlyphBounds.X + buffers.X;
			};

			// Special Zero
			chars.Add((char)48);
			GlyphBounds = glyphBounds;

			crop.Add(cropRect(Vector2.Zero, glyphBounds));
			vect.Add(vectRect(glyphBounds, GlyphBounds));
			glyph.Add(glyphRect(nextPos, GlyphBounds));
			nextPos.X += GlyphBounds.X + buffers.X;

			foreach (var i in extraChars)
			{
				chars.Add(i.Key);
				GlyphBounds = i.Value;

				crop.Add(cropRect(Vector2.Zero, GlyphBounds));
				vect.Add(vectRect(glyphBounds, GlyphBounds));
				glyph.Add(glyphRect(nextPos, GlyphBounds));
				nextPos.X += GlyphBounds.X + buffers.X;
			};
			//var spriteFont = ;
			//var spriteFont = new SpriteFont(Texture2D.FromFile(filePath), glyph, crop, chars, (int)spacing.Y, spacing.X, vect, ' ');
			return CreateInstance<SpriteFont>(Texture2D.FromFile(filePath), glyph, crop, chars, (int)spacing.Y, spacing.X, vect, ' ');
		}

	}
}
