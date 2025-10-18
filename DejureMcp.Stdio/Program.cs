using Dejure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Services.AddHttpClient<DejureOrgHttpClient>(httpClient =>
{
	httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0");
});

builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public class DejureTools(DejureOrgHttpClient dejureOrgHttpClient)
{
	[McpServerTool(Title = "Rechtsgebiete auflisten", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Get all fields of law (Rechtsgebiete)")]
	public async Task<List<Rechtsgebiet>> GetRechtsgebiete()
	{
		var dejurOrg = await dejureOrgHttpClient.Load();
		var rechtsgebiete = dejurOrg.Rechtsgebiete
			.Select(r => new Rechtsgebiet(r.Name, [.. r.Gesetze.Select(g => new Gesetz(g.Kürzel, g.Name))]))
			.ToList();
		return rechtsgebiete;
	}

	[McpServerTool(Title = "Gesetze auflisten", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Get all legislation (Gesetze)")]
	public async Task<List<Gesetz>> GetGesetze()
	{
		var dejurOrg = await dejureOrgHttpClient.Load();
		var gesetze = dejurOrg.Gesetze
			.Select(g => new Gesetz(g.Kürzel, g.Bezeichnung))
			.ToList();
		return gesetze;
	}

	[McpServerTool(Title = "Paragraphen auflisten", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Get all paragraphs of a legislation")]
	public async Task<GetParagraphsResponse> GetParagraphs(string gesetzesKürzel)
	{
		var dejurOrg = await dejureOrgHttpClient.Load();
		var gesetz = dejurOrg.Gesetze.Single(g => string.Equals(g.Kürzel, gesetzesKürzel, StringComparison.InvariantCultureIgnoreCase));
		var inhaltsverzeichnnis = await gesetz.LoadInhaltsverzeichnis();
		var paragraphs = inhaltsverzeichnnis.Paragraphen
			.Select(p => new Paragraph(p.Nummer, p.Name))
			.ToList();
		return new GetParagraphsResponse(inhaltsverzeichnnis.Intro, paragraphs);
	}

	[McpServerTool(Title = "Paragraph lesen", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Read paragraphs of a legislation")]
	public async Task<ReadParagraphResponse> ReadParagraph(string gesetzesKürzel, string paragraphNummer)
	{
		var paragraphText = await dejureOrgHttpClient.LoadPragraphText(gesetzesKürzel, paragraphNummer.Trim([' ', '§']));
		return new ReadParagraphResponse(paragraphText.Intro, paragraphText.Content);
	}

	public record Rechtsgebiet(string Name, List<Gesetz> Gesetze);
	public record Gesetz(string Kürzel, string Name);
	public record GetParagraphsResponse(string Intro, List<Paragraph> Paragraphs);
	public record Paragraph(string Nummer, string Name);
	public record ReadParagraphResponse(string Intro, string Text);
}