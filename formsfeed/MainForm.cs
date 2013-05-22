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
using System.IO;

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
            current_view = View.List;
        }

        public Cache cache;

        bool refreshing;
        int refresh_progress;
        int refresh_total;

        private Dictionary<Tuple<string, string>, ListViewItem> current_items;

        string last_webbrowser_feedurl;
        string last_webbrowser_id;

        string temp_html_file;
        string temp_html_file_feedurl;
        string temp_html_file_id;

        private delegate void VoidNoArgsDelegate();
        private delegate void VoidIntDelegate(int i);

        enum View
        {
            List,
            SingleItem
        }

        View current_view;

        private void SetView(View new_view)
        {
            if (current_view == new_view)
                return;
            SuspendLayout();
            switch (current_view)
            {
                case View.List:
                    itemsview.Hide();
                    listToolStripMenuItem.CheckState = CheckState.Unchecked;
                    break;
                case View.SingleItem:
                    webbrowser.Hide();
                    itemtoolstrip.Hide();
                    singleItemToolStripMenuItem.CheckState = CheckState.Unchecked;
                    break;
            }
            current_view = new_view;
            switch (new_view)
            {
                case View.List:
                    itemsview.Show();
                    listToolStripMenuItem.CheckState = CheckState.Checked;
                    break;
                case View.SingleItem:
                    itemtoolstrip.Show();
                    webbrowser.Show();
                    singleItemToolStripMenuItem.CheckState = CheckState.Checked;
                    break;
            }
            ResumeLayout();
        }

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
            RefreshItemsView();
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
            HashSet<Tuple<string, string>> items_to_remove = new HashSet<Tuple<string, string>>();
            foreach (var kvp in current_items)
            {
                DetailedInfo info = (DetailedInfo)kvp.Value.Tag;
                items_to_remove.Add(Tuple.Create(info.feed_uri, info.id));
            }
            itemsview.BeginUpdate();
            foreach (var info in infos)
            {
                var key = Tuple.Create(info.feed_uri, info.id);
                if (current_items.ContainsKey(key))
                {
                    items_to_remove.Remove(key);
                    continue;
                }
                ListViewItem item = new ListViewItem();
                item.Tag = info;
                item.Text = info.title;
                item.SubItems.Add(info.author);
                item.SubItems.Add(info.timestamp.ToLocalTime().ToString());
                itemsview.Items.Add(item);
                current_items[key] = item;
            }
            foreach (var key in items_to_remove)
            {
                itemsview.Items.Remove(current_items[key]);
                current_items.Remove(key);
            }
            itemsview.EndUpdate();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
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

        private void listToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetView(View.List);
        }

        private void singleItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetView(View.SingleItem);
        }

        private bool GetItemUrlOrData(out string result)
        {
            DetailedInfo summaryinfo = (DetailedInfo)itemsview.SelectedItems[0].Tag;
            last_webbrowser_feedurl = summaryinfo.feed_uri;
            last_webbrowser_id = summaryinfo.id;
            DetailedInfo info;
            if (!cache.TryGetDetailedInfo(summaryinfo.feed_uri, summaryinfo.id, out info))
            {
                result = string.Format("ERROR: Missing detailed item info for {0} {1}", summaryinfo.feed_uri, summaryinfo.id);
                return true;
            }
            string content_uri = info.get_content_uri();
            if (content_uri != null)
            {
                result = content_uri;
                return false;
            }
            string content_html = info.get_content_html();
            if (content_html != null)
            {
                result = content_html;
                return true;
            }
            result = string.Format("ERROR: Couldn't find content for {0} {1}", summaryinfo.feed_uri, summaryinfo.id);
            return true;
        }

        private void DeleteTempHtmlFile()
        {
            if (temp_html_file != null)
            {
                File.Delete(temp_html_file);
                temp_html_file = null;
            }
        }

        private string GetItemUrl()
        {
            DetailedInfo summaryinfo = (DetailedInfo)itemsview.SelectedItems[0].Tag;
            string result;
            if (GetItemUrlOrData(out result))
            {
                if (temp_html_file_feedurl == summaryinfo.feed_uri && temp_html_file_id == summaryinfo.id)
                {
                    return temp_html_file;
                }
                DeleteTempHtmlFile();
                temp_html_file = Path.GetTempFileName();
                using (FileStream f = new FileStream(temp_html_file, FileMode.Open, FileAccess.Write))
                {
                    byte[] bytes = (new UTF8Encoding()).GetBytes(result);
                    f.Write(bytes, 0, bytes.Length);
                }
                return temp_html_file;
            }
            return result;
        }

        private void MaybeUpdateWebbrowser()
        {
            if (!webbrowser.Visible || itemsview.SelectedItems.Count == 0)
                return;
            DetailedInfo summaryinfo = (DetailedInfo)itemsview.SelectedItems[0].Tag;
            if (summaryinfo.feed_uri == last_webbrowser_feedurl && summaryinfo.id == last_webbrowser_id)
                return;

            string data;
            if (GetItemUrlOrData(out data))
            {
                webbrowser.DocumentText = data;
            }
            else
            {
                webbrowser.Navigate(data);
            }
        }

        private void webbrowser_VisibleChanged(object sender, EventArgs e)
        {
            MaybeUpdateWebbrowser();
        }

        private void itemsview_SelectedIndexChanged(object sender, EventArgs e)
        {
            MaybeUpdateWebbrowser();
        }

        private void allitemsbutton_Click(object sender, EventArgs e)
        {
            SetView(View.List);
        }

        private void itemsview_Click(object sender, EventArgs e)
        {
            // SetView(View.SingleItem);
        }

        private void markAllItemsAsReadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cache.GetTag("(unread)").Remove(current_items.Keys);
            RefreshItemsView();
        }

        private void OpenCurrentItemInNewWindow()
        {
            Utils.OpenInDefaultBrowser(GetItemUrl());
        }

        private void openInNewWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenCurrentItemInNewWindow();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DeleteTempHtmlFile();
        }

        private void itemsview_DoubleClick(object sender, EventArgs e)
        {
            OpenCurrentItemInNewWindow();
        }

        private void copyLinkLocationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(GetItemUrl());
        }
    }
}
