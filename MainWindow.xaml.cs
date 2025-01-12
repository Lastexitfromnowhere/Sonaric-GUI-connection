using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Renci.SshNet;

namespace TunnelManager
{
    public partial class MainWindow : Window
    {
        private SshClient sshClient;
        private bool isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? 
                         WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GithubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Lastexitfromnowhere",
                UseShellExecute = true
            });
        }

        private void TwitterButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://x.com/brand_exit",
                UseShellExecute = true
            });
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (isConnected)
            {
                DisconnectFromServer();
                return;
            }

            try
            {
                var host = hostTextBox.Text;
                var username = usernameTextBox.Text;
                var password = passwordBox.Password;

                if (string.IsNullOrWhiteSpace(host) || 
                    string.IsNullOrWhiteSpace(username) || 
                    string.IsNullOrWhiteSpace(password))
                {
                    LogToTerminal("Error: Please fill in all fields");
                    return;
                }

                ConnectButton.IsEnabled = false;
                LogToTerminal("Attempting connection...");

                // SSH Configuration
                var connectionInfo = new ConnectionInfo(host, username,
                    new PasswordAuthenticationMethod(username, password));

                sshClient = new SshClient(connectionInfo);

                await Task.Run(() =>
                {
                    sshClient.Connect();
                    
                    var ports = new[] { 44003, 44004, 44005, 44006 };
                    foreach (var port in ports)
                    {
                        var forwardedPort = new ForwardedPortLocal(
                            "127.0.0.1",
                            (uint)port,
                            "127.0.0.1",
                            (uint)port
                        );
                        sshClient.AddForwardedPort(forwardedPort);
                        forwardedPort.Start();
                        
                        Dispatcher.Invoke(() =>
                            LogToTerminal($"Port {port} tunneled successfully")
                        );
                    }
                });

                if (sshClient.IsConnected)
                {
                    isConnected = true;
                    LogToTerminal("Connection established successfully");
                    ConnectButton.Content = "Disconnect";
                    
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "http://localhost:44004",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                LogToTerminal($"Error: {ex.Message}");
                MessageBox.Show($"Connection error: {ex.Message}", 
                              "Error", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
        }

        private void DisconnectFromServer()
        {
            try
            {
                sshClient?.Disconnect();
                sshClient?.Dispose();
                isConnected = false;
                ConnectButton.Content = "Connect";
                LogToTerminal("Disconnection completed");
            }
            catch (Exception ex)
            {
                LogToTerminal($"Error during disconnection: {ex.Message}");
            }
        }

        private void LogToTerminal(string message)
        {
            terminalTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            terminalTextBox.ScrollToEnd();
        }

        protected override void OnClosed(EventArgs e)
        {
            DisconnectFromServer();
            base.OnClosed(e);
        }
    }
}
