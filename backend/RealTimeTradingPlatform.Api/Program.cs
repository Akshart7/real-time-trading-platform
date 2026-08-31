using Microsoft.EntityFrameworkCore;
using RealTimeTradingPlatform.Api.Data;
using RealTimeTradingPlatform.Api.Configuration;
using RealTimeTradingPlatform.Api.Services;
using RealTimeTradingPlatform.Api.BackgroundServices;
using RealTimeTradingPlatform.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // needed for SignalR
    });
});

builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<TradingApiOptions>(
    builder.Configuration.GetSection("TradingApi"));

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();
builder.Services.AddSingleton<IMarketDataStatusService, MarketDataStatusService>();
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITradeService, TradeService>();
builder.Services.AddScoped<IPositionService, PositionService>();

builder.Services.AddHostedService<MarketDataBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AngularClient");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MarketDataHub>("/hubs/market-data");

// Automatically migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
