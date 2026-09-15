using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class LoginForm : Form
    {
        Label titleLabel;
        Label usernameLabel;
        Label passwordLabel;

        TextBox usernameTextBox;
        TextBox passwordTextBox;

        Button loginButton;

        public LoginForm()
        {
            InitializeComponent();

            this.Text = "Admin Login";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            titleLabel = new Label();
            titleLabel.Text = "ADMIN LOGIN";
            titleLabel.Location = new Point(175, 40);
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Times New Roman", 18, FontStyle.Bold);

            usernameLabel = new Label();
            usernameLabel.Text = "Username:";
            usernameLabel.Location = new Point(70, 120);
            usernameLabel.AutoSize = true;
            usernameLabel.Font = new Font("Times New Roman", 12);

            usernameTextBox = new TextBox();
            usernameTextBox.Location = new Point(180, 117);
            usernameTextBox.Size = new Size(220, 25);

            passwordLabel = new Label();
            passwordLabel.Text = "Password:";
            passwordLabel.Location = new Point(70, 170);
            passwordLabel.AutoSize = true;
            passwordLabel.Font = new Font("Times New Roman", 12);

            passwordTextBox = new TextBox();
            passwordTextBox.Location = new Point(180, 167);
            passwordTextBox.Size = new Size(220, 25);
            passwordTextBox.PasswordChar = '*';

            loginButton = new Button();
            loginButton.Text = "Login";
            loginButton.Location = new Point(180, 230);
            loginButton.Size = new Size(120, 40);
            loginButton.Font = new Font("Times New Roman", 12);

            loginButton.Click += LoginButton_Click;

            this.Controls.Add(titleLabel);
            this.Controls.Add(usernameLabel);
            this.Controls.Add(usernameTextBox);
            this.Controls.Add(passwordLabel);
            this.Controls.Add(passwordTextBox);
            this.Controls.Add(loginButton);
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            if (usernameTextBox.Text == "admin" &&
                passwordTextBox.Text == "admin123")
            {
                MessageBox.Show("Login successful!", "Success");

                Form1 dashboard = new Form1();
                dashboard.Show();

                this.Hide();
            }
            else
            {
                MessageBox.Show(
                    "Invalid username or password.",
                    "Login Failed"
                );
            }
        }
    }
}