using GraficasMixing.Models;   // tu namespace donde está el DbContext
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Registrar el DbContext con la cadena de conexión de appsettings.json
builder.Services.AddDbContext<GaficadoreTestContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GaficadoreTestConnection")));

builder.Services.AddDbContext<MasterMcontext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OmronConnection")));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();