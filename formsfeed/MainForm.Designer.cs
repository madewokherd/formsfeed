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
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.feedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.markAllItemsAsReadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fIleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.singleItemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.itemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.itemContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyLinkLocationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openInNewWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tagToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tagListSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.newTagToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.menuStrip1.SuspendLayout();
            this.itemContextMenu.SuspendLayout();
            this.statusbar.SuspendLayout();
            this.itemtoolstrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.feedToolStripMenuItem,
            this.fIleToolStripMenuItem,
            this.itemToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(354, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // feedToolStripMenuItem
            // 
            this.feedToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem,
            this.markAllItemsAsReadToolStripMenuItem});
            this.feedToolStripMenuItem.Name = "feedToolStripMenuItem";
            this.feedToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.feedToolStripMenuItem.Text = "Feed";
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.refreshToolStripMenuItem.Text = "Refresh";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
            // 
            // markAllItemsAsReadToolStripMenuItem
            // 
            this.markAllItemsAsReadToolStripMenuItem.Name = "markAllItemsAsReadToolStripMenuItem";
            this.markAllItemsAsReadToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.markAllItemsAsReadToolStripMenuItem.Text = "Mark all items as read";
            this.markAllItemsAsReadToolStripMenuItem.Click += new System.EventHandler(this.markAllItemsAsReadToolStripMenuItem_Click);
            // 
            // fIleToolStripMenuItem
            // 
            this.fIleToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.listToolStripMenuItem,
            this.singleItemToolStripMenuItem});
            this.fIleToolStripMenuItem.Name = "fIleToolStripMenuItem";
            this.fIleToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.fIleToolStripMenuItem.Text = "View";
            // 
            // listToolStripMenuItem
            // 
            this.listToolStripMenuItem.Checked = true;
            this.listToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.listToolStripMenuItem.Name = "listToolStripMenuItem";
            this.listToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.listToolStripMenuItem.Text = "List";
            this.listToolStripMenuItem.Click += new System.EventHandler(this.listToolStripMenuItem_Click);
            // 
            // singleItemToolStripMenuItem
            // 
            this.singleItemToolStripMenuItem.Name = "singleItemToolStripMenuItem";
            this.singleItemToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.singleItemToolStripMenuItem.Text = "Item Only";
            this.singleItemToolStripMenuItem.Click += new System.EventHandler(this.singleItemToolStripMenuItem_Click);
            // 
            // itemToolStripMenuItem
            // 
            this.itemToolStripMenuItem.DropDown = this.itemContextMenu;
            this.itemToolStripMenuItem.Enabled = false;
            this.itemToolStripMenuItem.Name = "itemToolStripMenuItem";
            this.itemToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.itemToolStripMenuItem.Text = "Item";
            // 
            // itemContextMenu
            // 
            this.itemContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyLinkLocationToolStripMenuItem,
            this.openInNewWindowToolStripMenuItem,
            this.tagToolStripMenuItem});
            this.itemContextMenu.Name = "contextMenuStrip1";
            this.itemContextMenu.OwnerItem = this.itemToolStripMenuItem;
            this.itemContextMenu.Size = new System.Drawing.Size(191, 92);
            // 
            // copyLinkLocationToolStripMenuItem
            // 
            this.copyLinkLocationToolStripMenuItem.Name = "copyLinkLocationToolStripMenuItem";
            this.copyLinkLocationToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.copyLinkLocationToolStripMenuItem.Text = "Copy Link Location";
            this.copyLinkLocationToolStripMenuItem.Click += new System.EventHandler(this.copyLinkLocationToolStripMenuItem_Click);
            // 
            // openInNewWindowToolStripMenuItem
            // 
            this.openInNewWindowToolStripMenuItem.Name = "openInNewWindowToolStripMenuItem";
            this.openInNewWindowToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.openInNewWindowToolStripMenuItem.Text = "Open in New Window";
            this.openInNewWindowToolStripMenuItem.Click += new System.EventHandler(this.openInNewWindowToolStripMenuItem_Click);
            // 
            // tagToolStripMenuItem
            // 
            this.tagToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tagListSeparator,
            this.newTagToolStripMenuItem});
            this.tagToolStripMenuItem.Name = "tagToolStripMenuItem";
            this.tagToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.tagToolStripMenuItem.Text = "Tag";
            this.tagToolStripMenuItem.DropDownOpening += new System.EventHandler(this.tagToolStripMenuItem_DropDownOpening);
            this.tagToolStripMenuItem.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tagToolStripMenuItem_DropDownItemClicked);
            // 
            // tagListSeparator
            // 
            this.tagListSeparator.Name = "tagListSeparator";
            this.tagListSeparator.Size = new System.Drawing.Size(149, 6);
            // 
            // newTagToolStripMenuItem
            // 
            this.newTagToolStripMenuItem.Name = "newTagToolStripMenuItem";
            this.newTagToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.newTagToolStripMenuItem.Text = "New tag...";
            this.newTagToolStripMenuItem.Click += new System.EventHandler(this.newTagToolStripMenuItem_Click);
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
            this.itemsview.ShowItemToolTips = true;
            this.itemsview.Size = new System.Drawing.Size(354, 315);
            this.itemsview.TabIndex = 1;
            this.itemsview.UseCompatibleStateImageBehavior = false;
            this.itemsview.View = System.Windows.Forms.View.Details;
            this.itemsview.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.itemsview_ItemSelectionChanged);
            this.itemsview.SelectedIndexChanged += new System.EventHandler(this.itemsview_SelectedIndexChanged);
            this.itemsview.Click += new System.EventHandler(this.itemsview_Click);
            this.itemsview.DoubleClick += new System.EventHandler(this.itemsview_DoubleClick);
            this.itemsview.MouseClick += new System.Windows.Forms.MouseEventHandler(this.itemsview_MouseClick);
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
            this.itemtoolstrip.Location = new System.Drawing.Point(0, 0);
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
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(354, 339);
            this.Controls.Add(this.webbrowser);
            this.Controls.Add(this.itemsview);
            this.Controls.Add(this.statusbar);
            this.Controls.Add(this.itemtoolstrip);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "FormsFeed";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.itemContextMenu.ResumeLayout(false);
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
        private System.Windows.Forms.ToolStrip itemtoolstrip;
        private System.Windows.Forms.ToolStripButton allitemsbutton;
        private System.Windows.Forms.ToolStripMenuItem feedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem markAllItemsAsReadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem itemToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip itemContextMenu;
        private System.Windows.Forms.ToolStripMenuItem copyLinkLocationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openInNewWindowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tagToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator tagListSeparator;
        private System.Windows.Forms.ToolStripMenuItem newTagToolStripMenuItem;
    }
}

