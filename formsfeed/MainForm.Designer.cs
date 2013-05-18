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
            this.listToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.singleItemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusbar = new System.Windows.Forms.StatusStrip();
            this.statusprogress = new System.Windows.Forms.ToolStripProgressBar();
            this.statuslabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.itemsview = new System.Windows.Forms.ListView();
            this.textcolumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.authorcolumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.datecolumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.webbrowser = new System.Windows.Forms.WebBrowser();
            this.itemtoolstrip = new System.Windows.Forms.ToolStrip();
            this.allitemsbutton = new System.Windows.Forms.ToolStripButton();
            this.feedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.markAllItemsAsReadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.statusbar.SuspendLayout();
            this.itemtoolstrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.feedToolStripMenuItem,
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
            this.listToolStripMenuItem,
            this.singleItemToolStripMenuItem,
            this.toolStripSeparator1,
            this.refreshToolStripMenuItem});
            this.fIleToolStripMenuItem.Name = "fIleToolStripMenuItem";
            this.fIleToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.fIleToolStripMenuItem.Text = "View";
            // 
            // listToolStripMenuItem
            // 
            this.listToolStripMenuItem.Checked = true;
            this.listToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.listToolStripMenuItem.Name = "listToolStripMenuItem";
            this.listToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.listToolStripMenuItem.Text = "List";
            this.listToolStripMenuItem.Click += new System.EventHandler(this.listToolStripMenuItem_Click);
            // 
            // singleItemToolStripMenuItem
            // 
            this.singleItemToolStripMenuItem.Name = "singleItemToolStripMenuItem";
            this.singleItemToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.singleItemToolStripMenuItem.Text = "Item Only";
            this.singleItemToolStripMenuItem.Click += new System.EventHandler(this.singleItemToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(129, 6);
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
            this.itemsview.MultiSelect = false;
            this.itemsview.Name = "itemsview";
            this.itemsview.Size = new System.Drawing.Size(354, 315);
            this.itemsview.TabIndex = 1;
            this.itemsview.UseCompatibleStateImageBehavior = false;
            this.itemsview.View = System.Windows.Forms.View.Details;
            this.itemsview.SelectedIndexChanged += new System.EventHandler(this.itemsview_SelectedIndexChanged);
            this.itemsview.Click += new System.EventHandler(this.itemsview_Click);
            this.itemsview.Resize += new System.EventHandler(this.itemsview_Resize);
            // 
            // textcolumn
            // 
            this.textcolumn.DisplayIndex = 1;
            this.textcolumn.Text = "Text";
            // 
            // authorcolumn
            // 
            this.authorcolumn.DisplayIndex = 0;
            this.authorcolumn.Text = "Author";
            this.authorcolumn.Width = 90;
            // 
            // datecolumn
            // 
            this.datecolumn.Text = "Date";
            this.datecolumn.Width = 150;
            // 
            // webbrowser
            // 
            this.webbrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webbrowser.Location = new System.Drawing.Point(0, 24);
            this.webbrowser.Margin = new System.Windows.Forms.Padding(0);
            this.webbrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.webbrowser.Name = "webbrowser";
            this.webbrowser.ScriptErrorsSuppressed = true;
            this.webbrowser.Size = new System.Drawing.Size(354, 315);
            this.webbrowser.TabIndex = 2;
            this.webbrowser.Visible = false;
            this.webbrowser.VisibleChanged += new System.EventHandler(this.webbrowser_VisibleChanged);
            // 
            // itemtoolstrip
            // 
            this.itemtoolstrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.itemtoolstrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.allitemsbutton});
            this.itemtoolstrip.Location = new System.Drawing.Point(0, 24);
            this.itemtoolstrip.Name = "itemtoolstrip";
            this.itemtoolstrip.Size = new System.Drawing.Size(354, 25);
            this.itemtoolstrip.TabIndex = 3;
            this.itemtoolstrip.Text = "toolStrip1";
            this.itemtoolstrip.Visible = false;
            // 
            // allitemsbutton
            // 
            this.allitemsbutton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.allitemsbutton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.allitemsbutton.Name = "allitemsbutton";
            this.allitemsbutton.Size = new System.Drawing.Size(85, 22);
            this.allitemsbutton.Text = "Show item list";
            this.allitemsbutton.Click += new System.EventHandler(this.allitemsbutton_Click);
            // 
            // feedToolStripMenuItem
            // 
            this.feedToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.markAllItemsAsReadToolStripMenuItem});
            this.feedToolStripMenuItem.Name = "feedToolStripMenuItem";
            this.feedToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.feedToolStripMenuItem.Text = "Feed";
            // 
            // markAllItemsAsReadToolStripMenuItem
            // 
            this.markAllItemsAsReadToolStripMenuItem.Name = "markAllItemsAsReadToolStripMenuItem";
            this.markAllItemsAsReadToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.markAllItemsAsReadToolStripMenuItem.Text = "Mark all items as read";
            this.markAllItemsAsReadToolStripMenuItem.Click += new System.EventHandler(this.markAllItemsAsReadToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(354, 339);
            this.Controls.Add(this.webbrowser);
            this.Controls.Add(this.itemtoolstrip);
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
            this.itemtoolstrip.ResumeLayout(false);
            this.itemtoolstrip.PerformLayout();
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
        private System.Windows.Forms.WebBrowser webbrowser;
        private System.Windows.Forms.ColumnHeader authorcolumn;
        private System.Windows.Forms.ColumnHeader textcolumn;
        private System.Windows.Forms.ColumnHeader datecolumn;
        private System.Windows.Forms.ToolStripMenuItem listToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem singleItemToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStrip itemtoolstrip;
        private System.Windows.Forms.ToolStripButton allitemsbutton;
        private System.Windows.Forms.ToolStripMenuItem feedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem markAllItemsAsReadToolStripMenuItem;
    }
}

