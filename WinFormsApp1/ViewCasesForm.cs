using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class ViewCasesForm : Form
    {
        TextBox casesTextBox;
        Button refreshButton;

        public ViewCasesForm()
        {
            InitializeComponent();

            this.Text = "Admin - View Cases";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            Label titleLabel = new Label();
            titleLabel.Text = "REGISTERED CYBERCRIME CASES";
            titleLabel.Location = new Point(230, 30);
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Times New Roman", 16, FontStyle.Bold);

            casesTextBox = new TextBox();
            casesTextBox.Location = new Point(50, 80);
            casesTextBox.Size = new Size(680, 400);
            casesTextBox.Multiline = true;
            casesTextBox.ScrollBars = ScrollBars.Vertical;
            casesTextBox.ReadOnly = true;
            casesTextBox.Font = new Font("Times New Roman", 11);

            refreshButton = new Button();
            refreshButton.Text = "Refresh Cases";
            refreshButton.Location = new Point(300, 510);
            refreshButton.Size = new Size(180, 40);
            refreshButton.Font = new Font("Times New Roman", 12);

            refreshButton.Click += RefreshButton_Click;

            this.Controls.Add(titleLabel);
            this.Controls.Add(casesTextBox);
            this.Controls.Add(refreshButton);

            LoadCases();
        }

        private void LoadCases()
        {
            if (File.Exists("complaints.txt"))
            {
                casesTextBox.Text = File.ReadAllText("complaints.txt");
            }
            else
            {
                casesTextBox.Text = "No complaints have been registered yet.";
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadCases();
        }
    }
}