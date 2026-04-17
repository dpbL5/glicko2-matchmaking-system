using Entity.RatingService.Application;
using Entity.RatingService.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
	.AddOptions<RatingDatabaseOptions>()
	.BindConfiguration(RatingDatabaseOptions.SectionName)
	.Validate(options => !string.IsNullOrWhiteSpace(options.Host), "RatingDatabase:Host is required.")
	.Validate(options => options.Port > 0 && options.Port <= 65535, "RatingDatabase:Port must be between 1 and 65535.")
	.Validate(options => !string.IsNullOrWhiteSpace(options.Name), "RatingDatabase:Name is required.")
	.Validate(options => !string.IsNullOrWhiteSpace(options.User), "RatingDatabase:User is required.")
	.Validate(options => !string.IsNullOrWhiteSpace(options.Password), "RatingDatabase:Password is required.")
	.ValidateOnStart();

builder.Services.AddDbContext<RatingDbContext>((serviceProvider, options) =>
{
	var dbOptions = serviceProvider.GetRequiredService<IOptions<RatingDatabaseOptions>>().Value;
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
