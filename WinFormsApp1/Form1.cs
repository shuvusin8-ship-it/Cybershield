using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void button6_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                LoginForm loginForm = new LoginForm();
                loginForm.Show();

                this.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ComplaintForm complaintForm = new ComplaintForm();
            complaintForm.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ViewCasesForm viewCasesForm = new ViewCasesForm();
            viewCasesForm.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            AddEvidenceForm addEvidenceForm = new AddEvidenceForm();
            addEvidenceForm.ShowDialog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            VerifyEvidenceForm verifyEvidenceForm = new VerifyEvidenceForm();
            verifyEvidenceForm.ShowDialog();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ReportsForm reportsForm = new ReportsForm();
            reportsForm.ShowDialog();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            CheckStatusForm checkStatusForm = new CheckStatusForm();
            checkStatusForm.ShowDialog();
        }
    }
}
