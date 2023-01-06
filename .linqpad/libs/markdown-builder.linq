<Query Kind="Program">
  <NuGetReference>Markdig</NuGetReference>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>Markdig</Namespace>
</Query>

void Main() {
	
}

public class MarkdownBuilder {
	private readonly StringBuilder sb = new();

	public void Append(string? markdown) {
		this.sb.Append(markdown);
	}

	public void AppendLine(string? markdown = null) {
		this.sb.AppendLine(markdown);
	}

	public void AppendLink(
		string url,
		string? text = null
	) {
		// If the text starts with a space, put it before the link, not within it
		if (text?.StartsWith(' ') ?? false) {
			text = text.TrimStart(' ');
			this.Append(" ");
		}
		
		this.AppendLink(url, textBuilder => {
			textBuilder.Append(text ?? url);
		});
	}

	public void AppendLink(
		string url,
		Action<MarkdownBuilder> textBuilder
	) {
		this.Append($"[");
		textBuilder(this);
		this.Append($"]");
		this.Append($"(");
		this.Append(url);
		this.Append($")");
	}

	public void AppendImage(
		string imageUrl,
		string? hoverText = null
	) {
		this.Append("!");
		this.AppendLink(
			url: imageUrl,
			text: hoverText?.TrimStart(' ')
		);
	}

	public void AppendImageLink(
		string imageUrl,
		string linkUrl,
		string? hoverText = null
	) {
		this.AppendLink(
			url: linkUrl,
			textBuilder: builder => {
				builder.AppendImage(
					imageUrl: imageUrl,
					hoverText: hoverText?.TrimStart(' ')
				);
			}
		);
	}

	public string Build() {
		return sb.ToString();
	}

	public void DumpAsHtml() {
		var markdown = this.Build();
		var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
		var html = Markdown.ToHtml(markdown, pipeline);
		Util.RawHtml(html).Dump();
	}
}

public static class CurseForgeShields {
	private static readonly string baseUrl = @"https://cf.way2muchnoise.eu/";

	public static void AppendCurseForgeProjectShield(
		this MarkdownBuilder md,
		string projectSlug,
		int? projectId,
		string? title = null,
		string? style = null,
		string? extra = null,
		string? badgeStyle = null,
		string? projectUrl = null
	) {
		var imageUrl = baseUrl;

		if (style is not null) {
			imageUrl += style + "_";
		}

		imageUrl += projectId?.ToString() ?? projectSlug;

		if (extra is not null) {
			imageUrl += "_" + extra;
		}

		imageUrl += ".svg";

		if (badgeStyle is not null) {
			imageUrl += "?badge_style=" + badgeStyle;
		}

		if (projectUrl is null) {
			md.AppendImage(
				imageUrl: imageUrl,
				hoverText: title ?? projectSlug
			);
		} else {
			md.AppendImageLink(
				imageUrl: imageUrl,
				linkUrl: projectUrl,
				hoverText: title ?? projectSlug
			);
		}
	}

	public static void AppendCurseForgeVersionsShield(
		this MarkdownBuilder md,
		string projectSlug,
		int? projectId,
		string? title = null,
		string? style = null,
		string? text = null,
		string? badgeStyle = null
	) {
		var imageUrl = "https://cf.way2muchnoise.eu/versions/";

		if (text is not null) {
			imageUrl += text + "_";
		}

		imageUrl += projectId?.ToString() ?? projectSlug;

		if (style is not null) {
			imageUrl += "_" + style;
		}

		imageUrl += ".svg";

		if (badgeStyle is not null) {
			imageUrl += "?badge_style=" + badgeStyle;
		}

		md.AppendImage(title ?? projectSlug, imageUrl);
	}
}

public static class ModrinthShields {
	private static readonly string baseUrl = @"https://img.shields.io/modrinth/dt/";
	// ![Modrinth Downloads](https://img.shields.io/modrinth/dt/text-utilities?label=downloads&logo=modrinth)

	public static void AppendModrinthModShield(
		this MarkdownBuilder md,
		string modSlug,
		string? modId,
		string modUrl,
		string? label,
		string? hoverText = null,
		bool logo = true,
		string? style = "flat"
	) {
		var imageUrl = baseUrl;
		imageUrl += modId;
		imageUrl += "?label=" + (label ?? modSlug);

		if (logo) {
			imageUrl += "&logo=modrinth";
		}

		if (style is not null) {
			imageUrl += "&style=" + style;
		}

		md.AppendImageLink(
			imageUrl: imageUrl,
			linkUrl: modUrl,
			hoverText: hoverText ?? label ?? modSlug
		);
	}
}
