namespace FormsFeed.WinForms
{
    partial class MainForm
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fIleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusbar = new System.Windows.Forms.StatusStrip();
            this.statusprogress = new System.Windows.Forms.ToolStripProgressBar();
            this.statuslabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.itemsview = new System.Windows.Forms.ListView();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.authorcolumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.textcolumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.datecolumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menuStrip1.SuspendLayout();
            this.statusbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fIleToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(354, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fIleToolStripMenuItem
            // 
            this.fIleToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem});
            this.fIleToolStripMenuItem.Name = "fIleToolStripMenuItem";
            this.fIleToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.fIleToolStripMenuItem.Text = "View";
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.refreshToolStripMenuItem.Text = "Refresh";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
            // 
            // statusbar
            // 
            this.statusbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusprogress,
            this.statuslabel});
            this.statusbar.Location = new System.Drawing.Point(0, 317);
            this.statusbar.Name = "statusbar";
            this.statusbar.Size = new System.Drawing.Size(354, 22);
            this.statusbar.TabIndex = 0;
            this.statusbar.Text = "statusStrip1";
            this.statusbar.Visible = false;
            // 
            // statusprogress
            // 
            this.statusprogress.Name = "statusprogress";
            this.statusprogress.Size = new System.Drawing.Size(100, 16);
            // 
            // statuslabel
            // 
            this.statuslabel.Name = "statuslabel";
            this.statuslabel.Size = new System.Drawing.Size(92, 17);
            this.statuslabel.Text = "update progress";
            // 
            // itemsview
            // 
            this.itemsview.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.textcolumn,
            this.authorcolumn,
            this.datecolumn});
            this.itemsview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.itemsview.FullRowSelect = true;
            this.itemsview.GridLines = true;
            this.itemsview.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.itemsview.HideSelection = false;
            this.itemsview.LabelWrap = false;
            this.itemsview.Location = new System.Drawing.Point(0, 24);
            this.itemsview.Margin = new System.Windows.Forms.Padding(0);
            this.itemsview.Name = "itemsview";
            this.itemsview.Size = new System.Drawing.Size(354, 315);
            this.itemsview.TabIndex = 1;
            this.itemsview.UseCompatibleStateImageBehavior = false;
            this.itemsview.View = System.Windows.Forms.View.Details;
            this.itemsview.Resize += new System.EventHandler(this.itemsview_Resize);
            // 
            // webBrowser1
            // 
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(0, 24);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(354, 315);
            this.webBrowser1.TabIndex = 2;
            this.webBrowser1.Visible = false;
            // 
            // authorcolumn
            // 
            this.authorcolumn.DisplayIndex = 0;
            this.authorcolumn.Text = "Author";
            this.authorcolumn.Width = 90;
            // 
            // textcolumn
            // 
            this.textcolumn.DisplayIndex = 1;
            this.textcolumn.Text = "Text";
            // 
            // datecolumn
            // 
            this.datecolumn.Text = "Date";
            this.datecolumn.Width = 150;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(354, 339);
            this.Controls.Add(this.webBrowser1);
            this.Controls.Add(this.itemsview);
            this.Controls.Add(this.statusbar);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "FormsFeed";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusbar.ResumeLayout(false);
            this.statusbar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fIleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusbar;
        private System.Windows.Forms.ToolStripProgressBar statusprogress;
        private System.Windows.Forms.ToolStripStatusLabel statuslabel;
        private System.Windows.Forms.ListView itemsview;
        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.ColumnHeader authorcolumn;
        private System.Windows.Forms.ColumnHeader textcolumn;
        private System.Windows.Forms.ColumnHeader datecolumn;
    }
}

