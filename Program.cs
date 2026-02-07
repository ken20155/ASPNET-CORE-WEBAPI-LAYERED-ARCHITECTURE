using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WebApiInterviewStatus.Dbconfig;
using WebApiInterviewStatus.Models;
using WebApiInterviewStatus.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

Dbname.Initialize(builder.Configuration);

// -------------------- DB CONTEXTS --------------------
builder.Services.AddDbContext<Db1Context>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Db1")!.Replace("{LogDb}", Dbname.LogDb!)
    ));

builder.Services.AddDbContext<Db2Context>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Db2")!.Replace("{MainDb}", Dbname.MainDb!)
    ));

builder.Services.AddDbContext<Db3Context>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Db3")!.Replace("{SysDb}", Dbname.SysDb!)
    ));

builder.Services.AddScoped<MainModel>();

// -------------------- CONTROLLERS --------------------
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new RouteTokenTransformerConvention(
        new SlugifyParameterTransformer()));
});

// -------------------- JWT AUTH --------------------
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];

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
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
    };

    // 🔥 ADD THIS FOR LOGOUT (BLACKLIST SUPPORT)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var blacklist = context.HttpContext.RequestServices
                .GetRequiredService<TokenBlacklistService>();

            var token = context.Request.Headers["Authorization"]
                .ToString()
                .Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(token) && blacklist.IsBlacklisted(token))
            {
                context.Fail("Token revoked");
            }

            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse(); 

            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                status = false,
                message = "Unauthorized"
            });
        }
    };
});

// -------------------- SWAGGER + JWT SUPPORT --------------------
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Token like: Bearer {token}"
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

// -------------------- RATE LIMITER --------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";

        var response = new
        {
            status = false,
            message = "Too many requests"
        };

        await context.HttpContext.Response.WriteAsJsonAsync(response, token);
    };

    options.AddPolicy("authPolicy", context =>
    {
        var roleId = context.User?.FindFirst("UserRoleId")?.Value;

        if (context.User?.Identity?.IsAuthenticated == true && roleId == "1")
            return RateLimitPartition.GetNoLimiter("adminRole");

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});



// -------------------- LOGOUT --------------------
builder.Services.AddSingleton<TokenBlacklistService>();



// -------------------- API VERSIONING --------------------
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

var app = builder.Build();

// -------------------- MIDDLEWARE --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseAuthentication();   
app.UseAuthorization();
app.MapControllers();
app.Run();
