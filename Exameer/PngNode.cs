using System;
using System.Collections.Generic;

namespace Exameer
{
	[Gtk.TreeNode (ListOnly=true)]
	public class PngNode : Gtk.TreeNode
	{
		[Gtk.TreeNodeValue (Column=0)]
		public string Name {get;set;}

		public string FileName {get;set;}

		public List<Rectangle> Rectangles {get;set;}

		public PngNode (string path)
		{
			this.FileName = path;

			Rectangles = new List<Rectangle>();

			var tmp = path.Split(new Char[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
			this.Name = tmp[tmp.Length - 1];
		}
	}
}

