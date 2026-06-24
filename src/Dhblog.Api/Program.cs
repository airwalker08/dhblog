using System.Text;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.SecurityToken;
using Amazon.SimpleSystemsManagement;
using Dhblog.Api.Authorization;
using Dhblog.Api.Services;
using Dhblog.Api.Settings;
using Dhblog.DataAccess;
using Dhblog.Database.Deploy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["JWT_SECRET"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? "local-dev-jwt-secret-change-in-production-min-32-chars";

builder.Services.Configure<JwtOptions>(o =>
{
    o.Secret = jwtSecret;
    o.ExpiryMinutes = int.Parse(builder.Configuration["JWT_EXPIRY_MINUTES"] ?? "60");
});

var dynamoOptions = new DynamoDbOptions
{
    Env = builder.Configuration["DHBLOG_ENV"] ?? "local",
    Endpoint = builder.Configuration["DYNAMODB_ENDPOINT"] ?? Environment.GetEnvironmentVariable("DYNAMODB_ENDPOINT"),
    Region = builder.Configuration["AWS_REGION"] ?? "us-east-1"
};
builder.Services.AddDhblogDataAccess(dynamoOptions);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "dhblog",
            ValidAudience = "dhblog",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var (code, write) in new (string Code, bool Write)[]
    {
        ("SETTINGS", false), ("SETTINGS", true),
        ("DIAGNOSTICS", false),
        ("BLOG", false), ("BLOG", true),
        ("FEED", false),
        ("PROFILE", false), ("PROFILE", true),
        ("ADMIN", false),
        ("ADMIN_USERS", false), ("ADMIN_USERS", true),
        ("ADMIN_ROLES", false), ("ADMIN_ROLES", true),
        ("ADMIN_TOPICS", false), ("ADMIN_TOPICS", true)
    })
    {
        var policyName = write ? $"Feature:{code}:Write" : $"Feature:{code}";
        options.AddPolicy(policyName, p => p.Requirements.Add(new FeatureRequirement(code, write)));
    }
});

builder.Services.AddScoped<IAuthorizationHandler, FeatureAuthorizationHandler>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BlogService>();
builder.Services.AddScoped<FeedService>();
builder.Services.AddScoped<FollowService>();
builder.Services.AddScoped<TopicService>();
builder.Services.AddScoped<DiagnosticsService>();
builder.Services.AddScoped<AdminService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IAppSettingsProvider, LocalAppSettingsProvider>();
}
else
{
    builder.Services.AddAWSService<IAmazonSimpleSystemsManagement>();
    builder.Services.AddSingleton<IAppSettingsProvider, SsmAppSettingsProvider>();
    builder.Services.AddAWSService<IAmazonS3>();
    builder.Services.AddAWSService<IAmazonSecurityTokenService>();
}

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

var settings = app.Services.GetRequiredService<IAppSettingsProvider>();
await settings.RefreshAsync();

if (app.Environment.IsDevelopment())
{
    var dynamo = app.Services.GetRequiredService<IAmazonDynamoDB>();
    await new DataSeeder(dynamo, dynamoOptions.Env).EnsureAdminFeaturesAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
