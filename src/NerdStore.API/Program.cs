using MediatR;
using EventSourcing;
using NerdStore.Catalogo.Data.Repository;
using NerdStore.Catalogo.Data;
using NerdStore.Catalogo.Domain;
using NerdStore.Catalogo.Domain.Events;
using NerdStore.Catalogo.Application.Services;
using NerdStore.Core.Data.EventSourcing;
using NerdStore.Core.Messages.CommonMessages.IntegrationEvents;
using NerdStore.Core.Messages.CommonMessages.Notifications;
using NerdStore.Core.Communication.Mediator;
using NerdStore.Pagamentos.Business.Events;
using NerdStore.Pagamentos.Business;
using NerdStore.Pagamentos.Data.Repository;
using NerdStore.Pagamentos.AntiCorruption;
using NerdStore.Pagamentos.Data;
using NerdStore.Vendas.Application.Commands;
using NerdStore.Vendas.Application.Events;
using NerdStore.Vendas.Application.Queries;
using NerdStore.Vendas.Domain;
using NerdStore.Vendas.Data.Repository;
using NerdStore.Vendas.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using NerdStore.Catalogo.Application.AutoMapper;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using NerdStore.API.Extensions;


var builder = WebApplication.CreateBuilder(args);

// Add builder.Services to the container.

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Total",
        builder =>
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", true, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
    .AddEnvironmentVariables();

if (builder.Environment.IsProduction())
{
    builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<CatalogoContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<VendasContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<PagamentoContext>(options =>
    options.UseSqlServer(connectionString));

// AutoMapper
builder.Services.AddAutoMapper(typeof(DomainToViewModelMappingProfile), typeof(ViewModelToDomainMappingProfile));

// Mediator
builder.Services.AddMediatR(typeof(Program).Assembly);
builder.Services.AddScoped<IMediatorHandler, MediatorHandler>();

// Notifications
builder.Services.AddScoped<INotificationHandler<DomainNotification>, DomainNotificationHandler>();

// Event Sourcing
builder.Services.AddSingleton<IEventStoreService, EventStoreService>();
builder.Services.AddSingleton<IEventSourcingRepository, EventSourcingRepository>();

// Catalogo
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IProdutoAppService, ProdutoAppService>();
builder.Services.AddScoped<IEstoqueService, EstoqueService>();
builder.Services.AddScoped<CatalogoContext>();

builder.Services.AddScoped<INotificationHandler<ProdutoAbaixoEstoqueEvent>, ProdutoEventHandler>();
builder.Services.AddScoped<INotificationHandler<PedidoIniciadoEvent>, ProdutoEventHandler>();
builder.Services.AddScoped<INotificationHandler<PedidoProcessamentoCanceladoEvent>, ProdutoEventHandler>();

// Vendas
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IPedidoQueries, PedidoQueries>();
builder.Services.AddScoped<VendasContext>();

builder.Services.AddScoped<IRequestHandler<AdicionarItemPedidoCommand, bool>, PedidoCommandHandler>();
builder.Services.AddScoped<IRequestHandler<AtualizarItemPedidoCommand, bool>, PedidoCommandHandler>();
builder.Services.AddScoped<IRequestHandler<RemoverItemPedidoCommand, bool>, PedidoCommandHandler>();
builder.Services.AddScoped<IRequestHandler<AplicarVoucherPedidoCommand, bool>, PedidoCommandHandler>();
builder.Services.AddScoped<IRequestHandler<IniciarPedidoCommand, bool>, PedidoCommandHandler>();
builder.Services.AddScoped<IRequestHandler<FinalizarPedidoCommand, bool>, PedidoCommandHandler>();
builder.Services.AddScoped<IRequestHandler<CancelarProcessamentoPedidoCommand, bool>, PedidoCommandHandler>();
builder.Services.AddScoped<IRequestHandler<CancelarProcessamentoPedidoEstornarEstoqueCommand, bool>, PedidoCommandHandler>();

builder.Services.AddScoped<INotificationHandler<PedidoRascunhoIniciadoEvent>, PedidoEventHandler>();
builder.Services.AddScoped<INotificationHandler<PedidoEstoqueRejeitadoEvent>, PedidoEventHandler>();
builder.Services.AddScoped<INotificationHandler<PedidoPagamentoRealizadoEvent>, PedidoEventHandler>();
builder.Services.AddScoped<INotificationHandler<PedidoPagamentoRecusadoEvent>, PedidoEventHandler>();

// Pagamento
builder.Services.AddScoped<IPagamentoRepository, PagamentoRepository>();
builder.Services.AddScoped<IPagamentoService, PagamentoService>();
builder.Services.AddScoped<IPagamentoCartaoCreditoFacade, PagamentoCartaoCreditoFacade>();
builder.Services.AddScoped<IPayPalGateway, PayPalGateway>();
builder.Services.AddScoped<IPagamentoConfigurationManager, PagamentoConfigurationManager>();
builder.Services.AddScoped<PagamentoContext>();

builder.Services.AddScoped<INotificationHandler<PedidoEstoqueConfirmadoEvent>, PagamentoEventHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogoContext>();
    db.Database.Migrate();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PagamentoContext>();
    db.Database.Migrate();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VendasContext>();
    db.Database.Migrate();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var supportedCultures = new[] { new CultureInfo("pt-BR") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("Total");

app.UseAuthorization();

app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();

app.Run();
