using Microsoft.UI.Xaml;

namespace II
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            
            RootFrame.Navigate(typeof(LoginPage));
        }
    }
}