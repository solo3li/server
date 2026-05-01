using Microsoft.EntityFrameworkCore;
using System.IO;
using Uis.Server.Data;
using Uis.Server.Models;
using Uis.Server.DTOs;

namespace Uis.Server.Services;

public interface IKycService { Task<bool> SubmitKycAsync(Guid userId, string nationalId, string phone, string? frontUrl, string? backUrl); Task<IEnumerable<KycRequest>> GetPendingRequestsAsync(); }
public class KycService : IKycService {
    private readonly ApplicationDbContext _db; public KycService(ApplicationDbContext db) { _db = db; }
    public async Task<bool> SubmitKycAsync(Guid userId, string nationalId, string phone, string? frontUrl, string? backUrl) {
        _db.KycRequests.Add(new KycRequest { 
            UserId = userId, 
            NationalId = nationalId, 
            Phone = phone,
            NationalIdFrontUrl = frontUrl,
            NationalIdBackUrl = backUrl,
            Status = "Pending"
        });
        await _db.SaveChangesAsync(); return true;
    }
    public async Task<IEnumerable<KycRequest>> GetPendingRequestsAsync() => await _db.KycRequests.Include(k=>k.User).Where(k => k.Status == "Pending").ToListAsync();
}

public interface ICatalogService { Task<IEnumerable<Service>> GetServicesAsync(); Task<IEnumerable<Category>> GetCategoriesAsync(); }
public class CatalogService : ICatalogService {
    private readonly ApplicationDbContext _db; public CatalogService(ApplicationDbContext db) { _db = db; }
    public async Task<IEnumerable<Service>> GetServicesAsync() => await _db.Services.Include(s => s.Category).ToListAsync();
    public async Task<IEnumerable<Category>> GetCategoriesAsync() => await _db.Categories.ToListAsync();
}

public interface IOrderService { Task<Order> CreateOrderAsync(Guid studentId, CreateOrderDto dto); Task<IEnumerable<Order>> GetOrdersAsync(); }
public class OrderService : IOrderService {
    private readonly ApplicationDbContext _db; public OrderService(ApplicationDbContext db) { _db = db; }
    public async Task<Order> CreateOrderAsync(Guid studentId, CreateOrderDto dto) {
        var order = new Order { StudentId = studentId, ServiceId = dto.ServiceId, Price = dto.Price };
        _db.Orders.Add(order); await _db.SaveChangesAsync(); return order;
    }
    public async Task<IEnumerable<Order>> GetOrdersAsync() => await _db.Orders.Include(o=>o.Student).Include(o=>o.Service).ToListAsync();
}

public interface IPaymentService { Task<bool> ProcessPaymentAsync(Guid orderId, decimal amount); }
public class PaymentService : IPaymentService {
    private readonly ApplicationDbContext _db; public PaymentService(ApplicationDbContext db) { _db = db; }
    public async Task<bool> ProcessPaymentAsync(Guid orderId, decimal amount) {
        _db.Payments.Add(new Payment { OrderId = orderId, Amount = amount, Status = "Completed", TransactionId = Guid.NewGuid().ToString() });
        await _db.SaveChangesAsync(); return true;
    }
}

public interface IEscrowService { Task<bool> HoldFundsAsync(Guid orderId, decimal amount); }
public class EscrowService : IEscrowService {
    private readonly ApplicationDbContext _db; public EscrowService(ApplicationDbContext db) { _db = db; }
    public async Task<bool> HoldFundsAsync(Guid orderId, decimal amount) {
        _db.Escrows.Add(new Escrow { OrderId = orderId, Amount = amount }); await _db.SaveChangesAsync(); return true;
    }
}

public interface IChatService { Task<Chat> CreateChatAsync(Guid orderId); }
public class ChatService : IChatService {
    private readonly ApplicationDbContext _db; public ChatService(ApplicationDbContext db) { _db = db; }
    public async Task<Chat> CreateChatAsync(Guid orderId) {
        var chat = new Chat { OrderId = orderId }; _db.Chats.Add(chat); await _db.SaveChangesAsync(); return chat;
    }
}

public interface IFileService { Task<string> UploadFileAsync(Stream fileStream, string fileName); }
public class FileService : IFileService {
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName) {
        var path = Path.Combine("wwwroot", "uploads", fileName);
        Directory.CreateDirectory(Path.Combine("wwwroot", "uploads"));
        using var stream = new FileStream(path, FileMode.Create);
        await fileStream.CopyToAsync(stream);
        return $"/uploads/{fileName}";
    }
}

public interface ITicketService { Task<Ticket> CreateTicketAsync(Guid userId, string subject); }
public class TicketService : ITicketService {
    private readonly ApplicationDbContext _db; public TicketService(ApplicationDbContext db) { _db = db; }
    public async Task<Ticket> CreateTicketAsync(Guid userId, string subject) {
        var ticket = new Ticket { UserId = userId, Subject = subject }; _db.Tickets.Add(ticket); await _db.SaveChangesAsync(); return ticket;
    }
}

public interface INotificationService 
{ 
    Task SendNotificationAsync(Guid userId, string message, string? title = null); 
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId);
    Task<List<Notification>> GetAllNotificationsAsync(int count = 50);
    Task MarkAsReadAsync(Guid notificationId);
    Task DeleteNotificationAsync(Guid notificationId);
}

public class NotificationService : INotificationService {
    private readonly ApplicationDbContext _db; 
    private readonly IEmailService _emailService;
    public NotificationService(ApplicationDbContext db, IEmailService emailService) { _db = db; _emailService = emailService; }

    public async Task SendNotificationAsync(Guid userId, string message, string? title = null) {
        _db.Notifications.Add(new Notification { UserId = userId, Message = message }); 
        await _db.SaveChangesAsync();

        // Also send email notification
        var user = await _db.Users.FindAsync(userId);
        if (user != null && !string.IsNullOrEmpty(user.Email)) {
            await _emailService.SendTemplatedEmailAsync(
                user.Email, 
                title ?? "تنبيه جديد من UIS", 
                title ?? "إشعار جديد", 
                message,
                "فتح التطبيق",
                "http://localhost:8081" // Mobile deep link placeholder
            );
        }
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId) {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetAllNotificationsAsync(int count = 50) {
        return await _db.Notifications
            .Include(n => n.User)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId) {
        var n = await _db.Notifications.FindAsync(notificationId);
        if (n != null) {
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task DeleteNotificationAsync(Guid notificationId) {
        var n = await _db.Notifications.FindAsync(notificationId);
        if (n != null) {
            _db.Notifications.Remove(n);
            await _db.SaveChangesAsync();
        }
    }
}