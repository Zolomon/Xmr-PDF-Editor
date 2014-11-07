using System;
using System.IO;
using Gtk;
using System.Collections.Generic;
using Exameer;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using Gdk;
using Cairo;

public partial class MainWindow: Gtk.Window
{	
	Gtk.NodeStore storePdf;

	Gtk.NodeStore StorePDF {
		get {
			if (storePdf == null) {
				storePdf = new Gtk.NodeStore (typeof(PdfNode));
			}
			return storePdf;
		}
	}

	Gtk.NodeStore storePng;

	Gtk.NodeStore StorePNG {
		get {
			if (storePng == null) {
				storePng = new Gtk.NodeStore (typeof(PngNode));
			}
			return storePng;
		}
	}

	public PngViewer PNGViewer { get; set; }

	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{

		Build ();

		this.nodeViewPDF.NodeStore = StorePDF;
		this.nodeViewPDF.AppendColumn ("Pdf", new Gtk.CellRendererText (), "text", 0);
		this.nodeViewPDF.ShowAll ();
		this.nodeViewPDF.NodeSelection.Changed += new System.EventHandler (OnPdfSelectionChanged);

		this.nodeViewPNG.NodeStore = StorePNG;
		this.nodeViewPNG.AppendColumn ("Png", new Gtk.CellRendererText (), "text", 0);
		this.nodeViewPNG.ShowAll ();
		this.nodeViewPNG.NodeSelection.Changed += new System.EventHandler (OnPngSelectionChanged);

		this.PNGViewer = new PngViewer ();
		this.PNGViewer.Resize (900, 1020);
		PNGViewer.ShowAll ();
	}

	void OnPdfSelectionChanged (object sender, EventArgs e)
	{
		NodeSelection es = (NodeSelection)sender;
		PdfNode node = (PdfNode)es.SelectedNode;

		if (node.Images.Count == 0) {
			ProcessStartInfo psi = new ProcessStartInfo ("convert", "-density 300  \"" + 
				node.FileName + "\" " + 
				node.Name.Replace (".pdf", ".png")
			);
			using (Process p = new Process()) {

				p.StartInfo = psi;
				p.Start ();

				Console.Write (".");
				if (false == p.WaitForExit ((int)TimeSpan.FromHours (1).TotalMilliseconds))
					throw new ArgumentException ("The program did not finish in time, aborting.");
				Console.WriteLine (".");

				var images = Directory.GetFiles (".", node.Name.Replace (".pdf", "*.png")).ToList ()
				.Where (x => x.Contains (node.Name.Replace (".pdf", "")))
				.Select (x => new PngNode (x));

				node.Images.AddRange (images);

				foreach (var image in images) {
					Console.WriteLine (image.Name);
				}

				PNGViewer.SetNodes (node.Images);

				//node.Images.ForEach(x => this.StorePNG.AddNode(new PngNode(x)));

				this.lblStatus.Text = String.Format ("converted {0}", node.Name);
			}
		} else {
			PNGViewer.SetNodes (node.Images);
		}
	}

	void OnPngSelectionChanged (object sender, EventArgs e)
	{
		NodeSelection es = (NodeSelection)sender;
		PngNode pn = (PngNode)es.SelectedNode;

		Console.WriteLine (pn.FileName);

		PNGViewer.SetNode (pn);
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected void OnBtnOpenFileDialogClicked (object sender, EventArgs e)
	{
		using (Gtk.FileChooserDialog fc = 
		       new FileChooserDialog ("Open PDFs", this,
		                              FileChooserAction.Open,
		                              "Cancel", ResponseType.Cancel,
		                              "Open", ResponseType.Accept)) {
			fc.SetCurrentFolder ("/media/zol/Green/Dropbox/lth/eda040 - realtidsprogrammering/exams");
			fc.SelectMultiple = true;

			if (fc.Run () == (int)ResponseType.Accept) {
				foreach (var name in fc.Filenames) {
					this.StorePDF.AddNode (new PdfNode (name));
				}
			}
			fc.Destroy ();
		}
	}

	protected void OnBtnConvertClicked (object sender, EventArgs e)
	{
		var pdfRectangles = new Dictionary<string, Dictionary<int, List<Tuple<Exameer.Rectangle, string>>> > ();

		// Merge rectangles
		foreach (PdfNode pdf in StorePDF) {
			foreach (var png in pdf.Images) {
				foreach (var rectangle in png.Rectangles) {
					if (pdfRectangles.ContainsKey (pdf.Name)) {
						if (pdfRectangles [pdf.Name].ContainsKey (rectangle.ID)) {
							pdfRectangles [pdf.Name] [rectangle.ID].Add (new Tuple<Exameer.Rectangle, string> (rectangle, png.FileName));
						} else {
							pdfRectangles [pdf.Name].Add (rectangle.ID, 
							                         new List<Tuple<Exameer.Rectangle, string>> () { 
														new Tuple<Exameer.Rectangle, string>(rectangle, png.FileName)
													}
							);
						}
					} else {
						pdfRectangles.Add (pdf.Name, 
						               new Dictionary<int, List<Tuple<Exameer.Rectangle, string>>> () {{
											rectangle.ID, new List<Tuple<Exameer.Rectangle, string>>() {
												new Tuple<Exameer.Rectangle, string>(rectangle, png.FileName)
											}}}
						);
					}
				}
			}
		}

		int problemNbr = 0;
		foreach (var pdf in pdfRectangles) {
			var createdDestinationDir = Directory.CreateDirectory ("./" + pdf.Key.Replace (".pdf", "") + "/");

			foreach (var pngrects in pdf.Value) {
				var surfaces = new List<Tuple<ImageSurface, Exameer.Rectangle>> ();

				foreach (var rectangle in pngrects.Value) {
					surfaces.Add (new Tuple<ImageSurface, Exameer.Rectangle> (new ImageSurface (rectangle.Item2), rectangle.Item1));
				}

				var png = new ImageSurface (pngrects.Value [0].Item2);

				var rectsMaxWidth = pngrects.Value.Max (x => x.Item1.Width);
				var rectsMaxHeight = pngrects.Value.Select (x => x.Item1.Height).Sum ();

				var newRectWidth = (int)((rectsMaxWidth / (float)pngrects.Value [0].Item1.Parent.Width) * png.Width);
				var newRectHeight = (int)((rectsMaxHeight / (float)pngrects.Value [0].Item1.Parent.Height) * png.Height); //pngrects.Value.Aggregate(0, (sum, next) => sum + next.Item1.Height);

				ImageSurface newImg = new ImageSurface (Format.Argb32, newRectWidth, newRectHeight);

				Context cr = new Context (newImg);

				//cr.SetSourceRGBA (1, 1, 1, 1);
				//cr.Paint ();

				// Assume they are sorted by their appearance in PNGs.
				int lastY = 0;
				foreach (var surface in surfaces) {

					var dstx = 0;
					var dsty = lastY;

					var r = surface.Item2;

					var srcx = (int)((r.x1 / (float)r.Parent.Width) * png.Width);
					var srcy = ((int)((r.y1 / (float)r.Parent.Height) * png.Height));

					var w = (int)((r.Width / (float)r.Parent.Width) * png.Width);
					var h = (int)((r.Height / (float)r.Parent.Height) * png.Height);
					cr.SetSourceSurface (surface.Item1, dstx - srcx, dsty - srcy);

					cr.Rectangle (dstx, dsty, w, h);
					cr.Fill ();
					//lastY = (int)((r.Height / (float)r.Parent.Height) * png.Height);
					lastY = dsty + h;
				
				}

				newImg.Flush ();
				newImg.WriteToPng ("./" + pdf.Key.Replace (".pdf", "") + "/" + 
				                   (problemNbr++) + ".png");

				newImg.Dispose ();
				//newImg.Dispose ();
				//surfaces.ForEach (x => x.Item1.Destroy ());
				surfaces.ForEach (x => x.Item1.Dispose ());

				png.Dispose ();
				//png.Destroy ();
			}

			problemNbr = 0;
		}
	}

}
