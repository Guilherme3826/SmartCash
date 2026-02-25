namespace SmartCash.EfCore.Models
{
    public class AppSettingsModel
    {
        public string? Ambiente { get; set; }
        public bool ModoEscuro { get; set; }

        // Token do sistema (Oculto)
        public string? BookmarkPastaBackup { get; set; }

        // Nome visual para a interface do usuário
        public string? NomePastaBackup { get; set; }
    }
}