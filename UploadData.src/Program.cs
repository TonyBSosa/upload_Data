 using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http.Features;
using UploadData.Services;
using UploadData.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Upload grande
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 100 * 1024 * 1024);

// CORS (para Angular)
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()
));

// Servicios
builder.Services.AddScoped<IUploadService, UploadService>();
var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddTransient<IDbConnection>(_ => new SqlConnection(cs));
builder.Services.AddScoped<IPlanMateriaRepository, PlanMateriaRepository>();
builder.Services.AddScoped<IPlanMateriaService, PlanMateriaService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "UploadData.src v1"));
}

app.UseCors();
app.MapControllers();
app.Run();
