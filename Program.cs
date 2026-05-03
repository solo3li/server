using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;
using Uis.Server.Data;
using Uis.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Increase multipart body size limit for image uploads (100MB)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
});

// Add services to the container.
builder.Services.AddControllersWithViews(); // MVC for Admin panel & APIs

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UIS API",
        Version = "v1",
        Description = "API for the University Interface System (UIS)"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configure SQLite Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? "default_secret_key");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };
});

// Configure Services
builder.Services.AddScoped<Uis.Server.Services.IEmailService, Uis.Server.Services.EmailService>();
builder.Services.AddScoped<Uis.Server.Services.IAuthService, Uis.Server.Services.AuthService>();
builder.Services.AddScoped<Uis.Server.Services.IOtpService, Uis.Server.Services.OtpService>();
builder.Services.AddScoped<Uis.Server.Services.IJwtService, Uis.Server.Services.JwtService>();
builder.Services.AddScoped<Uis.Server.Services.IUserService, Uis.Server.Services.UserService>();
builder.Services.AddScoped<Uis.Server.Services.IKycService, Uis.Server.Services.KycService>();
builder.Services.AddScoped<Uis.Server.Services.ICatalogService, Uis.Server.Services.CatalogService>();
builder.Services.AddScoped<Uis.Server.Services.IOrderService, Uis.Server.Services.OrderService>();
builder.Services.AddScoped<Uis.Server.Services.IPaymentService, Uis.Server.Services.PaymentService>();
builder.Services.AddScoped<Uis.Server.Services.IEscrowService, Uis.Server.Services.EscrowService>();
builder.Services.AddScoped<Uis.Server.Services.IChatService, Uis.Server.Services.ChatService>();
builder.Services.AddScoped<Uis.Server.Services.IFileService, Uis.Server.Services.FileService>();
builder.Services.AddScoped<Uis.Server.Services.ITicketService, Uis.Server.Services.TicketService>();
builder.Services.AddScoped<Uis.Server.Services.INotificationService, Uis.Server.Services.NotificationService>();

// Configure SignalR
builder.Services.AddSignalR();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Call Database Seeder
await Uis.Server.Data.DbSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UIS API v1");
    c.RoutePrefix = "swagger"; // Access at /swagger
});

if (app.Environment.IsDevelopment())
{
    // Development specific settings if any
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection(); // Disabled to prevent redirecting Cloudflare Tunnel HTTP traffic which breaks CORS preflight
app.UseStaticFiles(); // For local file storage and static assets

app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// Map SignalR Hubs
app.MapHub<ChatHub>("/hubs/chat");

// Add a simple health check endpoint
app.MapGet("/health", () => Results.Ok("Healthy"));

// Map MVC Controllers (Admin) and API Controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
