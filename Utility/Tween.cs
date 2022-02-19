using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Katabasis;

namespace Pong.Utility
{
	internal sealed class Tween
	{
		private static Tween instance = null!;
		private static readonly object padlock = new object();

		Tween()
		{
		}
		public static Tween Instance
		{
			get
			{
				lock (padlock)
				{
					if (instance == null)
					{
						instance = new Tween();
					}
					return instance;
				}
			}
		}
		public float TweenTo(float item, float source, float destination, float seconds, GameTime gameTime)
		{
			if (destination > source) 
			{
				var diff = destination - source;

				while (item < destination)
				{
					item += diff / seconds * (float)gameTime.ElapsedGameTime.TotalSeconds;
					return item;
				}
			}
			else
			{
				var diff = source - destination;

				while (item > destination)
				{
					item -= diff / seconds * (float)gameTime.ElapsedGameTime.TotalSeconds;
					return item;
				}
			}
			return destination;
		}
	}
}
