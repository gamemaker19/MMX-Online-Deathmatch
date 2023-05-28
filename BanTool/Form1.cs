using MMXOnline;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BanTool
{
    public partial class Form1 : Form
    {
        string secretPrefix;
        string dataBlob;
        
        // Must set regions here to be able to use this tool
        static BanToolRegion[] regions = { new BanToolRegion("<Insert IP here", "<Insert region name here>") };

        static MatchmakingQuerier matchmakingQuerier;

        public Form1()
        {
            InitializeComponent();
            secretPrefix = File.ReadAllText("secretPrefix.txt");
            matchmakingQuerier = new MatchmakingQuerier();
            banLengthComboBox.SelectedIndex = 0;
            banTypeComboBox.SelectedIndex = 0;
            regionComboBox.SelectedIndex = 0;
        }

        private BanToolRegion[] getRegions()
        {
            if (regionComboBox.SelectedIndex == 0)
            {
                return regions;
            }
            else if (regionComboBox.SelectedIndex == 1)
            {
                return new BanToolRegion[] { regions[0] };
            }
            else if (regionComboBox.SelectedIndex == 2)
            {
                return new BanToolRegion[] { regions[1] };
            }
            else if (regionComboBox.SelectedIndex == 3)
            {
                return new BanToolRegion[] { regions[2] };
            }

            return regions;
        }

        private void removeMatchBtn_Click(object sender, EventArgs e)
        {
            string regionStr = string.Join(',', getRegions().Select(r => r.name));
            var msgResult = MessageBox.Show($"Warning: you will shutdown all matches in the following region(s): {regionStr}\n\nContinue?", "Remove Matches Confirmation", MessageBoxButtons.OKCancel);
            if (msgResult == DialogResult.OK)
            {
                foreach (var region in getRegions())
                {
                    var result = matchmakingQuerier.send(region.ip, $"{secretPrefix}removeallmatches:{dataBlob}", "removeallmatches");
                    if (string.IsNullOrEmpty(result) || result.StartsWith("Error:"))
                    {
                        MessageBox.Show($"Error when shutting down matches. Try again or in another region(s).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                MessageBox.Show($"Removed all matches in regions(s): {regionStr}", "Success", MessageBoxButtons.OK);
            }
        }

        private void reportFileChooser_Click(object sender, EventArgs e)
        {
            string result = "";
            string fileName = "";
            dataBlob = "";

            labelSuccess.Visible = false;
            labelFail.Visible = false;

            labelReportFile.Visible = false;

            banStatusGroupBox.Visible = false;
            banGroupBox.Visible = false;
            notBannedGroupBox.Visible = false;
            
            banButton.Enabled = true;
            unbanButton.Enabled = true;

            try
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        fileName = dialog.FileName;
                        var fileContents = File.ReadAllText(dialog.FileName);
                        var reportObj = JsonConvert.DeserializeObject<ReportedPlayer>(fileContents);
                        dataBlob = reportObj.dataBlob;
                        if (dataBlob.Length > 5000)
                        {
                            throw new Exception("Datablob too long.");
                        }

                        foreach (var region in getRegions())
                        {
                            result = matchmakingQuerier.send(region.ip, $"{secretPrefix}getbanstatusdatablob:{dataBlob}", "getbanstatusdatablob");
                            if (string.IsNullOrEmpty(result) || result.StartsWith("Error:")) break;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                result = "Error:" + ex.Message;
            }

            result = result ?? "Error:Could not get ban status. The server may be down.";

            if (result.StartsWith("Error:"))
            {
                labelFail.Text = result;
                labelFail.Visible = true;
            }
            else
            {
                labelReportFile.Text = "Report file: " + Path.GetFileName(fileName);
                labelReportFile.Visible = true;
                result = result.RemovePrefix("Success:");
                if (!string.IsNullOrEmpty(result))
                {
                    banStatusGroupBox.Visible = true;
                    var banResponse = JsonConvert.DeserializeObject<BanResponse>(result);
                    labelBanStatus.Text = "Ban Status: " + banResponse.getStatusString();
                    labelBanStatusReason.Text = "Reason: " + banResponse.reason;
                    unbanButton.Visible = true;
                }
                else
                {
                    notBannedGroupBox.Visible = true;
                    banGroupBox.Visible = true;
                }
            }
        }

        private void banButton_Click(object sender, EventArgs e)
        {
            labelSuccess.Visible = false;
            labelFail.Visible = false;

            if (string.IsNullOrWhiteSpace(textBoxBanReason.Text))
            {
                MessageBox.Show("Error: must enter in a ban reason", "Error");
                return;
            }

            /*
            Indefinite
            1 day
            3 days
            1 week
            2 weeks
            1 month
            */
            int days = 0;
            if (banLengthComboBox.SelectedIndex == 1) days = 1;
            if (banLengthComboBox.SelectedIndex == 2) days = 3;
            if (banLengthComboBox.SelectedIndex == 3) days = 7;
            if (banLengthComboBox.SelectedIndex == 4) days = 14;
            if (banLengthComboBox.SelectedIndex == 5) days = 31;

            DateTime? bannedUntil = days == 0 ? null : DateTime.UtcNow.AddDays(days);

            var banRequest = new BanRequest(dataBlob, textBoxBanReason.Text, banTypeComboBox.SelectedIndex, bannedUntil);

            foreach (var region in getRegions())
            {
                string result = matchmakingQuerier.send(region.ip, $"{secretPrefix}bandatablob:{JsonConvert.SerializeObject(banRequest)}", "bandatablob");
                result = result ?? "Error:Could not submit ban. The server may be down.";

                if (result.StartsWith("Error:"))
                {
                    labelFail.Text = result;
                    labelFail.Visible = true;
                    return;
                }
            }

            labelSuccess.Text = "Successfully submited ban.";
            banButton.Enabled = false;
            labelSuccess.Visible = true;
        }

        private void unbanButton_Click(object sender, EventArgs e)
        {
            labelSuccess.Visible = false;
            labelFail.Visible = false;

            foreach (var region in getRegions())
            {
                string result = matchmakingQuerier.send(region.ip, $"{secretPrefix}unbandatablob:{dataBlob}", "unbandatablob");
                result = result ?? "Error:Could not submit unban. The server may be down.";

                if (result.StartsWith("Error:"))
                {
                    labelFail.Text = result;
                    labelFail.Visible = true;
                    return;
                }
            }
            
            labelSuccess.Text = "Successfully unbanned player.";
            unbanButton.Enabled = false;
            labelSuccess.Visible = true;
        }

        private void banStatusGroupBox_Enter(object sender, EventArgs e)
        {

        }
    }
}
