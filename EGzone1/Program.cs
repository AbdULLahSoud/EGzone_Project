using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection.Metadata.Ecma335;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddHttpClient(); // لـ OpenRouter Chatbot
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EGZone API",
        Version = "v1",
        Description = "Backend API for EGZone Multi-Vendor E-commerce"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "انسخ التوكن هنا مباشرة (بدون كلمة Bearer)"
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("Infrastructure")));

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

//email services
builder.Services.AddScoped<IEmailService, EmailService>();

// Dashboard services
builder.Services.AddScoped<EGzone1.Services.ISellerDashboardService, EGzone1.Services.SellerDashboardService>();

var app = builder.Build();


app.MapGet("", () => "http://localhost:5108/swagger/index.html");


// ✅ إظهار تفاصيل الأخطاء في Development فقط، في Production بيرجع رسالة عامة آمنة
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EGZone API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    // Production: Global Error Handler آمن بدون كشف تفاصيل الإيرور
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "حدث خطأ في الخادم، يرجى المحاولة لاحقاً" });
        });
    });
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EGZone API v1");
        c.RoutePrefix = "swagger";
    });
}

// 🌟 التعديل الثاني: تطبيق الـ Migrations أوتوماتيك لإنشاء الجداول على داتا بيز السيرفر
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MyDbContext>();
        // السطر ده بيبص على الداتا بيز، لو ملقاش الجداول بيكريتها فوراً
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        // لو حصل إيرور في الكريت هيطبعه (ممكن تشوفه في اللوجز)
        Console.WriteLine("Database Migration Error: " + ex.Message);
    }
}
// 1. تفعيل الـ CORS أولاً لاستقبال ومعالجة طلبات فلاتر فوراً
app.UseCors("AllowAll");

// 2. التوجيه والملفات الثابتة
app.UseHttpsRedirection();
app.UseStaticFiles();

// 3. الصلاحيات
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();