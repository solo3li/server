using System;
using System.Collections.Generic;

namespace Uis.Server.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? University { get; set; }
    public string? Major { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePicture { get; set; }
    
    // Boolean flags for easy access control
    public bool IsAdmin { get; set; } = false;
    public bool IsExecutor { get; set; } = false;
    public bool IsStaff { get; set; } = false;

    // Many-to-many relationship with Roles
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Stats
    public decimal Rating { get; set; } = 5.0m;
    public int CompletedOrdersCount { get; set; } = 0;
}

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // Student, Executor, Admin
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; } = false;
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // e.g., "Users.View", "Orders.Manage"
    public string? Group { get; set; } // e.g., "Management", "Finance"
    public string? Description { get; set; }
}

public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

public class EmailOtp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsUsed { get; set; }
}

public class KycRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string NationalId { get; set; } = string.Empty;
    public string? NationalIdFrontUrl { get; set; }
    public string? NationalIdBackUrl { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime? IdExpiryDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string RejectionReason { get; set; } = string.Empty;
}

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}

public class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    
    // New fields
    public Guid? ExecutorId { get; set; }
    public User? Executor { get; set; }
    public decimal Rating { get; set; } = 5.0m;
    public int ReviewsCount { get; set; } = 0;
    public string DeliveryTime { get; set; } = "يومان";
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;
    public Guid? ExecutorId { get; set; }
    public User? Executor { get; set; }
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public decimal Price { get; set; }
    public string Status { get; set; } = "AwaitingPayment";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public string TransactionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Escrow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Held"; // Held, Released, Refunded
}

public class Chat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    
    // For direct chats not linked to orders
    public Guid? StudentId { get; set; }
    public User? Student { get; set; }
    public Guid? ExecutorId { get; set; }
    public User? Executor { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid? OrderId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
}

public class TicketMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentType { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FileAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FilePath { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty; // Order, Kyc, Ticket
    public Guid EntityId { get; set; }
}

public class SystemSetting
{
    public string Key { get; set; } = string.Empty; // e.g., "SmtpHost", "EmailTemplate"
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
