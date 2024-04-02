using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors((options) => {
    options.AddPolicy("DevCors", (corsBuilder) =>
    {
        corsBuilder.WithOrigins("http://localhost:3000", "http://localhost:4200", "http://localhost:8000")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
        options.AddPolicy("ProdCors", (corsBuilder) =>
    {
        corsBuilder.WithOrigins("https://myProdSite.com")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

string? tokenKeyString = builder.Configuration.GetSection("AppSettings:TokenKey").Value;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters() {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(tokenKeyString ?? "")
            ),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseCors("ProdCors");
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
