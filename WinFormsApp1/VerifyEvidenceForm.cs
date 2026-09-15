using System;
using System.IO;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class VerifyEvidenceForm : Form
    {
        Label titleLabel;
        Label fileLabel;
        TextBox fileTextBox;
        Button selectButton;
        Button verifyButton;

        public VerifyEvidenceForm()
        {
            InitializeComponent();

            this.Text = "Verify Digital Evidence";
            this.Size = new Size(700, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            titleLabel = new Label();
            titleLabel.Text = "VERIFY DIGITAL EVIDENCE";
            titleLabel.Location = new Point(220, 30);
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Times New Roman", 16, FontStyle.Bold);

            fileLabel = new Label();
            fileLabel.Text = "Evidence File:";
            fileLabel.Location = new Point(50, 100);
            fileLabel.AutoSize = true;
            fileLabel.Font = new Font("Times New Roman", 12);

            fileTextBox = new TextBox();
            fileTextBox.Location = new Point(170, 97);
            fileTextBox.Size = new Size(350, 25);
            fileTextBox.ReadOnly = true;

            selectButton = new Button();
            selectButton.Text = "Select File";
            selectButton.Location = new Point(530, 95);
            selectButton.Size = new Size(100, 30);

            verifyButton = new Button();
            verifyButton.Text = "Verify Evidence";
            verifyButton.Location = new Point(260, 180);
            verifyButton.Size = new Size(180, 40);
            verifyButton.Font = new Font("Times New Roman", 12);

            selectButton.Click += SelectButton_Click;
            verifyButton.Click += VerifyButton_Click;

            this.Controls.Add(titleLabel);
            this.Controls.Add(fileLabel);
            this.Controls.Add(fileTextBox);
            this.Controls.Add(selectButton);
            this.Controls.Add(verifyButton);
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "All Files|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileTextBox.Text = openFileDialog.FileName;
            }
        }

        private void VerifyButton_Click(object sender, EventArgs e)
        {
            if (fileTextBox.Text == "")
            {
                MessageBox.Show("Please select an evidence file.");
                return;
            }

            if (!File.Exists("evidence.txt"))
            {
                MessageBox.Show("No saved evidence was found.");
                return;
            }

            string currentHash;

            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream stream = File.OpenRead(fileTextBox.Text))
                {
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    currentHash = Convert.ToHexString(hashBytes);
                }
            }

            string evidenceData = File.ReadAllText("evidence.txt");

            if (evidenceData.Contains(currentHash))
            {
                MessageBox.Show(
                    "EVIDENCE VERIFIED!" +
                    Environment.NewLine +
                    "The file has not been modified.",
                    "Verification Result"
                );
            }
            else
            {
                MessageBox.Show(
                    "VERIFICATION FAILED!" +
                    Environment.NewLine +
                    "The file may have been modified.",
                    "Verification Result"
                );
            }
        }
    }
}