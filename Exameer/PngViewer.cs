using System;
using Gtk;
using Gdk;
using System.Collections.Generic;
using Cairo;

namespace Exameer
{
	public partial class PngViewer : Gtk.Window
	{
		private PngNode CurrentNode { get; set; }

		private Pixbuf PixelBuffer { get; set; }

		private Pixbuf ScaledPixelBuffer { get; set; }

		private bool toResize { get; set; }

		private EditMode CurrentMode { get; set; }

		private float LeftLine { get; set; }

		private float RightLine { get; set; }

		private Tuple<int, int> Size { get; set; }

		private Gdk.Color borderColor = new Gdk.Color ();

		private int OffsetX {get;set;}
		private int OffsetY {get;set;}

		enum EditMode
		{
			None,
			LeftLine,
			RightLine,
			Rectangle
		}

		public PngViewer () : base(Gtk.WindowType.Toplevel)
		{
			this.Build ();

			drawingarea1.ExposeEvent += new ExposeEventHandler (OnExpose);

			Gdk.Color.Parse ("black", ref borderColor);
			drawingarea1.ModifyFg (StateType.Normal, borderColor);

			LeftLine = (float)scaleLeftLine.Value;
			RightLine = (float)scaleLeftLine.Value;


			OffsetX = 10;
			OffsetY = 0;
		}

		public void SetNode (PngNode pn)
		{
			CurrentNode = pn;
			PixelBuffer = new Pixbuf (CurrentNode.FileName);
			lblFilename.Text = CurrentNode.FileName;

			int winWidth = 0;
			int winHeight = 0;
			drawingarea1.GdkWindow.GetSize (out winWidth, out winHeight);

			Size = ScaleBufferToDrawingArea (winWidth, winHeight);

			RenderImage ();
			this.QueueDraw ();
		}

		public void ResizeWindow (int width)
		{
			// Update window to new size, keep old height.
			int w = 0, h = 0;
			this.GetSize (out w, out h);
			this.SetDefaultSize (width, h);
		}

		public Tuple<int, int> ScaleBufferToDrawingArea (int width, int height)
		{
			var size = ScaleToHeight (height, PixelBuffer);
			ScaledPixelBuffer = PixelBuffer.ScaleSimple (size.Item1, size.Item2, InterpType.Bilinear);
			return size;
		}

		Tuple<int, int> ScaleToHeight (int baseHeight, Pixbuf pixbuf)
		{
			var width = pixbuf.Width;
			var height = pixbuf.Height;

			var ratio = baseHeight / (float)height;

			var newWidth = (int)(width * ratio);
			var newHeight = baseHeight;

			return new Tuple<int,int> (newWidth, newHeight);
		}

		void RenderImage ()
		{
			Console.WriteLine("Rendering...");
			Cairo.Context cr = Gdk.CairoHelper.Create (drawingarea1.GdkWindow);

			// Draw rectangle
			cr.SetSourceRGB (0.2, 0.23, 0.9);
			cr.LineWidth = 1;
			cr.Rectangle (0+ OffsetX, 0+ OffsetY, ScaledPixelBuffer.Width, ScaledPixelBuffer.Height);
			cr.Stroke();

			int x1 = 0 + OffsetX;
			int y1 = 0 + OffsetY;

			// Draw image
			drawingarea1.GdkWindow.DrawPixbuf (Style.ForegroundGC (StateType.Normal), 
			                                   ScaledPixelBuffer, 0, 0, x1, y1, 
			                                   ScaledPixelBuffer.Width, 
			                                   ScaledPixelBuffer.Height, RgbDither.Normal, 0, 0);
			// Draw lines
			cr.SetSourceRGB(0.1,0,0);
			cr.LineWidth = 2;
			cr.Rectangle(LeftLine+OffsetX, 0, 1, ScaledPixelBuffer.Height);
			cr.Rectangle(RightLine+OffsetX, 0, 1, ScaledPixelBuffer.Height);
			cr.Fill();


			// Draw selections


			((IDisposable)cr.Target).Dispose ();                                      
			((IDisposable)cr).Dispose ();

		}

		void OnExpose (object o, Gtk.ExposeEventArgs args)
		{
			if (PixelBuffer != null) {
				RenderImage ();
				drawingarea1.ShowAll ();
			}
		}
	
		void OnSizeAllocated (object sender, Gtk.SizeAllocatedArgs args)
		{
			if (CurrentNode != null) {
			
				int w = 0, h = 0;
				drawingarea1.GdkWindow.GetSize (out w, out h);
				Size = ScaleBufferToDrawingArea (w, h);

				this.QueueDraw ();
			}
		}

		protected void OnBtnPrevImgClicked (object sender, EventArgs e)
		{
			throw new System.NotImplementedException ();
		}

		protected void OnBtnResetRectanglesClicked (object sender, EventArgs e)
		{
			throw new System.NotImplementedException ();
		}

		protected void OnBtnNextImgClicked (object sender, EventArgs e)
		{
			throw new System.NotImplementedException ();
		}

		protected void OnEntryLeftLineEditingDone (object sender, EventArgs e)
		{
			throw new System.NotImplementedException ();
		}

		protected void OnEntryRightLineEditingDone (object sender, EventArgs e)
		{
			throw new System.NotImplementedException ();
		}

		private List<Gdk.Key> pressedKeys = new List<Gdk.Key> ();

		protected void OnKeyPressEvent (object sender, KeyPressEventArgs e)
		{
			pressedKeys.Add (e.Event.Key);
		}

		protected void OnKeyReleaseEvent (object sender, KeyReleaseEventArgs e)
		{
			if (pressedKeys.Contains (e.Event.Key)) {
				pressedKeys.Remove (e.Event.Key);
				Console.WriteLine (e.Event.Key.ToString ());
			}
		}

		protected void OnScaleLeftLineValueChanged (object sender, EventArgs e)
		{
			LeftLine = (float)(scaleLeftLine.Value / 100.0) * ScaledPixelBuffer.Width;
			this.QueueDraw();
		}

		protected void OnScaleRightLineValueChanged (object sender, EventArgs e)
		{
			RightLine = (float)(scaleRightLine.Value/ 100.0) * ScaledPixelBuffer.Width;
			this.QueueDraw();
		}




	}
}

