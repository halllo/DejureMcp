namespace Dejure.Test;

[TestClass]
public sealed class Suche
{
	private static DejureOrgHttpClient _dejureOrgClient = null!;

	[ClassInitialize]
	public static void ClassInitialize(TestContext context)
	{
		var httpClient = new HttpClient();
		_dejureOrgClient = new DejureOrgHttpClient(httpClient);
	}

	[TestMethod]
	public async Task FindetGesetz()
	{
		var suchergebnis = await _dejureOrgClient.Suchen("Löschen");
		var ersterTreffer = suchergebnis.Gesetze.First();
		Assert.AreEqual("HGB", ersterTreffer.GesetzesKürzel);
		Assert.AreEqual("486", ersterTreffer.ParagraphNummer);
		Assert.AreEqual("Abladen. Verladen. Umladen. Löschen", ersterTreffer.Detail);
	}

	[TestMethod]
	public async Task FindetGesetzgebung()
	{
		var suchergebnis = await _dejureOrgClient.Suchen("Schriftform");
		var ersterTreffer = suchergebnis.Gesetzgebungen.First();
		Assert.AreEqual("BGBl. I 2017 S. 626 - 29.03.2017", ersterTreffer.Gesetz);
		Assert.AreEqual("Gesetz zum Abbau verzichtbarer Anordnungen der Schriftform im Verwaltungsrecht des Bundes", ersterTreffer.Detail);
	}

	[TestMethod]
	public async Task FindetUrteil()
	{
		var suchergebnis = await _dejureOrgClient.Suchen("Löschen");
		var letzterTreffer = suchergebnis.Rechtsprechungen.Last();
		Assert.AreEqual("OLG Frankfurt, 06.09.2018 - 16 U 193/17", letzterTreffer.Urteil);
		Assert.AreEqual("Google muss auch nach der DSGVO nicht jeden \"alten\" Artikel löschen", letzterTreffer.Detail);
	}
}
