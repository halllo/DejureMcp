namespace Dejure.Test;

[TestClass]
public sealed class LoadTest
{
	[TestMethod]
	public async Task Load10TimesWithoutUserAgent()
	{
		using var httpClient = new HttpClient();
		var dejureOrgClient = new DejureOrgHttpClient(httpClient);

		for (int i = 0; i < 10; i++)
		{
			await dejureOrgClient.LoadPragraphText("BRAO", "43e");
		}
	}

	[TestMethod]
	public async Task Load10TimesWithUserAgent()
	{
		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0");
		var dejureOrgClient = new DejureOrgHttpClient(httpClient);

		for (int i = 0; i < 10; i++)
		{
			await dejureOrgClient.LoadPragraphText("BRAO", "43e");
		}
	}
}
