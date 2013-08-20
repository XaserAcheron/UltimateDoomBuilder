﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ChangeMapForm : DelayedForm
	{
		private readonly MapOptions options;
		private readonly string filepathname;

		public MapOptions Options { get { return options; } }

		public ChangeMapForm(string filepathname, MapOptions options) {
			InitializeComponent();
			this.options = options;
			this.filepathname = filepathname;
		}

		private void LoadSettings() {
			int scanindex, checkoffset;
			int lumpsfound, lumpsrequired = 0;
			string lumpname;
			WAD wadfile;
			
			// Busy
			Cursor.Current = Cursors.WaitCursor;

			// Check if the file exists
			if(!File.Exists(filepathname))
			{
				// WAD file does not exist
				MessageBox.Show(this, "Could not open the WAD file: The file does not exist.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				this.DialogResult = DialogResult.Cancel;
				this.Close();
				return;
			}
			
			try
			{
				// Open the WAD file
				wadfile = new WAD(filepathname, true);
			}
			catch(Exception)
			{
				// Unable to open WAD file (or its config)
				MessageBox.Show(this, "Could not open the WAD file for reading. Please make sure the file you selected is valid and is not in use by any other application.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				this.DialogResult = DialogResult.Cancel;
				this.Close();
				return;
			}

			// Make an array for the map names
			List<ListViewItem> mapnames = new List<ListViewItem>();

			// Load this configuration
			Configuration cfg = General.LoadGameConfiguration(options.ConfigFile);

			// Get the map lump names
			IDictionary maplumpnames = cfg.ReadSetting("maplumpnames", new Hashtable());

			// Count how many required lumps we have to find
			foreach(DictionaryEntry ml in maplumpnames) {
				// Ignore the map header (it will not be found because the name is different)
				if(ml.Key.ToString() != MapManager.CONFIG_MAP_HEADER) {
					// Read lump setting and count it
					if(cfg.ReadSetting("maplumpnames." + ml.Key + ".required", false))
						lumpsrequired++;
				}
			}

			// Go for all the lumps in the wad
			for(scanindex = 0; scanindex < (wadfile.Lumps.Count - 1); scanindex++) {
				// Make sure this lump is not part of the map
				if(!maplumpnames.Contains(wadfile.Lumps[scanindex].Name)) {
					// Reset check
					lumpsfound = 0;
					checkoffset = 1;

					// Continue while still within bounds and lumps are still recognized
					while(((scanindex + checkoffset) < wadfile.Lumps.Count) &&
						  maplumpnames.Contains(wadfile.Lumps[scanindex + checkoffset].Name)) {
						// Count the lump when it is marked as required
						lumpname = wadfile.Lumps[scanindex + checkoffset].Name;
						if(cfg.ReadSetting("maplumpnames." + lumpname + ".required", false))
							lumpsfound++;

						// Check the next lump
						checkoffset++;
					}

					// Map found? Then add it to the list
					if(lumpsfound >= lumpsrequired)
						mapnames.Add(new ListViewItem(wadfile.Lumps[scanindex].Name));
				}
			}

			wadfile.Dispose();

			// Clear the list and add the new map names
			mapslist.BeginUpdate();
			mapslist.Items.Clear();
			mapslist.Items.AddRange(mapnames.ToArray());
			mapslist.Sort();

			//select current map
			foreach(ListViewItem item in mapslist.Items) {
				// Was this item previously selected?
				if(item.Text == options.LevelName) {
					// Select it again
					item.Selected = true;
					break;
				}
			}

			mapslist.EndUpdate();
			
			// Done
			Cursor.Current = Cursors.Default;
		}

		private void ChangeMapForm_Shown(object sender, EventArgs e){
			LoadSettings();
		}

		private void mapslist_DoubleClick(object sender, EventArgs e) {
			// Click OK
			if(mapslist.SelectedItems.Count > 0) apply.PerformClick();
		}

		private void apply_Click(object sender, EventArgs e) {
			options.CurrentName = mapslist.SelectedItems[0].Text;
			options.PreviousName = string.Empty;
			
			// Hide window
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void cancel_Click(object sender, EventArgs e) {
			// Just hide window
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}
	}
}