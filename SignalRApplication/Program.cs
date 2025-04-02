using ClassLibrary.Database;
using ClassLibrary.Services;
using Microsoft.EntityFrameworkCore;
using SignalRApplication.Hubs;

namespace SignalRApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSignalR();
            builder.Services.AddDbContext<ApplicationContext>(options => 
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("stringConnection")), 
                    ServiceLifetime.Transient
                    );

            builder.Services.AddSingleton<ConfigurationService>();

            var app = builder.Build();

            app.MapHub<HotificationHub>("/notify");

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
        }
    }
}
