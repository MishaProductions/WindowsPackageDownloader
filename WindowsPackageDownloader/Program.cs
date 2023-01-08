using WindowsPackageDownloader.Website;

namespace WindowsPackageDownloader
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing database");
            Cache.Init();
           
            await UUP.FetchUpdate("16299.15", Core.WUArch.amd64, "RETAIL", "rs3_release", "Active", "48");

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

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