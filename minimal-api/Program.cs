using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.Dtos;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Enuns;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dto;
using minimal_api.Infraestrutura.Db;
using minimal_api.Infraestrutura.Servicos;

#region Builder
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DbContexto>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
#endregion

#region Administrador
app.MapPost("administrador/login", ([FromBody] LoginDTO login, IAdministradorServico administradorServico) => 
{
    if (administradorServico.Login(login) != null)
        return Results.Ok("Login feito com sucesso");
    else
        return Results.Unauthorized();
}).WithTags("Administrador");

app.MapPost("administrador", ([FromBody] AdministradorDTO AdministradorDTO, IAdministradorServico administradorServico) => 
{
    var adm = new Administrador{
        Email = AdministradorDTO.Email,
        Senha = AdministradorDTO.Senha,
        Perfil = AdministradorDTO.Perfil.ToString()
    };

    administradorServico.Incluir(adm);

    return Results.Created();
}).WithTags("Administrador");

app.MapGet("administrador", ([FromQuery] int? pagina, IAdministradorServico administradorServico) => 
{
    var administradoresModelView = new List<AdministradorModelView>();
    var administradores = administradorServico.Todos(pagina);

    foreach (var adm in administradores)
    {
        administradoresModelView.Add(new AdministradorModelView{
            Id = adm.Id,
            Email = adm.Email,
            Perfil = adm.Perfil
        });
    }
    return Results.Ok(administradoresModelView);
}).WithTags("Administrador");

app.MapGet("administrador/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) => 
{
    var administrador = administradorServico.BuscaPorId(id);
    
    if (administrador is null)
        return Results.NotFound();

    return Results.Ok(new AdministradorModelView{
            Id = administrador.Id,
            Email = administrador.Email,
            Perfil = administrador.Perfil
        });

}).WithTags("Administrador");
#endregion

#region Veiculos

ErrosDeValidacao validaVeiculo(VeiculoDTO veiculoDTO)
{
    var validacao = new ErrosDeValidacao{
        Mensagens = new List<string>()
    };

    if (string.IsNullOrEmpty(veiculoDTO.Nome))
        validacao.Mensagens.Add("O nome não pode ser vazio");

    if (string.IsNullOrEmpty(veiculoDTO.Marca))
        validacao.Mensagens.Add("A marca não pode ser vazia");

    if (veiculoDTO.Ano < 1950)
        validacao.Mensagens.Add("Ano invalido");

    return validacao;
}

app.MapPost("veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => 
{
    var validacao = validaVeiculo(veiculoDTO);

    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    var veiculo = new Veiculo{
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano
    };

    veiculoServico.Incluir(veiculo);

    return Results.Created();
}).WithTags("Veiculos");

app.MapGet("veiculos", ( [FromQuery] int? pagina, IVeiculoServico veiculoServico) => 
{
    var veiculos = veiculoServico.Todos(pagina);

    return Results.Ok(veiculos);
}).WithTags("Veiculos");

app.MapGet("veiculos/{id}", ( [FromRoute] int id, IVeiculoServico veiculoServico) => 
{
    var veiculo = veiculoServico.BuscaPorId(id);

    if (veiculo is null)
        return Results.NotFound();

    return Results.Ok(veiculo);
}).WithTags("Veiculos");

app.MapPut("veiculos/{id}", ( [FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) => 
{
    var validacao = validaVeiculo(veiculoDTO);

    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    var veiculo = veiculoServico.BuscaPorId(id);

    if (veiculo is null)
        return Results.NotFound();

    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    veiculoServico.Atualizar(veiculo);
    return Results.Ok(veiculo);
}).WithTags("Veiculos");

app.MapDelete("veiculos/{id}", ( [FromRoute] int id, IVeiculoServico veiculoServico) => 
{
    var veiculo = veiculoServico.BuscaPorId(id);

    if (veiculo is null)
        return Results.NotFound();

    veiculoServico.Apagar(veiculo);
    return Results.Ok(veiculo);
}).WithTags("Veiculos");
#endregion

app.Run();
