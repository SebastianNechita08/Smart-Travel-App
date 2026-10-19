using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Data.Sqlite;
using System.IO;
using System;
using System.Text.RegularExpressions;

namespace II
{
    public sealed partial class RegisterPage : Page
    {
        private string dbPath = Path.Combine(AppContext.BaseDirectory, "users.db");

        public RegisterPage()
        {
            this.InitializeComponent();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();
                string tableCommand = "CREATE TABLE IF NOT EXISTS Users (Username TEXT PRIMARY KEY, Email TEXT, Password TEXT)";
                var createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteNonQuery();
            }
        }

        private async void RegisterSubmit_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text;
            string user = UsernameBox.Text;
            string pass = PasswordBox.Password;
            string confirmPass = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                StatusMessage.Text = "Toate campurile sunt obligatorii."; return;
            }
            if (!email.Contains("@") || !email.Contains("."))
            {
                StatusMessage.Text = "Introduceti o adresa de email valida."; return;
            }
            if (pass != confirmPass)
            {
                StatusMessage.Text = "Parolele nu coincid."; return;
            }
            if (!IsPasswordComplex(pass))
            {
                StatusMessage.Text = "Parola: minim 8 caractere, o litera mica, o majuscula, o cifra si un simbol special."; return;
            }

            try
            {
                using (var db = new SqliteConnection($"Filename={dbPath}"))
                {
                    db.Open();
                    var insertCommand = new SqliteCommand("INSERT INTO Users (Username, Email, Password) VALUES (@user, @email, @pass)", db);
                    insertCommand.Parameters.AddWithValue("@user", user);
                    insertCommand.Parameters.AddWithValue("@email", email);
                    insertCommand.Parameters.AddWithValue("@pass", pass);
                    insertCommand.ExecuteNonQuery();
                }

                ContentDialog successDialog = new ContentDialog
                {
                    Title = "Cont Creat cu Succes!",
                    Content = "Felicitari! Contul tau a fost salvat. Apasa OK pentru a te autentifica.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await successDialog.ShowAsync();
                this.Frame.Navigate(typeof(LoginPage));
            }
            catch (SqliteException ex)
            {
                if (ex.SqliteErrorCode == 19) StatusMessage.Text = "Acest Nume de Utilizator exista deja!";
                else StatusMessage.Text = "Eroare baza de date: " + ex.Message;
            }
        }

        private bool IsPasswordComplex(string password)
        {
            if (password.Length < 8) return false;
            if (!Regex.IsMatch(password, @"[a-z]")) return false;
            if (!Regex.IsMatch(password, @"[A-Z]")) return false;
            if (!Regex.IsMatch(password, @"[0-9]")) return false;
            if (!Regex.IsMatch(password, @"[\W_]")) return false;
            return true;
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LoginPage));
        }
    }
}