using Dejure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Services.AddHttpClient<DejureOrgHttpClient>(httpClient =>
{
	httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0");
});

builder.Services.AddMcpServer()
	.WithStdioServerTransport()
	.WithToolsFromAssembly()
	.WithResourcesFromAssembly()
	.WithListResourcesHandler(async (request, _) =>
	{
		var dejureOrgHttpClient = request.Services!.GetRequiredService<DejureOrgHttpClient>();
		var dejurOrg = await dejureOrgHttpClient.Load();
		return new ListResourcesResult
		{
			Resources = [.. dejurOrg.Rechtsgebiete.Select(r => new Resource
			{
				Uri = $"dejure://rechtsgebiete/{Uri.EscapeDataString(r.Name)}",
				Name = r.Name,
				MimeType = "application/json",
			})],
		};
	})
	.WithReadResourceHandler(async (request, ct) =>
	{
		var dejureOrgHttpClient = request.Services!.GetRequiredService<DejureOrgHttpClient>();
		var dejurOrg = await dejureOrgHttpClient.Load();
		var rechtsgebiet = dejurOrg.Rechtsgebiete.FirstOrDefault(r => string.Equals($"dejure://rechtsgebiete/{Uri.EscapeDataString(r.Name)}", request.Params!.Uri, StringComparison.InvariantCultureIgnoreCase));
		if (rechtsgebiet != null)
		{
			return new ReadResourceResult
			{
				Contents = [new TextResourceContents
				{
					Text = JsonSerializer.Serialize(new
					{
						Rechtsgebiet = rechtsgebiet?.Name,
						Gesetze = rechtsgebiet?.Gesetze.Select(g => new DejureResources.GesetzDto(g.Kürzel, g.Name)).ToList(),
					}),
					MimeType = "application/json",
					Uri = request.Params!.Uri
				}]
			};
		}
		else
		{
			throw new McpException($"Resource not found: {request.Params?.Uri}");
		}
	})
	;

await builder.Build().RunAsync();

[McpServerToolType]
public class DejureTools(DejureOrgHttpClient dejureOrgHttpClient)
{
	[McpServerTool(Name = "dejure_rechtsgebiete_auflisten", Title = "Rechtsgebiete auflisten", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Listet alle Rechtsgebiete und deren Gesetze auf.")]
	public async Task<List<RechtsgebietDto>> GetRechtsgebiete()
	{
		var dejurOrg = await dejureOrgHttpClient.Load();
		var rechtsgebiete = dejurOrg.Rechtsgebiete
			.Select(r => new RechtsgebietDto(r.Name, [.. r.Gesetze.Select(g => new GesetzDto(g.Kürzel, g.Name))]))
			.ToList();
		return rechtsgebiete;
	}

	[McpServerTool(Name = "dejure_gesetze_auflisten", Title = "Gesetze auflisten", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Listet alle Gesetze auf.")]
	public async Task<List<GesetzDto>> GetGesetze()
	{
		var dejurOrg = await dejureOrgHttpClient.Load();
		var gesetze = dejurOrg.Gesetze
			.Select(g => new GesetzDto(g.Kürzel, g.Bezeichnung))
			.ToList();
		return gesetze;
	}

	[McpServerTool(Name = "dejure_paragraphen_auflisten", Title = "Paragraphen auflisten", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Listet alle Paragraphen eines Gesetzes auf.")]
	public async Task<GetParagraphsResponse> GetParagraphs(string gesetzesKuerzel)
	{
		var dejurOrg = await dejureOrgHttpClient.Load();
		var gesetz = dejurOrg.Gesetze.Single(g => string.Equals(g.Kürzel, gesetzesKuerzel, StringComparison.InvariantCultureIgnoreCase));
		var inhaltsverzeichnnis = await gesetz.LoadInhaltsverzeichnis();
		var paragraphs = inhaltsverzeichnnis.Paragraphen
			.Select(p => new ParagraphDto(p.Nummer, p.Name))
			.ToList();
		return new GetParagraphsResponse(inhaltsverzeichnnis.Intro, paragraphs);
	}

	[McpServerTool(Name = "dejure_paragraph_lesen", Title = "Paragraph lesen", Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
	[Description("Liest einen Paragraphen eines Gesetzes.")]
	public async Task<ReadParagraphResponse> ReadParagraph(string gesetzesKuerzel, string paragraphNummer)
	{
		var paragraphText = await dejureOrgHttpClient.LoadPragraphText(gesetzesKuerzel, paragraphNummer.Trim([' ', '§']));
		return new ReadParagraphResponse(paragraphText.Intro, paragraphText.Heading, paragraphText.Content);
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

	public record RechtsgebietDto(string Rechtsgebiet, List<GesetzDto> Gesetze);
	public record GesetzDto(string Kuerzel, string Gesetz);
	public record GetParagraphsResponse(string Intro, List<ParagraphDto> Paragraphs);
	public record ParagraphDto(string Nummer, string Paragraph);
	public record ReadParagraphResponse(string Intro, string Heading, string Text);
	public record SuchenResponse(List<SuchenResponse.Gesetz> Gesetze, List<SuchenResponse.Gesetzgebung> Gesetzgebungen, List<SuchenResponse.Rechtsprechung> Rechtsprechungen)
	{
		public record Gesetz(string GesetzesKuerzel, string ParagraphNummer, string Detail);
		public record Gesetzgebung(string Gesetz, string Detail);
		public record Rechtsprechung(string Urteil, string Detail);
	}
}

[McpServerResourceType]
public class DejureResources(DejureOrgHttpClient dejureOrgHttpClient)
{
	[McpServerResource(UriTemplate = "dejure://gesetze", Name = "Gesetze", MimeType = "application/json")]
	[Description("Rechtsgebiete")]
	public async Task<string> GesetzeResource()
	{
		var dejurOrg = await dejureOrgHttpClient.Load();
		return JsonSerializer.Serialize(dejurOrg.Gesetze.Select(g => new GesetzDto(g.Kürzel, g.Name)));
	}

	[McpServerResource(UriTemplate = "dejure://gesetze/{gesetzesKuerzel}", Name = "Gesetz", MimeType = "application/json")]
	[Description("Gesetz")]
	public async Task<TextResourceContents> GesetzeResource(RequestContext<ReadResourceRequestParams> requestContext, string gesetzesKuerzel)
	{
		var dejurOrg = await dejureOrgHttpClient.Load();
		var gesetz = dejurOrg.Gesetze.FirstOrDefault(g => string.Equals(g.Kürzel, gesetzesKuerzel, StringComparison.InvariantCultureIgnoreCase));
		if (gesetz != null)
		{
			var inhaltsverzeichnis = await gesetz.LoadInhaltsverzeichnis();
			InhaltsverzeichnnisDto dto = new InhaltsverzeichnnisDto(
				inhaltsverzeichnis.Intro,
				[.. inhaltsverzeichnis.Paragraphen.Select(p => new ParagraphDto(p.Nummer, p.Name))]
			);
			return new TextResourceContents
			{
				Text = JsonSerializer.Serialize(dto),
				MimeType = "application/json",
				Uri = requestContext.Params!.Uri
			};
		}
		else
		{
			throw new McpException($"Resource not found: {requestContext.Params?.Uri}");
		}
	}

	[McpServerResource(UriTemplate = "dejure://gesetze/{gesetzesKuerzel}/paragraphen/{paragraphNummer}", Name = "Paragraph", MimeType = "text/plain")]
	[Description("Paragraph")]
	public async Task<TextResourceContents> ParagraphenResource(RequestContext<ReadResourceRequestParams> requestContext, string gesetzesKuerzel, string paragraphNummer)
	{
		var paragraphText = await dejureOrgHttpClient.LoadPragraphText(gesetzesKuerzel, paragraphNummer.Trim([' ', '§']));
		if (paragraphText != null && !string.IsNullOrWhiteSpace(paragraphText.Content))
		{
			return new TextResourceContents
			{
				Text = $"{paragraphText.Intro}\n{paragraphText.Heading}\n\n{paragraphText.Content}".Trim(),
				MimeType = "text/plain",
				Uri = requestContext.Params!.Uri
			};
		}
		else
		{
			throw new McpException($"Resource not found: {requestContext.Params?.Uri}");
		}
	}

	public record GesetzDto(string Kuerzel, string Gesetz);
	public record InhaltsverzeichnnisDto(string Intro, List<ParagraphDto> Paragraphen);
	public record ParagraphDto(string Kuerzel, string Paragraph);
}