using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FormsFeed;

namespace FormsFeed.WinForms
{
    public partial class MainForm : Form
    {
        public MainForm(Cache cache)
        {
            this.cache = cache;
            this.current_items = new Dictionary<Tuple<string, string>, ListViewItem>();
            InitializeComponent();
            this.itemsview.ListViewItemSorter = new ListViewItemSort();
        }

        public Cache cache;

        bool refreshing;
        int refresh_progress;
        int refresh_total;

        private Dictionary<Tuple<string, string>, ListViewItem> current_items;

        private delegate void VoidNoArgsDelegate();
        private delegate void VoidIntDelegate(int i);

        private void UpdateProgressControls()
        {
            if (!refreshing)
                return;
            statusprogress.Maximum = refresh_total;
            statusprogress.Minimum = 0;
            statusprogress.Value = refresh_progress;
            statuslabel.Text = string.Format("Updating {0}/{1}", refresh_progress, refresh_total);
            statusbar.Show();
        }

        private void FinishRefresh()
        {
            statusbar.Hide();
            refreshing = false;
            RefreshItemsView();
        }

        private void RefreshProgress(int total)
        {
            refresh_progress += 1;
            refresh_total = total;
            UpdateProgressControls();
        }

        private void RefreshThreadProgress(int total)
        {
            object[] args = new object[1];
            args[0] = total;
            BeginInvoke(new VoidIntDelegate(RefreshProgress), args);
        }

        private void RefreshThread()
        {
            cache.UpdateAll(new Cache.ProgressCallback(RefreshThreadProgress));
            BeginInvoke(new VoidNoArgsDelegate(FinishRefresh));
        }

        public void RefreshItems()
        {
            if (!refreshing)
            {
                refreshing = true;
                refresh_progress = -1;
                refresh_total = 0;
                Thread refresh_thread = new Thread(new ThreadStart(RefreshThread));
                refresh_thread.IsBackground = true;
                refresh_thread.Start();
            }
        }

        public void RefreshItemsView()
        {
            Tag tag = cache.GetTag("(unread)");
            List<DetailedInfo> infos = new List<DetailedInfo>(tag.GetSummaries());
            itemsview.BeginUpdate();
            foreach (var info in infos)
            {
                var key = Tuple.Create(info.feed_uri, info.id);
                if (current_items.ContainsKey(key))
                    continue;
                ListViewItem item = new ListViewItem();
                item.Tag = info;
                item.Text = info.title;
                item.SubItems.Add(info.author);
                item.SubItems.Add(info.timestamp.ToString());
                itemsview.Items.Add(item);
                current_items[key] = item;
            }
            itemsview.EndUpdate();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            RefreshItemsView();
            RefreshItems();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshItems();
        }

        private void itemsview_Resize(object sender, EventArgs e)
        {
            itemsview.Columns[0].Width = itemsview.Width - (itemsview.Columns[1].Width + itemsview.Columns[2].Width + 24);
        }
    }
}
