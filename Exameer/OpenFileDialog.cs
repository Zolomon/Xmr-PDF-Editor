using System;

namespace Exameer
{
	public partial class OpenFileDialog : Gtk.Dialog
	{
		MainWindow parent {
			get;
			set;
		}

		public OpenFileDialog (MainWindow parent)
		{
			this.parent = parent;
			this.Build ();
		}
		protected void OnButtonOkClicked (object sender, EventArgs e)
		{

		}		

		protected void OnButtonCancelClicked (object sender, EventArgs e)
		{
			throw new System.NotImplementedException ();
		}


	}
}

