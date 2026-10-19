using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;

namespace II
{
    public class Attraction
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string City { get; set; }
    }

    public sealed partial class Search_page : Page
    {
        private DispatcherTimer _slideTimer;
        private List<string> _imageUrls;
        private int _currentImageIndex = 0;
        private bool _isImage1Active = true;

        private bool _isSearched = false;

        public Search_page()
        {
            this.InitializeComponent();
            SetupSlideshow();
        }

        #region SLIDESHOW BACKGROUND LOGIC

        private void SetupSlideshow()
        {


            _imageUrls = new List<string>
            {
                "https://i.pinimg.com/1200x/d2/75/0b/d2750b3a77f0ace9b0edd4de10019932.jpg",
                "https://i.pinimg.com/1200x/6a/3f/5a/6a3f5ac025967499d11f9a0ded877112.jpg",
                "https://i.pinimg.com/1200x/66/97/6d/66976d63a9a93cac803aadbd87dc65dc.jpg",
                "https://i.pinimg.com/736x/f5/b9/8a/f5b98a457cd298f6013c6bc9e731c409.jpg",
                "https://i.pinimg.com/1200x/c5/48/69/c5486955123520af86e14b97818a1f2d.jpg",
                "https://i.pinimg.com/1200x/48/ed/08/48ed08a653cea15cccbd52c4a3f9d4c0.jpg"
            };

            BgImage1.Source = new BitmapImage(new Uri(_imageUrls[0]));

            _slideTimer = new DispatcherTimer();
            _slideTimer.Interval = TimeSpan.FromSeconds(5);
            _slideTimer.Tick += SlideTimer_Tick;
            _slideTimer.Start();
        }

        private void SlideTimer_Tick(object sender, object e)
        {
            _currentImageIndex = (_currentImageIndex + 1) % _imageUrls.Count;
            string nextUrl = _imageUrls[_currentImageIndex];

            double screenWidth = MainRoot.ActualWidth;

            if (_isImage1Active)
            {
                BgImage2.Source = new BitmapImage(new Uri(nextUrl));
                SlideAnimation(SlideTransform2, -screenWidth, 0);
                SlideAnimation(SlideTransform1, 0, screenWidth);
            }
            else
            {
                BgImage1.Source = new BitmapImage(new Uri(nextUrl));
                SlideAnimation(SlideTransform1, -screenWidth, 0);
                SlideAnimation(SlideTransform2, 0, screenWidth);
            }

            _isImage1Active = !_isImage1Active;
        }

        private void SlideAnimation(Microsoft.UI.Xaml.Media.TranslateTransform transform, double from, double to)
        {
            Storyboard sb = new Storyboard();
            DoubleAnimation anim = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseInOut, Exponent = 3 }
            };
            Storyboard.SetTarget(anim, transform);
            Storyboard.SetTargetProperty(anim, "X");
            sb.Children.Add(anim);
            sb.Begin();
        }

        #endregion

        #region LOGICA DE CĂUTARE & ANIMAȚII UI

        private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            AnimateSearchToTop();

            string cityQuery = args.QueryText;
            string selectedCategory = "tourist attraction";

            if (CategoryFilter.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                selectedCategory = item.Tag.ToString();
            }

            var results = await HereMapManager.GetAttractionsFromApi(cityQuery, selectedCategory);

            ResultsGrid.ItemsSource = results;

            if (results.Count == 0)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Nu am găsit nimic",
                    Content = $"Nu am găsit orașul '{cityQuery}' sau nu există atracții din categoria '{selectedCategory}' aici. Încearcă un oraș valid!",
                    CloseButtonText = "Am înțeles",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private void AnimateSearchToTop()
        {
            if (_isSearched) return;
            _isSearched = true;

            // AM MODIFICAT AICI: Glisăm bara MULT mai sus pentru a nu lovi rezultatele
            double distanceToTop = -(MainRoot.ActualHeight / 2) + 60;

            Storyboard sb = new Storyboard();

            DoubleAnimation slideUp = new DoubleAnimation
            {
                To = distanceToTop,
                Duration = new Duration(TimeSpan.FromSeconds(0.6)),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4 }
            };
            Storyboard.SetTarget(slideUp, SearchTransform);
            Storyboard.SetTargetProperty(slideUp, "Y");
            sb.Children.Add(slideUp);

            DoubleAnimation fadeWelcome = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(0.3))
            };
            Storyboard.SetTarget(fadeWelcome, WelcomeText);
            Storyboard.SetTargetProperty(fadeWelcome, "Opacity");
            sb.Children.Add(fadeWelcome);

            ResultsGrid.Visibility = Visibility.Visible;
            DoubleAnimation fadeResults = new DoubleAnimation
            {
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(0.8)),
                BeginTime = TimeSpan.FromSeconds(0.3)
            };
            Storyboard.SetTarget(fadeResults, ResultsGrid);
            Storyboard.SetTargetProperty(fadeResults, "Opacity");
            sb.Children.Add(fadeResults);

            sb.Begin();
        }

        private void ResultsGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedAttraction = (Attraction)e.ClickedItem;
            this.Frame.Navigate(typeof(DetailsPage), clickedAttraction);
        }

        #endregion

        #region DELOGARE & CURĂȚARE

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _slideTimer?.Stop();
            UserSession.CurrentUsername = "Vizitator Anonim";

            // Resetăm pagina astfel încât următorul utilizator să vadă ecranul gol și bara pe centru!
            ResetPageToInitialState();

            this.Frame.Navigate(typeof(LoginPage));
        }

        private void ResetPageToInitialState()
        {
            _isSearched = false;

            // Ascundem și golim rezultatele
            ResultsGrid.Visibility = Visibility.Collapsed;
            ResultsGrid.Opacity = 0;
            ResultsGrid.ItemsSource = null;

            // Ducem bara de căutare înapoi la mijloc
            SearchTransform.Y = 0;

            // Reafișăm textul mare "Wonderland"
            WelcomeText.Opacity = 1;

            // Golim textul căutat ca să fie curat
            SearchBox.Text = "";
            CategoryFilter.SelectedIndex = 0;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _slideTimer?.Stop();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (_slideTimer != null && !_slideTimer.IsEnabled)
            {
                _slideTimer.Start();
            }
        }

        #endregion
    }
}