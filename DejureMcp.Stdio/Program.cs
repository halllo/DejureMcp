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
	[McpServerTool(Name = "dejure_rechtsgebiete_auflisten", Title = "Rechtsgebiete auflisten", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Listet alle Rechtsgebiete und deren Gesetze auf.")]
	public async Task<List<Rechtsgebiet>> GetRechtsgebiete()
	{
		var dejurOrg = await dejureOrgHttpClient.Load();
		var rechtsgebiete = dejurOrg.Rechtsgebiete
			.Select(r => new Rechtsgebiet(r.Name, [.. r.Gesetze.Select(g => new Gesetz(g.Kürzel, g.Name))]))
			.ToList();
		return rechtsgebiete;
	}

	[McpServerTool(Name = "dejure_gesetze_auflisten", Title = "Gesetze auflisten", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Listet alle Gesetze auf.")]
	public async Task<List<Gesetz>> GetGesetze()
	{
		var dejurOrg = await dejureOrgHttpClient.Load();
		var gesetze = dejurOrg.Gesetze
			.Select(g => new Gesetz(g.Kürzel, g.Bezeichnung))
			.ToList();
		return gesetze;
	}

	[McpServerTool(Name = "dejure_paragraphen_auflisten", Title = "Paragraphen auflisten", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Listet alle Paragraphen eines Gesetzes auf.")]
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

	[McpServerTool(Name = "dejure_paragraph_lesen", Title = "Paragraph lesen", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Lädt einen Paragraphen eines Gesetzes.")]
	public async Task<ReadParagraphResponse> ReadParagraph(string gesetzesKürzel, string paragraphNummer)
	{
		var paragraphText = await dejureOrgHttpClient.LoadPragraphText(gesetzesKürzel, paragraphNummer.Trim([' ', '§']));
		return new ReadParagraphResponse(paragraphText.Intro, paragraphText.Content);
	}

	[McpServerTool(Name = "dejure_suchen", Title = "Gesetze suchen", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Sucht nach Gesetzen und Rechtsprechungen.")]
	public async Task<SuchenResponse> Suchen(string anfrage)
	{
		var suchergebnis = await dejureOrgHttpClient.Suchen(anfrage);
		return new SuchenResponse(
			Gesetze: [.. suchergebnis.Gesetze.Select(g => new SuchenResponse.Gesetz(g.GesetzesKürzel, g.ParagraphNummer, g.Detail))],
			Gesetzgebungen: [.. suchergebnis.Gesetzgebungen.Select(g => new SuchenResponse.Gesetzgebung(g.Gesetz, g.Detail))],
			Rechtsprechungen: [.. suchergebnis.Rechtsprechungen.Select(r => new SuchenResponse.Rechtsprechung(r.Urteil, r.Detail))]);
	}

	public record Rechtsgebiet(string Name, List<Gesetz> Gesetze);
	public record Gesetz(string Kürzel, string Name);
	public record GetParagraphsResponse(string Intro, List<Paragraph> Paragraphs);
	public record Paragraph(string Nummer, string Name);
	public record ReadParagraphResponse(string Intro, string Text);
	public record SuchenResponse(List<SuchenResponse.Gesetz> Gesetze, List<SuchenResponse.Gesetzgebung> Gesetzgebungen, List<SuchenResponse.Rechtsprechung> Rechtsprechungen)
	{
		public record Gesetz(string GesetzesKürzel, string ParagraphNummer, string Detail);
		public record Gesetzgebung(string Gesetz, string Detail);
		public record Rechtsprechung(string Urteil, string Detail);
	}
}