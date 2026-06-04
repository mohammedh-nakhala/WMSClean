using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WMSClean.Services;
using WMSClean.Data;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpClient();

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// إعدادات اللغة
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider
    {
        CookieName = "lang"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

// Middleware لتطبيق اللغة
app.Use(async (context, next) =>
{
    var lang = context.Session.GetString("lang");
    if (string.IsNullOrEmpty(lang))
    {
        lang = context.Request.Cookies["lang"];
    }

    if (!string.IsNullOrEmpty(lang) && (lang == "en" || lang == "ar"))
    {
        try
        {
            var culture = new CultureInfo(lang);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
        catch { }
    }
    await next();
});

var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();


app.MapGet("/", async context =>
{
    context.Response.Redirect("/Home/Index");
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = { "SuperAdmin", "Admin", "Customer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            Console.WriteLine($"✅ Role '{role}' created.");
        }
    }

    // Super Admin
    var superAdminEmail = "superadmin@wms.com";
    var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);
    if (superAdminUser == null)
    {
        superAdminUser = new IdentityUser
        {
            UserName = superAdminEmail,
            Email = superAdminEmail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(superAdminUser, "Super@123");
        await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
        Console.WriteLine("✅ Super Admin user created.");
    }

    // Admin
    var adminEmail = "admin@wms.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
        Console.WriteLine("✅ Admin user created.");
    }

    // Customer
    var customerEmail = "customer@test.com";
    var customerUser = await userManager.FindByEmailAsync(customerEmail);
    if (customerUser == null)
    {
        customerUser = new IdentityUser
        {
            UserName = customerEmail,
            Email = customerEmail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(customerUser, "Customer@123");
        await userManager.AddToRoleAsync(customerUser, "Customer");
        Console.WriteLine("✅ Customer user created.");
    }
}

app.Run();