using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Virtual_Scanner
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Security : Page
    {

        private int passwordRetries = 0;
        public Security()
        {
            this.InitializeComponent();
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (revealModeCheckBox.IsChecked == true)
            {
                passwordBox1.PasswordRevealMode = PasswordRevealMode.Visible;
            }
            else
            {
                passwordBox1.PasswordRevealMode = PasswordRevealMode.Hidden;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            GoBack();
        }

        private void GoBack()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (passwordBox1.Password == "Best181bar")
            {

            }
            else
            {
                passwordRetries++;
                if (passwordRetries > 1) txtPasswordError.Text = "Password entered is incorrect - " + (4 - passwordRetries).ToString() + " attempts left!";
                txtPasswordError.Visibility = Visibility.Visible;
            }
            if (passwordRetries > 3)
            {
                GoBack();
            }
                
        }

        private void passwordBox1_PasswordChanged(object sender, RoutedEventArgs e)
        {
            txtPasswordError.Visibility = Visibility.Collapsed;
        }
    }
}
