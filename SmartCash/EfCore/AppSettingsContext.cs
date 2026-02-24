using System.Text.Json.Serialization;
using SmartCash.EfCore.Models;

namespace SmartCash.EfCore
{
    // O Source Generator lerá esta classe e criará a lógica de tradução JSON em binário nativo
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(AppSettingsModel))]
    public partial class AppSettingsContext : JsonSerializerContext
    {
    }
}