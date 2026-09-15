using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class ReportsForm : Form
    {
        Label titleLabel;
        TextBox reportTextBox;
        Button refreshButton;

        public ReportsForm()
        {
            InitializeComponent();

            this.Text = "Cybercrime Reports";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            titleLabel = new Label();
            titleLabel.Text = "CYBERCRIME SYSTEM REPORT";
            titleLabel.Location = new Point(210, 30);
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Times New Roman", 16, FontStyle.Bold);

            reportTextBox = new TextBox();
            reportTextBox.Location = new Point(80, 90);
            reportTextBox.Size = new Size(540, 280);
            reportTextBox.Multiline = true;
            reportTextBox.ReadOnly = true;
            reportTextBox.ScrollBars = ScrollBars.Vertical;
            reportTextBox.Font = new Font("Times New Roman", 12);

            refreshButton = new Button();
            refreshButton.Text = "Generate Report";
            refreshButton.Location = new Point(260, 400);
            refreshButton.Size = new Size(180, 40);
            refreshButton.Font = new Font("Times New Roman", 12);

            refreshButton.Click += RefreshButton_Click;

            this.Controls.Add(titleLabel);
            this.Controls.Add(reportTextBox);
            this.Controls.Add(refreshButton);

            GenerateReport();
        }

        private void GenerateReport()
        {
            int complaintCount = 0;
            int evidenceCount = 0;

            if (File.Exists("complaints.txt"))
            {
                string complaints = File.ReadAllText("complaints.txt");

                if (complaints != "")
                {
                    complaintCount = complaints.Split(
                        new string[] { "Case ID:" },
                        StringSplitOptions.RemoveEmptyEntries
                    ).Length;
                }
            }

            if (File.Exists("evidence.txt"))
            {
                string evidence = File.ReadAllText("evidence.txt");

                if (evidence != "")
                {
                    evidenceCount = evidence.Split(
                        new string[] { "Evidence ID:" },
                        StringSplitOptions.RemoveEmptyEntries
                    ).Length;
                }
            }

            reportTextBox.Text =
                "CYBERCRIME SYSTEM REPORT" + Environment.NewLine +
                "====================================" + Environment.NewLine +
                Environment.NewLine +
                "Total Complaints: " + complaintCount + Environment.NewLine +
                "Total Evidence Files: " + evidenceCount + Environment.NewLine +
                Environment.NewLine +
                "System Status: Active" + Environment.NewLine +
                "Report Generated: " + DateTime.Now + Environment.NewLine;
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            GenerateReport();
        }
    }
}