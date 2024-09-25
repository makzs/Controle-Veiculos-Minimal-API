using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

var key = builder.Configuration.GetSection("Jwt").ToString();
if (string.IsNullOrEmpty(key)) key = "123456";

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option => {
    option.TokenValidationParameters = new TokenValidationParameters{
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme{
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta maneira: Bearer {seutoken}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement{
        {
                new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }, new string[] {}
        }
    });
});

builder.Services.AddDbContext<DbContexto>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
#endregion

#region Administrador
string GerarTokenJwt(Administrador administrador){
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>(){
        new Claim("Email", administrador.Email),
        new Claim("Pefil", administrador.Perfil),
        new Claim(ClaimTypes.Role, administrador.Perfil)
    };

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddDays(1),
        signingCredentials: credentials

    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

app.MapPost("administrador/login", ([FromBody] LoginDTO login, IAdministradorServico administradorServico) => 
{
    var adm = administradorServico.Login(login);
    if (adm != null){
        string token = GerarTokenJwt(adm);
        return Results.Ok(new AdmLogado 
        {
            Email = adm.Email,
            Perfil = adm.Perfil,
            Token = token
        });
    }
    else
        return Results.Unauthorized();
}).AllowAnonymous().WithTags("Administrador");

app.MapPost("administrador", ([FromBody] AdministradorDTO AdministradorDTO, IAdministradorServico administradorServico) => 
{
    var adm = new Administrador{
        Email = AdministradorDTO.Email,
        Senha = AdministradorDTO.Senha,
        Perfil = AdministradorDTO.Perfil.ToString()
    };

    administradorServico.Incluir(adm);

    return Results.Created();
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
.WithTags("Administrador");

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
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
.WithTags("Administrador");

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

})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"})
.WithTags("Administrador");
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
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Editor"})
.WithTags("Veiculos");

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
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Editor"})
.WithTags("Veiculos");

app.MapDelete("veiculos/{id}", ( [FromRoute] int id, IVeiculoServico veiculoServico) => 
{
    var veiculo = veiculoServico.BuscaPorId(id);

    if (veiculo is null)
        return Results.NotFound();

    veiculoServico.Apagar(veiculo);
    return Results.Ok(veiculo);
}).RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm,Editor"})
.WithTags("Veiculos");
#endregion

#region App

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion
