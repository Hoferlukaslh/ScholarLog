/*
    Fichier      :  GlobalUsings.cs
    Projet       :  ScholarLog

    Description  :
        Directives using globales pour tous les fichiers code-behind Avalonia.

    Auteur       :  Lukas Hofer - TINF2
    Date         :  10.03.2026
*/


// Système (Base .NET)
global using System;                                // Types de base (int, string, etc.)
global using System.IO;                             // Gestion fichiers et flux
global using System.Linq;                           // LINQ (query et collections)
global using System.Threading.Tasks;                // async / await
global using System.Runtime.InteropServices;        // Interop avec OS (Linux, Windows)
global using System.Collections.ObjectModel;        // ObservableCollection

// Avalonia (UI / contrôles)
global using Avalonia;                              // Classes de base Avalonia
global using Avalonia.Controls;                     // Contrôles UI (Button, Label, Grid)
global using Avalonia.Controls.Shapes;              // Ellipse, Rectangle, Path
global using Avalonia.Markup.Xaml;                  // Initialisation XAML
global using Avalonia.Media;                        // Couleurs, brushes, gradients
global using Avalonia.Interactivity;                // Events comme Click
global using Avalonia.Threading;                    // DispatcherTimer, Dispatcher
global using Avalonia.Data;                         // DataBinding
global using Avalonia.Styling;                      // Styles, classes
global using Avalonia.Platform.Storage;             // Fichiers / stockage cross-platform
global using Avalonia.Controls.Primitives;          // Conteneurs de base
global using Avalonia.Data.Core.Plugins;            // Plugins de binding
global using Avalonia.Controls.ApplicationLifetimes;// Application lifetime (SingleWindow, Classic)
global using Avalonia.Controls.Templates;           // Permet d'utiliser les systèmes de templates Avalonia
global using System.Diagnostics.CodeAnalysis;       // Fournit des attributs pour améliorer l'analyse du code par le compilateur

// Librairie MVVM
global using CommunityToolkit.Mvvm.ComponentModel;  // ObservableObject, [ObservableProperty]

// Dossier projet
global using ScholarLog.Views;                      // Fenêtres et UserControls
global using ScholarLog.ViewModels;                 // Classes ViewModel
global using ScholarLog.Component;                  // Composants personnalisés
// global using ScholarLog.Pages;                   // TODO : A modifier l'emplacement actuel !