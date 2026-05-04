using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Uis.Server.DTOs;
using Uis.Server.Models;
using Uis.Server.Services;
using Uis.Server.Data;
using System.Security.Claims;

namespace Uis.Server.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ApplicationDbContext _db;
    public AuthController(IAuthService auth, ApplicationDbContext db) { _auth = auth; _db = db; }

    [HttpPost("login")] public async Task<IActionResult> Login(LoginDto dto) {
        var token = await _auth.LoginAsync(dto);
        if (token == null) return Unauthorized();
        
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == dto.Email);
        return Ok(new { 
            Token = token, 
            Id = user?.Id,
            Name = user?.FullName,
            Email = user?.Email,
            IsExecutor = user?.IsExecutor,
            Roles = user?.Roles.Select(r => r.Name)
        });
    }

    [HttpPost("register")] public async Task<IActionResult> Register(RegisterDto dto) {
        var success = await _auth.RegisterAsync(dto);
        if (!success) return BadRequest("Registration failed.");
        return Ok("User registered.");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound("User not found.");

        return Ok(new { Message = "You can now reset your password." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return NotFound("User not found.");

        user.PasswordHash = request.NewPassword;
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Password reset successfully." });
    }
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase {
    private readonly ApplicationDbContext _db; public UsersController(ApplicationDbContext db) { _db = db; }
    
    [HttpGet("Me")] public async Task<IActionResult> GetMe() {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == Guid.Parse(userIdStr));
        if(user == null) return NotFound();
        return Ok(new {
            user.Id,
            user.FullName,
            user.Email,
            user.IsExecutor,
            user.IsAdmin,
            user.Rating,
            user.CompletedOrdersCount,
            user.ProfilePicture,
            user.University,
            user.Major,
            user.Bio,
            Roles = user.Roles.Select(r => r.Name)
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase {
    private readonly ApplicationDbContext _db; public ServicesController(ApplicationDbContext db) { _db = db; }
    [HttpGet] public async Task<IActionResult> GetAll() {
        var services = await _db.Services.Include(s => s.Category).Include(s => s.Executor).Where(s => s.IsActive).ToListAsync();
        return Ok(services.Select(s => new {
            s.Id, s.Title, s.Description, s.BasePrice, CategoryName = s.Category.Name, s.CategoryId, s.ImageUrl,
            s.Rating, s.ReviewsCount, s.DeliveryTime,
            ProviderName = s.Executor?.FullName ?? "منصة UIS",
            ProviderAvatarUrl = s.Executor?.ProfilePicture
        }));
    }
    
    [HttpGet("{id}")] public async Task<IActionResult> GetById(Guid id) {
        var s = await _db.Services.Include(s => s.Category).Include(s => s.Executor).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        return Ok(new {
            s.Id, s.Title, s.Description, s.BasePrice, CategoryName = s.Category.Name, s.CategoryId, s.ImageUrl,
            s.Rating, s.ReviewsCount, s.DeliveryTime,
            ProviderName = s.Executor?.FullName ?? "منصة UIS",
            ProviderAvatarUrl = s.Executor?.ProfilePicture
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase {
    private readonly ApplicationDbContext _db; public CategoriesController(ApplicationDbContext db) { _db = db; }
    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _db.Categories.ToListAsync());
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase {
    private readonly ApplicationDbContext _db; 
    public OrdersController(ApplicationDbContext db) { _db = db; }

    [HttpGet] public async Task<IActionResult> GetMyOrders() {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        var uid = Guid.Parse(userIdStr);
        var orders = await _db.Orders.Include(o => o.Service).Include(o => o.Student).Include(o => o.Executor)
            .Where(o => o.StudentId == uid || o.ExecutorId == uid)
            .OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(o => new {
            o.Id, o.Status, o.Price, o.CreatedAt, 
            ServiceTitle = o.Service.Title, o.ServiceId, 
            ServiceImageUrl = o.Service.ImageUrl,
            o.StudentId, o.ExecutorId,
            StudentName = o.Student.FullName,
            ExecutorName = o.Executor?.FullName,
            ServiceCategory = o.Service.Category?.Name
        }));
    }

    [HttpGet("Available")] public async Task<IActionResult> GetAvailableOrders() {
        var orders = await _db.Orders.Include(o => o.Service).Include(o => o.Student)
            .Where(o => o.Status == "Pending").OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(o => new {
            o.Id, o.Status, o.Price, o.CreatedAt, ServiceTitle = o.Service.Title, StudentName = o.Student.FullName,
            ServiceImageUrl = o.Service.ImageUrl
        }));
    }

    [HttpGet("{id}")] public async Task<IActionResult> GetById(Guid id) {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        var uid = Guid.Parse(userIdStr);
        var o = await _db.Orders.Include(x => x.Service).Include(x => x.Student).Include(x => x.Executor)
            .FirstOrDefaultAsync(x => x.Id == id && (x.StudentId == uid || x.ExecutorId == uid));
        if (o == null) return NotFound();
        return Ok(new {
            o.Id, o.Status, o.Price, o.CreatedAt, 
            ServiceTitle = o.Service.Title, o.ServiceId, 
            ServiceImageUrl = o.Service.ImageUrl,
            o.StudentId, o.ExecutorId,
            StudentName = o.Student.FullName,
            ExecutorName = o.Executor?.FullName
        });
    }

    [HttpPost] public async Task<IActionResult> Create(CreateOrderDto dto) {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        
        var studentId = Guid.Parse(userIdStr);
        
        // Ensure student exists in DB (avoid FK violation if token is old)
        var student = await _db.Users.FindAsync(studentId);
        if (student == null) return Unauthorized("User not found in database.");

        var order = new Order { 
            StudentId = studentId, 
            ServiceId = dto.ServiceId, 
            Price = dto.Price,
            Status = "Pending" // Set to Pending immediately since we are bypassing payment
        };
        
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return Ok(order);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase {
    private readonly ApplicationDbContext _db;
    private readonly IEscrowService _escrow;
    public PaymentsController(ApplicationDbContext db, IEscrowService escrow) { _db = db; _escrow = escrow; }
    
    [HttpPost("{orderId}")] public async Task<IActionResult> Process(Guid orderId, [FromBody] decimal amount) {
        _db.Payments.Add(new Payment { OrderId = orderId, Amount = amount, Status = "Completed", TransactionId = Guid.NewGuid().ToString() });
        var order = await _db.Orders.FindAsync(orderId);
        if(order != null) { 
            order.Status = "Pending"; 
            await _escrow.HoldFundsAsync(orderId, amount);
        }
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet("Earnings")]
    public async Task<IActionResult> GetMyEarnings()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        var uid = Guid.Parse(userIdStr);

        var earnings = await _db.Payments
            .Include(p => p.Order)
            .Where(p => p.Order.ExecutorId == uid && p.Status == "Completed")
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        var total = earnings.Sum(e => e.Amount);
        
        return Ok(new {
            Total = total,
            Transactions = earnings.Select(e => new {
                e.Id,
                e.Amount,
                e.Order.ServiceId,
                Title = "إتمام طلب #" + e.OrderId.ToString().Substring(0, 8),
                Date = e.Order.CreatedAt,
                Type = "in"
            })
        });
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase {
    private readonly ApplicationDbContext _db; 
    private readonly IFileService _fileService;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<Uis.Server.Hubs.ChatHub> _hub;
    public ChatController(ApplicationDbContext db, IFileService fileService, Microsoft.AspNetCore.SignalR.IHubContext<Uis.Server.Hubs.ChatHub> hub) { 
        _db = db; 
        _fileService = fileService;
        _hub = hub;
    }
    
    [HttpGet("Order/{orderId}")] public async Task<IActionResult> GetOrderChat(Guid orderId) {
        var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(myIdStr == null) return Unauthorized();
        var myId = Guid.Parse(myIdStr);

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && (o.StudentId == myId || o.ExecutorId == myId));
        if (order == null) return NotFound("Order not found or access denied.");

        var chat = await _db.Chats.Include(c => c.Messages).ThenInclude(m => m.Sender).FirstOrDefaultAsync(c => c.OrderId == orderId);
        
        if (chat == null) {
            chat = new Chat { OrderId = orderId };
            _db.Chats.Add(chat);
            await _db.SaveChangesAsync();
        }

        return Ok(new {
            chat.Id,
            Messages = chat.Messages.OrderBy(m => m.SentAt).Select(m => new {
                m.Id, m.Content, m.SentAt, m.SenderId, SenderName = m.Sender.FullName, m.AttachmentUrl, m.AttachmentType
            })
        });
    }

    [HttpGet("Private/{userId}")] public async Task<IActionResult> GetPrivateChat(Guid userId) {
        var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(myIdStr == null) return Unauthorized();
        var myId = Guid.Parse(myIdStr);

        var chat = await _db.Chats.Include(c => c.Messages).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.OrderId == null && 
            ((c.StudentId == myId && c.ExecutorId == userId) || (c.StudentId == userId && c.ExecutorId == myId)));
        
        if (chat == null) {
            chat = new Chat { StudentId = myId, ExecutorId = userId };
            _db.Chats.Add(chat);
            await _db.SaveChangesAsync();
        }

        return Ok(new {
            chat.Id,
            Messages = chat.Messages.OrderBy(m => m.SentAt).Select(m => new {
                m.Id, m.Content, m.SentAt, m.SenderId, SenderName = m.Sender.FullName, m.AttachmentUrl, m.AttachmentType
            })
        });
    }

    [HttpPost("{chatId}/Message")] public async Task<IActionResult> SendMessage(Guid chatId, [FromForm] string? content, IFormFile? attachment, [FromForm] string? attachmentType) {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        var uid = Guid.Parse(userIdStr);
        var sender = await _db.Users.FindAsync(uid);
        
        var msg = new Message { ChatId = chatId, SenderId = uid, Content = content ?? "", SentAt = DateTime.UtcNow };

        if (attachment != null && attachment.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(attachment.FileName);
            msg.AttachmentUrl = await _fileService.UploadFileAsync(attachment.OpenReadStream(), fileName);
            msg.AttachmentType = attachmentType ?? "file";
        }

        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", new {
            Id = msg.Id,
            Content = msg.Content,
            SentAt = msg.SentAt,
            SenderId = msg.SenderId,
            SenderName = sender?.FullName,
            AttachmentUrl = msg.AttachmentUrl,
            AttachmentType = msg.AttachmentType
        });

        return Ok(msg);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketController : ControllerBase {
    private readonly ApplicationDbContext _db; 
    private readonly IFileService _fileService;
    private readonly IHubContext<Uis.Server.Hubs.ChatHub> _hub;
    public TicketController(ApplicationDbContext db, IFileService fileService, IHubContext<Uis.Server.Hubs.ChatHub> hub) { 
        _db = db; 
        _fileService = fileService;
        _hub = hub;
    }
    
    [HttpGet] public async Task<IActionResult> GetMyTickets() {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        var uid = Guid.Parse(userIdStr);
        var tickets = await _db.Tickets.Where(t => t.UserId == uid).OrderByDescending(t => t.Id).ToListAsync();
        return Ok(tickets.Select(t => new { t.Id, t.Subject, t.Status }));
    }

    [HttpGet("{id}")] public async Task<IActionResult> GetById(Guid id) {
        var ticket = await _db.Tickets.Include(t => t.Messages).ThenInclude(m => m.Sender).FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null) return NotFound();
        return Ok(new {
            ticket.Id, ticket.Subject, ticket.Status,
            Messages = ticket.Messages.OrderBy(m => m.SentAt).Select(m => new {
                m.Id, m.Content, m.SentAt, m.SenderId, SenderName = m.Sender.FullName, m.AttachmentUrl, m.AttachmentType
            })
        });
    }

    [HttpPost] public async Task<IActionResult> Create([FromBody] string subject) {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        var ticket = new Ticket { UserId = Guid.Parse(userIdStr), Subject = subject };
        _db.Tickets.Add(ticket); await _db.SaveChangesAsync();
        return Ok(ticket);
    }

    [HttpPost("{id}/Reply")] public async Task<IActionResult> Reply(Guid id, [FromForm] string? content, IFormFile? attachment, [FromForm] string? attachmentType) {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        var uid = Guid.Parse(userIdStr);
        var sender = await _db.Users.FindAsync(uid);

        var msg = new TicketMessage { TicketId = id, SenderId = uid, Content = content ?? "", SentAt = DateTime.UtcNow };

        if (attachment != null && attachment.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(attachment.FileName);
            msg.AttachmentUrl = await _fileService.UploadFileAsync(attachment.OpenReadStream(), fileName);
            msg.AttachmentType = attachmentType ?? "file";
        }

        _db.TicketMessages.Add(msg);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group("ticket-" + id.ToString()).SendAsync("ReceiveTicketMessage", new {
            Id = msg.Id,
            Content = msg.Content,
            SentAt = msg.SentAt,
            SenderId = msg.SenderId,
            SenderName = sender?.FullName,
            AttachmentUrl = msg.AttachmentUrl,
            AttachmentType = msg.AttachmentType
        });

        return Ok(msg);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KycController : ControllerBase {
    private readonly ApplicationDbContext _db;
    private readonly IFileService _fileService;
    private readonly IKycService _kycService;

    public KycController(ApplicationDbContext db, IFileService fileService, IKycService kycService) { 
        _db = db; 
        _fileService = fileService;
        _kycService = kycService;
    }

    [HttpGet("Status")] public async Task<IActionResult> GetStatus() {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        var uid = Guid.Parse(userIdStr);
        var kyc = await _db.KycRequests.OrderByDescending(k => k.Id).FirstOrDefaultAsync(k => k.UserId == uid);
        return Ok(new { Status = kyc?.Status, RejectionReason = kyc?.RejectionReason });
    }

    [HttpPost] 
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Submit([FromForm] KycSubmitDto dto) {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        var uid = Guid.Parse(userIdStr);

        string? frontUrl = null;
        string? backUrl = null;

        if (dto.NationalIdFront != null && dto.NationalIdFront.Length > 0) {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.NationalIdFront.FileName);
            frontUrl = await _fileService.UploadFileAsync(dto.NationalIdFront.OpenReadStream(), fileName);
        }

        if (dto.NationalIdBack != null && dto.NationalIdBack.Length > 0) {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.NationalIdBack.FileName);
            backUrl = await _fileService.UploadFileAsync(dto.NationalIdBack.OpenReadStream(), fileName);
        }

        await _kycService.SubmitKycAsync(uid, dto.NationalId, dto.Phone, frontUrl, backUrl);
        return Ok(new { success = true });
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase {
    private readonly INotificationService _notificationService;
    public NotificationsController(INotificationService notificationService) { 
        _notificationService = notificationService;
    }

    [HttpGet] public async Task<IActionResult> GetMyNotifications() {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if(userIdStr == null) return Unauthorized();
        var uid = Guid.Parse(userIdStr);
        return Ok(await _notificationService.GetUserNotificationsAsync(uid));
    }

    [HttpPost("MarkRead/{id}")] public async Task<IActionResult> MarkRead(Guid id) {
        await _notificationService.MarkAsReadAsync(id);
        return Ok(new { success = true });
    }

    [HttpDelete("{id}")] public async Task<IActionResult> Delete(Guid id) {
        await _notificationService.DeleteNotificationAsync(id);
        return Ok(new { success = true });
    }
}
