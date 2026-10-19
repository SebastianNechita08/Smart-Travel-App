using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace II
{
    public sealed partial class LoginPage : Page
    {
        private string dbPath = Path.Combine(AppContext.BaseDirectory, "users.db");

        public LoginPage()
        {
            this.InitializeComponent();
            InitializeDatabase();

            // Date de test (le poți șterge când lansezi aplicația)
            UsernameBox.Text = "admin";
            PasswordBox.Password = "123";
        }

        private void InitializeDatabase()
        {
            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();
                string tableCommand = "CREATE TABLE IF NOT EXISTS Users (Username TEXT PRIMARY KEY, Email TEXT, Password TEXT);";
                var createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteNonQuery();
            }
        }

        #region LOGICA AUTENTIFICARE (LOGIN)

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string user = UsernameBox.Text;
            string pass = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                ShowStatus("Introduceți utilizatorul și parola.", true);
                return;
            }

            string loginResult = CheckLoginStatus(user, pass);

            if (loginResult == "Success")
            {
                // SALVĂM USERUL AICI!
                UserSession.CurrentUsername = user;

                this.Frame.Navigate(typeof(Search_page));
            }
            else
            {
                ShowStatus(loginResult, true);
            }
        }

        private string CheckLoginStatus(string user, string pass)
        {
            if (user == "admin" && pass == "123") return "Success";

            if (!File.Exists(dbPath)) return "Baza de date lipsește.";

            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();
                var cmd = new SqliteCommand("SELECT Password FROM Users WHERE Username = @user", db);
                cmd.Parameters.AddWithValue("@user", user);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetString(0) == pass ? "Success" : "Parola este greșită.";
                    }
                    return "Utilizatorul nu există.";
                }
            }
        }
        #endregion

        #region LOGICA INREGISTRARE (REGISTER)

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string newEmail = RegEmailBox.Text;
            string newUser = RegUserBox.Text;
            string newPass = RegPassBox.Password;
            string confirmPass = RegPassConfirmBox.Password;

            // 1. Verificare câmpuri goale
            if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(newUser) || string.IsNullOrWhiteSpace(newPass))
            {
                ShowRegStatus("Vă rugăm să completați toate câmpurile.", true);
                return;
            }

            // 2. Verificări detaliate pentru E-MAIL
            if (!newEmail.Contains("@"))
            {
                ShowRegStatus("Email incorect.", true);
                return;
            }
            if (!newEmail.Contains("."))
            {
                ShowRegStatus("Email incorect: domeniul trebuie să conțină un punct (ex: .com, .ro).", true);
                return;
            }

            // 3. Verificare coincidență parole
            if (newPass != confirmPass)
            {
                ShowRegStatus("Eroare: Parolele introduse nu coincid!", true);
                return;
            }

            // 4. Verificare detaliată pentru PAROLĂ
            string passwordError = GetPasswordErrorMessage(newPass);
            if (passwordError != null)
            {
                ShowRegStatus(passwordError, true);
                return;
            }

            try
            {
                // 5. Salvare în baza de date
                using (var db = new SqliteConnection($"Filename={dbPath}"))
                {
                    db.Open();
                    var insertCmd = new SqliteCommand("INSERT INTO Users (Username, Email, Password) VALUES (@user, @email, @pass)", db);
                    insertCmd.Parameters.AddWithValue("@user", newUser);
                    insertCmd.Parameters.AddWithValue("@email", newEmail);
                    insertCmd.Parameters.AddWithValue("@pass", newPass);
                    insertCmd.ExecuteNonQuery();
                }

                // 6. Afișare CĂSUȚĂ DE SUCCES
                ContentDialog successDialog = new ContentDialog
                {
                    Title = "Cont Creat cu Succes! 🎉",
                    Content = "Felicitări! Contul tău a fost creat. Apasă OK pentru a te autentifica.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();

                // 7. Resetare formular și întoarcere la Login
                RegEmailBox.Text = "";
                RegUserBox.Text = "";
                RegPassBox.Password = "";
                RegPassConfirmBox.Password = "";

                SwitchToLogin_Click(null, null);
                UsernameBox.Text = newUser; // Punem numele direct în căsuța de login ca să fie mai ușor
                ShowStatus("Introdu parola pentru a intra în cont.", false); // Aici folosim ShowStatus pentru că ne-am întors la Login
            }
            catch (SqliteException ex)
            {
                if (ex.SqliteErrorCode == 19)
                {
                    ShowRegStatus("Eroare: Acest nume de utilizator este deja folosit! Alege altul.", true);
                }
                else
                {
                    ShowRegStatus("Eroare baza de date: " + ex.Message, true);
                }
            }
        }

        // Funcție care spune EXACT ce problemă are parola
        private string GetPasswordErrorMessage(string password)
        {
            if (password.Length < 8)
                return "Parola este prea scurtă! Trebuie să aibă minim 8 caractere.";

            if (!Regex.IsMatch(password, @"[a-z]"))
                return "Parola trebuie să conțină cel puțin o literă mică (a-z).";

            if (!Regex.IsMatch(password, @"[A-Z]"))
                return "Parola trebuie să conțină cel puțin o literă MARE (ex: A, B, C).";

            if (!Regex.IsMatch(password, @"[0-9]"))
                return "Parola trebuie să conțină cel puțin o cifră (0-9).";

            if (!Regex.IsMatch(password, @"[\W_]"))
                return "Parola trebuie să conțină cel puțin un simbol special (ex: !, @, #, $).";

            return null;
        }

        #endregion

        #region ANIMATII SI SCHIMBARE FORMULARE

        private void SwitchToRegister_Click(object sender, RoutedEventArgs e)
        {
            double distance = MainGrid.ActualWidth - FormPanel.ActualWidth;

            AnimateTransform(FormTransform, -distance);
            AnimateTransform(BrandingTransform, distance - 50);

            LoginForm.Visibility = Visibility.Collapsed;
            RegisterForm.Visibility = Visibility.Visible;
            StatusMessage.Text = "";
            if (RegStatusMessage != null) RegStatusMessage.Text = ""; // Curățăm și erorile de la register
        }

        private void SwitchToLogin_Click(object sender, RoutedEventArgs e)
        {
            AnimateTransform(FormTransform, 0);
            AnimateTransform(BrandingTransform, 0);

            RegisterForm.Visibility = Visibility.Collapsed;
            LoginForm.Visibility = Visibility.Visible;
            StatusMessage.Text = "";
            if (RegStatusMessage != null) RegStatusMessage.Text = ""; // Curățăm și erorile de la register
        }

        private void AnimateTransform(TranslateTransform transform, double toValue)
        {
            Storyboard sb = new Storyboard();
            DoubleAnimation anim = new DoubleAnimation
            {
                To = toValue,
                Duration = new Duration(TimeSpan.FromSeconds(0.7)),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseInOut, Exponent = 5 }
            };

            Storyboard.SetTarget(anim, transform);
            Storyboard.SetTargetProperty(anim, "X");
            sb.Children.Add(anim);
            sb.Begin();
        }

        // Aceasta scrie erori pe ecranul de Login
        private void ShowStatus(string message, bool isError)
        {
            StatusMessage.Text = message;
            StatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                isError ? Microsoft.UI.Colors.Red : Microsoft.UI.Colors.LightGreen);
        }

        // Aceasta scrie erori pe ecranul de Înregistrare
        private void ShowRegStatus(string message, bool isError)
        {
            if (RegStatusMessage != null)
            {
                RegStatusMessage.Text = message;
                RegStatusMessage.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    isError ? Microsoft.UI.Colors.Red : Microsoft.UI.Colors.LightGreen);
            }
        }
        #endregion
    }
}