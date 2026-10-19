using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel; // Obligatoriu pentru ObservableCollection
using System.IO;

namespace II
{
    public class Review
    {
        public string Username { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string PhotoUrl { get; set; }
        public string RatingText => $"Nota: {Rating}/10 ⭐️";
    }

    public sealed partial class DetailsPage : Page
    {
        private Attraction _currentAttraction;
        private string dbPath = Path.Combine(AppContext.BaseDirectory, "users.db");

        // Lista care spune automat ecranului să se actualizeze
        private ObservableCollection<Review> _recenziiPeEcran = new ObservableCollection<Review>();

        public DetailsPage()
        {
            this.InitializeComponent();
            InitializeDatabase();

            // Legăm lista o singură dată la elementul din XAML
            ReviewsList.ItemsSource = _recenziiPeEcran;
        }

        private void InitializeDatabase()
        {
            // Am adăugat setările de Cache ca să prevenim eroarea "Database is locked"
            using (var db = new SqliteConnection($"Filename={dbPath};Mode=ReadWriteCreate;Cache=Shared"))
            {
                db.Open();
                var command = new SqliteCommand(
                    "CREATE TABLE IF NOT EXISTS Reviews (Id INTEGER PRIMARY KEY AUTOINCREMENT, AttractionName TEXT, City TEXT, Username TEXT, Rating INTEGER, Comment TEXT, PhotoUrl TEXT);", db);
                command.ExecuteNonQuery();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Attraction selectedAttraction)
            {
                _currentAttraction = selectedAttraction;

                TitleText.Text = selectedAttraction.Name;
                DescriptionText.Text = selectedAttraction.Description;

                if (!string.IsNullOrEmpty(selectedAttraction.ImageUrl))
                {
                    HeroImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(selectedAttraction.ImageUrl));
                }
                else
                {
                    HeroImage.Source = null;
                }

                LoadReviews();
            }
        }

        private void LoadReviews()
        {
            // Golim ce era pe ecran înainte să punem noile recenzii
            _recenziiPeEcran.Clear();

            try
            {
                using (var db = new SqliteConnection($"Filename={dbPath};Mode=ReadWriteCreate;Cache=Shared"))
                {
                    db.Open();

                    var cmd = new SqliteCommand("SELECT Username, Rating, Comment, PhotoUrl FROM Reviews WHERE AttractionName = @name AND City = @city", db);

                    // .Trim() curăță spațiile greșite primite de la HERE API
                    cmd.Parameters.AddWithValue("@name", _currentAttraction.Name.Trim());
                    cmd.Parameters.AddWithValue("@city", _currentAttraction.City.Trim());

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _recenziiPeEcran.Add(new Review
                            {
                                Username = reader.GetString(0),
                                Rating = reader.GetInt32(1),
                                Comment = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                PhotoUrl = reader.IsDBNull(3) ? "" : reader.GetString(3)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Eroare la încărcarea recenziilor: " + ex.Message);
            }
        }

        private void SaveReview_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CommentBox.Text)) return;

            int rating = (int)RatingSlider.Value;
            string comment = CommentBox.Text;
            string photo = UserPhotoBox.Text;

            // Conversie link Google Drive
            if (!string.IsNullOrWhiteSpace(photo) && photo.Contains("drive.google.com"))
            {
                if (photo.Contains("/file/d/"))
                {
                    try
                    {
                        int startPos = photo.IndexOf("/file/d/") + 8;
                        int endPos = photo.IndexOf("/", startPos);

                        if (endPos == -1)
                        {
                            endPos = photo.IndexOf("?", startPos);
                        }
                        if (endPos == -1)
                        {
                            endPos = photo.Length;
                        }

                        string fileId = photo.Substring(startPos, endPos - startPos);
                        photo = $"https://drive.google.com/uc?id={fileId}";
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Eroare la procesarea link-ului Google Drive: " + ex.Message);
                    }
                }
            }

            string currentUser = UserSession.CurrentUsername;

            try
            {
                using (var db = new SqliteConnection($"Filename={dbPath};Mode=ReadWriteCreate;Cache=Shared"))
                {
                    db.Open();
                    var cmd = new SqliteCommand("INSERT INTO Reviews (AttractionName, City, Username, Rating, Comment, PhotoUrl) VALUES (@name, @city, @user, @rating, @comment, @photo)", db);

                    // Aplicăm .Trim() și la salvare pentru a menține consistența
                    cmd.Parameters.AddWithValue("@name", _currentAttraction.Name.Trim());
                    cmd.Parameters.AddWithValue("@city", _currentAttraction.City.Trim());
                    cmd.Parameters.AddWithValue("@user", currentUser);
                    cmd.Parameters.AddWithValue("@rating", rating);
                    cmd.Parameters.AddWithValue("@comment", comment);
                    cmd.Parameters.AddWithValue("@photo", photo);

                    cmd.ExecuteNonQuery();
                }

                CommentBox.Text = "";
                UserPhotoBox.Text = "";
                RatingSlider.Value = 0;

                LoadReviews();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Eroare la salvarea recenziei: " + ex.Message);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        #region LOGICA ZOOM IMAGINE

        // Când dăm click pe imaginea mică din recenzie
        private void ReviewImage_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is Image clickedImage && clickedImage.DataContext is Review clickedReview)
            {
                // Verificăm să aibă o imagine validă
                if (!string.IsNullOrEmpty(clickedReview.PhotoUrl))
                {
                    // Copiem imaginea în Overlay-ul gigant și îl facem vizibil
                    ZoomedImage.Source = clickedImage.Source;
                    ImageZoomOverlay.Visibility = Visibility.Visible;
                }
            }
        }

        // Când dăm click pe fundalul negru să se închidă
        private void CloseZoom_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ImageZoomOverlay.Visibility = Visibility.Collapsed;
        }

        // Când dăm click pe butonul roșu X
        private void CloseZoomButton_Click(object sender, RoutedEventArgs e)
        {
            ImageZoomOverlay.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}