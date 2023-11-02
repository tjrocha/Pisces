using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Reclamation.TimeSeries.Hydromet;
using Reclamation.Core;

namespace Reclamation.TimeSeries.Forms.Hydromet
{
    public partial class ServerSelection : UserControl
    {
        public string CustomIP 
        { 
            get { return this.textBoxCustomSource.Text; } 
            set { this.textBoxCustomSource.Text = value; } 
        }

        public ServerSelection()
        {
            InitializeComponent();
            ReadSettings();
        }

        private void SaveToUserPref()
        {
            this.textBoxCustomSource.Enabled = false;

            if (this.radioButtonBoiseLinux.Checked)
            {
                UserPreference.Save("HydrometServer", HydrometHost.PNLinux.ToString());
            }
            else if (this.radioButtonYakHydromet.Checked)
            {
                UserPreference.Save("HydrometServer", HydrometHost.YakimaLinux.ToString());
            }
            else if (this.radioButtonGP.Checked)
            {
                UserPreference.Save("HydrometServer", HydrometHost.GreatPlains.ToString());
            }
            else if (this.radioButtonCustomSource.Checked)
            {
                UserPreference.Save("HydrometServer", HydrometHost.Custom.ToString());
                this.textBoxCustomSource.Enabled = true;
            }

            
            UserPreference.Save("TimeSeriesDatabaseName", this.textBoxDbName.Text);
        }

        private void ReadSettings()
        {
            var svr = HydrometInfoUtility.HydrometServerFromPreferences();
            this.textBoxCustomSource.Enabled = false;

            // retiring PN 
            if (svr == HydrometHost.PNLinux)
            {
                this.radioButtonBoiseLinux.Checked = true;
            }
            else if (svr == HydrometHost.YakimaLinux)
            {
                this.radioButtonYakHydromet.Checked = true;
            }
            else if (svr == HydrometHost.GreatPlains)
            {
                this.radioButtonGP.Checked = true;
            }
            else if (svr == HydrometHost.Custom)
            {
                this.radioButtonCustomSource.Checked = true;
                this.textBoxCustomSource.Enabled = true;

                CustomIP = UserPreference.Lookup("HydrometCustomServer", "");
            }

            this.textBoxDbName.Text = UserPreference.Lookup("TimeSeriesDatabaseName", "timeseries");
        }

        private void serverChanged(object sender, EventArgs e)
        {
            SaveToUserPref();
        }


        private void textBoxCustomSource_TextChanged(object sender, EventArgs e)
        {
            UserPreference.Save("HydrometCustomServer", CustomIP);
        }

        private void textBoxDbName_TextChanged(object sender, EventArgs e)
        {
            UserPreference.Save("TimeSeriesDatabaseName", this.textBoxDbName.Text);
        }
    }
}
