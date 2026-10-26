using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

// Request bodies. [ApiController] returns 400 automatically when these rules fail.

public class RegisterRequest
{
    [Required, MaxLength(50)] public string FirstName { get; set; } = "";
    [Required, MaxLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = "";

    // At least 8 characters with an upper-case letter, a lower-case letter, a number and a symbol.
    [Required]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,100}$",
        ErrorMessage = "Password needs at least 8 characters, with an upper-case letter, a lower-case letter, a number and a symbol.")]
    public string Password { get; set; } = "";

    /// <summary>Organiser or Participant.</summary>
    [Required] public string Role { get; set; } = "";
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}

public class LoginRequest
{
    [Required] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class UpdateProfileRequest
{
    [Required, MaxLength(50)] public string FirstName { get; set; } = "";
    [Required, MaxLength(50)] public string LastName { get; set; } = "";
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}

public class EventRequest
{
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(2000)] public string Description { get; set; } = "";
    [Required] public DateTime EventDate { get; set; }
    [Required, MaxLength(200)] public string Location { get; set; } = "";
    [Range(0.01, 9999.99)] public decimal DistanceKm { get; set; }
    [Range(1, int.MaxValue)] public int EventTypeId { get; set; }
}

public class CategoryRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    /// <summary>Age or Distance.</summary>
    [Required] public string CategoryType { get; set; } = "";
    [Range(0, 120)] public int? MinAge { get; set; }
    [Range(0, 120)] public int? MaxAge { get; set; }
    [Range(0.01, 9999.99)] public decimal? DistanceKm { get; set; }
    [Range(1, int.MaxValue)] public int? MaxParticipants { get; set; }
}

public class EnrolRequest
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

public class StatusRequest
{
    /// <summary>Pending, Confirmed or Cancelled.</summary>
    [Required] public string Status { get; set; } = "";
}

public class ResultRequest
{
    [Range(1, int.MaxValue)] public int EnrolmentId { get; set; }
    /// <summary>hh:mm:ss, e.g. 01:21:37</summary>
    [Required] public TimeSpan FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int FinishPosition { get; set; }
}

public class UpdateResultRequest
{
    /// <summary>hh:mm:ss, e.g. 01:21:37</summary>
    [Required] public TimeSpan FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int FinishPosition { get; set; }
}
