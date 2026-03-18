using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ScholarLog.ViewModels;

namespace ScholarLog.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        
        // On s'abonne aux changements du ViewModel pour afficher le Flyout
        this.DataContextChanged += SettingsPage_DataContextChanged;
    }

    private void SettingsPage_DataContextChanged(object? sender, System.EventArgs e)
    {
        if (this.DataContext is SettingsViewModel vm)
        {
            vm.PropertyChanged += (s, args) =>
            {
                // Si le ViewModel dit qu'il y a une erreur, on affiche le Flyout
                if (args.PropertyName == nameof(SettingsViewModel.ShowPathError) && vm.ShowPathError)
                {
                    FlyoutBase.ShowAttachedFlyout(this.FindControl<TextBlock>("userbddPath"));
                    vm.ShowPathError = false; // On reset pour la prochaine fois
                }
            };
        }
    }

    // L'ouverture d'un explorateur de fichier est une action de VUE.
    // On la garde dans le code-behind, mais on donne le résultat au ViewModel.
    public async void BrowseFile_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionner la base de données",
            AllowMultiple = false,
            FileTypeFilter = new[] 
            { 
                new FilePickerFileType("SQLite") { Patterns = new[] { "*.db", "*.sqlite" } } 
            }
        });

        if (files != null && files.Count > 0 && this.DataContext is SettingsViewModel vm)
        {
            // On met à jour la propriété du ViewModel directement
            vm.PathToBDD = files[0].Path.LocalPath;
        }
    }
}