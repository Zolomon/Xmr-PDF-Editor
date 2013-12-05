using System;
using System.IO;
using Gtk;
using System.Collections.Generic;
using Exameer;
using System.Diagnostics;
using System.Linq;

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
		this.nodeViewPDF.AppendColumn("Pdf", new Gtk.CellRendererText (), "text", 0);
		this.nodeViewPDF.ShowAll();
		//this.nodeViewPDF.NodeSelection.Changed += new System.EventHandler (OnSelectionChanged);

		this.nodeViewPNG.NodeStore = StorePNG;
		this.nodeViewPNG.AppendColumn("Png", new Gtk.CellRendererText (), "text", 0);
		this.nodeViewPNG.ShowAll();
		this.nodeViewPNG.NodeSelection.Changed += new System.EventHandler(OnPngSelectionChanged);

		this.PNGViewer = new PngViewer();
		PNGViewer.ShowAll();
	}

	void OnPngSelectionChanged (object sender, EventArgs e)
	{
		NodeSelection es = (NodeSelection)sender;
		PngNode pn = (PngNode)es.SelectedNode;

		Console.WriteLine (pn.FileName);

		PNGViewer.SetNode(pn);
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
			fc.SetCurrentFolder ("/home/bengt/Dropbox/lth/fmaa01 - endimensionell analys/a2/exams/");
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
		foreach (PdfNode node in StorePDF) {
			ProcessStartInfo psi = new ProcessStartInfo("convert", "-density 300  \"" + node.FileName + "\" " + node.Name.Replace(".pdf", ".png"));
			using (Process p = new Process()) {

				p.StartInfo = psi;
				p.Start();

				Console.Write(".");
				if (false == p.WaitForExit ((int)TimeSpan.FromHours(1).TotalMilliseconds))
                	throw new ArgumentException("The program did not finish in time, aborting.");
				Console.WriteLine(".");

				var images = Directory.GetFiles(".", node.Name.Replace(".pdf", ".png"));
				node.Images.AddRange(images);

				node.Images.ForEach(x => this.StorePNG.AddNode(new PngNode(x)));

				this.lblStatus.Text = String.Format("converted {0}", node.Name);
			}
		}
	}

}
