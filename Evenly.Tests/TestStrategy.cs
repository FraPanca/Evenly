using Evenly.Server.Domain;
using Evenly.Server.Enums;
using Evenly.Server.Strategy;
using NUnit.Framework.Legacy;

namespace Evenly.Test;

public class TestStrategiaDivisione
{
    private Utente pagante = null!;
    private Utente parte = null!;
    private Gruppo gruppo = null!;

    [SetUp]
    public void SetUp()
    {
        pagante = new Utente("gio_p", "Giovanni", "Poli", "gio@email.it", "Gio@Pwd12", "EUR");
        parte = new Utente("elena_r", "Elena", "Ricci", "elena@email.it", "Elena@12!", "EUR");
        gruppo = new Gruppo("SpesaGruppo", "Spesa@Pw1", "EUR");
    }

    [Test]
    public void TestDivisioneEqua()
    {
        var spesa = new Spesa("Cena", 90.0m, DateTime.Today, pagante, gruppo, MetodoDivisione.EQUA);
        var quote = new DivisioneEqua().CalcolaQuote(spesa, new List<Utente> { pagante, parte });

        ClassicAssert.AreEqual(2, quote.Count);
        Assert.That(quote.Sum(q => q.GetImporto()), Is.EqualTo(90.0m));
        foreach (var q in quote)
            ClassicAssert.AreEqual(45.0m, q.GetImporto());
    }

    [Test]
    public void TestDivisioneEquaTrePersone()
    {
        var terzo = new Utente("terzo", "Terzo", "T", "t@t.it", "T@Pwd12!", "EUR");
        var spesa = new Spesa("Hotel", 100.0m, DateTime.Today, pagante, gruppo, MetodoDivisione.EQUA);
        var quote = new DivisioneEqua().CalcolaQuote(spesa, new List<Utente> { pagante, parte, terzo });

        ClassicAssert.AreEqual(3, quote.Count);
        Assert.That(quote.Sum(q => q.GetImporto()), Is.EqualTo(100.0m));
    }

    [Test]
    public void TestDivisioneImportiEsatti()
    {
        var spesa = new Spesa("Hotel", 100m, DateTime.Today, pagante, gruppo, MetodoDivisione.IMPORTI_ESATTI);
        var importi = new Dictionary<Utente, decimal> { { pagante, 70m }, { parte, 30m } };
        var quote = new DivisioneImportiEsatti().CalcolaQuote(spesa, new List<Utente> { pagante, parte }, importi);

        ClassicAssert.AreEqual(70m, quote.Find(q => q.GetUtente() == pagante)!.GetImporto());
        ClassicAssert.AreEqual(30m, quote.Find(q => q.GetUtente() == parte)!.GetImporto());
    }

    [Test]
    public void TestDivisioneImportiEsattiSommaErrata()
    {
        var spesa = new Spesa("Hotel", 100m, DateTime.Today, pagante, gruppo, MetodoDivisione.IMPORTI_ESATTI);
        var importiSbagliati = new Dictionary<Utente, decimal> { { pagante, 60m }, { parte, 30m } };

        Assert.Throws<ArgumentException>(() =>
            new DivisioneImportiEsatti()
                .CalcolaQuote(spesa, new List<Utente> { pagante, parte }, importiSbagliati));
    }

    [Test]
    public void TestDivisionePercentuale()
    {
        var spesa = new Spesa("Affitto", 200m, DateTime.Today, pagante, gruppo, MetodoDivisione.PERCENTUALE);
        var percentuali = new Dictionary<Utente, decimal> { { pagante, 60m }, { parte, 40m } };
        var quote = new DivisionePercentuale().CalcolaQuote(spesa, new List<Utente> { pagante, parte }, percentuali);

        ClassicAssert.AreEqual(120m, quote.Find(q => q.GetUtente() == pagante)!.GetImporto());
        ClassicAssert.AreEqual(80m, quote.Find(q => q.GetUtente() == parte)!.GetImporto());
    }

    [Test]
    public void TestDivisionePercentualeNon100()
    {
        var spesa = new Spesa("Test", 100m, DateTime.Today, pagante, gruppo, MetodoDivisione.PERCENTUALE);
        var percentualiSbagliati = new Dictionary<Utente, decimal> { { pagante, 60m }, { parte, 30m } };

        Assert.Throws<ArgumentException>(() =>
            new DivisionePercentuale()
                .CalcolaQuote(spesa, new List<Utente> { pagante, parte }, percentualiSbagliati));
    }
}
