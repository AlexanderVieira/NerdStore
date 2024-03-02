﻿using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NerdStore.Catalogo.Data.Constants;
using System;
using System.IO;
using System.Linq;

namespace NerdStore.Catalogo.Data.Extensions
{
    public static class DbContextExtensions
    {
        public static void AddCatalogoData(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(ContextConstants.DB_CONNECTION_NAME) ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<CatalogoContext>(options =>
            {
                options.UseSqlServer(connectionString,
                  x =>
                  {
                      x.MigrationsHistoryTable("__EFMigrationsHistory");
                      x.MigrationsAssembly(typeof(CatalogoContext).Assembly.GetName().Name);
                  });
            });
           
        }        

        public static void SeedCatalogoData(this IApplicationBuilder app, IConfiguration configuration)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<CatalogoContext>())
                {
                    ArgumentNullException.ThrowIfNull(context, nameof(context));
                    if (context.Produtos.Any()) return;
                    context.Database.Migrate();

                    var assembly = typeof(DbContextExtensions).Assembly;
                    var files = assembly.GetManifestResourceNames();

                    var executedSeedings = context.SeedingEntries.ToArray();
                    var filePrefix = $"{assembly.GetName().Name}.Seedings.";
                    foreach (var file in files.Where(f => f.StartsWith(filePrefix) && f.EndsWith(".sql"))
                                              .Select(f => new
                                              {
                                                  PhysicalFile = f,
                                                  LogicalFile = f.Replace(filePrefix, String.Empty)
                                              })
                                              .OrderBy(f => f.LogicalFile))
                    {
                        if (executedSeedings.Any(e => e.Name == file.LogicalFile))
                            continue;

                        string command = string.Empty;
                        using (Stream stream = assembly.GetManifestResourceStream(file.PhysicalFile))
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                command = reader.ReadToEnd();
                            }
                        }

                        if (String.IsNullOrWhiteSpace(command))
                            continue;

                        using (var transaction = context.Database.BeginTransaction())
                        {
                            try
                            {
                                context.Database.ExecuteSqlRaw(command);
                                context.SeedingEntries.Add(new Entities.SeedingEntry() { Name = file.LogicalFile });
                                context.SaveChanges();
                                transaction.Commit();
                            }
                            catch
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }

                    }
                }
            }
        }
        
    }
}