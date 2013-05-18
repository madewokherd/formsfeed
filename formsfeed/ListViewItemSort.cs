using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FormsFeed;

namespace FormsFeed.WinForms
{
    class ListViewItemSort : IComparer
    {
        public ListViewItemSort()
        {
        }

        public int Compare(object x, object y)
        {
            DateTime a = ((DetailedInfo)((ListViewItem)x).Tag).timestamp;
            DateTime b = ((DetailedInfo)((ListViewItem)y).Tag).timestamp;
            return DateTime.Compare(b, a);
        }
    }
}
