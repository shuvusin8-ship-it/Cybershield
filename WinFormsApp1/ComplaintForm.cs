using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class ComplaintForm : Form
    {
        Label nameLabel;
        TextBox nameTextBox;

        Label emailLabel;
        TextBox emailTextBox;

        Label crimeLabel;
        ComboBox crimeComboBox;

        Label dateLabel;
        DateTimePicker datePicker;

        Label descriptionLabel;
        TextBox descriptionTextBox;

        Button submitButton;

        public ComplaintForm()
        {
            InitializeComponent();

            this.Text = "Cybercrime Complaint";
            this.Size = new Size(700, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            nameLabel = new Label();
            nameLabel.Text = "Name:";
            nameLabel.Location = new Point(50, 50);
            nameLabel.AutoSize = true;
            nameLabel.Font = new Font("Times New Roman", 12);

            nameTextBox = new TextBox();
            nameTextBox.Location = new Point(180, 47);
            nameTextBox.Size = new Size(400, 25);

            emailLabel = new Label();
            emailLabel.Text = "Email:";
            emailLabel.Location = new Point(50, 100);
            emailLabel.AutoSize = true;
            emailLabel.Font = new Font("Times New Roman", 12);

            emailTextBox = new TextBox();
            emailTextBox.Location = new Point(180, 97);
            emailTextBox.Size = new Size(400, 25);

            crimeLabel = new Label();
            crimeLabel.Text = "Crime Type:";
            crimeLabel.Location = new Point(50, 150);
            crimeLabel.AutoSize = true;
            crimeLabel.Font = new Font("Times New Roman", 12);

            crimeComboBox = new ComboBox();
            crimeComboBox.Location = new Point(180, 147);
            crimeComboBox.Size = new Size(400, 25);

            crimeComboBox.Items.Add("Online Fraud");
            crimeComboBox.Items.Add("Phishing");
            crimeComboBox.Items.Add("Hacking");
            crimeComboBox.Items.Add("Identity Theft");
            crimeComboBox.Items.Add("Cyberbullying");
            crimeComboBox.Items.Add("Fake Profile");

            dateLabel = new Label();
            dateLabel.Text = "Date:";
            dateLabel.Location = new Point(50, 200);
            dateLabel.AutoSize = true;
            dateLabel.Font = new Font("Times New Roman", 12);

            datePicker = new DateTimePicker();
            datePicker.Location = new Point(180, 197);
            datePicker.Size = new Size(200, 25);

            descriptionLabel = new Label();
            descriptionLabel.Text = "Description:";
            descriptionLabel.Location = new Point(50, 250);
            descriptionLabel.AutoSize = true;
            descriptionLabel.Font = new Font("Times New Roman", 12);

            descriptionTextBox = new TextBox();
            descriptionTextBox.Location = new Point(180, 247);
            descriptionTextBox.Size = new Size(400, 120);
            descriptionTextBox.Multiline = true;

            submitButton = new Button();
            submitButton.Text = "Submit Complaint";
            submitButton.Location = new Point(250, 410);
            submitButton.Size = new Size(180, 40);
            submitButton.Font = new Font("Times New Roman", 12);

            submitButton.Click += SubmitButton_Click;

            this.Controls.Add(nameLabel);
            this.Controls.Add(nameTextBox);
            this.Controls.Add(emailLabel);
            this.Controls.Add(emailTextBox);
            this.Controls.Add(crimeLabel);
            this.Controls.Add(crimeComboBox);
            this.Controls.Add(dateLabel);
            this.Controls.Add(datePicker);
            this.Controls.Add(descriptionLabel);
            this.Controls.Add(descriptionTextBox);
            this.Controls.Add(submitButton);
        }

        private void SubmitButton_Click(object sender, EventArgs e)
        {
            if (nameTextBox.Text == "" ||
                emailTextBox.Text == "" ||
                crimeComboBox.Text == "" ||
                descriptionTextBox.Text == "")
            {
                MessageBox.Show("Please fill all the details.");
                return;
            }

            string caseId = "CASE" + DateTime.Now.ToString("yyyyMMddHHmmss");

            string complaint =
                "Case ID: " + caseId + Environment.NewLine +
                "Name: " + nameTextBox.Text + Environment.NewLine +
                "Email: " + emailTextBox.Text + Environment.NewLine +
                "Crime Type: " + crimeComboBox.Text + Environment.NewLine +
                "Date: " + datePicker.Value.ToShortDateString() + Environment.NewLine +
                "Status: Submitted" + Environment.NewLine +
                "Description: " + descriptionTextBox.Text + Environment.NewLine +
                "----------------------------------------" + Environment.NewLine;

            File.AppendAllText("complaints.txt", complaint);

            MessageBox.Show(
                "Complaint submitted successfully!" +
                Environment.NewLine +
                "Your Case ID is: " + caseId,
                "Complaint Submitted"
            );
        }
    }
}