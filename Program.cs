using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Store.Models;
using Supabase;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;


//using Store.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);




builder.Services.AddSingleton<Client>(sp =>
{
    var supabaseUrl = builder.Configuration["Supabase:Url"];
    var supabaseKey = builder.Configuration["Supabase:AnonKey"];
    return new Client(supabaseUrl, supabaseKey);
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Store",
        Version = "v1",
        Description = "API for Store",
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });

});

var jwtSecret = builder.Configuration["Supabase:JwtSecret"];

if (string.IsNullOrEmpty(jwtSecret))
{
    throw new Exception("Supabase JwtSecret is missing");
}

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
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret))
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var supabaseClient = context.HttpContext.RequestServices.GetRequiredService<Client>();
            var userIdClaim = context.Principal.FindFirst("sub")?.Value
                              ?? context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out Guid userId))
            {
                try
                {
                    var userRole = await supabaseClient.From<UserRole>()
                        .Where(x => x.Id == userId)
                        .Single();

                    if (userRole != null)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Role, userRole.Role)
                        };

                        var identity = new ClaimsIdentity(context.Principal.Identity);
                        identity.AddClaims(claims);
                        context.Principal = new ClaimsPrincipal(identity);
                    }
                }
                catch
                {
                    context.NoResult();
                }
            }
        }
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7041",
                "http://localhost:4200"
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddAuthorization();

var app = builder.Build();


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseCors("AllowFrontend");   

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
