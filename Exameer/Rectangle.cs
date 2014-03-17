using System;

namespace Exameer
{
	public class Rectangle
	{
		public int x1 { get; set; }

		public int y1 { get; set; }

		public int x2 { get; set; }

		public int y2 { get; set; }

		public int Width {
			get {return x2-x1;}
			set { x2 = x1+value;}
		}

		public int Height {
			get {return y2-y1;}
			set { y2 = y1+value;}
		}

		private static int IDS { get; set; }

		public int ID {get;set;}

		public Rectangle Parent { get; set; }

		public Rectangle (int id, Cairo.Rectangle rectangle) : this(id, 
		                                                            (int)rectangle.X,
		                                                    		(int)rectangle.Y,
		                                                    		(int)(rectangle.X+rectangle.Width),
		                                                    		(int)(rectangle.Y+rectangle.Height))
		{
		}

		public Rectangle (int id, int x1, int y1, int x2, int y2)
		{
			//this.ID = IDS++;
			this.ID = id;
			this.x1 = x1;
			this.y1 = y1;
			this.x2 = x2;
			this.y2 = y2;
		}

		public bool IsInside(int x, int y) {
			return (x1 < x && x < x2) && (y1 < y && y < y2);
		}

		public Cairo.Rectangle Convert() {
			return new Cairo.Rectangle(x1,y1,x2-x1,y2-y1);
		}
	}
}

