using System;

namespace Exameer
{
	public static class ExtensionMethods
	{
		public static bool IsInisde(this Rectangle rectangle, int x, int y) {

			return (rectangle.x1 < x && x < rectangle.x2) && 
				(rectangle.y1 < y && y < rectangle.y2);
		}
	}
}

