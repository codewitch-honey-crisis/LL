using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LL;
namespace llgui
{
	public partial class Main : Form
	{
		Parser _parser;
		string _locfmt;
		public Main()
		{
			InitializeComponent();
			ErrorListView.View = View.Details;
			_locfmt = Loc.Text;
			_parser = new EbnfParser(new EbnfTokenizer(Box.Input));
			_parser.ShowHidden = true;
			Box.Parser = _parser;
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(DialogResult.OK==openFileDialog.ShowDialog(this))
			{
				saveFileDialog.InitialDirectory= Path.GetDirectoryName(openFileDialog.FileName);
				saveFileDialog.FileName = openFileDialog.SafeFileName;
				using(var sr = new StreamReader(openFileDialog.OpenFile()))
				{
					Box.Text = sr.ReadToEnd();
				}
			}
		}

		private void Box_SelectionChanged(object sender, EventArgs e)
		{

		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(!string.IsNullOrEmpty(saveFileDialog.FileName))
			{
				using (var sw = new StreamWriter(saveFileDialog.OpenFile()))
				{
					sw.Write(Box.Text);
				}
			} else
			if(DialogResult.OK==saveFileDialog.ShowDialog(this))
			{
				using(var sw = new StreamWriter(saveFileDialog.OpenFile()))
				{
					sw.Write(Box.Text);
				}
			}
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (DialogResult.OK == saveFileDialog.ShowDialog(this))
			{
				using (var sw = new StreamWriter(saveFileDialog.OpenFile()))
				{
					sw.Write(Box.Text);
				}
			}

		}

		private void checkTimer_Tick(object sender, EventArgs e)
		{
			checkTimer.Enabled = false;
			ErrorListView.Items.Clear();
			EbnfDocument ebnf=null;
			try
			{
				ebnf = EbnfDocument.Parse(Box.Text);
			}
			catch(ExpectingException ee)
			{
				var lvi = new ListViewItem(new string[] { "Error", ee.Message });
				ErrorListView.Items.Add(lvi);
				return;
			}
			foreach(var msg in ebnf.Validate(false))
			{
				var lvi = new ListViewItem(new string[] { msg.ErrorLevel.ToString(), msg.Message });
				ErrorListView.Items.Add(lvi);

			}
			try
			{
				var cfg = ebnf.ToCfg();
				foreach (var msg in cfg.PrepareLL1(false))
				{
					var lvi = new ListViewItem(new string[] { msg.ErrorLevel.ToString(), msg.Message });
					ErrorListView.Items.Add(lvi);
				}
			}
			catch(InvalidOperationException)
			{
				var lvi = new ListViewItem(new string[] { "Error", "Grammar is irresolvable recursive"});
				ErrorListView.Items.Add(lvi);
			}
		}

		private void Box_TextChanged(object sender, EventArgs e)
		{
			checkTimer.Enabled = true;
		}

		
	}
}
