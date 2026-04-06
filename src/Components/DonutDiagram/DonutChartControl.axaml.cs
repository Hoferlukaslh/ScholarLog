using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;

namespace ScholarLog.Components.DonutDiagram;

public partial class DonutChartControl : UserControl
{
    // Déclaration de la propriété Avalonia pour lier les données
    public static readonly StyledProperty<IEnumerable<DonutItem>?> ItemsSourceProperty =
        AvaloniaProperty.Register<DonutChartControl, IEnumerable<DonutItem>?>(nameof(ItemsSource));

    public IEnumerable<DonutItem>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public DonutChartControl()
    {
        InitializeComponent();
    }

    // Écoute les changements de la propriété ItemsSource pour redessiner le graphique
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty)
        {
            // 1. Si l'ancienne liste était "Observable", on se désabonne pour éviter les fuites mémoire
            if (change.OldValue is INotifyCollectionChanged oldObservable)
            {
                oldObservable.CollectionChanged -= OnItemsSourceCollectionChanged;
            }

            // 2. Si la nouvelle liste est "Observable", on s'abonne à ses changements (Add/Clear)
            if (change.NewValue is INotifyCollectionChanged newObservable)
            {
                newObservable.CollectionChanged += OnItemsSourceCollectionChanged;
            }

            // 3. On redessine dans tous les cas
            DessinerGraphique();
        }
    }

// Cette méthode est appelée dès que vous faites .Clear() ou .Add() dans HomePage.axaml.cs
    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        DessinerGraphique();
    }

    private void OnDonutCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        DessinerGraphique();
    }

    private void DessinerGraphique()
    {
        DonutCanvas.Children.Clear();
        LegendPanel.Children.Clear();

        var donnees = ItemsSource;
        if (donnees == null || !donnees.Any()) return;

        double largeur = DonutCanvas.Bounds.Width;
        double hauteur = DonutCanvas.Bounds.Height;
        if (largeur <= 0 || hauteur <= 0) return;

        double total = donnees.Sum(d => d.Value);
        if (total <= 0) return;

        double centerX = largeur / 2;
        double centerY = hauteur / 2;
        double dimensionMinimale = Math.Min(largeur, hauteur);

        double outerRadius = (dimensionMinimale / 2) - 5;
        if (outerRadius <= 0) return;

        double innerRadius = outerRadius * 0.6;
        double currentAngle = -Math.PI / 2;

        string[] colorPalette =
            { "#CC4A90E2", "#CC50E3C2", "#CCF5A623", "#CCD0021B", "#CCBD10E0", "#CCB8E986", "#CC8B572A" };
        int colorIndex = 0;

        foreach (var item in donnees)
        {
            if (item.Value <= 0) continue;

            double proportion = item.Value / total;
            double angleProportion = proportion * 2 * Math.PI;

            if (angleProportion >= 2 * Math.PI)
                angleProportion = 2 * Math.PI - 0.001;

            double nextAngle = currentAngle + angleProportion;
            int isLargeArc = angleProportion > Math.PI ? 1 : 0;

            // Calculs trigonométriques
            double startX_Outer = centerX + outerRadius * Math.Cos(currentAngle);
            double startY_Outer = centerY + outerRadius * Math.Sin(currentAngle);
            double endX_Outer = centerX + outerRadius * Math.Cos(nextAngle);
            double endY_Outer = centerY + outerRadius * Math.Sin(nextAngle);
            double endX_Inner = centerX + innerRadius * Math.Cos(nextAngle);
            double endY_Inner = centerY + innerRadius * Math.Sin(nextAngle);
            double startX_Inner = centerX + innerRadius * Math.Cos(currentAngle);
            double startY_Inner = centerY + innerRadius * Math.Sin(currentAngle);

            string pathData = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "M {0},{1} A {2},{2} 0 {3} 1 {4},{5} L {6},{7} A {8},{8} 0 {3} 0 {9},{10} Z",
                startX_Outer, startY_Outer, outerRadius, isLargeArc, endX_Outer, endY_Outer,
                endX_Inner, endY_Inner, innerRadius, startX_Inner, startY_Inner);

            var sliceColor = SolidColorBrush.Parse(colorPalette[colorIndex % colorPalette.Length]);

            var path = new Path
            {
                Data = StreamGeometry.Parse(pathData),
                Fill = sliceColor,
                Stroke = Brushes.Transparent,
                StrokeThickness = 1
            };

            ToolTip.SetTip(path, $"{item.Label} : {item.Value:0.#}h ({proportion * 100:0.#}%)");
            DonutCanvas.Children.Add(path);

            // Création de la légende
            var legendItem = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(10, 2),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var colorBox = new Border
            {
                Width = 10, Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = sliceColor,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var label = new TextBlock
            {
                Text = $"{item.Label} : {item.Value:0.#}h ({proportion * 100:0.#}%)",
                FontSize = 12,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            if (this.TryFindResource("PrimaryForeground", out var fgRes) && fgRes is IBrush fgBrush)
                label.Foreground = fgBrush;

            legendItem.Children.Add(colorBox);
            legendItem.Children.Add(label);
            LegendPanel.Children.Add(legendItem);

            currentAngle = nextAngle;
            colorIndex++;
        }

        // Ajout de la somme des heures
        var texteTotal = new TextBlock
        {
            Text = total.ToString("0.#h"),
            FontSize = dimensionMinimale * 0.12, // Taille de police dynamique en fonction de la taille du donut
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        // Appliquer la même couleur de texte que la légende, si définie
        if (this.TryFindResource("PrimaryForeground", out var totalFgRes) && totalFgRes is IBrush totalFgBrush)
        {
            texteTotal.Foreground = totalFgBrush;
        }

        // Le conteneur fait exactement la taille du trou intérieur du donut
        var conteneurCentral = new Border
        {
            Width = innerRadius * 2,
            Height = innerRadius * 2,
            Child = texteTotal
        };

        // On positionne le conteneur en haut à gauche de la zone du cercle intérieur
        Canvas.SetLeft(conteneurCentral, centerX - innerRadius);
        Canvas.SetTop(conteneurCentral, centerY - innerRadius);

        DonutCanvas.Children.Add(conteneurCentral);
    }
}

public class DonutItem
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}