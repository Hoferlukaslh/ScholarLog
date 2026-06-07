# ==============================================================================
# Script de Build et de Packaging complet pour ScholarLog
# ==============================================================================

# Installer toutes les dépendances dans le WSL : 

# sudo apt update
# sudo apt install -y dpkg flatpak flatpak-builder
# flatpak remote-add --user --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo
# flatpak install --user flathub org.freedesktop.Platform//25.08 org.freedesktop.Sdk//25.08
# flatpak install --user flathub org.freedesktop.Platform//25.08 --arch=aarch64
# flatpak install --user flathub org.freedesktop.Sdk//25.08 --arch=aarch64
# sudo apt install qemu-user-static

$BuildDir = "$env:USERPROFILE\Desktop\Build"
$AppName = "ScholarLog"
$AppId = "com.HoferLukas.ScholarLog"
$SrcDir = $PSScriptRoot
$ImagesDir = "$SrcDir\Assets\Images"

# Chemin vers l'exécutable ISCC (vérifiez votre version et chemin)
$IsccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

# Fonction utilitaire : Écriture en UTF-8 SANS BOM (crucial pour Linux)
Function Write-LinuxFile($Path, $Content) {
    $Utf8NoBom = New-Object System.Text.UTF8Encoding($False)
    [System.IO.File]::WriteAllText($Path, $Content, $Utf8NoBom)
}

# Nettoyage et création du dossier de build
If (!(Test-Path $BuildDir)) { New-Item -ItemType Directory -Path $BuildDir | Out-Null }

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "1. GÉNÉRATION DES EXÉCUTABLES (.NET PUBLISH)" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

$Targets = @(
    @{ Rid = "win-x86"; Dir = "WIN_x86" },
    @{ Rid = "win-x64"; Dir = "WIN_x64" },
    @{ Rid = "win-arm64"; Dir = "WIN_arm64" },
    @{ Rid = "linux-x64"; Dir = "linux_x64" },
    @{ Rid = "linux-arm64"; Dir = "linux_arm64" },
    @{ Rid = "osx-x64"; Dir = "osx_x64" },
    @{ Rid = "osx-arm64"; Dir = "osx_arm64" }
)

foreach ($Target in $Targets) {
    $OutPath = "$BuildDir\$($Target.Dir)"
    Write-Host "-> Compilation pour $($Target.Rid)..." -ForegroundColor Yellow
    dotnet publish -c Release -f net10.0 -r $($Target.Rid) --self-contained true `
      -p:PublishSingleFile=true `
      -p:PublishTrimmed=true `
      -p:PublishReadyToRun=true `
      -p:IncludeNativeLibrariesForSelfExtract=true `
      -p:IncludeAllContentForSelfExtract=true `
      -o $OutPath | Out-Null
}

$LinuxArches = @(
    @{ Suffix = "x64"; DebArch = "amd64"; FlatpakArch = "x86_64" },
    @{ Suffix = "arm64"; DebArch = "arm64"; FlatpakArch = "aarch64" }
)

$BuildDirForWsl = $BuildDir -replace '\\', '/'
$WslBuildDir = (wsl wslpath -u "$BuildDirForWsl").Trim()

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "2. CRÉATION DES PAQUETS DEBIAN (.DEB) VIA WSL" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

foreach ($Arch in $LinuxArches) {
    Write-Host "-> Génération Debian ($($Arch.DebArch))..." -ForegroundColor Yellow
    $DebDirName = "scholarlog-$($Arch.DebArch)"
    $DebDir = "$BuildDir\$DebDirName"
    
    New-Item -ItemType Directory -Force -Path "$DebDir\usr\bin" | Out-Null
    New-Item -ItemType Directory -Force -Path "$DebDir\usr\share\applications" | Out-Null
    New-Item -ItemType Directory -Force -Path "$DebDir\usr\share\pixmaps" | Out-Null
    New-Item -ItemType Directory -Force -Path "$DebDir\DEBIAN" | Out-Null

    Copy-Item -Path "$BuildDir\linux_$($Arch.Suffix)\$AppName" -Destination "$DebDir\usr\bin\$AppName" -Force
    Copy-Item -Path "$ImagesDir\ScholarLog.png" -Destination "$DebDir\usr\share\pixmaps\ScholarLog.png" -Force
    Copy-Item -Path "$ImagesDir\ScholarLog.xpm" -Destination "$DebDir\usr\share\pixmaps\ScholarLog.xpm" -Force

    $Control = "Package: scholarlog`nVersion: 1.0.0`nSection: utils`nPriority: optional`nArchitecture: $($Arch.DebArch)`nMaintainer: Hofer Lukas <lukas.hofer.ju@gmail.com>`nDescription: Logiciel GUI (mono-utilisateur) d'inscription de notes et de journal de travail.`n"
    Write-LinuxFile "$DebDir\DEBIAN\control" $Control

    $Desktop = "[Desktop Entry]`nType=Application`nName=ScholarLog`nComment=Gestionnaire de logs scolaires`nExec=ScholarLog`nIcon=ScholarLog`nTerminal=false`nCategories=Education;Utility;`n"
    Write-LinuxFile "$DebDir\usr\share\applications\scholarlog.desktop" $Desktop

    $Cmd = "cp -r '$WslBuildDir/$DebDirName' /tmp/ && cd /tmp && chmod 755 $DebDirName/DEBIAN && chmod 644 $DebDirName/DEBIAN/control && chmod 755 $DebDirName && dpkg-deb --root-owner-group --build $DebDirName && cp $DebDirName.deb '$WslBuildDir/' && rm -rf $DebDirName $DebDirName.deb"
    wsl bash -c $Cmd
}

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "3. CRÉATION DES PAQUETS FLATPAK VIA WSL" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

foreach ($Arch in $LinuxArches) {
    Write-Host "-> Génération Flatpak ($($Arch.FlatpakArch))..." -ForegroundColor Yellow
    $Staging = "$BuildDir\flatpak-staging-$($Arch.Suffix)"
    New-Item -ItemType Directory -Force -Path $Staging | Out-Null

    Copy-Item -Path "$BuildDir\linux_$($Arch.Suffix)\$AppName" -Destination "$Staging\$AppName" -Force
    Copy-Item -Path "$ImagesDir\com.HoferLukas.ScholarLog.png" -Destination "$Staging\com.HoferLukas.ScholarLog.png" -Force

    $Desktop = "[Desktop Entry]`nType=Application`nName=ScholarLog`nExec=ScholarLog`nIcon=$AppId`nCategories=Education;Utility;`n"
    Write-LinuxFile "$Staging\$AppId.desktop" $Desktop

    $Manifest = "app-id: $AppId`nruntime: org.freedesktop.Platform`nruntime-version: '25.08'`nsdk: org.freedesktop.Sdk`ncommand: $AppName`nfinish-args:`n  - --socket=x11`n  - --socket=wayland`n  - --device=dri`n  - --filesystem=host`nmodules:`n  - name: scholarlog`n    buildsystem: simple`n    build-commands:`n      - install -D $AppName /app/bin/$AppName`n      - install -D $AppId.desktop /app/share/applications/$AppId.desktop`n      - install -D com.HoferLukas.ScholarLog.png /app/share/icons/hicolor/256x256/apps/com.HoferLukas.ScholarLog.png`n    sources:`n      - type: file`n        path: $AppName`n      - type: file`n        path: $AppId.desktop`n      - type: file`n        path: com.HoferLukas.ScholarLog.png`n"
    Write-LinuxFile "$Staging\$AppId.yml" $Manifest

    $StagingWsl = "$WslBuildDir/flatpak-staging-$($Arch.Suffix)"
    $Cmd = "mkdir -p /tmp/flatpak-build-$($Arch.Suffix) && cp -r '$StagingWsl/'* /tmp/flatpak-build-$($Arch.Suffix)/ && cd /tmp/flatpak-build-$($Arch.Suffix) && flatpak-builder --arch=$($Arch.FlatpakArch) build-dir $AppId.yml --force-clean && flatpak build-export repo build-dir && flatpak build-bundle --arch=$($Arch.FlatpakArch) repo ScholarLog-$($Arch.Suffix).flatpak $AppId && cp ScholarLog-$($Arch.Suffix).flatpak '$WslBuildDir/' && rm -rf /tmp/flatpak-build-$($Arch.Suffix)"
    wsl bash -c $Cmd
}

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "4. GÉNÉRATION DE L'INSTALLATEUR WINDOWS (INNO SETUP)" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan



if (Test-Path $IsccPath) {
    Write-Host "-> Compilation de l'installateur avec Inno Setup..." -ForegroundColor Yellow
    
    # Chemin vers votre fichier .iss
    $IssScript = "$SrcDir\..\Installers\Windows\InnoSetupConfig.iss"
    
    # Appel du compilateur
    & $IsccPath $IssScript
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "-> Installateur Windows généré avec succès !" -ForegroundColor Green
    } else {
        Write-Host "-> Erreur lors de la compilation Inno Setup." -ForegroundColor Red
    }
} else {
    Write-Host "-> Inno Setup non trouvé à l'emplacement : $IsccPath. Installation ignorée." -ForegroundColor Red
}

Write-Host "`n==================================================" -ForegroundColor Green
Write-Host " DÉPLOIEMENT TERMINÉ !" -ForegroundColor Green