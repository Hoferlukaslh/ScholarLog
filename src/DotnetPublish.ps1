# Définition du chemin vers le dossier Build sur le bureau de l'utilisateur actuel
$BuildDir = "$env:USERPROFILE\Desktop\Build"

# --- Publier pour Windows --------------------------------------------

# x86
dotnet publish -c Release -f net10.0 -r win-x86 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeAllContentForSelfExtract=true `
  -o "$BuildDir\WIN_x86"

# x64
dotnet publish -c Release -f net10.0 -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeAllContentForSelfExtract=true `
  -o "$BuildDir\WIN_x64"

# ARM64
dotnet publish -c Release -f net10.0 -r win-arm64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeAllContentForSelfExtract=true `
  -o "$BuildDir\WIN_arm64"


# --- Publier pour Linux ------------------------------------------------

# x64
dotnet publish -c Release -f net10.0 -r linux-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeAllContentForSelfExtract=true `
  -o "$BuildDir\linux_x64"

# ARM64
dotnet publish -c Release -f net10.0 -r linux-arm64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeAllContentForSelfExtract=true `
  -o "$BuildDir\linux_arm64"


# --- Publier pour MacOS ----------------------------------------------

# x64
dotnet publish -c Release -f net10.0 -r osx-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeAllContentForSelfExtract=true `
  -o "$BuildDir\osx_x64"

# ARM64
dotnet publish -c Release -f net10.0 -r osx-arm64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=true `
  -p:PublishReadyToRun=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeAllContentForSelfExtract=true `
  -o "$BuildDir\osx_arm64"