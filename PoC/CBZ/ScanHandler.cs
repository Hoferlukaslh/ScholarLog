namespace CBZ;

using SkiaSharp;
using System.IO.Compression;
using PdfConvert = PDFtoImage.Conversion;


/// <summary>
/// Classe permettant de gerer les scans.
/// Compression, ouverture des archives CBZ.
/// </summary>
public class ScanHandler
{
    // Crée un fichier PDF à partir d'un dossier contenant des images.
    public static void CreatePDF(string imagesFolder, string pdfPath, int quality = 50)
    {
        if (Directory.Exists(imagesFolder))
        {
            // Récupérer et trier les images (important pour l'ordre des pages)
            // Ne prendre que des images compatibles (Webp, Jpg, Png)
            var imageFiles = Directory.GetFiles(imagesFolder)
                                      .Where(f => f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) || 
                                                  f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                                  f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                                      .OrderBy(f => f) // Trie alphabétiquement
                                      .ToList();

            if (imageFiles.Count > 0)
            {
                var pdfMetadata = new SKDocumentPdfMetadata{EncodingQuality = quality };

                try
                {
                    Console.WriteLine($"Création du PDF avec {imageFiles.Count} pages...");
                    
                    

                    // Création du flux de sortie et du document PDF SkiaSharp
                    using (var stream = File.Create(pdfPath))
                    using (var document = SKDocument.CreatePdf(stream, pdfMetadata))
                    {
                        foreach (var imagePath in imageFiles)
                        {
                            // Charger l'image en mémoire
                            using (var bitmap = SKBitmap.Decode(imagePath))
                            {
                                if (bitmap == null) continue;

                                //  Créer une nouvelle page PDF à la taille exacte de l'image
                                using (var canvas = document.BeginPage(bitmap.Width, bitmap.Height))
                                {
                                    // Dessiner l'image sur la page
                                    canvas.DrawBitmap(bitmap, 0, 0);
                                } // Le canvas est libéré ici
                            
                                document.EndPage();
                            } // Le bitmap est libéré ici
                        }
                        
                        document.Close(); // Finaliser et fermer le document
                    }

                    Console.WriteLine($"Succès : PDF généré dans '{pdfPath}'.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Une erreur est survenue lors de la création du PDF : {ex.Message}");
                }
            }
            
            else Console.WriteLine("Erreur : Aucune image trouvée dans le dossier.");
        }
        
        else Console.WriteLine($"Erreur : Le dossier d'images '{imagesFolder}' n'existe pas.");
    }

    public static void ExtractCbzImages(string pathToArchive, string folderPath)
    {
        // Vérification de l'existence de l'archive
        if (File.Exists(pathToArchive))
        {
            try
            {
                // Création du dossier de destination s'il n'existe pas
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Extraction de l'archive
                ZipFile.ExtractToDirectory(pathToArchive, folderPath, true);
                Console.WriteLine($"Succès : Extraction terminée dans '{folderPath}'.");
            }
            catch (InvalidDataException)
            {
                Console.WriteLine("Erreur : Le fichier spécifié n'est pas une archive valide (il est peut-être corrompu).");
                    
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Une erreur inattendue est survenue : {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Erreur : L'archive '{pathToArchive}' est introuvable.");
        }
    }


    public static void ExtractPdfPages(string pdfPath, string outputFolder, 
        SKEncodedImageFormat format = SKEncodedImageFormat.Webp, int quality = 25)
    {
        if (!File.Exists(pdfPath)) return;
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        // On utilise un bloc using pour garantir la fermeture du fichier dès la fin de la fonction
        using (FileStream stream = File.OpenRead(pdfPath))
        {
            int i = 1;
            
            var images = PdfConvert.ToImages(stream, leaveOpen: true);

            foreach (SKBitmap originalBitmap in images)
            {
                if (originalBitmap == null || originalBitmap.IsEmpty) continue;

                // Utilisation d'un bloc try-finally pour s'assurer que TOUT est disposé
                SKBitmap finalBitmap = null;
                try
                {
                    finalBitmap = ResizeToMaxPixels(originalBitmap, 2_500_000);
                    string filePath = Path.Combine(outputFolder, $"{i:D3}.webp");

                    using (var image = SKImage.FromBitmap(finalBitmap))
                    using (var data = image.Encode(format, quality))
                    {
                        if (data != null)
                        {
                            using (var outputStream = File.Create(filePath))
                            {
                                data.SaveTo(outputStream);
                            }
                            Console.WriteLine($"Page {i} traitée.");
                        }
                    }
                }
                finally
                {
                    // CRITIQUE : Libérer l'image originale ET l'image redimensionnée
                    originalBitmap.Dispose(); 
                    finalBitmap?.Dispose();
                    i++;
                }
            }
        } // Le 'stream' est fermé et le fichier PDF est libéré ici.
    }
    
    private static SKBitmap ResizeToMaxPixels(SKBitmap source, double maxPixels)
    {
        double currentPixels = (double)source.Width * source.Height;

        // Si l'image est déjà plus petite que la cible, on ne fait rien
        if (currentPixels <= maxPixels)
        {
            // On retourne l'original (attention : ne pas dispose l'original si on l'utilise !)
            return source;
        }

        // Calcul du facteur d'échelle (Ratio)
        double scale = Math.Sqrt(maxPixels / currentPixels);
        int newWidth = (int)Math.Round(source.Width * scale);
        int newHeight = (int)Math.Round(source.Height * scale);

        // Redimensionnement avec haute qualité (Sinc/Lanczos)
        return source.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
    }
    
    
    /// Compresse le contenu d'un dossier dans un fichier ZIP.
    public static void CompresserRepertoire(string sourceDirectory, string destinationZipFilePath)
    {
        try
        {
            // Vérifier si le dossier source existe
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException($"Le dossier source n'existe pas : {sourceDirectory}");

            String destDirectory = "";
            
            // Vérifier si le dossier de destination existe, sinon le créer
            destDirectory = Path.GetDirectoryName(destinationZipFilePath);
            if (!Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);


            // Écrase si existe
            if (File.Exists(destinationZipFilePath)) File.Delete(destinationZipFilePath); 

            // Création de l'archive
            ZipFile.CreateFromDirectory(sourceDirectory, destinationZipFilePath, CompressionLevel.Optimal, false);

            Console.WriteLine("L'archive a été créée avec succès !");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur est survenue : {ex.Message}");
        }
    }
}