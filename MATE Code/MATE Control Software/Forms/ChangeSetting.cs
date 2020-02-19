using System;
using System.Threading;
using System.Windows.Forms;

namespace Linda
{
    public partial class ChangeSetting : Form
    {
        public ChangeSetting()
        {
            InitializeComponent();
            maskedTextBox1.Text = (Properties.Settings.Default.joy1deadzoneX.ToString());
            maskedTextBox2.Text = (Properties.Settings.Default.joy1deadzoneYa.ToString());
            maskedTextBox3.Text = (Properties.Settings.Default.joy1deadzoneYb.ToString());
            maskedTextBox4.Text = (Properties.Settings.Default.joy1deadzoneRz.ToString());
            maskedTextBox5.Text = (Properties.Settings.Default.joy1deadzoneS.ToString());

            maskedTextBox6.Text = (Properties.Settings.Default.joy2deadzoneX.ToString());
            maskedTextBox7.Text = (Properties.Settings.Default.joy2deadzoneY.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.joy1deadzoneX = (Convert.ToDouble(maskedTextBox1.Text));
            Properties.Settings.Default.joy1deadzoneYa = (Convert.ToDouble(maskedTextBox2.Text));
            Properties.Settings.Default.joy1deadzoneYb = (Convert.ToDouble(maskedTextBox3.Text));
            Properties.Settings.Default.joy1deadzoneRz = (Convert.ToDouble(maskedTextBox4.Text));
            Properties.Settings.Default.joy1deadzoneS = (Convert.ToDouble(maskedTextBox5.Text));


            Properties.Settings.Default.joy2deadzoneX = (Convert.ToDouble(maskedTextBox6.Text));
            Properties.Settings.Default.joy2deadzoneY = (Convert.ToDouble(maskedTextBox7.Text));

            Properties.Settings.Default.Save();

            Thread.Sleep(250);

            this.Close();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
