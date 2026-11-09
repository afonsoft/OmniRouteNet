using System.Text.Json;
using Shouldly;
using Xunit;

namespace LLMRouter.Tests;

/// <summary>SPEC-161: UI parity fixture — every upstream dashboard route mapped, partial or out-of-scope.</summary>
public class Spec161UiParityFixtureTests
{
    // # Covers RF-011
    [Fact]
    public void Fixture_Exists_And_Covers_All_124_Upstream_Routes()
    {
        var fixture = ReadFixture();
        fixture.RootElement.GetProperty("routes").GetArrayLength().ShouldBe(124);
    }

    // # Covers RF-011
    [Fact]
    public void Every_Route_Has_A_Valid_Status()
    {
        var allowed = new[] { "mapped", "missing", "out-of-scope" };
        foreach (var r in ReadFixture().RootElement.GetProperty("routes").EnumerateArray())
        {
            var status = r.GetProperty("status").GetString()!;
            allowed.ShouldContain(status, $"route {r.GetProperty("upstreamRoute").GetString()} has invalid status");
            r.GetProperty("upstreamRoute").GetString().ShouldStartWith("/dashboard");
            if (status == "mapped")
                r.GetProperty("localRoute").GetString().ShouldStartWith("/");
            if (status is "missing" or "out-of-scope")
                r.GetProperty("spec").GetString().ShouldNotBeNullOrWhiteSpace();
        }
    }

    // # Covers RF-011
    [Fact]
    public void Mapped_Local_Routes_Exist_As_Razor_Pages()
    {
        var razorFiles = Directory.GetFiles(
            Path.Combine(RepoRoot(), "src", "LLMRouter.Client"), "*.razor", SearchOption.AllDirectories);
        var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in razorFiles)
            foreach (var m in System.Text.RegularExpressions.Regex.Matches(
                         File.ReadAllText(f), @"^@page\s+""([^""]+)""",
                         System.Text.RegularExpressions.RegexOptions.Multiline).Cast<System.Text.RegularExpressions.Match>())
                declared.Add(m.Groups[1].Value);

        foreach (var r in ReadFixture().RootElement.GetProperty("routes").EnumerateArray())
        {
            if (r.GetProperty("status").GetString() != "mapped") continue;
            var local = r.GetProperty("localRoute").GetString()!;
            var found = declared.Any(d => string.Equals(
                NormalizeRoute(d), NormalizeRoute(local), StringComparison.OrdinalIgnoreCase));
            found.ShouldBeTrue($"fixture maps {r.GetProperty("upstreamRoute").GetString()} → {local}, but no @page declares it");
        }
    }

    private static string NormalizeRoute(string route) =>
        System.Text.RegularExpressions.Regex.Replace(route, @"\{[^}]+\}", "{X}");

    private static JsonDocument ReadFixture()
    {
        var path = Path.Combine(RepoRoot(), "tests", "fixtures", "ui-parity.json");
        File.Exists(path).ShouldBeTrue("tests/fixtures/ui-parity.json is missing");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LLMRouter.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
