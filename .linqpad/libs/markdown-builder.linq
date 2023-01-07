<Query Kind="Program">
  <NuGetReference>Markdig</NuGetReference>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>Markdig</Namespace>
  <Namespace>Xunit</Namespace>
  <Namespace>System.Runtime.CompilerServices</Namespace>
</Query>

#load "xunit"

void Main() {
	RunTests();  // Call RunTests() or press Alt+Shift+T to initiate testing.
}

public class MarkdownBuilder {
	private readonly StringBuilder sb = new();

	public MarkdownBuilder Append(char markdown) {
		this.sb.Append(markdown);
		return this;
	}

	public MarkdownBuilder Append(string? markdown) {
		this.sb.Append(markdown);
		return this;
	}

	public MarkdownBuilder AppendLine(string? markdown = null) {
		this.sb.AppendLine(markdown);
		return this;
	}

	public MarkdownBuilder AppendLink(
		string url,
		string? title = null,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		// If the text starts with a space, put it before the link, not within it
		if (title?.StartsWith(' ') ?? false) {
			title = title.TrimStart(' ');
			this.Append(' ');
		}
		
		this.AppendLink(
			url: url,
			titleBuilder: x => x.Append(title ?? url),
			skipPrefixSpace: skipPrefixSpace,
			skipSuffixSpace: skipSuffixSpace
		);

		return this;
	}

	public MarkdownBuilder AppendLink(
		string url,
		Action<MarkdownBuilder> titleBuilder,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		if (!skipPrefixSpace) {
			this.Append(' ');
		}
		
		this.Append('[');
		titleBuilder(this);
		this.Append(']');
		this.Append('(');
		this.Append(url);
		this.Append(')');
		
		if (!skipSuffixSpace) {
			this.Append(' ');
		}

		return this;
	}

	public MarkdownBuilder AppendImage(
		string imageUrl,
		string? altText = null,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		if (!skipPrefixSpace) {
			this.Append(' ');
		}

		this.Append("!");
		this.AppendLink(
			url: imageUrl,
			title: altText?.TrimStart(' '),
			skipPrefixSpace: true,
			skipSuffixSpace: skipSuffixSpace
		);

		return this;
	}

	public MarkdownBuilder AppendImageLink(
		string imageUrl,
		string linkUrl,
		string? altText = null,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		this.AppendLink(
			url: linkUrl,
			titleBuilder: builder => {
				builder.AppendImage(
					imageUrl: imageUrl,
					altText: altText?.TrimStart(' ')
				);
			},
			skipPrefixSpace: skipPrefixSpace,
			skipSuffixSpace: skipSuffixSpace
		);

		return this;
	}
	
	public MarkdownBuilder AppendShield(
		string shieldUrl,
		string? linkUrl = null,
		string? altText = null,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		if (linkUrl is null) {
			this.AppendImage(
				imageUrl: shieldUrl,
				altText: altText,
				skipPrefixSpace: skipPrefixSpace,
				skipSuffixSpace: skipSuffixSpace
			);
		} else {
			this.AppendImageLink(
				imageUrl: shieldUrl,
				linkUrl: linkUrl,
				altText: linkUrl,
				skipPrefixSpace: skipPrefixSpace,
				skipSuffixSpace: skipSuffixSpace
			);
		}

		return this;
	}

	public MarkdownBuilder AppendShield(
		string label,
		string message,
		string color,
		string? linkUrl = null,
		string? altText = null,
		bool skipPrefixSpace = false,
		bool skipSuffixSpace = false
	) {
		var shieldUrl = $"https://img.shields.io/static/v1?label={label}&message={message}&color={color}";
		if (linkUrl is null) {
			this.AppendImage(
				imageUrl: shieldUrl,
				altText: altText,
				skipPrefixSpace: skipPrefixSpace,
				skipSuffixSpace: skipSuffixSpace
			);
		} else {
			this.AppendImageLink(
				imageUrl: shieldUrl,
				linkUrl: linkUrl,
				altText: linkUrl,
				skipPrefixSpace: skipPrefixSpace,
				skipSuffixSpace: skipSuffixSpace
			);
		}

		return this;
	}

	public string AsMarkdown() {
		return sb.ToString();
	}

	public string AsHtml() {
		var markdown = this.AsMarkdown();
		var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
		return Markdown.ToHtml(markdown, pipeline);
	}

	public MarkdownBuilder DumpAsHtml() {
		var html = this.AsHtml();
		Util.RawHtml(html).Dump();
		return this;
	}
}

public static class CurseForgeShields {
	private static readonly string baseUrl = @"https://cf.way2muchnoise.eu/";

	public static MarkdownBuilder AppendCurseForgeProjectShield(
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
				altText: title ?? projectSlug
			);
		} else {
			md.AppendImageLink(
				imageUrl: imageUrl,
				linkUrl: projectUrl,
				altText: title ?? projectSlug
			);
		}

		return md;
	}

	public static MarkdownBuilder AppendCurseForgeVersionsShield(
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

		return md;
	}
}

public static class ModrinthShields {
	private static readonly string baseUrl = @"https://img.shields.io/modrinth/dt/";

	public static MarkdownBuilder AppendModrinthModShield(
		this MarkdownBuilder md,
		string modSlug,
		string? modId,
		string modUrl,
		string? label = null,
		string? hoverText = null,
		bool logo = true,
		string? style = "flat",
		string? color = null,
		string? logoColor = null
	) {
		var imageUrl = baseUrl;
		imageUrl += modId;
		imageUrl += "?label=" + (label ?? modSlug);

		if (color is not null) {
			imageUrl += "&color=" + color;
		}
		
		if (logo) {
			imageUrl += "&logo=modrinth";
		}

		if (logoColor is not null) {
			imageUrl += "&logoColor=" + logoColor;
		}

		if (style is not null) {
			imageUrl += "&style=" + style;
		}

		md.AppendImageLink(
			imageUrl: imageUrl,
			linkUrl: modUrl,
			altText: hoverText ?? label ?? modSlug
		);
		
		return md;
	}
}

#region private::Tests

[Fact]
void Link() => Test(md => md.AppendLink(
	url: "https://google.com",
	title: "Google"
));

[Fact]
void Image() => Test(md => md.AppendImage(
	imageUrl: "https://www.google.com/images/branding/googlelogo/1x/googlelogo_light_color_272x92dp.png",
	altText: "Google"
));

void Test(Action<MarkdownBuilder> md, [CallerMemberName] string callerName = "") {
	var builder = new MarkdownBuilder();
	md(builder);

	new {
		markdown = builder.AsMarkdown(),
		html = builder.AsHtml(),
		render = Util.RawHtml(builder.AsHtml())
	}.Dump(callerName);
}

#endregion