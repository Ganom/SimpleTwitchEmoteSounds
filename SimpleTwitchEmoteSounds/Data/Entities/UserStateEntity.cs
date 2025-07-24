#region

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace SimpleTwitchEmoteSounds.Data.Entities;

[Table("UserState")]
public class UserStateEntity
{
    [Key]
    public int Id { get; set; } = 1;

    public string Username { get; set; } = "";
    public double Height { get; set; } = 1200;
    public double Width { get; set; } = 900;
    public int PosX { get; set; }
    public int PosY { get; set; }
}
