namespace llgui
{
	partial class Main
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fileMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.Box = new LL.Highlighter();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.Status = new System.Windows.Forms.ToolStripStatusLabel();
			this.Loc = new System.Windows.Forms.ToolStripStatusLabel();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.ErrorListView = new System.Windows.Forms.ListView();
			this.splitter = new System.Windows.Forms.Splitter();
			this.columnHeaderErrorLevel = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeaderErrorMessage = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.checkTimer = new System.Windows.Forms.Timer(this.components);
			this.menuStrip.SuspendLayout();
			this.statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip
			// 
			this.menuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Size = new System.Drawing.Size(800, 28);
			this.menuStrip.TabIndex = 1;
			this.menuStrip.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.fileMenuSeparator1,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(44, 24);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Size = new System.Drawing.Size(144, 26);
			this.openToolStripMenuItem.Text = "&Open...";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			// 
			// fileMenuSeparator1
			// 
			this.fileMenuSeparator1.Name = "fileMenuSeparator1";
			this.fileMenuSeparator1.Size = new System.Drawing.Size(141, 6);
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(144, 26);
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			// 
			// saveAsToolStripMenuItem
			// 
			this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
			this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(144, 26);
			this.saveAsToolStripMenuItem.Text = "Save &As...";
			this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
			// 
			// openFileDialog
			// 
			this.openFileDialog.Filter = "EBNF files|*.ebnf|All files|*.*";
			// 
			// Box
			// 
			this.Box.AcceptsTab = true;
			this.Box.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Box.DetectUrls = false;
			this.Box.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Box.Font = new System.Drawing.Font("Lucida Console", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Box.Location = new System.Drawing.Point(0, 28);
			this.Box.Name = "Box";
			this.Box.Parser = null;
			this.Box.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedBoth;
			this.Box.ShowSelectionMargin = true;
			this.Box.Size = new System.Drawing.Size(800, 300);
			this.Box.TabIndex = 0;
			this.Box.Text = "";
			this.Box.WordWrap = false;
			this.Box.SelectionChanged += new System.EventHandler(this.Box_SelectionChanged);
			this.Box.TextChanged += new System.EventHandler(this.Box_TextChanged);
			// 
			// statusStrip
			// 
			this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Status,
            this.Loc});
			this.statusStrip.Location = new System.Drawing.Point(0, 425);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(800, 25);
			this.statusStrip.TabIndex = 2;
			this.statusStrip.Text = "statusStrip";
			// 
			// Status
			// 
			this.Status.Name = "Status";
			this.Status.Size = new System.Drawing.Size(566, 20);
			this.Status.Spring = true;
			this.Status.Text = "Idle";
			this.Status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// Loc
			// 
			this.Loc.Name = "Loc";
			this.Loc.Size = new System.Drawing.Size(219, 20);
			this.Loc.Text = "Line {0}, Column {1}, Position {2}";
			// 
			// saveFileDialog
			// 
			this.saveFileDialog.Filter = "EBNF files|*.ebnf|All files|*.*";
			// 
			// ErrorListView
			// 
			this.ErrorListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderErrorLevel,
            this.columnHeaderErrorMessage});
			this.ErrorListView.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.ErrorListView.HideSelection = false;
			this.ErrorListView.Location = new System.Drawing.Point(0, 328);
			this.ErrorListView.Name = "ErrorListView";
			this.ErrorListView.Size = new System.Drawing.Size(800, 97);
			this.ErrorListView.TabIndex = 3;
			this.ErrorListView.UseCompatibleStateImageBehavior = false;
			// 
			// splitter
			// 
			this.splitter.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.splitter.Location = new System.Drawing.Point(0, 325);
			this.splitter.Name = "splitter";
			this.splitter.Size = new System.Drawing.Size(800, 3);
			this.splitter.TabIndex = 4;
			this.splitter.TabStop = false;
			// 
			// columnHeaderErrorLevel
			// 
			this.columnHeaderErrorLevel.Text = "";
			this.columnHeaderErrorLevel.Width = 30;
			// 
			// columnHeaderErrorMessage
			// 
			this.columnHeaderErrorMessage.Text = "Message";
			this.columnHeaderErrorMessage.Width = 2000;
			// 
			// checkTimer
			// 
			this.checkTimer.Tick += new System.EventHandler(this.checkTimer_Tick);
			// 
			// Main
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.splitter);
			this.Controls.Add(this.Box);
			this.Controls.Add(this.menuStrip);
			this.Controls.Add(this.ErrorListView);
			this.Controls.Add(this.statusStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip;
			this.Name = "Main";
			this.Text = "LL Parser Generator";
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private LL.Highlighter Box;
		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator fileMenuSeparator1;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel Status;
		private System.Windows.Forms.ToolStripStatusLabel Loc;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.ListView ErrorListView;
		private System.Windows.Forms.Splitter splitter;
		private System.Windows.Forms.ColumnHeader columnHeaderErrorLevel;
		private System.Windows.Forms.ColumnHeader columnHeaderErrorMessage;
		private System.Windows.Forms.Timer checkTimer;
	}
}

