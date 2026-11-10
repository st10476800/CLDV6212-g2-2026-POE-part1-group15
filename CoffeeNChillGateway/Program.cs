using System.Reflection;
using System.Text;
using CoffeeNChillGateway.Filters;
using CoffeeNChillGateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Read the settings from appsettings.json (on the VPS, Docker overrides them with environment variables)
string storageConnection = builder.Configuration.GetConnectionString("AzureStorage");
string jwtKey = builder.Configuration["Jwt:Key"];
string jwtIssuer = builder.Configuration["Jwt:Issuer"];
string jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(storageConnection))
{
    throw new InvalidOperationException("ConnectionStrings:AzureStorage is missing.");
}
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");
}

// Our services
builder.Services.AddSingleton<UserTableService>(new UserTableService(storageConnection));
builder.Services.AddSingleton<MenuTableService>(new MenuTableService(storageConnection));
builder.Services.AddSingleton<OrderTableService>(new OrderTableService(storageConnection));
builder.Services.AddSingleton<QueueStorageService>(new QueueStorageService(storageConnection));
builder.Services.AddSingleton<BlobStorageService>(new BlobStorageService(storageConnection));
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<TokenService>();

// JWT authentication: the gateway checks the signature, issuer, audience and expiry of every token
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        TokenValidationParameters parameters = new TokenValidationParameters();
        parameters.ValidateIssuer = true;
        parameters.ValidIssuer = jwtIssuer;
        parameters.ValidateAudience = true;
        parameters.ValidAudience = jwtAudience;
        parameters.ValidateLifetime = true;
        parameters.ValidateIssuerSigningKey = true;
        parameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        parameters.ClockSkew = TimeSpan.FromMinutes(1);
        options.TokenValidationParameters = parameters;
    });

builder.Services.AddAuthorization();

// PropertyNamingPolicy = null keeps the JSON names exactly as in our C# classes (OrderId, CustomerName and so on)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    OpenApiInfo info = new OpenApiInfo();
    info.Title = "CoffeeNChill Gateway API";
    info.Version = "v1";
    info.Description = "Gateway for the CoffeeNChill menu, orders and staff documents.";
    options.SwaggerDoc("v1", info);

    // This makes Swagger show our /// comments
    string xmlFile = Assembly.GetExecutingAssembly().GetName().Name + ".xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // This adds the Authorize button where you paste your token
    OpenApiSecurityScheme bearerScheme = new OpenApiSecurityScheme();
    bearerScheme.Name = "Authorization";
    bearerScheme.Description = "Paste only the token. Swagger adds the word Bearer for you.";
    bearerScheme.In = ParameterLocation.Header;
    bearerScheme.Type = SecuritySchemeType.Http;
    bearerScheme.Scheme = "bearer";
    bearerScheme.BearerFormat = "JWT";
    options.AddSecurityDefinition("Bearer", bearerScheme);

    // This puts the padlock only on the endpoints that need a token
    options.OperationFilter<AuthorizeOperationFilter>();
});

WebApplication app = builder.Build();

// Swagger is on in every environment because we show it on the live VPS
app.UseSwagger();
app.UseSwaggerUI();

// The order matters: first find out who the user is, then check what they are allowed to do
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
