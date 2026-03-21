

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SkiaSharp;

namespace CBZ;

class Program
{
    static async Task Main(string[] args)
    {
        string pdf = @"C:\Users\lukas\Desktop\ScholarLog\PoC\CBZ\TEST\base.pdf";
        
        if (!File.Exists(pdf))
        {
            Console.WriteLine("Erreur : Le fichier PDF source est introuvable.");
            return;
        }

        string pdfName = Path.GetFileNameWithoutExtension(pdf);
        string pdfBaseDirectory = Path.GetDirectoryName(pdf);
        string imageFolder = Path.Combine(pdfBaseDirectory, pdfName);
        string cbzPath = imageFolder + ".cbz";
        string finalPdfPath = Path.Combine(pdfBaseDirectory, "Generated.pdf");

        // --------------------------------------------------------------------------------
        // Étape 1 : Extraire le PDF, redimensionner à la volée, et sauvegarder en WebP
        // --------------------------------------------------------------------------------
        Console.WriteLine("--- Étape 1 : Extraction et redimensionnement du PDF ---");
        var pdfImages = PdfManager.ExtractImages(pdf);
        
        // Création du pipeline LINQ : on applique le redimensionnement sur chaque image du flux
        var resizedImages = pdfImages.Select(img => ImageProcessor.ResizeToMaxPixels(img, 2_500_000));
        
        // On sauvegarde le résultat physique dans le dossier
        DirectoryManager.SaveImages(resizedImages, imageFolder, SKEncodedImageFormat.Webp, 40);

        // --------------------------------------------------------------------------------
        // Étape 2 : Créer l'archive CBZ à partir du dossier
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n--- Étape 2 : Création de l'archive CBZ ---");
        ArchiveManager.CompressDirectory(imageFolder, cbzPath);

        // --------------------------------------------------------------------------------
        // Étape 3 : Supprimer le dossier temporaire d'images
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n--- Étape 3 : Nettoyage du dossier temporaire ---");
        if (Directory.Exists(imageFolder)) Directory.Delete(imageFolder, true);

        // --------------------------------------------------------------------------------
        // Étape 4 : Générer le nouveau PDF DIRECTEMENT depuis l'archive CBZ (En mémoire)
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n--- Étape 4 : Génération du PDF final depuis le CBZ ---");
        // Magie du SRP : ArchiveManager lit le CBZ, PdfManager consomme les images. 
        // Zéro fichier temporaire créé sur le disque !
        var cbzImages = ArchiveManager.ExtractImages(cbzPath);
        PdfManager.CreatePdf(cbzImages, finalPdfPath, quality: 50);

        // --------------------------------------------------------------------------------
        // Étape 5 : Supprimer l'archive CBZ de test
        // --------------------------------------------------------------------------------
        Console.WriteLine("\n--- Étape 5 : Nettoyage de l'archive ---");
        if (File.Exists(cbzPath)) File.Delete(cbzPath);

        Console.WriteLine("\nDémonstration terminée !");
    }
}