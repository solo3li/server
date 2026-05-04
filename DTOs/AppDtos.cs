namespace Uis.Server.DTOs;

public class LoginDto { 
    public string Email { get; set; } = string.Empty; 
    public string Password { get; set; } = string.Empty; 
}

public class RegisterDto { 
    public string FullName { get; set; } = string.Empty; 
    public string Email { get; set; } = string.Empty; 
    public string Password { get; set; } = string.Empty; 
    public string Role { get; set; } = "Student"; 
}

public class CreateOrderDto { 
    public Guid ServiceId { get; set; } 
    public decimal Price { get; set; } 
}

public class KycSubmitDto {
    public string NationalId { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public IFormFile? NationalIdFront { get; set; }
    public IFormFile? NationalIdBack { get; set; }
}