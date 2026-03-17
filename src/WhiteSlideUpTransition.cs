using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using System;
using System.Threading.Tasks;
using Avalonia.Media;

namespace AvaloniaUI.CustomTransitions
{
    // Classe de transition personnalisée qui hérite de IPageTransition
    public class WhiteSlideUpTransition : IPageTransition
    {
        // Durée par défaut de l'animation
        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(500);

        // Méthode principale qui exécute l'animation
        public async Task Start(Visual from, Visual to, bool forward, System.Threading.CancellationToken cancellationToken)
        {
            // Éviter les animations si une page est nulle
            if (from == null || to == null) return;

            // Définir les animations de sortie (pour la page 'from')
            // Animation d'opacité : la page devient blanche (transparente)
            var fromFadeAnimation = new Animation
            {
                Duration = Duration,
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Setters = { new Setter(Visual.OpacityProperty, 1.0) },
                        Cue = new Cue(0.0)
                    },
                    new KeyFrame
                    {
                        Setters = { new Setter(Visual.OpacityProperty, 0.0) },
                        Cue = new Cue(1.0)
                    }
                }
            };

            // Animation de mouvement : la page glisse vers le haut
            var fromSlideAnimation = new Animation
            {
                Duration = Duration,
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Setters = { new Setter(TranslateTransform.YProperty, 0.0) },
                        Cue = new Cue(0.0)
                    },
                    new KeyFrame
                    {
                        Setters = { new Setter(TranslateTransform.YProperty, -from.Bounds.Height) }, // Glisser vers le haut (Y négatif)
                        Cue = new Cue(1.0)
                    }
                }
            };

            // Définir les animations d'entrée (pour la page 'to')
            // Animation d'opacité : la page devient opaque
            var toFadeAnimation = new Animation
            {
                Duration = Duration,
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Setters = { new Setter(Visual.OpacityProperty, 0.0) },
                        Cue = new Cue(0.0)
                    },
                    new KeyFrame
                    {
                        Setters = { new Setter(Visual.OpacityProperty, 1.0) },
                        Cue = new Cue(1.0)
                    }
                }
            };

            // Animation de mouvement : la page glisse depuis le bas
            var toSlideAnimation = new Animation
            {
                Duration = Duration,
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame
                    {
                        Setters = { new Setter(TranslateTransform.YProperty, from.Bounds.Height) }, // Glisser depuis le bas (Y positif)
                        Cue = new Cue(0.0)
                    },
                    new KeyFrame
                    {
                        Setters = { new Setter(TranslateTransform.YProperty, 0.0) },
                        Cue = new Cue(1.0)
                    }
                }
            };

            // Appliquer les animations et attendre la fin
            // Nous utilisons Task.WhenAll pour lancer toutes les animations simultanément
            await Task.WhenAll(
                fromFadeAnimation.RunAsync(from, cancellationToken),
                fromSlideAnimation.RunAsync(from, cancellationToken),
                toFadeAnimation.RunAsync(to, cancellationToken),
                toSlideAnimation.RunAsync(to, cancellationToken)
            );

            // Nettoyer les propriétés d'animation pour éviter les interférences futures
            from.ClearValue(TranslateTransform.YProperty);
            to.ClearValue(TranslateTransform.YProperty);
        }
    }
}