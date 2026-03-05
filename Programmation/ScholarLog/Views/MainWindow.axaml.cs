using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ScholarLog.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ToggleSidebar_Click(object? sender, RoutedEventArgs e)
    {
        if (Sidebar.Width == 250)
        {
            // --- FERMETURE ---
            Sidebar.Width = 60;
            ToggleIcon.Content = "\uE12A"; 
        
            // On rend le texte transparent
            SetLabelsOpacity(0);
        }
        else
        {
            // --- OUVERTURE ---
            Sidebar.Width = 250;
            ToggleIcon.Content = "\uE128"; 
        
            // On rend le texte opaque
            SetLabelsOpacity(1);
        }
    }

    private void SetLabelsOpacity(double opacity)
    {
        // On change l'opacité (l'animation XAML prendra le relais)
        TextCollapse.Opacity = opacity;
        TextLogo.Opacity = opacity;
        TextMap.Opacity = opacity;
        TextSettings.Opacity = opacity;

        // Optionnel : on empêche de cliquer sur le texte quand il est invisible
        bool visible = opacity > 0;
        TextCollapse.IsHitTestVisible = visible;
        TextLogo.IsHitTestVisible = visible;
        TextMap.IsHitTestVisible = visible;
        TextSettings.IsHitTestVisible = visible;
    }
}