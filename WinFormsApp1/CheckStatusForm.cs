using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class CheckStatusForm : Form
    {
        Label titleLabel;
        Label caseIdLabel;
        TextBox caseIdTextBox;
        Button checkButton;
        TextBox resultTextBox;

        public CheckStatusForm()
        {
            InitializeComponent();

            this.Text = "Check Case Status";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            titleLabel = new Label();
            titleLabel.Text = "CHECK CASE STATUS";
            titleLabel.Location = new Point(240, 30);
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Times New Roman", 16, FontStyle.Bold);

            caseIdLabel = new Label();
            caseIdLabel.Text = "Enter Case ID:";
            caseIdLabel.Location = new Point(70, 100);
            caseIdLabel.AutoSize = true;
            caseIdLabel.Font = new Font("Times New Roman", 12);

            caseIdTextBox = new TextBox();
            caseIdTextBox.Location = new Point(200, 97);
            caseIdTextBox.Size = new Size(280, 25);

            checkButton = new Button();
            checkButton.Text = "Check Status";
            checkButton.Location = new Point(270, 150);
            checkButton.Size = new Size(160, 40);
            checkButton.Font = new Font("Times New Roman", 12);

            resultTextBox = new TextBox();
            resultTextBox.Location = new Point(70, 220);
            resultTextBox.Size = new Size(540, 180);
            resultTextBox.Multiline = true;
            resultTextBox.ReadOnly = true;
            resultTextBox.ScrollBars = ScrollBars.Vertical;
            resultTextBox.Font = new Font("Times New Roman", 12);

            checkButton.Click += CheckButton_Click;

            this.Controls.Add(titleLabel);
            this.Controls.Add(caseIdLabel);
            this.Controls.Add(caseIdTextBox);
            this.Controls.Add(checkButton);
            this.Controls.Add(resultTextBox);
        }

        private void CheckButton_Click(object sender, EventArgs e)
        {
            string caseId = caseIdTextBox.Text.Trim();

            if (caseId == "")
            {
                MessageBox.Show("Please enter your Case ID.");
                return;
            }

            if (!File.Exists("complaints.txt"))
            {
                MessageBox.Show("No complaint records found.");
                return;
            }

            string complaints = File.ReadAllText("complaints.txt");

            int casePosition = complaints.IndexOf("Case ID: " + caseId);

            if (casePosition == -1)
            {
                resultTextBox.Text = "Case ID not found.";
                return;
            }

            int nextCasePosition = complaints.IndexOf(
                "----------------------------------------",
                casePosition
            );

            if (nextCasePosition == -1)
            {
                nextCasePosition = complaints.Length;
            }

            string caseData = complaints.Substring(
                casePosition,
                nextCasePosition - casePosition
            );

            resultTextBox.Text = caseData;
        }
    }
}
