using System;

namespace Exameer
{
	public partial class NewIdDialog : Gtk.Dialog
	{
		public NewIdDialog ()
		{
			this.Build ();
		}

		public int NewId ()
		{
			return int.Parse(this.entryNewID.Text);
		}

	}
}

