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
            InitializeComponent();
        }

        public Cache cache;

        bool refreshing;
        int refresh_progress;
        int refresh_total;

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

        public void Refresh()
        {
            if (!refreshing)
            {
                refreshing = true;
                refresh_progress = -1;
                refresh_total = 0;
                Thread refresh_thread = new Thread(new ThreadStart(RefreshThread));
                refresh_thread.Start();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Refresh();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Refresh();
        }
    }
}
