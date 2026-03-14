/*
    Fichier      :  JournalPage.axaml.cs
    Projet       :  ScholarLog

    Description  :
        Code-behind de la vue JournalPage

    Auteur       :  Lukas Hofer - TINF2
    Date         :  10.03.2026
*/


using Avalonia.Controls;
using Avalonia.Interactivity; 


namespace ScholarLog.Pages;


public partial class JournalPage : UserControl
{
    public JournalPage()
    {
        InitializeComponent();
        DataContext = this; 
                
        this.Loaded += JournalPage_Loaded;
    }
    
    private async void JournalPage_Loaded(object? sender, RoutedEventArgs e)
    {
        this.Loaded -= JournalPage_Loaded; 
    }
    
}