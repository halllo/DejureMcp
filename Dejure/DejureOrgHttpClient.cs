using HtmlAgilityPack;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dejure
{
	public class DejureOrgHttpClient
	{
		private readonly HttpClient _httpClient;

		public DejureOrgHttpClient(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public async Task<DejureOrg> Load()
		{
			using var response = await _httpClient.GetAsync("https://dejure.org/");
			var content = await response.Content.ReadAsStringAsync();

			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(content);
			return new DejureOrg(_httpClient, htmlDoc);
		}

		public async Task<DejureOrg.Gesetz.Inhaltsverzeichnnis.Paragraph.Text> LoadPragraphText(string gesetzesKürzel, string paragraphNummer)
		{
			using var response = await _httpClient.GetAsync($"https://dejure.org/gesetze/{gesetzesKürzel}/{paragraphNummer}.html");
			var content = await response.Content.ReadAsStringAsync();

			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(content);
			return new DejureOrg.Gesetz.Inhaltsverzeichnnis.Paragraph.Text(gesetzesKürzel, paragraphNummer, htmlDoc);
		}

		public async Task<DejureOrg.Suchergebnis> Suchen(string anfrage)
		{
			using var response = await _httpClient.GetAsync($"https://dejure.org/cgi-bin/jquery-suche01.fcgi?term={anfrage.Encode()}&korrektur=1");
			var content = await response.Content.ReadAsStringAsync();
			return DejureOrg.ParseSuchergebnis(content);
		}
	}
}
