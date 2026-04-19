using Entity.RatingService.Application;
using Entity.RatingService.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var ratingDatabaseOptions = ResolveRatingDatabaseOptions(builder.Configuration);
ValidateRatingDatabaseOptions(ratingDatabaseOptions);

builder.Services.AddSingleton(ratingDatabaseOptions);

builder.Services.AddDbContext<RatingDbContext>((serviceProvider, options) =>
{
	var dbOptions = serviceProvider.GetRequiredService<RatingDatabaseOptions>();
	var connectionString = $"Server={dbOptions.Host};Port={dbOptions.Port};Database={dbOptions.Name};User={dbOptions.User};Password={dbOptions.Password};SslMode=None;AllowPublicKeyRetrieval=True;";

	options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddScoped<RatingDatabaseInitializer>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
	errorApp.Run(async context =>
	{
		var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
		app.Logger.LogError(exception, "Unhandled exception");

		context.Response.StatusCode = StatusCodes.Status500InternalServerError;
		context.Response.ContentType = "application/problem+json";

		var problem = new ProblemDetails
		{
			Title = "An unexpected error occurred.",
			Status = StatusCodes.Status500InternalServerError,
			Detail = app.Environment.IsDevelopment() ? exception?.Message : null,
			Instance = context.Request.Path
		};

		await context.Response.WriteAsJsonAsync(problem);
	});
});

await using (var scope = app.Services.CreateAsyncScope())
{
	var initializer = scope.ServiceProvider.GetRequiredService<RatingDatabaseInitializer>();
	await initializer.InitializeAsync(CancellationToken.None);
}

app.MapControllers();

app.Run();

static RatingDatabaseOptions ResolveRatingDatabaseOptions(IConfiguration configuration)
{
	var sectionOptions = configuration.GetSection(RatingDatabaseOptions.SectionName).Get<RatingDatabaseOptions>()
		?? new RatingDatabaseOptions();

	var host = Environment.GetEnvironmentVariable("DB_HOST")
		?? Environment.GetEnvironmentVariable("RATING_DB_HOST")
		?? sectionOptions.Host;

	var name = Environment.GetEnvironmentVariable("DB_NAME")
		?? Environment.GetEnvironmentVariable("RATING_DB_NAME")
		?? sectionOptions.Name;

	var user = Environment.GetEnvironmentVariable("DB_USER")
		?? Environment.GetEnvironmentVariable("RATING_DB_USER")
		?? sectionOptions.User;

	var password = Environment.GetEnvironmentVariable("DB_PASSWORD")
		?? Environment.GetEnvironmentVariable("RATING_DB_PASSWORD")
		?? sectionOptions.Password;

	var port = sectionOptions.Port;
	var configuredPort = Environment.GetEnvironmentVariable("DB_PORT")
		?? Environment.GetEnvironmentVariable("RATING_DB_INTERNAL_PORT")
		?? Environment.GetEnvironmentVariable("RATING_DB_PORT");

	if (!string.IsNullOrWhiteSpace(configuredPort) && int.TryParse(configuredPort, out var parsedPort))
	{
		port = parsedPort;
	}

	return new RatingDatabaseOptions
	{
		Host = host,
		Port = port,
		Name = name,
		User = user,
		Password = password
	};
}

static void ValidateRatingDatabaseOptions(RatingDatabaseOptions options)
{
	var errors = new List<string>();

	if (string.IsNullOrWhiteSpace(options.Host))
	{
		errors.Add("DB_HOST (or RATING_DB_HOST / RatingDatabase:Host) is required.");
	}

	if (options.Port <= 0 || options.Port > 65535)
	{
		errors.Add("DB_PORT must be between 1 and 65535.");
	}

	if (string.IsNullOrWhiteSpace(options.Name))
	{
		errors.Add("DB_NAME (or RATING_DB_NAME / RatingDatabase:Name) is required.");
	}

	if (string.IsNullOrWhiteSpace(options.User))
	{
		errors.Add("DB_USER (or RATING_DB_USER / RatingDatabase:User) is required.");
	}

	if (string.IsNullOrWhiteSpace(options.Password))
	{
		errors.Add("DB_PASSWORD (or RATING_DB_PASSWORD / RatingDatabase:Password) is required.");
	}

	if (errors.Count > 0)
	{
		throw new InvalidOperationException($"Invalid database configuration: {string.Join(" ", errors)}");
	}
}
