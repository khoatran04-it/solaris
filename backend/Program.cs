using backend.Data;
using backend.Middlewares;
using backend.Models;
using backend.Services;
using backend.Services.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. CORS Setting
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteApp", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 2. Database Setting
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SolarisDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorNumbersToAdd: null
    ))
    .AddInterceptors(new TimestampInterceptor())
);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddEndpointsApiExplorer();

// CẬP NHẬT SWAGGER: Thêm nút Authorize (Ổ khóa) để test API kẹp Token
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header sử dụng Bearer scheme. \r\n\r\n Nhập 'Bearer' [khoảng trắng] và dán Token của bạn vào.\r\n\r\nVí dụ: \"Bearer eyJhbGciOiJIUzI1Ni...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly));

// CẤU HÌNH JWT AUTHENTICATION
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Cho phép dùng HTTP ở môi trường Dev
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),

        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],

        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Hết hạn là chặn ngay, không có độ trễ
    };
});

// Đăng ký Authorization
builder.Services.AddAuthorization();

// 3. Đăng ký Services (DI)
builder.Services.AddScoped<ISupplierTypeService, SupplierTypeService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ISupplierAddressService, SupplierAddressService>();
builder.Services.AddScoped<ICustomerTypeService, CustomerTypeService>();
builder.Services.AddScoped<ICustomerTierService, CustomerTierService>();
builder.Services.AddScoped<ICustomerGroupService, CustomerGroupService>();
builder.Services.AddScoped<ICustomerAddressService, CustomerAddressService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IUoMCategoryService, UoMCategoryService>();
builder.Services.AddScoped<IUoMService, UoMService>();
builder.Services.AddScoped<IUoMConversionService, UoMConversionService>();
builder.Services.AddScoped<IProductCategoryGroupService, ProductCategoryGroupService>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductVariantService, ProductVariantService>();
builder.Services.AddScoped<IAttributeDefinitionService, AttributeDefinitionService>();
builder.Services.AddScoped<ICategoryAttributeService, CategoryAttributeService>();
builder.Services.AddScoped<IProductBatchService, ProductBatchService>();
builder.Services.AddScoped<ISupplierProductService, SupplierProductService>();
builder.Services.AddScoped<IPromotionCampaignService, PromotionCampaignService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

// --- Đăng ký DI cho nhóm Identity & Access ---
builder.Services.AddScoped<IIARoleService, IARoleService>();
builder.Services.AddScoped<IIAUserService, IAUserService>();
builder.Services.AddScoped<IIAPermissionService, IAPermissionService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// --- Đăng ký DI cho nhóm Procurement (Phase 3) ---
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IInventoryReceiptService, InventoryReceiptService>();

// --- Đăng ký DI cho nhóm Sales & Outbound (Phase 4) ---
builder.Services.AddScoped<IDistanceService, DistanceService>();
builder.Services.AddScoped<IOrderRoutingService, OrderRoutingService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IInventoryIssueService, InventoryIssueService>();
builder.Services.AddScoped<ICustomerReturnService, CustomerReturnService>();

// --- Đăng ký DI cho nhóm Logistics & Transfer (Phase 5) ---
builder.Services.AddScoped<IInventoryTransferService, InventoryTransferService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IDeliveryTripService, DeliveryTripService>();

// --- Đăng ký DI cho nhóm Audit, Adjustment & Reconciliation (Phase 6) ---
builder.Services.AddScoped<IInventoryAuditService, InventoryAuditService>();
builder.Services.AddScoped<IInventoryAdjustmentService, InventoryAdjustmentService>();
builder.Services.AddScoped<IInventoryReconciliationService, InventoryReconciliationService>();

// --- Đăng ký DI cho Dashboard & Analytics ---
builder.Services.AddScoped<IDashboardService, DashboardService>();

// --- Đăng ký DI cho nhóm Shop E-Commerce ---
builder.Services.AddScoped<IShopAuthService, ShopAuthService>();
builder.Services.AddScoped<IShopCustomerService, ShopCustomerService>();
builder.Services.AddScoped<IShopProductService, ShopProductService>();
builder.Services.AddScoped<IShopCartService, ShopCartService>();
builder.Services.AddScoped<IShopOrderService, ShopOrderService>();
builder.Services.AddScoped<IShopReturnService, ShopReturnService>();

// --- Đăng ký DI cho nhóm Payment & Shipping (Phase 2) ---
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddHttpClient<IGhnService, GhnService>();

// --- Đăng ký DI cho nhóm AI Chatbot (Phase 3) ---
builder.Services.AddHttpClient<IGeminiChatService, GeminiChatService>();

var app = builder.Build();

// 4. Middlewares
app.UseCors("AllowViteApp");
app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Tự động cập nhật Migration và đồng bộ dữ liệu mầm khi khởi động
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<SolarisDbContext>();

        // Tự động áp dụng tất cả các Migration còn thiếu lên Database Server
        if (db.Database.IsRelational())
        {
            logger.LogInformation("Đang kiểm tra và tự động áp dụng EF Core Migrations lên Database...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Database Migrations đã được cập nhật thành công!");
        }

        // Khởi tạo dữ liệu mầm (Roles, Permissions, Super Admin, EAV Attributes, Slugs)
        await DbInitializer.SeedAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Đã xảy ra lỗi trong quá trình tự động cập nhật Database hoặc Seed data.");
    }
}

app.Run();