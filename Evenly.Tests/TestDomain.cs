using Evenly.Server.Domain;
using Evenly.Server.Enums;
using NUnit.Framework.Legacy;

namespace Evenly.Test;

public class TestUtente
{
    private Utente utente = null!;

    [SetUp]
    public void SetUp()
    {
        utente = new Utente("mario_r", "Mario", "Rossi", "mario@email.it", "Pass@1234", "EUR");
    }

    [Test]
    public void TestGetterUtente()
    {
        ClassicAssert.AreEqual("mario_r", utente.GetUsername());
        ClassicAssert.AreEqual("Mario", utente.GetNome());
        ClassicAssert.AreEqual("Rossi", utente.GetCognome());
        ClassicAssert.AreEqual("mario@email.it", utente.GetEmail());
        ClassicAssert.AreEqual("EUR", utente.GetValutaPredefinita());
    }

    [Test]
    public void TestSetterUtente()
    {
        utente.SetUsername("luigi_v");
        ClassicAssert.AreEqual("luigi_v", utente.GetUsername());
        utente.SetEmail("luigi@email.it");
        ClassicAssert.AreEqual("luigi@email.it", utente.GetEmail());
        utente.SetValutaPredefinita("USD");
        ClassicAssert.AreEqual("USD", utente.GetValutaPredefinita());
    }

    [Test]
    public void TestPasswordMemorizzataComeFunzioneHash()
    {
        ClassicAssert.AreNotEqual("Pass@1234", utente.GetPasswordHash());
        ClassicAssert.IsNotNull(utente.GetPasswordHash());
    }

    [Test]
    public void TestVerificaPasswordCorretta()
    {
        ClassicAssert.IsTrue(utente.VerificaPassword("Pass@1234"));
    }

    [Test]
    public void TestVerificaPasswordErrata()
    {
        ClassicAssert.IsFalse(utente.VerificaPassword("WrongPwd!"));
    }
}

public class TestGruppo
{
    private Gruppo gruppo = null!;

    [SetUp]
    public void SetUp()
    {
        gruppo = new Gruppo("Vacanza2025", "Grp@Pwd12", "EUR");
    }

    [Test]
    public void TestGetterGruppo()
    {
        ClassicAssert.AreEqual("Vacanza2025", gruppo.GetGroupName());
        ClassicAssert.AreEqual("EUR", gruppo.GetValutaRiferimento());
        Assert.That(gruppo.GetGroupId(), Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void TestSetterGruppo()
    {
        gruppo.SetGroupName("Nuovo Nome");
        ClassicAssert.AreEqual("Nuovo Nome", gruppo.GetGroupName());
        gruppo.SetValutaRiferimento("USD");
        ClassicAssert.AreEqual("USD", gruppo.GetValutaRiferimento());
    }

    [Test]
    public void TestGeneraLinkInvito()
    {
        LinkInvito link = gruppo.GeneraLinkInvito();
        ClassicAssert.IsNotNull(link);
        ClassicAssert.IsFalse(link.IsUtilizzato());
        link.Utilizza();
        ClassicAssert.IsTrue(link.IsUtilizzato());
    }
}

public class TestPartecipazione
{
    private Partecipazione partAmmin = null!;
    private Partecipazione partMembro = null!;

    [SetUp]
    public void SetUp()
    {
        var u = new Utente("alice_p", "Alice", "Pieri", "alice@email.it", "Alice@12!", "EUR");
        var g = new Gruppo("Gruppo Test", "Grp@Pwd12", "EUR");
        partAmmin = new Partecipazione(u, g, RuoloGruppo.AMMINISTRATORE, DateTime.Today);
        partMembro = new Partecipazione(u, g, RuoloGruppo.MEMBRO, DateTime.Today);
    }

    [Test]
    public void TestIsAmministratore()
    {
        ClassicAssert.IsTrue(partAmmin.IsAmministratore());
        ClassicAssert.IsFalse(partMembro.IsAmministratore());
    }
}

public class TestSpesa
{
    private Spesa spesa = null!;
    private Utente pagante = null!;
    private Gruppo gruppo = null!;

    [SetUp]
    public void SetUp()
    {
        pagante = new Utente("bob_v", "Bob", "Verdi", "bob@email.it", "BobPwd1!", "EUR");
        gruppo = new Gruppo("CenaAmici", "Cena@Pwd1", "EUR");
        spesa = new Spesa("Cena al ristorante", 90.0m,
            new DateTime(2025, 6, 1), pagante, gruppo, MetodoDivisione.EQUA);
    }

    [Test]
    public void TestGetterSpesa()
    {
        ClassicAssert.AreEqual("Cena al ristorante", spesa.GetCausale());
        ClassicAssert.AreEqual(90.0m, spesa.GetImporto());
        ClassicAssert.AreEqual(new DateTime(2025, 6, 1), spesa.GetData());
        ClassicAssert.AreEqual(MetodoDivisione.EQUA, spesa.GetMetodoDivisione());
        ClassicAssert.AreEqual("bob_v", spesa.GetPagante().GetUsername());
    }

    [Test]
    public void TestSetterSpesa()
    {
        spesa.SetCausale("Benzina");
        ClassicAssert.AreEqual("Benzina", spesa.GetCausale());
        spesa.SetMetodoDivisione(MetodoDivisione.PERCENTUALE);
        ClassicAssert.AreEqual(MetodoDivisione.PERCENTUALE, spesa.GetMetodoDivisione());
    }
}

public class TestRimborso
{
    private Rimborso rimborso = null!;

    [SetUp]
    public void SetUp()
    {
        var debitore = new Utente("anna_m", "Anna", "Marini", "anna@email.it", "Anna@Pwd1", "EUR");
        var creditore = new Utente("luca_f", "Luca", "Ferrari", "luca@email.it", "Luca@Pwd1", "EUR");
        var gruppo = new Gruppo("GruppoR", "Rimb@Pwd1", "EUR");
        rimborso = new Rimborso(debitore, creditore, 50.0m, gruppo, DateTime.Today);
    }

    [Test]
    public void TestGetterRimborso()
    {
        ClassicAssert.AreEqual("anna_m", rimborso.GetDebitore().GetUsername());
        ClassicAssert.AreEqual("luca_f", rimborso.GetCreditore().GetUsername());
        ClassicAssert.AreEqual(50.0m, rimborso.GetImporto());
        ClassicAssert.AreEqual(DateTime.Today, rimborso.GetData());
    }
}

public class TestSaldo
{
    private Utente utente = null!;
    private Gruppo gruppo = null!;

    [SetUp]
    public void SetUp()
    {
        utente = new Utente("sara_c", "Sara", "Conti", "sara@email.it", "Sara@Pw1!", "EUR");
        gruppo = new Gruppo("GruppoSaldo", "Sald@Pw1!", "EUR");
    }

    [Test]
    public void TestGetStato()
    {
        ClassicAssert.AreEqual(StatoSaldo.CREDITO, new Saldo(utente, gruppo, 30.0m).GetStato());
        ClassicAssert.AreEqual(StatoSaldo.DEBITO, new Saldo(utente, gruppo, -15.0m).GetStato());
        ClassicAssert.AreEqual(StatoSaldo.PARI, new Saldo(utente, gruppo, 0.0m).GetStato());
    }

    [Test]
    public void TestCalcolaSaldo()
    {
        var saldo = new Saldo(utente, gruppo, 0m);
        saldo.CalcolaSaldo(new List<Spesa>(), new List<Rimborso>());
        ClassicAssert.AreEqual(StatoSaldo.PARI, saldo.GetStato());
    }
}
