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
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 2. Database Setting
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SolarisDbContext>(options =>
    options.UseSqlServer(connectionString)
           .AddInterceptors(new TimestampInterceptor())
);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddEndpointsApiExplorer();

// 🔥 CẬP NHẬT SWAGGER: Thêm nút Authorize (Ổ khóa) để test API kẹp Token
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

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

// ==========================================
// 🔥 CẤU HÌNH JWT AUTHENTICATION
// ==========================================
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

// --- Đăng ký DI cho nhóm Audit, Adjustment & Reconciliation (Phase 6) ---
builder.Services.AddScoped<IInventoryAuditService, InventoryAuditService>();
builder.Services.AddScoped<IInventoryAdjustmentService, InventoryAdjustmentService>();
builder.Services.AddScoped<IInventoryReconciliationService, InventoryReconciliationService>();

var app = builder.Build();

// 4. Middlewares
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowViteApp");

// 🔥 BẮT BUỘC: Authentication (Xác thực ai là ai) phải nằm trước Authorization (Xác thực có quyền gì)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();