<Query Kind="Program">
  <NuGetReference>Flurl</NuGetReference>
  <NuGetReference>Grynwald.MarkdownGenerator</NuGetReference>
  <NuGetReference>Markdig</NuGetReference>
  <NuGetReference>RestSharp</NuGetReference>
  <Namespace>Flurl</Namespace>
  <Namespace>Flurl.Util</Namespace>
  <Namespace>Grynwald.MarkdownGenerator</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Markdig</Namespace>
</Query>

// https://github.com/ap0llo/markdown-generator
void Main() {
	var md = new MdDocument();
	md.Root.Add(new MdTable(
		headerRow: new(
			cells: new[] { "Foo00000", "Bar" }
		),
		rows: new MdTableRow[] {
			new(cells: new MdSpan[] { "Static Shield", new MdStaticShieldIOSpan(
				label: "foo",
				message: "bar",
				color: "red",
				linkUrl: "https://google.com"
			) }),
			new(cells: new MdSpan[] { "CurseForge Shield", new MdCurseForgeShieldSpan(
				projectSlug: "fabric-api",
				projectId: 306612,
				projectUrl: "https://www.curseforge.com/minecraft/mc-mods/fabric-api",
				style: "short"
			) }),
			new(cells: new MdSpan[] { "Modrinth Shield", new MdModrinthShieldSpan(
				projectSlug: "fabric-api",
				projectId: "P7dR8mSH",
				projectUrl: "https://modrinth.com/mod/fabric-api",
				label: string.Empty,
				style: "flat",
				color: "26292f",
				logo: true
			) })
		}
	));

	new {
		Markdown = md.AsMarkdown(),
		Html = md.AsHtml(),
		Render = md.AsLinqPadHtmlObject()
	}.Dump("MdTable with MdShieldSpan");
}

public static class MdDocumentExtensions {
	public static string AsMarkdown(this MdDocument md) {
		return md.ToString();
	}
	
	public static string AsHtml(this MdDocument md) {
		var markdown = md.AsMarkdown();
		var pipeline = new MarkdownPipelineBuilder()
			.UseAdvancedExtensions()
			.UseEmojiAndSmiley()
			.UseAutoLinks()
			.Build();

		return Markdown.ToHtml(markdown, pipeline);
	}

	public static object AsLinqPadHtmlObject(this MdDocument md) {
		var html = md.AsHtml();
		return Util.RawHtml(html);
	}

	public static MdDocument DumpAsHtml(this MdDocument md) {
		md.AsLinqPadHtmlObject().Dump();
		return md;
	}
}

public abstract class CustomMdSpan {
	public abstract MdSpan AsMdSpan();
	public static implicit operator MdSpan(CustomMdSpan x) => x.AsMdSpan();
}

public abstract class MdShieldSpan : CustomMdSpan {
	protected abstract string GetShieldUrl();
	protected abstract string GetAltText();
	protected virtual string? GetLinkUrl() => null;

	public override MdSpan AsMdSpan() {
		var linkUrl = this.GetLinkUrl();

		var imageSpan = new MdImageSpan(
			description: this.GetAltText(),
			uri: this.GetShieldUrl()
		);
		
		if (linkUrl is null) {
			return imageSpan;
		}

		return new MdLinkSpan(
			text: imageSpan,
			uri: linkUrl
		);
	}
}

public class MdStaticShieldIOSpan : MdShieldSpan {
	public MdStaticShieldIOSpan(string label, string message, string color) {
		this.Label = label;
		this.Message = message;
		this.Color = color;
	}

	public MdStaticShieldIOSpan(string label, string message, string color, string linkUrl) {
		this.Label = label;
		this.Message = message;
		this.Color = color;
		this.LinkUrl = linkUrl;
	}

	public string Label { get; set; }
	public string Message { get; set; }
	public string Color { get; set; }
	public string? LinkUrl { get; set; }
	
	protected override string? GetLinkUrl() => this.LinkUrl;
	protected override string GetAltText() => this.Label;
	protected override string GetShieldUrl() {
		var url = new Url("https://img.shields.io/");
		url.AppendPathSegments("static", "v1");
		url.SetQueryParams(new {
			label = this.Label,
			message = this.Message,
			color = this.Color
		});

		return url.ToString(encodeSpaceAsPlus: true);
	}
}

public class MdCurseForgeShieldSpan : MdShieldSpan {
	public MdCurseForgeShieldSpan(
		string projectSlug,
		int? projectId,
		string? projectUrl = null,
		string? title = null,
		string? style = null,
		string? extra = null,
		string? badgeStyle = null
	) {
		this.ProjectSlug = projectSlug;
		this.ProjectId = projectId;
		this.ProjectUrl = projectUrl;
		this.Title = title;
		this.Style = style;
		this.Extra = extra;
		this.BadgeStyle = badgeStyle;
	}

	public string ProjectSlug { get; set; }
	public int? ProjectId { get; set; }
	public string? ProjectUrl { get; set; }
	public string? Title { get; set; }
	public string? Style { get; set; }
	public string? Extra { get; set; }
	public string? BadgeStyle { get; set; }

	protected override string GetAltText() {
		return this.Title ?? this.ProjectSlug;
	}

	protected override string? GetLinkUrl() {
		return this.ProjectUrl;
	}

	protected override string GetShieldUrl() {
		var url = new Url("https://cf.way2muchnoise.eu/");
		var pathSegment = string.Empty;
		if (this.Style is not null) {
			pathSegment += this.Style + "_";
		}

		pathSegment += this.ProjectId?.ToString() ?? this.ProjectSlug;

		if (this.Extra is not null) {
			pathSegment += "_" + this.Extra;
		}

		pathSegment += ".svg";
		url.AppendPathSegment(pathSegment);
		
		url.SetQueryParams(new {
			badge_style = this.BadgeStyle
		});

		return url.ToString(encodeSpaceAsPlus: true);
	}
}

public class MdModrinthShieldSpan : MdShieldSpan {
	public MdModrinthShieldSpan(
		string projectSlug,
		string? projectId,
		string projectUrl,
		string? label = null,
		string? altText = null,
		string? style = "flat",
		string? color = null,
		bool logo = true,
		string? logoColor = null
	) {
		this.ProjectSlug = projectSlug;
		this.ProjectId = projectId;
		this.ProjectUrl = projectUrl;
		this.Label = label;
		this.Style = style;
		this.Color = color;
		this.Logo = logo ? "modrinth" : null;
		this.LogoColor = logoColor;
	}

	public string ProjectSlug { get; set; }
	public string? ProjectId { get; set; }
	public string ProjectUrl { get; set; }
	public string? Label { get; set; }
	public string? AltText { get; set; }
	public string? Style { get; set; }
	public string? Color { get; set; }
	public string? Logo { get; set; }
	public string? LogoColor { get; set; }

	protected override string GetAltText() {
		return this.AltText ?? this.ProjectSlug;
	}

	protected override string? GetLinkUrl() {
		return this.ProjectUrl;
	}

	protected override string GetShieldUrl() {
		var url = new Url("https://img.shields.io/modrinth/dt/");
		url.AppendPathSegment(this.ProjectId);

		url.SetQueryParams(new {
			label = this.Label,
			color = this.Color,
			logo = this.Logo,
			logoColor = this.LogoColor,
			style = this.Style,
		});

		return url.ToString(encodeSpaceAsPlus: true);
	}
}
