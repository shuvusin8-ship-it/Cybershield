using System;
using System.IO;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class AddEvidenceForm : Form
    {
        Label titleLabel;
        Label fileLabel;
        TextBox fileTextBox;
        Button selectButton;
        Button saveButton;

        public AddEvidenceForm()
        {
            InitializeComponent();

            this.Text = "Add Cybercrime Evidence";
            this.Size = new Size(700, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            titleLabel = new Label();
            titleLabel.Text = "ADD DIGITAL EVIDENCE";
            titleLabel.Location = new Point(230, 30);
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

            saveButton = new Button();
            saveButton.Text = "Save Evidence";
            saveButton.Location = new Point(270, 180);
            saveButton.Size = new Size(160, 40);
            saveButton.Font = new Font("Times New Roman", 12);

            selectButton.Click += SelectButton_Click;
            saveButton.Click += SaveButton_Click;

            this.Controls.Add(titleLabel);
            this.Controls.Add(fileLabel);
            this.Controls.Add(fileTextBox);
            this.Controls.Add(selectButton);
            this.Controls.Add(saveButton);
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter =
                "All Files|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileTextBox.Text = openFileDialog.FileName;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (fileTextBox.Text == "")
            {
                MessageBox.Show("Please select an evidence file.");
                return;
            }

            string evidenceId = "EVD" + DateTime.Now.ToString("yyyyMMddHHmmss");

            string hash;

            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream stream = File.OpenRead(fileTextBox.Text))
                {
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    hash = Convert.ToHexString(hashBytes);
                }
            }

            string evidenceData =
                "Evidence ID: " + evidenceId + Environment.NewLine +
                "File Name: " + Path.GetFileName(fileTextBox.Text) + Environment.NewLine +
                "File Path: " + fileTextBox.Text + Environment.NewLine +
                "SHA-256 Hash: " + hash + Environment.NewLine +
                "Date: " + DateTime.Now + Environment.NewLine +
                "----------------------------------------" + Environment.NewLine;

            File.AppendAllText("evidence.txt", evidenceData);

            MessageBox.Show(
                "Evidence saved successfully!" +
                Environment.NewLine +
                "Evidence ID: " + evidenceId,
                "Success"
            );
        }
    }
}
