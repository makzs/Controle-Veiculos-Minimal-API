using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using minimal_api.Dominio.Entidades;

namespace Test.Domain.Entidades;

[TestClass]
public class AdministradorTest
{
    [TestMethod]
    public void testarGetSetPropriedades()
    {
        // Arrange
        var adm = new Administrador();

        // Act
        adm.Id = 1;
        adm.Email = "teste@email.com";
        adm.Senha = "senhaTeste";
        adm.Perfil = "Adm";

        // Assert
        Assert.AreEqual(1, adm.Id);
        Assert.AreEqual("teste@email.com", adm.Email);
        Assert.AreEqual("senhaTeste", adm.Senha);
        Assert.AreEqual("Adm", adm.Perfil);
    }
}
