using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleTwitchEmoteSounds.Data.Entities;

[Table("AppSettings")]
public class AppSettingsEntity
{
    [Key]
    public int Id { get; set; } = 1;
    
    public string EnableHotkeyData { get; set; } = string.Empty;
    
    public List<SoundCommandEntity> SoundCommands { get; set; } = [];
}