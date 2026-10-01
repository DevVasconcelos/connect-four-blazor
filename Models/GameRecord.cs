using System.ComponentModel.DataAnnotations;

namespace ConnectFour.Models;

public sealed class GameRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Player 1 name is required.")]
    [StringLength(30, MinimumLength = 1, ErrorMessage = "Player 1 name must be 1 to 30 characters.")]
    public string PlayerOneName { get; set; } = "Player 1";

    [Required(ErrorMessage = "Player 2 name is required.")]
    [StringLength(30, MinimumLength = 1, ErrorMessage = "Player 2 name must be 1 to 30 characters.")]
    public string PlayerTwoName { get; set; } = "Player 2";

    [Required(ErrorMessage = "An outcome is required.")]
    [StringLength(30)]
    public string Outcome { get; set; } = "Draw";

    [Range(1, 42, ErrorMessage = "Move count must be between 1 and 42.")]
    public int MoveCount { get; set; } = 1;

    [StringLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
    public string? Notes { get; set; }

    public DateTime PlayedAtUtc { get; set; } = DateTime.UtcNow;
}
