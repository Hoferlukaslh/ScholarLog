# ScanHandler
Ce module a pour objectif de transformer des documents PDF volumineux en archives CBZ (Comic Book Archive) optimisées. Cette transformation permet de réduire drastiquement l'empreinte mémoire des documents avant leur stockage dans une base de données SQLite sous forme de BLOB.

Le but ultime est de garantir que le stockage total des scans ne dépasse pas 60 Mo pour l'ensemble de la base de données, tout en conservant une lisibilité optimale.


| Classe           | Rôle Principal                                                             |
|------------------|----------------------------------------------------------------------------|
| ImageProcessor   | Calculs géométriques et redimensionnement haute qualité.                   |
| PdfManager       | Extraction des flux d'images depuis un PDF et génération de fichiers .pdf. |
| ArchiveManager   | Lecture/Écriture des archives .cbz et filtrage des formats supportés.      |
| DirectoryManager | Pont entre les flux d'images en mémoire et le système de fichiers          |


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
  DirectoryManager ..> ArchiveManager : utilise (IsSupportedImageFilePDF)
    
```