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
	public async Task FindetGesetz1()
	{
		var suchergebnis = await _dejureOrgClient.Suchen("Löschen");
		var ersterTreffer = suchergebnis.Gesetze.First();
		Assert.AreEqual("HGB", ersterTreffer.GesetzesKürzel);
		Assert.AreEqual("486", ersterTreffer.ParagraphNummer);
		Assert.AreEqual("Abladen. Verladen. Umladen. Löschen", ersterTreffer.Detail);
	}

	[TestMethod]
	public async Task FindetGesetz2()
	{
		var suchergebnis = await _dejureOrgClient.Suchen("berufsgeheimnis");
		var ersterTreffer = suchergebnis.Gesetze.Last();
		Assert.AreEqual("DSA/GdD", ersterTreffer.GesetzesKürzel);
		Assert.AreEqual("84", ersterTreffer.ParagraphNummer);
		Assert.AreEqual("Berufsgeheimnis", ersterTreffer.Detail);
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
		Assert.AreEqual("BGH, 06.03.2013 - 1 StR 578/12", letzterTreffer.Urteil);
		Assert.AreEqual("Brandstiftung (Vorsatz; Versuch -dpp-  unmittelbares Ansetzen); schwere Brandstiftung (Begriff der Zerstörung; zur Wohnung von Menschen dienendes Gebäude -dpp-  gemischt genutzte Gebäude, Kellerräume; Gefahr der Gesundheitsschädigung); besonders schwere Brandstiftung (Erschweren des Löschens)", letzterTreffer.Detail);
	}
}
