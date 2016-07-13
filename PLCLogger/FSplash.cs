using System;
using System.Windows.Forms;

namespace PLCLogger
{
    public partial class FSplash : Form
    {
        public FSplash()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Close();
        }

    }
}
