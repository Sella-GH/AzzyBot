namespace AzzyBot.Database.Models;

internal class AzuraCastChecksEntity
{
    public int Id { get; set; }
    public bool FileChanges { get; set; }
    public bool ServerStatus { get; set; }
    public bool Updates { get; set; }
    public bool UpdatesShowChangelog { get; set; }

    public int AzuraCastId { get; set; }
    public virtual AzuraCastEntity AzuraCast { get; set; } = new();
}
