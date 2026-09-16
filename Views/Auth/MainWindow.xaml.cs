using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using StockPortalApp.Data;

namespace StockPortalApp
{
    public partial class MainWindow : Window
    {
        private DashboardWindow? _dashboardWindow;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateChakraSpokes();
            StartChakraSpin();
            PlayLotusBloom();
        }

        /// <summary>
        /// Mirrors the original JS: 24 spokes radiating from r=20 to r=90
        /// around a 200x200 viewBox centered at (100,100).
        /// </summary>
        private void GenerateChakraSpokes()
        {
            const double cx = 100, cy = 100, rInner = 20, rOuter = 90;
            for (int i = 0; i < 24; i++)
            {
                double angle = i / 24.0 * 2 * Math.PI;
                var line = new Line
                {
                    X1 = cx + rInner * Math.Cos(angle),
                    Y1 = cy + rInner * Math.Sin(angle),
                    X2 = cx + rOuter * Math.Cos(angle),
                    Y2 = cy + rOuter * Math.Sin(angle),
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                ChakraCanvas.Children.Add(line);
            }
        }

        /// <summary>90s linear, infinite rotation — matches @keyframes spin.</summary>
        private void StartChakraSpin()
        {
            var anim = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(90))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            ChakraRotate.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        /// <summary>Staggered scale/opacity "bloom" entrance for the lotus petals.</summary>
        private void PlayLotusBloom()
        {
            (ScaleTransform transform, double delaySeconds)[] petals =
            {
                (Scale1, 0.05), (Scale2, 0.14), (Scale3, 0.23), (Scale4, 0.32),
                (Scale5, 0.41), (Scale6, 0.50), (Scale7, 0.59)
            };

            foreach (var (transform, delay) in petals)
            {
                transform.ScaleX = 0.35;
                transform.ScaleY = 0.35;

                var scaleAnim = new DoubleAnimation(0.35, 1.0, TimeSpan.FromSeconds(1.0))
                {
                    BeginTime = TimeSpan.FromSeconds(delay),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }
        }

        private bool _isPasswordVisible = false;

        private void PasswordBoxInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isPasswordVisible)
            {
                PasswordTextBoxInput.Text = PasswordBoxInput.Password;
            }
            UpdatePasswordPlaceholder();
        }

        private void PasswordTextBoxInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                PasswordBoxInput.Password = PasswordTextBoxInput.Text;
            }
            UpdatePasswordPlaceholder();
        }

        private void UpdatePasswordPlaceholder()
        {
            var currentPass = _isPasswordVisible ? PasswordTextBoxInput.Text : PasswordBoxInput.Password;
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(currentPass)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void TogglePasswordVisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            var eyeOpenIcon = (Path)TogglePasswordVisibilityButton.Template.FindName("EyeOpenIcon", TogglePasswordVisibilityButton);
            var eyeClosedIcon = (Path)TogglePasswordVisibilityButton.Template.FindName("EyeClosedIcon", TogglePasswordVisibilityButton);

            if (_isPasswordVisible)
            {
                PasswordTextBoxInput.Text = PasswordBoxInput.Password;
                PasswordTextBoxInput.Visibility = Visibility.Visible;
                PasswordBoxInput.Visibility = Visibility.Collapsed;
                PasswordTextBoxInput.Focus();
                PasswordTextBoxInput.CaretIndex = PasswordTextBoxInput.Text.Length;

                if (eyeOpenIcon != null) eyeOpenIcon.Visibility = Visibility.Collapsed;
                if (eyeClosedIcon != null) eyeClosedIcon.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordBoxInput.Password = PasswordTextBoxInput.Text;
                PasswordBoxInput.Visibility = Visibility.Visible;
                PasswordTextBoxInput.Visibility = Visibility.Collapsed;
                PasswordBoxInput.Focus();

                if (eyeOpenIcon != null) eyeOpenIcon.Visibility = Visibility.Visible;
                if (eyeClosedIcon != null) eyeClosedIcon.Visibility = Visibility.Collapsed;
            }

            UpdatePasswordPlaceholder();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Contact your district IT coordinator to reset your password.",
                "Forgot password",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            var id = MemberIdTextBox.Text.Trim();
            var password = _isPasswordVisible ? PasswordTextBoxInput.Text : PasswordBoxInput.Password;

            LoginErrorText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
            {
                LoginErrorText.Text = "Please enter both your Membership ID and password.";
                LoginErrorText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                SignInButton.IsEnabled = false;
                SignInButton.Content = "Signing in…";

                var result = await DbHelper.ValidateLoginAsync(id, password);

                SignInButton.IsEnabled = true;
                SignInButton.Content = "Sign In";

                if (!result.Success)
                {
                    LoginErrorText.Text = result.ErrorMessage ?? "Invalid credentials. Try again.";
                    LoginErrorText.Visibility = Visibility.Visible;
                    return;
                }

                var displayName = string.IsNullOrWhiteSpace(result.FullName) ? id : result.FullName!;

                if (_dashboardWindow == null)
                {
                    _dashboardWindow = new DashboardWindow(this, displayName);
                }
                else
                {
                    _dashboardWindow.SetUser(displayName);
                }

                _dashboardWindow.Show();
                Hide();
            }
            catch (Exception ex)
            {
                SignInButton.IsEnabled = true;
                SignInButton.Content = "Sign In";
                LoginErrorText.Text = $"Sign-in error: {ex.Message}";
                LoginErrorText.Visibility = Visibility.Visible;
            }
        }
    }
}
