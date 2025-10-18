namespace Dejure.Test;

[TestClass]
public sealed class Rechtsgebiete
{
	private static IReadOnlyList<DejureOrg.Rechtsgebiet> _rechtsgebiete = null!;

	[ClassInitialize]
	public static async Task ClassInitialize(TestContext context)
	{
		var httpClient = new HttpClient();
		var dejureOrgClient = new DejureOrgHttpClient(httpClient);
		var dejureOrg = await dejureOrgClient.Load();
		_rechtsgebiete = dejureOrg.Rechtsgebiete;
	}

	[TestMethod]
	public void Has34()
	{
		Assert.AreEqual(34, _rechtsgebiete.Count);
	}

	[TestMethod]
	public void StartsWithBürgerlichesRecht()
	{
		Assert.AreEqual("Bürgerliches Recht", _rechtsgebiete.First().Name);
	}

	[TestMethod]
	public void EndsWithÜbergreifendes()
	{
		Assert.AreEqual("Übergreifendes", _rechtsgebiete.Last().Name);
	}

	[TestMethod]
	public void BürgerlichesContains33Gesetze()
	{
		var bürgerliches = _rechtsgebiete.Single(r => r.Name == "Bürgerliches Recht");
		Assert.AreEqual(33, bürgerliches.Gesetze.Count);
	}

	[TestMethod]
	public void BürgerlichesStartsWithBürgerlichesGesetzbuch()
	{
		var bürgerliches = _rechtsgebiete.Single(r => r.Name == "Bürgerliches Recht");
		Assert.AreEqual("BGB", bürgerliches.Gesetze.First().Kürzel);
		Assert.AreEqual("Bürgerliches Gesetzbuch", bürgerliches.Gesetze.First().Name);
	}
}
