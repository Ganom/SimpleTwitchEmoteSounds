using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleTwitchEmoteSounds.Data.Entities;

[Table("SoundCommands")]
public class SoundCommandEntity
{
    [Key]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool IsExpanded { get; set; } = true;
    public string PlayChance { get; set; } = "1";
    public int SelectedMatchType { get; set; } = 1;
    public string Volume { get; set; } = "1";
    public int TimesPlayed { get; set; }
    public string CooldownSeconds { get; set; } = "0";
    
    public int AppSettingsId { get; set; }
    public AppSettingsEntity AppSettings { get; set; } = null!;
    
    public List<SoundFileEntity> SoundFiles { get; set; } = [];
}