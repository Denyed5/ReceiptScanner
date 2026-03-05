using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ReceiptScanner.Data;
using ReceiptScanner.Areas.Identity.Data;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ReceiptScannerContextConnection") ?? throw new InvalidOperationException("Connection string 'ReceiptScannerContextConnection' not found.");;

builder.Services.AddDbContext<ReceiptScannerContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ReceiptScannerUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ReceiptScannerContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<ReceiptScanner.Services.OcrService>();
builder.Services.AddScoped<ReceiptScanner.Services.ReceiptParserService>();



var app = builder.Build();

app.MapRazorPages();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.Run();