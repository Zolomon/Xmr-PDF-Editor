using System;

namespace Exameer
{
	public partial class NewIdDialog : Gtk.Dialog
	{
		public void SetId (int id)
		{
			this.entryNewID.Text = string.Format ("{}", id);
		}

		public NewIdDialog ()
		{
			this.Build ();
		}

		public int NewId ()
		{
			return int.Parse (this.entryNewID.Text);
		}

	}
}

