using Avalonia.Controls;

namespace Linguistics.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        NavigationList.SelectionChanged += OnNavigationChanged;
        NavigationList.SelectedIndex = 0;
    }

    private void OnNavigationChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (NavigationList.SelectedItem is not ListBoxItem item)
        {
            return;
        }

        PageTitle.Text = item.Content?.ToString() ?? "Linguistics";
        PageDescription.Text = item.Tag?.ToString() ?? "This area is not available yet.";
    }
}
