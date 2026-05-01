using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Uis.Server.Data;
using Uis.Server.Models;
using Uis.Server.Services;

namespace Uis.Server.Controllers;

[Route("[controller]")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IFileService _fileService;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<Uis.Server.Hubs.ChatHub> _hub;

    public AdminController(ApplicationDbContext db, INotificationService notificationService, IFileService fileService, Microsoft.AspNetCore.SignalR.IHubContext<Uis.Server.Hubs.ChatHub> hub)
    {
        _db = db;
        _notificationService = notificationService;
        _fileService = fileService;
        _hub = hub;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var stats = new
        {
            TotalUsers = await _db.Users.CountAsync(),
            TotalOrders = await _db.Orders.CountAsync(),
            PendingKyc = await _db.KycRequests.CountAsync(k => k.Status == "Pending"),
            OpenTickets = await _db.Tickets.CountAsync(t => t.Status == "Open"),
            TotalRevenue = await _db.Payments.Where(p => p.Status == "Completed").SumAsync(p => p.Amount)
        };

        ViewBag.Stats = stats;
        
        // Recent Orders for dashboard
        var recentOrders = await _db.Orders.Include(o => o.Student).Include(o => o.Service).OrderByDescending(o => o.CreatedAt).Take(5).ToListAsync();
        ViewBag.RecentOrders = recentOrders;

        return View();
    }

    // --- User Management ---
    [HttpGet("Users")]
    public async Task<IActionResult> Users(string? search, string? role, bool? isActive, DateTime? startDate, DateTime? endDate)
    {
        var query = _db.Users.Include(u => u.Roles).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

        if (!string.IsNullOrEmpty(role))
            query = query.Where(u => u.Roles.Any(r => r.Name == role));

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (startDate.HasValue)
        {
            var start = startDate.Value.ToUniversalTime();
            query = query.Where(u => u.CreatedAt >= start);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.ToUniversalTime();
            query = query.Where(u => u.CreatedAt <= end);
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.IsActive = isActive;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

        return View(users);
    }

    [HttpGet("Users/Create")]
    public async Task<IActionResult> CreateUser()
    {
        ViewBag.AllRoles = await _db.Roles.ToListAsync();
        return View(new User());
    }

    [HttpPost("Users/Create")]
    public async Task<IActionResult> CreateUser(User user, Guid[] roleIds, string password)
    {
        user.PasswordHash = password;
        user.CreatedAt = DateTime.UtcNow;
        
        if (roleIds != null)
        {
            foreach (var roleId in roleIds)
            {
                var role = await _db.Roles.FindAsync(roleId);
                if (role != null)
                {
                    user.Roles.Add(role);
                    if (role.Name == "Admin") user.IsAdmin = true;
                    if (role.Name == "Executor") user.IsExecutor = true;
                }
            }
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Users));
    }

    [HttpGet("Users/Edit/{id}")]
    public async Task<IActionResult> EditUser(Guid id)
    {
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        
        ViewBag.AllRoles = await _db.Roles.ToListAsync();
        return View(user);
    }

    [HttpPost("Users/Edit/{id}")]
    public async Task<IActionResult> EditUser(Guid id, User user, Guid[] roleIds, string? password)
    {
        if (id != user.Id) return BadRequest();

        var existingUser = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id);
        if (existingUser == null) return NotFound();

        existingUser.FullName = user.FullName;
        existingUser.Email = user.Email;
        existingUser.University = user.University;
        existingUser.Major = user.Major;
        existingUser.Bio = user.Bio;
        existingUser.IsActive = user.IsActive;

        if (!string.IsNullOrEmpty(password))
        {
            existingUser.PasswordHash = password;
        }

        // Update Roles
        existingUser.Roles.Clear();
        existingUser.IsAdmin = false;
        existingUser.IsExecutor = false;
        
        if (roleIds != null)
        {
            foreach (var roleId in roleIds)
            {
                var role = await _db.Roles.FindAsync(roleId);
                if (role != null)
                {
                    existingUser.Roles.Add(role);
                    if (role.Name == "Admin") existingUser.IsAdmin = true;
                    if (role.Name == "Executor") existingUser.IsExecutor = true;
                }
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Users));
    }

    [HttpPost("Users/Delete/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost("Users/ToggleStatus/{id}")]
    public async Task<IActionResult> ToggleUserStatus(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpGet("Users/{id}")]
    public async Task<IActionResult> UserDetails(Guid id)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        // Get user activity summary
        ViewBag.OrdersCount = await _db.Orders.CountAsync(o => o.StudentId == id || o.ExecutorId == id);
        ViewBag.RecentOrders = await _db.Orders
            .Where(o => o.StudentId == id || o.ExecutorId == id)
            .Include(o => o.Service)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();
        
        ViewBag.KycRequest = await _db.KycRequests.FirstOrDefaultAsync(k => k.UserId == id);
        
        // Load all roles for the change role dropdown
        ViewBag.AllRoles = await _db.Roles.OrderBy(r => r.Name).ToListAsync();

        return View(user);
    }

    [HttpPost("UpdateUserRole")]
    public async Task<IActionResult> UpdateUserRole(Guid userId, Guid roleId)
    {
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
        var role = await _db.Roles.FindAsync(roleId);
        
        if (user != null && role != null)
        {
            if (!user.Roles.Any(r => r.Id == roleId))
            {
                user.Roles.Add(role);
                
                // Sync boolean flags
                if (role.Name == "Admin") user.IsAdmin = true;
                if (role.Name == "Executor") user.IsExecutor = true;
                
                await _db.SaveChangesAsync();
            }
        }
        return RedirectToAction(nameof(UserDetails), new { id = userId });
    }

    [HttpPost("RemoveUserRole")]
    public async Task<IActionResult> RemoveUserRole(Guid userId, Guid roleId)
    {
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId);
        var role = user?.Roles.FirstOrDefault(r => r.Id == roleId);
        
        if (user != null && role != null)
        {
            user.Roles.Remove(role);
            
            // Sync boolean flags
            if (role.Name == "Admin") user.IsAdmin = false;
            if (role.Name == "Executor") user.IsExecutor = false;
            
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(UserDetails), new { id = userId });
    }

    // --- KYC Management ---
    [HttpGet("Kyc")]
    public async Task<IActionResult> Kyc(string? search, string? status)
    {
        var query = _db.KycRequests.Include(k => k.User).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(k => k.User.FullName.Contains(search) || k.User.Email.Contains(search) || k.NationalId.Contains(search));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(k => k.Status == status);

        var requests = await query.OrderByDescending(k => k.Status == "Pending").ThenByDescending(k => k.Id).ToListAsync();
        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(requests);
    }

    [HttpGet("Kyc/{id}")]
    public async Task<IActionResult> KycDetails(Guid id)
    {
        var request = await _db.KycRequests
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (request == null) return NotFound();

        return View(request);
    }

    [HttpPost("Kyc/Approve/{id}")]
    public async Task<IActionResult> ApproveKyc(Guid id)
    {
        var request = await _db.KycRequests.Include(k => k.User).ThenInclude(u => u.Roles).FirstOrDefaultAsync(k => k.Id == id);
        if (request != null)
        {
            request.Status = "Approved";
            
            // Grant Executor role and flag
            request.User.IsExecutor = true;
            var executorRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Executor");
            if (executorRole != null && !request.User.Roles.Any(r => r.Name == "Executor"))
            {
                request.User.Roles.Add(executorRole);
            }
            
            await _db.SaveChangesAsync();
            await _notificationService.SendNotificationAsync(request.UserId, "تم توثيق حسابك بنجاح! يمكنك الآن البدء في تقديم الخدمات كمنفذ.", "توثيق الحساب");
        }
        return RedirectToAction(nameof(Kyc));
    }

    [HttpPost("Kyc/Reject/{id}")]
    public async Task<IActionResult> RejectKyc(Guid id, string reason)
    {
        var request = await _db.KycRequests.FindAsync(id);
        if (request != null)
        {
            request.Status = "Rejected";
            request.RejectionReason = reason;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Kyc));
    }

    // --- Category Management ---
    [HttpGet("Categories")]
    public async Task<IActionResult> Categories(string? search)
    {
        var query = _db.Categories.AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.Contains(search));

        var categories = await query.ToListAsync();
        ViewBag.Search = search;
        return View(categories);
    }

    [HttpGet("Categories/{id}")]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();
        return Json(category);
    }

    [HttpPost("Categories/Create")]
    public async Task<IActionResult> CreateCategory(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            _db.Categories.Add(new Category { Name = name });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost("Categories/Edit")]
    public async Task<IActionResult> EditCategory(Guid id, string name)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.Name = name;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost("Categories/Delete/{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat != null)
        {
            _db.Categories.Remove(cat);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Categories));
    }

    // --- Role Management ---
    [HttpGet("Roles")]
    public async Task<IActionResult> Roles(string? search, bool? isSystem)
    {
        var query = _db.Roles.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(r => r.Name.Contains(search) || r.Description.Contains(search));

        if (isSystem.HasValue)
            query = query.Where(r => r.IsSystemRole == isSystem.Value);

        var roles = await query.OrderByDescending(r => r.IsSystemRole).ToListAsync();
        ViewBag.Search = search;
        ViewBag.IsSystem = isSystem;
        return View(roles);
    }

    [HttpGet("Roles/Edit/{id}")]
    public async Task<IActionResult> EditRole(Guid id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound();
        return View(role);
    }

    [HttpPost("Roles/Edit/{id}")]
    public async Task<IActionResult> EditRole(Guid id, string name, string description)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound();

        if (!role.IsSystemRole)
        {
            role.Name = name;
        }
        role.Description = description;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Roles));
    }

    [HttpPost("Roles/Create")]
    public async Task<IActionResult> CreateRole(string name, string description)
    {
        if (!string.IsNullOrEmpty(name))
        {
            _db.Roles.Add(new Role { Name = name, Description = description, IsSystemRole = false });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Roles));
    }

    [HttpPost("Roles/Delete/{id}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role != null && !role.IsSystemRole)
        {
            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Roles));
    }

    [HttpGet("Roles/Permissions/{id}")]
    public async Task<IActionResult> RolePermissions(Guid id)
    {
        var role = await _db.Roles
            .Include("RolePermissions.Permission")
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return NotFound();

        ViewBag.AllPermissions = await _db.Permissions.ToListAsync();
        return View(role);
    }

    [HttpPost("Roles/UpdatePermissions")]
    public async Task<IActionResult> UpdateRolePermissions(Guid roleId, Guid[] permissionIds)
    {
        var existingPermissions = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _db.RolePermissions.RemoveRange(existingPermissions);

        if (permissionIds != null)
        {
            foreach (var pId in permissionIds)
            {
                _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = pId });
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Roles));
    }

    // --- Permission Management ---
    [HttpGet("Permissions")]
    public async Task<IActionResult> Permissions()
    {
        var permissions = await _db.Permissions.OrderBy(p => p.Group).ToListAsync();
        return View(permissions);
    }

    [HttpPost("Permissions/Create")]
    public async Task<IActionResult> CreatePermission(string name, string group, string description)
    {
        if (!string.IsNullOrEmpty(name))
        {
            _db.Permissions.Add(new Permission { Name = name, Group = group, Description = description });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Permissions));
    }

    [HttpPost("Permissions/Delete/{id}")]
    public async Task<IActionResult> DeletePermission(Guid id)
    {
        var p = await _db.Permissions.FindAsync(id);
        if (p != null)
        {
            _db.Permissions.Remove(p);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Permissions));
    }

    // --- Service Management ---
    [HttpGet("Services")]
    public async Task<IActionResult> Services(string? search, Guid? categoryId, decimal? minPrice, decimal? maxPrice, bool? isActive)
    {
        var query = _db.Services.Include(s => s.Category).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.Title.Contains(search) || s.Description.Contains(search));

        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);

        if (minPrice.HasValue)
            query = query.Where(s => s.BasePrice >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(s => s.BasePrice <= maxPrice.Value);

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var services = await query.OrderByDescending(s => s.Id).ToListAsync();
        
        ViewBag.Categories = await _db.Categories.ToListAsync();
        ViewBag.Search = search;
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.IsActive = isActive;

        return View(services);
    }

    [HttpGet("Services/Create")]
    public async Task<IActionResult> CreateService()
    {
        ViewBag.Categories = await _db.Categories.ToListAsync();
        return View();
    }

    [HttpPost("Services/Create")]
    public async Task<IActionResult> CreateService(Service service, IFormFile? imageFile)
    {
        if (ModelState.IsValid)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                service.ImageUrl = await _fileService.UploadFileAsync(imageFile.OpenReadStream(), fileName);
            }

            _db.Services.Add(service);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Services));
        }
        ViewBag.Categories = await _db.Categories.ToListAsync();
        return View(service);
    }

    [HttpGet("Services/Edit/{id}")]
    public async Task<IActionResult> EditService(Guid id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service == null) return NotFound();

        ViewBag.Categories = await _db.Categories.ToListAsync();
        return View(service);
    }

    [HttpPost("Services/Edit/{id}")]
    public async Task<IActionResult> EditService(Guid id, Service service, IFormFile? imageFile)
    {
        if (id != service.Id) return BadRequest();

        if (ModelState.IsValid)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                service.ImageUrl = await _fileService.UploadFileAsync(imageFile.OpenReadStream(), fileName);
            }
            else
            {
                var existingService = await _db.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                if (existingService != null)
                {
                    service.ImageUrl = existingService.ImageUrl;
                }
            }

            _db.Entry(service).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Services));
        }
        ViewBag.Categories = await _db.Categories.ToListAsync();
        return View(service);
    }

    [HttpPost("Services/Delete/{id}")]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service != null)
        {
            _db.Services.Remove(service);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Services));
    }

    [HttpPost("Services/ToggleStatus/{id}")]
    public async Task<IActionResult> ToggleServiceStatus(Guid id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service != null)
        {
            service.IsActive = !service.IsActive;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Services));
    }

    // --- Order Management ---
    [HttpGet("Orders")]
    public async Task<IActionResult> Orders(string? search, string? status, decimal? minPrice, decimal? maxPrice, DateTime? startDate, DateTime? endDate, Guid? executorId)
    {
        var query = _db.Orders.Include(o => o.Student).Include(o => o.Service).Include(o => o.Executor).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(o => o.Id.ToString().Contains(search) || o.Student.FullName.Contains(search) || o.Service.Title.Contains(search));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        if (minPrice.HasValue)
            query = query.Where(o => o.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(o => o.Price <= maxPrice.Value);

        if (startDate.HasValue)
        {
            var start = startDate.Value.ToUniversalTime();
            query = query.Where(o => o.CreatedAt >= start);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.ToUniversalTime();
            query = query.Where(o => o.CreatedAt <= end);
        }

        if (executorId.HasValue)
            query = query.Where(o => o.ExecutorId == executorId.Value);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        ViewBag.ExecutorId = executorId;
        
        ViewBag.Executors = await _db.Users.Where(u => u.IsExecutor).OrderBy(u => u.FullName).ToListAsync();

        return View(orders);
    }

    [HttpPost("UpdateOrderStatus")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, string status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order != null)
        {
            order.Status = status;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Orders));
    }

    [HttpGet("Orders/{id}")]
    public async Task<IActionResult> OrderDetails(Guid id)
    {
        var order = await _db.Orders
            .Include(o => o.Student)
            .Include(o => o.Executor)
            .Include(o => o.Service)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        ViewBag.Payment = await _db.Payments.FirstOrDefaultAsync(p => p.OrderId == id);
        ViewBag.Escrow = await _db.Escrows.FirstOrDefaultAsync(e => e.OrderId == id);
        
        // Load Chat and Messages
        ViewBag.Chat = await _db.Chats
            .Include(c => c.Messages.OrderBy(m => m.SentAt))
            .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.OrderId == id);

        return View(order);
    }

    // --- Ticket Management ---
    [HttpGet("Tickets")]
    public async Task<IActionResult> Tickets(string? search, string? status, Guid? userId, DateTime? startDate, DateTime? endDate)
    {
        var query = _db.Tickets.Include(t => t.User).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t => t.Subject.Contains(search) || t.User.FullName.Contains(search));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value);

        if (startDate.HasValue)
        {
            var start = startDate.Value.ToUniversalTime();
            query = query.Where(t => t.CreatedAt >= start);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.ToUniversalTime();
            query = query.Where(t => t.CreatedAt <= end);
        }

        var tickets = await query.OrderByDescending(t => t.Status == "Open").ThenByDescending(t => t.CreatedAt).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.UserId = userId;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        
        ViewBag.Users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();

        return View(tickets);
    }

    [HttpGet("Tickets/{id}")]
    public async Task<IActionResult> TicketDetails(Guid id)
    {
        var ticket = await _db.Tickets
            .Include(t => t.User)
            .Include(t => t.Messages.OrderBy(m => m.SentAt))
            .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        var admin = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@uis.com");
        ViewBag.AdminId = admin?.Id;

        return View(ticket);
    }

    [HttpPost("Tickets/ReplyApi")]
    public async Task<IActionResult> ReplyToTicketApi([FromForm] ReplyRequest request)
    {
        var admin = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@uis.com");
        if (admin == null) return Unauthorized();

        var message = new TicketMessage
        {
            TicketId = request.TicketId,
            SenderId = admin.Id,
            Content = request.Content ?? "",
            SentAt = DateTime.UtcNow
        };

        if (request.Attachment != null && request.Attachment.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.Attachment.FileName);
            message.AttachmentUrl = await _fileService.UploadFileAsync(request.Attachment.OpenReadStream(), fileName);
            message.AttachmentType = request.AttachmentType ?? "file";
        }

        _db.TicketMessages.Add(message);
        await _db.SaveChangesAsync();

        // Real-time broadcast
        await _hub.Clients.Group("ticket-" + request.TicketId.ToString()).SendAsync("ReceiveTicketMessage", new {
            Id = message.Id,
            Content = message.Content,
            SentAt = message.SentAt,
            SenderId = message.SenderId,
            SenderName = "مدير النظام",
            AttachmentUrl = message.AttachmentUrl,
            AttachmentType = message.AttachmentType
        });

        return Json(new { 
            success = true, 
            senderName = "مدير النظام", 
            content = message.Content, 
            attachmentUrl = message.AttachmentUrl,
            attachmentType = message.AttachmentType,
            time = message.SentAt.ToLocalTime().ToString("HH:mm") 
        });
    }

    public class ReplyRequest
    {
        public Guid TicketId { get; set; }
        public string? Content { get; set; }
        public IFormFile? Attachment { get; set; }
        public string? AttachmentType { get; set; }
    }

    [HttpPost("Tickets/Reply")]
    public async Task<IActionResult> ReplyToTicket(Guid ticketId, string? content, IFormFile? attachment, string? attachmentType)
    {
        var admin = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@uis.com");
        if (admin == null) return Unauthorized();

        var message = new TicketMessage
        {
            TicketId = ticketId,
            SenderId = admin.Id,
            Content = content ?? "",
            SentAt = DateTime.UtcNow
        };

        if (attachment != null && attachment.Length > 0)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(attachment.FileName);
            message.AttachmentUrl = await _fileService.UploadFileAsync(attachment.OpenReadStream(), fileName);
            message.AttachmentType = attachmentType ?? "file";
        }

        _db.TicketMessages.Add(message);
        await _db.SaveChangesAsync();

        // Real-time broadcast
        await _hub.Clients.Group("ticket-" + ticketId.ToString()).SendAsync("ReceiveTicketMessage", new {
            Id = message.Id,
            Content = message.Content,
            SentAt = message.SentAt,
            SenderId = message.SenderId,
            SenderName = "مدير النظام",
            AttachmentUrl = message.AttachmentUrl,
            AttachmentType = message.AttachmentType
        });

        return RedirectToAction(nameof(TicketDetails), new { id = ticketId });
    }

    [HttpPost("Tickets/Close/{id}")]
    public async Task<IActionResult> CloseTicket(Guid id)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket != null)
        {
            ticket.Status = "Closed";
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Tickets));
    }

    [HttpPost("Tickets/ResolveDispute")]
    public async Task<IActionResult> ResolveDispute(Guid ticketId, string action)
    {
        var ticket = await _db.Tickets.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket == null || !ticket.OrderId.HasValue) return NotFound();

        var escrow = await _db.Escrows.FirstOrDefaultAsync(e => e.OrderId == ticket.OrderId.Value);
        var order = await _db.Orders.FindAsync(ticket.OrderId.Value);

        if (escrow != null && order != null)
        {
            if (action == "refund")
            {
                escrow.Status = "Refunded";
                order.Status = "Cancelled";
                // In a real app, integrate with Paymob/Wallet Refund here
            }
            else if (action == "release")
            {
                escrow.Status = "Released";
                order.Status = "Completed";
                // In a real app, transfer funds to Executor Wallet here
            }

            ticket.Status = "Closed";
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(TicketDetails), new { id = ticketId });
    }

    // --- Monitoring & General Chat ---
    [HttpGet("Chats")]
    public async Task<IActionResult> Chats(string? search, DateTime? startDate, DateTime? endDate)
    {
        var query = _db.Chats
            .Include(c => c.Order)
            .Include(c => c.Student)
            .Include(c => c.Executor)
            .Include(c => c.Messages)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.Order.Id.ToString().Contains(search) || 
                                     c.Student.FullName.Contains(search) || 
                                     c.Executor.FullName.Contains(search));
        }

        if (startDate.HasValue)
        {
            var start = startDate.Value.ToUniversalTime();
            query = query.Where(c => c.Messages.Any(m => m.SentAt >= start));
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.ToUniversalTime();
            query = query.Where(c => c.Messages.Any(m => m.SentAt <= end));
        }

        var chats = await query.OrderByDescending(c => c.Messages.Max(m => m.SentAt)).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        
        return View(chats);
    }

    [HttpGet("Chats/{id}")]
    public async Task<IActionResult> ChatDetails(Guid id)
    {
        var chat = await _db.Chats
            .Include(c => c.Order)
            .Include(c => c.Student)
            .Include(c => c.Executor)
            .Include(c => c.Messages.OrderBy(m => m.SentAt))
            .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chat == null) return NotFound();

        return View(chat);
    }

    // --- Payment Management ---
    [HttpGet("Payments")]
    public async Task<IActionResult> Payments(string? search, string? status, decimal? minAmount, decimal? maxAmount, DateTime? startDate, DateTime? endDate)
    {
        var query = _db.Payments.Include(p => p.Order).ThenInclude(o => o.Student).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.TransactionId.Contains(search) || p.Order.Student.FullName.Contains(search));

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        if (minAmount.HasValue)
            query = query.Where(p => p.Amount >= minAmount.Value);

        if (maxAmount.HasValue)
            query = query.Where(p => p.Amount <= maxAmount.Value);

        if (startDate.HasValue)
        {
            var start = startDate.Value.ToUniversalTime();
            query = query.Where(p => p.CreatedAt >= start);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.ToUniversalTime();
            query = query.Where(p => p.CreatedAt <= end);
        }

        var payments = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.MinAmount = minAmount;
        ViewBag.MaxAmount = maxAmount;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

        return View(payments);
    }

    [HttpGet("Payments/{id}")]
    public async Task<IActionResult> PaymentDetails(Guid id)
    {
        var payment = await _db.Payments
            .Include(p => p.Order)
            .ThenInclude(o => o.Student)
            .Include(p => p.Order)
            .ThenInclude(o => o.Service)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null) return NotFound();

        ViewBag.Escrow = await _db.Escrows.FirstOrDefaultAsync(e => e.OrderId == payment.OrderId);

        return View(payment);
    }

    [HttpGet("GetDashboardStats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.UtcNow.Date.AddDays(-i)).Reverse();
        
        var orderData = await _db.Orders
            .Where(o => o.CreatedAt >= DateTime.UtcNow.Date.AddDays(-7))
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var labels = last7Days.Select(d => d.ToString("MM-dd")).ToList();
        var data = last7Days.Select(d => orderData.FirstOrDefault(x => x.Date == d)?.Count ?? 0).ToList();

        return Json(new { labels, data });
    }

    // --- Notification Management ---
    [HttpGet("Notifications")]
    public async Task<IActionResult> Notifications(string? search, Guid? userId, DateTime? date)
    {
        var query = _db.Notifications.Include(n => n.User).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(n => n.Message.Contains(search));

        if (userId.HasValue)
            query = query.Where(n => n.UserId == userId.Value);

        if (date.HasValue)
        {
            var d = date.Value.ToUniversalTime().Date;
            query = query.Where(n => n.CreatedAt.Date == d);
        }

        var notifications = await query.OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync();
        
        ViewBag.Users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();
        ViewBag.Search = search;
        ViewBag.UserId = userId;
        ViewBag.Date = date?.ToString("yyyy-MM-dd");

        return View(notifications);
    }

    [HttpPost("Notifications/Send")]
    public async Task<IActionResult> SendNotification(Guid? userId, string message, bool allUsers = false)
    {
        if (string.IsNullOrEmpty(message)) return RedirectToAction(nameof(Notifications));

        if (allUsers)
        {
            var userIds = await _db.Users.Select(u => u.Id).ToListAsync();
            foreach (var id in userIds)
            {
                await _notificationService.SendNotificationAsync(id, message, "تنبيه إداري");
            }
        }
        else if (userId.HasValue)
        {
            await _notificationService.SendNotificationAsync(userId.Value, message, "تنبيه إداري");
        }

        return RedirectToAction(nameof(Notifications));
    }

    [HttpPost("Notifications/Delete/{id}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        await _notificationService.DeleteNotificationAsync(id);
        return RedirectToAction(nameof(Notifications));
    }

    // --- System Settings ---
    [HttpGet("Settings/Email")]
    public async Task<IActionResult> EmailSettings()
    {
        var settings = await _db.SystemSettings.Where(s => s.Key.StartsWith("Email")).ToListAsync();
        return View(settings);
    }

    [HttpPost("Settings/Email/Update")]
    public async Task<IActionResult> UpdateEmailSettings(Dictionary<string, string> settings)
    {
        foreach (var (key, value) in settings)
        {
            var s = await _db.SystemSettings.FindAsync(key);
            if (s != null)
            {
                s.Value = value;
            }
            else
            {
                _db.SystemSettings.Add(new SystemSetting { Key = key, Value = value });
            }
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث إعدادات البريد بنجاح";
        return RedirectToAction(nameof(EmailSettings));
    }
}
