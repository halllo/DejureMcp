namespace Dejure.Test;

[TestClass]
public sealed class Gesetze
{
	private static IReadOnlyList<DejureOrg.Gesetz> _gesetze = null!;
	private static DejureOrgHttpClient _dejureOrgClient = null!;

	[ClassInitialize]
	public static async Task ClassInitialize(TestContext context)
	{
		var httpClient = new HttpClient();
		_dejureOrgClient = new DejureOrgHttpClient(httpClient);
		var dejureOrg = await _dejureOrgClient.Load();
		_gesetze = dejureOrg.Gesetze;
	}

	[TestMethod]
	public void Has315()
	{
		Assert.HasCount(315, _gesetze);
	}

	[TestMethod]
	public void StartsWithAAG()
	{
		var first = _gesetze.First();
		Assert.AreEqual("AAG (Gesetz über den Ausgleich der Arbeitgeberaufwendungen für Entgeltfortzahlung)", first.Name);
		Assert.AreEqual("AAG", first.Kürzel);
		Assert.AreEqual("Gesetz über den Ausgleich der Arbeitgeberaufwendungen für Entgeltfortzahlung", first.Bezeichnung);
	}

	[TestMethod]
	public async Task InhaltsverzeichnnisBRAOHas290ParagraphenIncluding43e()
	{
		var brao = _gesetze.Single(g => g.Kürzel == "BRAO");
		var inhaltsverzeichnis = await brao.LoadInhaltsverzeichnis();
		var paragraphen = inhaltsverzeichnis.Paragraphen;
		Assert.HasCount(290, paragraphen);

		var p43e = paragraphen.Single(p => p.Nummer == "43e");
		Assert.AreEqual("§ 43e Inanspruchnahme von Dienstleistungen", p43e.Name);
	}

	[TestMethod]
	public async Task DsgvoErwägungsgründe()
	{
		var dsgvo = await _gesetze.Single(g => g.Kürzel == "DSGVO").LoadInhaltsverzeichnis();
		var erwägungsgründe = dsgvo.Paragraphen.Single(p => p.Nummer.StartsWith("Erw"));
		var erwägungsgründeText = await erwägungsgründe.LoadText();
		Assert.StartsWith("DAS EUROPÄISCHE PARLAMENT UND DER RAT DER EUROPÄISCHEN UNION", erwägungsgründeText.Content);
	}

	[TestMethod]
	public async Task BRAO43eTextStartsAndEndsWithCertainText()
	{
		var brao = await _gesetze.Single(g => g.Kürzel == "BRAO").LoadInhaltsverzeichnis();
		var p43e = brao.Paragraphen.Single(p => p.Nummer == "43e");
		var b43eText = await p43e.LoadText();
		Assert.StartsWith("(1) 1Der Rechtsanwalt darf Dienstleistern den Zugang zu Tatsachen eröffnen, auf die sich die Verpflichtung zur Verschwiegenheit gemäß § 43a Absatz 2 Satz 1 bezieht", b43eText.Content);
		Assert.EndsWith("(8) Die Vorschriften zum Schutz personenbezogener Daten bleiben unberührt.", b43eText.Content);
	}

	[TestMethod]
	public async Task DirectBRAO43eTextStartsAndEndsWithCertainText()
	{
		var b43eText = await _dejureOrgClient.LoadPragraphText("BRAO", "43e");
		Assert.StartsWith("(1) 1Der Rechtsanwalt darf Dienstleistern den Zugang zu Tatsachen eröffnen, auf die sich die Verpflichtung zur Verschwiegenheit gemäß § 43a Absatz 2 Satz 1 bezieht", b43eText.Content);
		Assert.EndsWith("(8) Die Vorschriften zum Schutz personenbezogener Daten bleiben unberührt.", b43eText.Content);
	}

	[TestMethod]
	public async Task DirectBGB34TextStartsAndEndsWithCertainText()
	{
		var bgb34Text = await _dejureOrgClient.LoadPragraphText("BGB", "34");
		Assert.EndsWith("Kapitel 1 - Allgemeine Vorschriften (§§ 21 - 54)", bgb34Text.Intro);
		Assert.AreEqual("§ 34 Ausschluss vom Stimmrecht", bgb34Text.Heading);
		Assert.StartsWith("Ein Mitglied ist nicht stimmberechtigt, wenn die Beschlussfassung die Vornahme eines Rechtsgeschäfts mit ihm oder die Einleitung oder Erledigung eines Rechtsstreits zwischen ihm und dem Verein betrifft.", bgb34Text.Content);
	}
}
