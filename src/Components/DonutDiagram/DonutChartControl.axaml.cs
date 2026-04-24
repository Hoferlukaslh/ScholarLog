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
    public static readonly StyledProperty<IEnumerable<DonutItem>?> ItemsSourceProperty =
        AvaloniaProperty.Register<DonutChartControl, IEnumerable<DonutItem>?>(nameof(ItemsSource));

    public IEnumerable<DonutItem>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private readonly string[] _colorPalette = 
        { "#CC4A90E2", "#CC50E3C2", "#CCF5A623", "#CCD0021B", "#CCBD10E0", "#CCB8E986", "#CC8B572A" };

    private bool? _estModeLarge = null;

    public DonutChartControl()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty)
        {
            if (change.OldValue is INotifyCollectionChanged oldObservable)
                oldObservable.CollectionChanged -= OnItemsSourceCollectionChanged;

            if (change.NewValue is INotifyCollectionChanged newObservable)
                newObservable.CollectionChanged += OnItemsSourceCollectionChanged;

            RafraichirCompletement();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        AdapterMiseEnPage(e.NewSize.Width, e.NewSize.Height);
    }

    private void AdapterMiseEnPage(double w, double h)
    {
        if (w <= 0 || h <= 0) return;

        bool doitEtreLarge = (w >= h * 1.2) && (w > 250);

        if (doitEtreLarge)
        {
            LegendScroll.MaxHeight = Math.Max(50, h - 20); 
            LegendScroll.MaxWidth = Math.Max(50, w * 0.45); 
        }
        else
        {
            LegendScroll.MaxHeight = Math.Max(50, h * 0.45); 
            LegendScroll.MaxWidth = double.PositiveInfinity;
        }

        if (_estModeLarge.HasValue && _estModeLarge.Value == doitEtreLarge) return;
        _estModeLarge = doitEtreLarge;

        if (doitEtreLarge)
        {
            MainGrid.RowDefinitions = RowDefinitions.Parse("*, 0");
            MainGrid.ColumnDefinitions = ColumnDefinitions.Parse("*, Auto");

            Grid.SetRow(DonutCanvas, 0);
            Grid.SetColumn(DonutCanvas, 0);
            Grid.SetRowSpan(DonutCanvas, 2);
            Grid.SetColumnSpan(DonutCanvas, 1);
            DonutCanvas.Margin = new Thickness(0, 0, 15, 0); 

            Grid.SetRow(LegendScroll, 0);
            Grid.SetColumn(LegendScroll, 1);
            Grid.SetRowSpan(LegendScroll, 2);
            Grid.SetColumnSpan(LegendScroll, 1);
            
            LegendPanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            LegendPanel.Margin = new Thickness(0, 10, 10, 10);
        }
        else
        {
            MainGrid.RowDefinitions = RowDefinitions.Parse("*, Auto");
            MainGrid.ColumnDefinitions = ColumnDefinitions.Parse("*, 0");

            Grid.SetRow(DonutCanvas, 0);
            Grid.SetColumn(DonutCanvas, 0);
            Grid.SetRowSpan(DonutCanvas, 1);
            Grid.SetColumnSpan(DonutCanvas, 2);
            DonutCanvas.Margin = new Thickness(0, 0, 0, 15);

            Grid.SetRow(LegendScroll, 1);
            Grid.SetColumn(LegendScroll, 0);
            Grid.SetRowSpan(LegendScroll, 1);
            Grid.SetColumnSpan(LegendScroll, 2);
            
            LegendPanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            LegendPanel.Margin = new Thickness(0, 0, 0, 10);
        }
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RafraichirCompletement();
    }

    private void OnDonutCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        DessinerDonut();
    }

    private void RafraichirCompletement()
    {
        GenererLegende();
        DessinerDonut();
    }

    private void GenererLegende()
    {
        LegendPanel.Children.Clear();

        var donnees = ItemsSource;
        if (donnees == null || !donnees.Any()) return;

        double total = donnees.Sum(d => d.Value);
        if (total <= 0) return;

        IBrush? texteBrush = null;
        if (this.TryFindResource("PrimaryForeground", out var fgRes) && fgRes is IBrush brush)
            texteBrush = brush;

        int colorIndex = 0;
        foreach (var item in donnees)
        {
            if (item.Value <= 0) continue;

            double proportion = item.Value / total;
            var sliceColor = SolidColorBrush.Parse(_colorPalette[colorIndex % _colorPalette.Length]);

            var legendItem = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto, *"),
                Margin = new Thickness(5, 2)
            };

            var colorBox = new Border
            {
                Width = 10, Height = 10,
                CornerRadius = new CornerRadius(5),
                Background = sliceColor,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, 
                Margin = new Thickness(0, 3, 8, 0)
            };
            Grid.SetColumn(colorBox, 0);

            var label = new TextBlock
            {
                Text = $"{item.Label} : {item.Value:0.#}h ({proportion * 100:0.#}%)",
                FontSize = 12,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap 
            };
            
            if (texteBrush != null)
                label.Foreground = texteBrush;
            
            Grid.SetColumn(label, 1);

            legendItem.Children.Add(colorBox);
            legendItem.Children.Add(label);
            LegendPanel.Children.Add(legendItem);

            colorIndex++;
        }
    }

    private void DessinerDonut()
    {
        DonutCanvas.Children.Clear();

        var donnees = ItemsSource;
        if (donnees == null || !donnees.Any()) return;

        double largeur = DonutCanvas.Bounds.Width;
        double hauteur = DonutCanvas.Bounds.Height;
        
        if (largeur <= 10 || hauteur <= 10) return; 

        double total = donnees.Sum(d => d.Value);
        if (total <= 0) return;

        double centerX = largeur / 2;
        double centerY = hauteur / 2;
        double dimensionMinimale = Math.Min(largeur, hauteur);

        double outerRadius = (dimensionMinimale / 2) - 5;
        if (outerRadius <= 5) return; 

        double innerRadius = outerRadius * 0.6;
        double currentAngle = -Math.PI / 2;
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

            var sliceColor = SolidColorBrush.Parse(_colorPalette[colorIndex % _colorPalette.Length]);

            var path = new Path
            {
                Data = StreamGeometry.Parse(pathData),
                Fill = sliceColor,
                Stroke = Brushes.Transparent,
                StrokeThickness = 1
            };

            ToolTip.SetTip(path, $"{item.Label} : {item.Value:0.#}h ({proportion * 100:0.#}%)");
            DonutCanvas.Children.Add(path);

            currentAngle = nextAngle;
            colorIndex++;
        }

        double fontSizeCalculee = Math.Max(10, Math.Min(dimensionMinimale * 0.15, innerRadius * 0.8));
        
        var texteTotal = new TextBlock
        {
            Text = total.ToString("0.#h"),
            FontSize = fontSizeCalculee, 
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        if (this.TryFindResource("PrimaryForeground", out var totalFgRes) && totalFgRes is IBrush totalFgBrush)
        {
            texteTotal.Foreground = totalFgBrush;
        }

        var conteneurCentral = new Border
        {
            Width = innerRadius * 2,
            Height = innerRadius * 2,
            Child = texteTotal
        };

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