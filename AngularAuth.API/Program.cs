
using AngularAuth.API.Context;
using Microsoft.EntityFrameworkCore;

namespace AngularAuth.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(option =>
            {
                option.AddPolicy("MyPolicy", builder =>
                {
                    builder.AllowAnyOrigin().
                    AllowAnyMethod().
                    AllowAnyHeader();
                });
            });

            //added to connect de DB
            builder.Services.AddDbContext<AppDbContext>(option =>
            {
                option.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnStr"));
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(u =>
                {
                    u.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
                    u.RoutePrefix = string.Empty;
                });
            }

            app.UseHttpsRedirection();
            app.UseCors("MyPolicy");
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
