using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleTwitchEmoteSounds.Data.Entities;

[Table("SoundFiles")]
public class SoundFileEntity
{
    [Key]
    public int Id { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Percentage { get; set; } = "1";
    
    public int SoundCommandId { get; set; }
    public SoundCommandEntity SoundCommand { get; set; } = null!;
}