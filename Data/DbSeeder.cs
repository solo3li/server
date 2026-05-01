using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Uis.Server.Models;
using Uis.Server.Services;

namespace Uis.Server.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        // 1. Clear existing data for a fresh start (Optional but requested)
        // context.Services.RemoveRange(context.Services);
        // context.Categories.RemoveRange(context.Categories);
        // await context.SaveChangesAsync();

        // 2. Seed Roles
        var adminRole = await GetOrCreateRole(context, "Admin", "Full access to all system features", true);
        var studentRole = await GetOrCreateRole(context, "Student", "Standard student access", true);
        var executorRole = await GetOrCreateRole(context, "Executor", "Service execution access", true);
        await context.SaveChangesAsync();

        // 3. Seed Permissions
        if (!await context.Permissions.AnyAsync())
        {
            var permissions = new List<Permission>
            {
                new Permission { Group = "Management", Name = "Users.View", Description = "Can view users list and details" },
                new Permission { Group = "Management", Name = "Users.Edit", Description = "Can toggle user status and edit roles" },
                new Permission { Group = "Management", Name = "Roles.Manage", Description = "Full control over roles and permissions" },
                new Permission { Group = "KYC", Name = "Kyc.Process", Description = "Can approve or reject KYC requests" },
                new Permission { Group = "Catalog", Name = "Services.Manage", Description = "Can CRUD services and categories" },
                new Permission { Group = "Operations", Name = "Orders.View", Description = "Can monitor all orders" },
                new Permission { Group = "Operations", Name = "Orders.Manage", Description = "Can update order statuses" },
                new Permission { Group = "Finance", Name = "Payments.View", Description = "Can view transaction history" },
                new Permission { Group = "Finance", Name = "Disputes.Resolve", Description = "Can arbitrate and resolve financial disputes" },
                new Permission { Group = "Support", Name = "Tickets.Manage", Description = "Can respond to and close support tickets" }
            };
            context.Permissions.AddRange(permissions);
            await context.SaveChangesAsync();

            foreach (var p in permissions)
            {
                context.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = p.Id });
            }
            await context.SaveChangesAsync();
        }

        // 4. Seed Admin
        await GetOrCreateUser(context, "admin@uis.com", "مدير النظام الرئيسي", "admin123", new List<Role> { adminRole, studentRole }, 
            isAdmin: true, isStaff: true, uni: "جامعة القاهرة", major: "إدارة نظم معلومات", 
            bio: "مدير المنصة المسؤول عن المراجعة والتحكيم.");

        // 5. Seed Categories
        if (!await context.Categories.AnyAsync())
        {
            context.Categories.AddRange(
                new Category { Name = "مشاريع تخرج" },
                new Category { Name = "تصميم جرافيك" },
                new Category { Name = "برمجة تطبيقات" },
                new Category { Name = "ترجمة معتمدة" },
                new Category { Name = "أبحاث علمية" },
                new Category { Name = "تحليل بيانات" },
                new Category { Name = "كتابة محتوى" },
                new Category { Name = "استشارات أكاديمية" }
            );
            await context.SaveChangesAsync();
        }
        var categories = await context.Categories.ToListAsync();

        // 6. Seed Services with High-Quality Images
        if (!await context.Services.AnyAsync())
        {
            var services = new List<Service>
            {
                new Service { 
                    Title = "تصميم عرض تقديمي احترافي لمشروع التخرج", 
                    Description = "تصميم عروض PowerPoint و Canva احترافية متوافقة مع معايير الجامعة، تشمل الرسوم البيانية والتحريك السلس.", 
                    BasePrice = 150, 
                    CategoryId = categories.First(c => c.Name == "تصميم جرافيك").Id,
                    ImageUrl = "https://images.unsplash.com/photo-1542744094-3a31f272c490?q=80&w=800",
                    Rating = 4.9m, ReviewsCount = 128, DeliveryTime = "يومان"
                },
                new Service { 
                    Title = "برمجة تطبيق موبايل (React Native) متكامل", 
                    Description = "تنفيذ الجانب البرمجي لمشاريع التخرج باستخدام تقنيات حديثة مع ربط بقواعد البيانات وشرح الكود.", 
                    BasePrice = 1200, 
                    CategoryId = categories.First(c => c.Name == "برمجة تطبيقات").Id,
                    ImageUrl = "https://images.unsplash.com/photo-1551650975-87deedd944c3?q=80&w=800",
                    Rating = 5.0m, ReviewsCount = 45, DeliveryTime = "7 أيام"
                },
                new Service { 
                    Title = "ترجمة أكاديمية للمقالات والأبحاث العلمية", 
                    Description = "ترجمة دقيقة من الإنجليزية للعربية وبالعكس مع مراعاة المصطلحات العلمية والتدقيق اللغوي.", 
                    BasePrice = 80, 
                    CategoryId = categories.First(c => c.Name == "ترجمة معتمدة").Id,
                    ImageUrl = "https://images.unsplash.com/photo-1544650039-2287f6071477?q=80&w=800",
                    Rating = 4.7m, ReviewsCount = 89, DeliveryTime = "يوم واحد"
                },
                new Service { 
                    Title = "كتابة وتنسيق البحث العلمي (APA Style)", 
                    Description = "مساعدة في صياغة الأبحاث وتنسيق المراجع حسب المعايير العالمية المطلوبة في الجامعات.", 
                    BasePrice = 300, 
                    CategoryId = categories.First(c => c.Name == "أبحاث علمية").Id,
                    ImageUrl = "https://images.unsplash.com/photo-1456324504439-367cee3b3c32?q=80&w=800",
                    Rating = 4.8m, ReviewsCount = 210, DeliveryTime = "4 أيام"
                },
                new Service { 
                    Title = "تحليل بيانات باستخدام SPSS أو Python", 
                    Description = "إجراء التحليلات الإحصائية وتفسير النتائج لمشاريع التخرج والرسائل العلمية.", 
                    BasePrice = 450, 
                    CategoryId = categories.First(c => c.Name == "تحليل بيانات").Id,
                    ImageUrl = "https://images.unsplash.com/photo-1551288049-bbdac8626ad1?q=80&w=800",
                    Rating = 4.9m, ReviewsCount = 34, DeliveryTime = "3 أيام"
                },
                new Service { 
                    Title = "تصميم هوية بصرية متكاملة (Logo & Branding)", 
                    Description = "تصميم شعار واختيار ألوان وخطوط لمشروعك الناشئ أو مسابقتك الجامعية.", 
                    BasePrice = 250, 
                    CategoryId = categories.First(c => c.Name == "تصميم جرافيك").Id,
                    ImageUrl = "https://images.unsplash.com/photo-1626785774573-4b799315345d?q=80&w=800",
                    Rating = 4.6m, ReviewsCount = 67, DeliveryTime = "3 أيام"
                }
            };
            context.Services.AddRange(services);
            await context.SaveChangesAsync();
        }

        await SeedSystemSettings(context);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSystemSettings(ApplicationDbContext context)
    {
        if (!await context.SystemSettings.AnyAsync())
        {
            context.SystemSettings.AddRange(
                new SystemSetting { Key = "Email.SmtpServer", Value = "smtp.gmail.com", Description = "SMTP Server Host" },
                new SystemSetting { Key = "Email.SmtpPort", Value = "587", Description = "SMTP Server Port" },
                new SystemSetting { Key = "Email.SenderName", Value = "UIS", Description = "Sender Display Name" },
                new SystemSetting { Key = "Email.SenderEmail", Value = "fps60y@gmail.com", Description = "Sender Email Address" },
                new SystemSetting { Key = "Email.Password", Value = "pljh isws wssg oakn", Description = "SMTP App Password" },
                new SystemSetting { Key = "Email.Template.Base", Value = EmailTemplates.GetDefaultBaseTemplate(), Description = "Main HTML Template Wrapper (Use {TITLE} and {CONTENT})" }
            );
        }
    }

    private static async Task<Role> GetOrCreateRole(ApplicationDbContext context, string name, string? description = null, bool isSystem = false)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == name);
        if (role == null)
        {
            role = new Role { Name = name, Description = description, IsSystemRole = isSystem };
            context.Roles.Add(role);
        }
        return role;
    }

    private static async Task<User> GetOrCreateUser(ApplicationDbContext context, string email, string fullName, string password, List<Role> roles, 
        bool isAdmin = false, bool isExecutor = false, bool isStaff = false, string? uni = null, string? major = null, string? bio = null)
    {
        var user = await context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                Email = email,
                FullName = fullName,
                PasswordHash = password,
                University = uni,
                Major = major,
                Bio = bio,
                IsAdmin = isAdmin,
                IsExecutor = isExecutor,
                IsStaff = isStaff,
                IsActive = true
            };
            foreach (var role in roles)
            {
                user.Roles.Add(role);
            }
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
        return user;
    }
}
