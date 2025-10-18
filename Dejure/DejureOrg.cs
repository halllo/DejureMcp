using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dejure
{
	public class DejureOrg
	{
		private readonly HttpClient http;
		private readonly HtmlDocument html;
		internal DejureOrg(HttpClient http, HtmlDocument html)
		{
			this.http = http;
			this.html = html;
		}

		public IReadOnlyList<Rechtsgebiet> Rechtsgebiete => this.html.DocumentNode
			.SelectNodes("//div[contains(@class, 'gesetzesliste')]")
			.Select(node => new Rechtsgebiet(node))
			.ToList();

		public class Rechtsgebiet
		{
			private readonly HtmlNode node;
			internal Rechtsgebiet(HtmlNode node)
			{
				this.node = node;
			}

			public string Name => node.SelectSingleNode(".//h3")?.InnerText.Trim() ?? string.Empty;

			public IReadOnlyList<Gesetz> Gesetze => node
				.SelectNodes(".//a[starts-with(@href, '/gesetze')]")
				.Select(node => new Gesetz(node))
				.ToList();

			public class Gesetz
			{
				private readonly HtmlNode node;
				internal Gesetz(HtmlNode node)
				{
					this.node = node;
				}

				public string Name => node.InnerText.Trim();

				public string Url => "https://dejure.org" + node.GetAttributeValue("href", string.Empty);

				public string Kürzel => node.GetAttributeValue("href", string.Empty).Replace("/gesetze/", "").Trim();
			}
		}

		public IReadOnlyList<Gesetz> Gesetze => html.DocumentNode
			.SelectNodes("//div[@id='alphabetisch']")
			.SelectMany(node => node.SelectNodes(".//li"))
			.Select(node => new Gesetz(this.http, node))
			.ToList();

		public class Gesetz
		{
			private readonly HttpClient http;
			private readonly HtmlNode node;
			internal Gesetz(HttpClient http, HtmlNode node)
			{
				this.http = http;
				this.node = node;
			}

			public string Name => node.InnerText.Trim();

			public string Url => "https://dejure.org" + node.SelectSingleNode(".//a")?.GetAttributeValue("href", string.Empty);

			public string Kürzel => node.SelectSingleNode(".//a")?.GetAttributeValue("href", string.Empty).Replace("/gesetze/", "").Trim() ?? string.Empty;

			public string Bezeichnung => string.Join("", node.ChildNodes
				.Where(n => n.NodeType == HtmlNodeType.Text)
				.Select(n => n.InnerText))
				.Trim([' ', '(', ')']);

			public string Html => node.OuterHtml;


			public async Task<Inhaltsverzeichnnis> LoadInhaltsverzeichnis()
			{
				using var response = await this.http.GetAsync(Url);
				var content = await response.Content.ReadAsStringAsync();

				var htmlDoc = new HtmlDocument();
				htmlDoc.LoadHtml(content);
				return new Inhaltsverzeichnnis(this, htmlDoc);
			}

			public class Inhaltsverzeichnnis
			{
				private readonly HtmlDocument html;
				private Gesetz Gesetz { get; }
				internal Inhaltsverzeichnnis(Gesetz gesetz, HtmlDocument html)
				{
					this.html = html;
					this.Gesetz = gesetz;
				}

				public string Intro => this.html.DocumentNode.SelectSingleNode("//div[@id='headgesetz']")?.InnerText.Trim() ?? string.Empty;

				public IReadOnlyList<Paragraph> Paragraphen => this.html.DocumentNode
					.SelectNodes($"//p[.//a[starts-with(@href, '/gesetze/{Gesetz.Kürzel}/') and not(@class='zu_paragraph')]]")
					.Select(node => new Paragraph(node, this))
					.ToList();

				public class Paragraph
				{
					private readonly HtmlNode node;
					public Inhaltsverzeichnnis Inhaltsverzeichnnis { get; }
					internal Paragraph(HtmlNode node, Inhaltsverzeichnnis inhaltsverzeichnnis)
					{
						this.node = node;
						this.Inhaltsverzeichnnis = inhaltsverzeichnnis;
					}

					public string Nummer => this.node.SelectSingleNode(".//a")?.InnerText.Trim() ?? string.Empty;

					public string Name => this.node.InnerText.Trim();

					public string Url => this.node.SelectSingleNode(".//a") != null
						? "https://dejure.org" + this.node.SelectSingleNode(".//a")!.GetAttributeValue("href", string.Empty)
						: string.Empty;

					public async Task<Text> LoadText()
					{
						using var response = await this.Inhaltsverzeichnnis.Gesetz.http.GetAsync(Url);
						var content = await response.Content.ReadAsStringAsync();

						var htmlDoc = new HtmlDocument();
						htmlDoc.LoadHtml(content);
						return new Text(Inhaltsverzeichnnis.Gesetz, this, htmlDoc);
					}

					public class Text
					{
						private readonly Gesetz? gesetz;
						private readonly string? gesetzesKürzel;
						private readonly Paragraph? paragraph;
						private readonly string? paragraphNummer;
						private readonly HtmlDocument html;
						internal Text(Gesetz gesetz, Paragraph paragraph, HtmlDocument html)
						{
							this.gesetz = gesetz;
							this.paragraph = paragraph;
							this.html = html;
						}

						internal Text(string gesetzesKürzel, string paragraphNummer, HtmlDocument html)
						{
							this.gesetzesKürzel = gesetzesKürzel;
							this.paragraphNummer = paragraphNummer;
							this.html = html;
						}

						public string Intro => this.html.DocumentNode.SelectSingleNode("//div[@id='headgesetz']")?.InnerText.Trim() ?? string.Empty;

						public string Content => this.html.DocumentNode.SelectSingleNode("//div[@id='gesetzestext']")?.InnerText.Trim() ?? string.Empty;
					}
				}
			}
		}
	}
}
