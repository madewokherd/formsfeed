using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using FormsFeed;

namespace FormsFeed.WinForms
{
    public partial class NewTagDialog : Form
    {
        Cache cache;
        Tuple<string, string> item_id;

        public NewTagDialog(Cache cache, Tuple<string,string> item_id)
        {
            this.cache = cache;
            this.item_id = item_id;
            InitializeComponent();
        }

        private void tagnametext_TextChanged(object sender, EventArgs e)
        {
            okbutton.Enabled = FormsFeed.Tag.IsValidName(cache, tagnametext.Text);
        }
    }
}
