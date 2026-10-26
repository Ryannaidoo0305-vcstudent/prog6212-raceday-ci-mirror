using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Each test run of the API gets its own in-memory database name.
var testDatabaseName = "RaceDayTests_" + Guid.NewGuid();

// EF Core. The tests run with the "Testing" environment and use an in-memory database instead.
builder.Services.AddDbContext<RaceDayDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.UseInMemoryDatabase(testDatabaseName);
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb"));
    }
});

// Server-side session: the browser only gets an HttpOnly cookie with the session ID.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Swagger, with the descriptions taken from the /// comments on the controllers.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = Path.Combine(AppContext.BaseDirectory, "RaceDay.Api.xml");
    if (File.Exists(xmlFile))
    {
        options.IncludeXmlComments(xmlFile);
    }
});

var app = builder.Build();

// Create the database on startup: apply the migration on SQL Server,
// or just create the in-memory database (with the seed data) for the tests.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseSession();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

// Lets the test project start the API with WebApplicationFactory.
public partial class Program { }
