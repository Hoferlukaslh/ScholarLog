/*
    Fichier      :  AppSettingsJsonContext.cs
    Projet       :  ScholarLog

    Description  :
        Contexte de sérialisation JSON généré à la compilation.
        Requis quand le trimming ou AOT est activé dans le projet,
        car la réflexion JSON classique est désactivée dans ce mode.
*/

using System.Text.Json.Serialization;

namespace ScholarLog.Data;

[JsonSerializable(typeof(AppSettings))]
internal partial class AppSettingsJsonContext : JsonSerializerContext { }