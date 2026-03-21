# ScanHandler
explication

but
objectif


## Diagramme de classe 
``` mermaid
classDiagram

  class ImageProcessor {
    %% Responsabilité : Manipulation d'images en mémoire %%
    +ResizeToMaxPixels(source: SKBitmap, maxPixels: double) SKBitmap$
  }

  class PdfManager {
    %% Responsabilité : Génération et lecture de PDF %%
    +CreatePdf(images: IEnumerable~SKBitmap~, pdfPath: string, quality: int)$
    +ExtractImages(pdfPath: string) IEnumerable~SKBitmap~$
  }

  class ArchiveManager {
    %% Responsabilité : Manipulation de fichiers compressés %%
    +ExtractImages(pathToArchive: string) IEnumerable~SKBitmap~$
    +CompressDirectory(sourceDirectory: string, destinationZipFilePath: string)$
    ~IsSupportedImageFilePDF(filePath: string) bool$
  }

  class DirectoryManager {
    %% Responsabilité : Interactions avec les dossiers classiques %%
    +GetImagesFromDirectory(folderPath: string) IEnumerable~SKBitmap~$
    +SaveImages(images: IEnumerable~SKBitmap~, outputFolder: string, format: SKEncodedImageFormat, quality: int)$
  }

  %% Relations d'utilisation (dépendances) %%
  DirectoryManager ..> ArchiveManager : utilise (IsImageFile)
    
```