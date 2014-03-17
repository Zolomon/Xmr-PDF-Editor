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

		private List<PngNode> Nodes { get; set; }

		private Pixbuf PixelBuffer { get; set; }

		private Pixbuf ScaledPixelBuffer { get; set; }

		private bool toResize { get; set; }

		private EditMode CurrentMode { get; set; }

		private float LeftLine { get; set; }

		private float RightLine { get; set; }

		private Tuple<int, int> Size { get; set; }

		private Gdk.Color borderColor = new Gdk.Color ();

		private int OffsetX { get; set; }

		private int OffsetY { get; set; }

		private Point<int> MousePos { get; set; }

		int CurrentID { get; set; }
		//public List<Cairo.Rectangle> selections = new List<Cairo.Rectangle> ();

		public Point<int> Topleft { get; set; }

		private int NodeIdx { get; set; }

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
			RightLine = (float)scaleRightLine.Value;


			OffsetX = 0;
			OffsetY = 0;

			drawingarea1.AddEvents ((int)(EventMask.ButtonPressMask 
				| EventMask.ButtonReleaseMask
				| EventMask.PointerMotionMask)
			);
			drawingarea1.ButtonPressEvent += new ButtonPressEventHandler (OnButtonPressEvent);
			drawingarea1.ButtonReleaseEvent += new ButtonReleaseEventHandler (OnButtonReleaseEvent);
			drawingarea1.MotionNotifyEvent += new MotionNotifyEventHandler (OnMotionNotifyEvent);

			CurrentID = 0;
		}

		public void OnButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			var x = (int)args.Event.X;
			var y = (int)args.Event.Y;

			if (args.Event.Button == 3) {
				// Remove selection if we click on it.
				int i = 0;
				bool toRemove = false;
				foreach (var selection in CurrentNode.Rectangles) {
					if (selection.IsInisde (x + OffsetX, y + OffsetY)) {
						toRemove = true;
						break;
					}
					i++;
				}

				if (toRemove)
					CurrentNode.Rectangles.RemoveAt (i);

			}


			// If LMB
			if (args.Event.Button == 1 && ScaledPixelBuffer != null) {

				// If we left-click on a rectangle, let's select it.
				bool toSelect = false;
				int selectedId = -1;
				foreach (var selection in CurrentNode.Rectangles) {
					if (selection.IsInside (x, y)) {
						toSelect = true;
						//selectedId = selection.ID;
						NewIdDialog nid = new NewIdDialog ();
						nid.Modal = true;
						ResponseType resp = (ResponseType)nid.Run ();
						if (resp == ResponseType.Ok) {
							selection.ID = nid.NewId ();
							nid.Destroy ();
						}
					}
				}

				// If we didn't hit a rectangle, then we can draw a new one!
				if (!toSelect) {

					// Set top left point
					var tmpRec = new Rectangle (0, 0 + OffsetX, 0 + OffsetY, 
					                               ScaledPixelBuffer.Width, ScaledPixelBuffer.Height);
					if (tmpRec.IsInside (x, y)) {
						Topleft = new Point<int> (x, y);

						Console.WriteLine ("Inside drawing area");
					}

				}

			}

			this.QueueDraw ();
		}

		public void OnMotionNotifyEvent (object sender, MotionNotifyEventArgs args)
		{
			//Console.WriteLine("On motion notify event {0}", args.ToString());
			var x = (int)args.Event.X;
			var y = (int)args.Event.Y;

			MousePos = new Point<int> (x, y);

			this.QueueDraw ();
		}

		public void OnButtonReleaseEvent (object sender, ButtonReleaseEventArgs args)
		{
			var x = (int)args.Event.X;
			var y = (int)args.Event.Y;

			// If LMB
			if (args.Event.Button == 1 && ScaledPixelBuffer != null) {

				var rectangle = new Rectangle (0, 0 + OffsetX, 0 + OffsetY, ScaledPixelBuffer.Width, ScaledPixelBuffer.Height);
				if (rectangle.IsInside (x, y)) {
					if (Topleft != null) {
						var x1 = LeftLine; //Toplerft.X;
						var y1 = Topleft.Y;
						var x2 = RightLine; //x;
						var y2 = y;

						// TODO: Handle case when mouse is outside of canvas area.. 

						var parentRect = new Rectangle (0, 0, 0, ScaledPixelBuffer.Width, ScaledPixelBuffer.Height);

						CurrentNode.Rectangles.Add (new Rectangle (CurrentID++, (int)x1, (int)y1, (int)x2, (int)y2) { Parent = parentRect });
						Topleft = null;
					}

					Console.WriteLine ("Inside drawing area");
				}
			}
			this.QueueDraw ();
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

			// Reset values when we go to next image.
			Topleft = null;

			RenderImage ();

			this.QueueDraw ();
		}

		public void SetNodes (List<PngNode> images)
		{
			NodeIdx = 0;

			this.Nodes = images;
			if (Nodes.Count > 0) {
				SetNode (this.Nodes [0]);
			}
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
			Console.WriteLine ("Rendering...");
			Cairo.Context cr = Gdk.CairoHelper.Create (drawingarea1.GdkWindow);

			// Draw rectangle
			cr.SetSourceRGB (0.2, 0.23, 0.9);
			cr.LineWidth = 1;
			cr.Rectangle (0 + OffsetX, 0 + OffsetY, ScaledPixelBuffer.Width, ScaledPixelBuffer.Height);
			cr.Stroke ();

			int x1 = 0 + OffsetX;
			int y1 = 0 + OffsetY;

			// Draw image
			drawingarea1.GdkWindow.DrawPixbuf (Style.ForegroundGC (StateType.Normal), 
			                                   ScaledPixelBuffer, 0, 0, x1, y1, 
			                                   ScaledPixelBuffer.Width, 
			                                   ScaledPixelBuffer.Height, RgbDither.Normal, 0, 0);
			// Draw lines
			cr.SetSourceRGB (0.1, 0, 0);
			cr.LineWidth = 2;
			cr.Rectangle (LeftLine + OffsetX, 0, 1, ScaledPixelBuffer.Height);
			cr.Rectangle (RightLine + OffsetX, 0, 1, ScaledPixelBuffer.Height);
			cr.Fill ();

			cr.SelectFontFace ("Monospace", FontSlant.Normal, FontWeight.Bold);
			cr.SetFontSize (20);

			// Draw selections
			foreach (var rectangle in CurrentNode.Rectangles) {
				//Draw rectangle
				cr.SetSourceRGBA (0.1, 0.1, 0, 0.1);
				cr.LineWidth = 2;
				var rec = new Cairo.Rectangle (rectangle.x1 + OffsetX,
				                              rectangle.y1 + OffsetY,
				                              rectangle.Width,
				                              rectangle.Height);

				cr.Rectangle (rec);
				cr.Fill ();

				// Draw text shadow
				cr.SetSourceRGB (0, 0, 0);
				cr.MoveTo (rectangle.x1 + (int)(rectangle.Width / 2.0) - 9, rectangle.y1 + (int)(rectangle.Height / 2.0));
				cr.ShowText (rectangle.ID.ToString ());

				// Draw text
				cr.SetSourceRGB (1, 1, 1);
				cr.MoveTo (rectangle.x1 + (int)(rectangle.Width / 2.0) - 10, rectangle.y1 + (int)(rectangle.Height / 2.0));
				cr.ShowText (rectangle.ID.ToString ());
			}

			if (Topleft != null) {
				cr.SetSourceRGBA (0.1, 0.1, 0, 0.1);
				cr.LineWidth = 2;
				cr.Rectangle (new Cairo.Rectangle (Topleft.X + OffsetX,
				                                  Topleft.Y + OffsetY,
				                                  MousePos.X - Topleft.X,
				                                  MousePos.Y - Topleft.Y)
				);
				cr.Fill ();
			}


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
			NodeIdx = (NodeIdx - 1) % Nodes.Count;
			SetNode (Nodes [NodeIdx]);
		}

		protected void OnBtnResetRectanglesClicked (object sender, EventArgs e)
		{

		}

		protected void OnBtnNextImgClicked (object sender, EventArgs e)
		{
			try {
				NodeIdx = (NodeIdx + 1) % Nodes.Count;
				SetNode (Nodes [NodeIdx]);	
			} catch (Exception ex) {
				
			}
		}

		protected void OnEntryLeftLineEditingDone (object sender, EventArgs e)
		{
			this.QueueDraw ();
		}

		protected void OnEntryRightLineEditingDone (object sender, EventArgs e)
		{
			this.QueueDraw ();
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
			this.QueueDraw ();
		}

		protected void OnScaleRightLineValueChanged (object sender, EventArgs e)
		{
			RightLine = (float)(scaleRightLine.Value / 100.0) * ScaledPixelBuffer.Width;
			this.QueueDraw ();
		}

		protected void OnDeleteEvent (object sender, DeleteEventArgs args)
		{
			// Prevent window from closing
			args.RetVal = true;
		}
	}
}

