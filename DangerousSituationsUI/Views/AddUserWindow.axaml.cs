using Avalonia.Controls;

namespace DangerousSituationsUI;

public partial class AddUserWindow : Window
{
    public string Username => NameBox.Text;
    public string Email => EmailBox.Text;
    public string Password => PasswordBox.Text;
    public bool IsAdmin => IsAdminBox.IsChecked ?? false;

    public AddUserWindow()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(true); 
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false); 
    }
}