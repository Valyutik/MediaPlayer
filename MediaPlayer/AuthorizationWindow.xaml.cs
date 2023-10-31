using System.Windows;

namespace MediaPlayer;

public partial class AuthorizationWindow
{
    private readonly string _loginAdmin = "admin";
    private readonly string _loginClient = "client";
    
    public AuthorizationWindow()
    {
        InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (LoginTextBox.Text == _loginAdmin && PasswordBox.Password == _loginAdmin)
        {
            var mainWindow = new AdminMainWindow();
            mainWindow.Show();
            this.Close();
        }
        else if (LoginTextBox.Text == _loginClient && PasswordBox.Password == _loginClient)
        {
            var mainWindow = new ClientMainWindow();
            mainWindow.Show();
            this.Close();
        }
        else
        {
            MessageBox.Show("Неправильный логин или пароль");
        }
    }
}