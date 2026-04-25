/*
    Fichier      :  SettingsPage.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la vue des paramètres.
        Gère les interactions avec le système d'exploitation pour la configuration
        de l'application, notamment la sélection du chemin de la base de données
        SQLite et l'affichage des alertes visuelles (Flyouts).

    Auteur       :  Lukas Hofer - TINF2
    Date         :  19.03.2026

    Remarques    :
        - Utilise le StorageProvider d'Avalonia pour l'ouverture de fichiers.
        - Écoute les changements de propriétés du ViewModel pour déclencher
          l'affichage de bulles d'erreur (ShowPathError) sur des contrôles spécifiques.
        - Assure la mise à jour du chemin local de la BDD après sélection.
*/

using System;
using Avalonia;
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

        // abonnement aux changements du ViewModel pour afficher le Flyout
        this.DataContextChanged += SettingsPage_DataContextChanged;
    }

    public static readonly StyledProperty<int> GridColumnsProperty =
        AvaloniaProperty.Register<SettingsPage, int>(nameof(GridColumns), 4);

    public int GridColumns
    {
        get => GetValue(GridColumnsProperty);
        set => SetValue(GridColumnsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Si la propriété qui change est la taille du composant
        if (change.Property == BoundsProperty)
        {
            var bounds = change.GetNewValue<Rect>();

            if (bounds.Width < 900) GridColumns = 2;
            else if (bounds.Width < 1200) GridColumns = 3;
            else GridColumns = 4;
        }
    }

    private void SettingsPage_DataContextChanged(object? sender, System.EventArgs e)
    {
        if (this.DataContext is SettingsViewModel vm)
        {
            // On s'abonne avec une vraie méthode nommée
            vm.PropertyChanged += Vm_PropertyChanged;
        }
    }
    
    // On sort la logique dans une méthode à part
    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (sender is SettingsViewModel vm && args.PropertyName == nameof(SettingsViewModel.ShowPathError) && vm.ShowPathError)
        {
            FlyoutBase.ShowAttachedFlyout(this.FindControl<TextBlock>("userbddPath"));
            vm.ShowPathError = false;
        }
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        if (this.DataContext is SettingsViewModel vm)
        {
            // On coupe le lien quand on quitte la page des paramètres
            vm.PropertyChanged -= Vm_PropertyChanged;
        }
    }


    public async void BrowseFile_Click(object? sender, RoutedEventArgs e)
    {
        // Le try/catch est VITAL ici pour macOS. S'il y a une erreur de droits, 
        // l'application ne crashera pas, elle ira dans le catch.
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Sélectionner la base de données",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("SQLite") 
                    { 
                        Patterns = new[] { "*.db", "*.sqlite" },
                        // Indispensable pour que macOS comprenne le filtre
                        AppleUniformTypeIdentifiers = new[] { "public.database", "public.data", "public.item" }
                    }
                }
            });

            if (files != null && files.Count > 0 && this.DataContext is SettingsViewModel vm)
            {
                string? localPath = files[0].TryGetLocalPath();
                if (!string.IsNullOrEmpty(localPath))
                {
                    // ON UTILISE AWAIT ! Et on appelle le bon nom de méthode (Update au lieu de Change)
                    await vm.UpdateDatabasePathAsync(localPath);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SettingsPage] Erreur critique interceptée : {ex}");
            if (this.DataContext is SettingsViewModel vm)
            {
                vm.StatusMessage = "Erreur système lors de la lecture du fichier.";
            }
        }
    }
}