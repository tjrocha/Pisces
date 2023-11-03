using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Reclamation.TimeSeries.Hydromet;

namespace Reclamation.TimeSeries.Forms.ImportForms
{
    public partial class ImportHydromet : Form
    {
        public ImportHydromet()
        {
            InitializeComponent();

     
                radioButtonDailyAverage.Checked = true;
                radioButtonHyd1.Checked = true;
                this.dateTimePicker2.Value = DateTime.Now.AddDays(-1);

                this.textBox1.Text = Properties.Settings.Default.HydrometInput;

        }

        private void FormImportHydromet_Load(object sender, EventArgs e)
        {
        }

        public TimeInterval TimeInterval
        {
            get {
                if (radioButtonDailyAverage.Checked)
                    return TimeInterval.Daily;
                else if (radioButtonMpoll.Checked)
                    return TimeSeries.TimeInterval.Monthly;

            return TimeInterval.Irregular;
            }
        }

        public bool UseSimpleName
        {
            get { return this.checkBoxSimpleName.Checked; }
        }
        public DateTime T1
        {
            get { return this.dateTimePicker1.Value; }
            set { this.dateTimePicker1.Value = value; }
        }
        public DateTime T2
        {
            get { return this.dateTimePicker2.Value; }
            set { this.dateTimePicker2.Value = value; }
        }

        public string ParameterCode
        {
            get
            {
                string[] tokens = this.textBox1.Text.Trim().Split(new char[] { ' ' });
                if (tokens.Length != 2)
                {
                    return "";
                }
                return tokens[1].Trim();
            }
        }

        
        public string Cbtt
        {
            get{
                string[] tokens = this.textBox1.Text.Trim().Split(new char[] { ' ' });
                if (tokens.Length != 2)
                {
                    return "";
                }
                return tokens[0].Trim();
            }
        }

        public HydrometHost HydrometServer
        {
            get
            {
                if (this.radioButtonHyd1.Checked)
                    return HydrometHost.PN;

                if( this.radioButtonGP.Checked)
                  return HydrometHost.GreatPlains;

                return HydrometHost.PN; // default

            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
           Properties.Settings.Default.HydrometInput = this.textBox1.Text;
        }
    }
}