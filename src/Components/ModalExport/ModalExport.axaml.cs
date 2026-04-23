/*
    Fichier      :  ModalExport.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind du composant réutilisable ModalExport.
        Expose les StyledProperties nécessaires pour le binding depuis la page
        parente, et gère les opérations système (presse-papier, sélecteur de
        fichiers) qui ne peuvent pas être réalisées dans un ViewModel.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  23.04.2026

    Propriétés exposées :
        IsOpen               (bool)     - Contrôle la visibilité du modal.
        Title                (string)   - Titre affiché en en-tête du modal.
        PreviewText          (string)   - Contenu texte affiché dans l'aperçu.
        SuggestedFileBaseName(string)   - Nom de fichier de base proposé lors
                                          de l'enregistrement (sans extension).
                                          Ex : "JournalDeTravail_ALL"
        CloseCommand         (ICommand) - Commande appelée par le bouton ✕.
        FormatChangedCommand (ICommand) - Commande appelée lors du changement
                                          de format, avec le format en paramètre
                                          ("MD", "CSV" ou "JSON").

    Remarques :
        - Quand IsOpen passe à true, le format est automatiquement réinitialisé
          à "MD" (premier radio-bouton recoché visuellement).
        - L'extension du fichier (.md, .csv, .json) est déterminée à partir du
          radio-bouton actuellement sélectionné au moment de la sauvegarde.
*/

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace ScholarLog.Components.ModalExport;

public partial class ModalExport : UserControl
{
    // -------------------------------------------------------------------------
    // StyledProperties
    // -------------------------------------------------------------------------

    /// <summary>Contrôle la visibilité du modal.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ModalExport, bool>(nameof(IsOpen), defaultValue: false);

    /// <summary>Titre affiché dans l'en-tête du modal.</summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ModalExport, string>(nameof(Title), defaultValue: "Exporter");

    /// <summary>Texte à afficher dans la zone d'aperçu et à exporter.</summary>
    public static readonly StyledProperty<string> PreviewTextProperty =
        AvaloniaProperty.Register<ModalExport, string>(nameof(PreviewText), defaultValue: string.Empty);

    /// <summary>
    /// Nom de base proposé pour le fichier (sans extension).
    /// Le composant ajoute automatiquement l'extension selon le format sélectionné.
    /// </summary>
    public static readonly StyledProperty<string> SuggestedFileBaseNameProperty =
        AvaloniaProperty.Register<ModalExport, string>(nameof(SuggestedFileBaseName), defaultValue: "export");

    /// <summary>Commande exécutée lors du clic sur le bouton de fermeture (✕).</summary>
    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<ModalExport, ICommand?>(nameof(CloseCommand));

    /// <summary>
    /// Commande exécutée lors du changement de format.
    /// Reçoit le format en paramètre : "MD", "CSV" ou "JSON".
    /// </summary>
    public static readonly StyledProperty<ICommand?> FormatChangedCommandProperty =
        AvaloniaProperty.Register<ModalExport, ICommand?>(nameof(FormatChangedCommand));

    // -------------------------------------------------------------------------
    // Accesseurs CLR
    // -------------------------------------------------------------------------

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public new string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string PreviewText
    {
        get => GetValue(PreviewTextProperty);
        set => SetValue(PreviewTextProperty, value);
    }

    public string SuggestedFileBaseName
    {
        get => GetValue(SuggestedFileBaseNameProperty);
        set => SetValue(SuggestedFileBaseNameProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public ICommand? FormatChangedCommand
    {
        get => GetValue(FormatChangedCommandProperty);
        set => SetValue(FormatChangedCommandProperty, value);
    }

    // -------------------------------------------------------------------------
    // Constructeur
    // -------------------------------------------------------------------------

    public ModalExport()
    {
        InitializeComponent();
    }

    // -------------------------------------------------------------------------
    // Gestion des changements de propriétés
    // -------------------------------------------------------------------------

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Quand le modal s'ouvre, réinitialiser le radio-bouton à "MD"
        if (change.Property == IsOpenProperty && change.GetNewValue<bool>())
        {
            RbMd.IsChecked = true;
        }
    }

    // -------------------------------------------------------------------------
    // Déterminer l'extension active selon le radio sélectionné
    // -------------------------------------------------------------------------

    private string GetCurrentExtension()
    {
        // Le GroupName "ExportFormat" permet de retrouver le bouton coché
        // via le nom du radio (RbMd est le seul nommé, les autres via IsChecked)
        if (this.FindControl<RadioButton>("RbMd")?.IsChecked == true)
            return "md";

        // Parcourir les RadioButtons du groupe pour trouver le coché
        foreach (var rb in this.GetVisualDescendants().OfType<RadioButton>())
        {
            if (rb.GroupName == "ExportFormat" && rb.IsChecked == true)
                return (rb.Content?.ToString() ?? "md").ToLower();
        }

        return "md";
    }

    // -------------------------------------------------------------------------
    // Copier dans le presse-papier
    // -------------------------------------------------------------------------

    private async void BoutonCopier_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(PreviewText)) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Clipboard != null)
            await topLevel.Clipboard.SetTextAsync(PreviewText);
    }

    // -------------------------------------------------------------------------
    // Enregistrer sur disque
    // -------------------------------------------------------------------------

    private async void BoutonSauvegarder_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(PreviewText)) return;

        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            string extension = GetCurrentExtension();
            string suggestedName = $"{SuggestedFileBaseName}.{extension}";

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Enregistrer l'exportation",
                SuggestedFileName = suggestedName,
                DefaultExtension = extension
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(PreviewText);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ModalExport] Erreur lors de la sauvegarde : {ex.Message}");
        }
    }
}
