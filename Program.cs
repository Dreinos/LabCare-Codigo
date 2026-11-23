using LabCare.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("LabCare");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configura ConnectionStrings:LabCare mediante secretos de usuario o variables de entorno.");
}

builder.Services.AddDbContext<LabCareContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

app.UseExceptionHandler("/error");
app.UseStatusCodePagesWithReExecute("/error", "?codigo={0}");
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/solicitudes"));

app.Run();
