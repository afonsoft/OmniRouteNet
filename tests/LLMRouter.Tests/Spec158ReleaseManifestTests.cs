using System.Text.Json;
using Shouldly;
using Xunit;

namespace LLMRouter.Tests;

/// <summary>SPEC-158: release manifest contract — pin, tag, cherry-picks, counts.</summary>
public class Spec158ReleaseManifestTests
{
    // # Covers RF-008
    [Fact]
    public void Manifest_Exists_And_Is_Valid_Json_With_Required_Fields()
    {
        var manifest = ReadManifest();
        manifest.RootElement.GetProperty("repo").GetString().ShouldBe("cheaper-inference/OmniRoute");
        manifest.RootElement.GetProperty("tag").GetString().ShouldNotBeNull();
        manifest.RootElement.GetProperty("commit").GetString().ShouldNotBeNull();
        manifest.RootElement.GetProperty("commitDate").GetString().ShouldNotBeNull();
        manifest.RootElement.GetProperty("counts").GetProperty("gapDoc").GetString().ShouldNotBeNull();
    }

    // # Covers RF-008
    [Fact]
    public void Commit_Is_40_Hex_And_Matches_Current_Pin()
    {
        var m = ReadManifest().RootElement;
        var commit = m.GetProperty("commit").GetString()!;
        commit.Length.ShouldBe(40);
        commit.ShouldMatch("^[0-9a-f]{40}$");
        m.GetProperty("tag").GetString()!.ShouldStartWith("v");
    }

    // # Covers RF-008
    [Fact]
    public void History_Note_Warns_About_Rewritten_History()
    {
        var note = ReadManifest().RootElement.GetProperty("historyNote").GetString()!;
        note.ShouldContain("NOT an ancestor");
        note.ShouldContain("tree-based");
    }

    // # Covers RF-008
    [Fact]
    public void Cherry_Picks_Are_Tracked_With_Spec()
    {
        var picks = ReadManifest().RootElement.GetProperty("cherryPicksFromLaterBranches");
        picks.GetArrayLength().ShouldBeGreaterThanOrEqualTo(2);
        foreach (var p in picks.EnumerateArray())
        {
            p.GetProperty("branch").GetString().ShouldNotBeNullOrWhiteSpace();
            p.GetProperty("upstreamPR").GetString().ShouldStartWith("#");
            p.GetProperty("feature").GetString().ShouldNotBeNullOrWhiteSpace();
            p.GetProperty("spec").GetString().ShouldStartWith("SPEC-");
        }
    }

    // # Covers RF-008
    [Fact]
    public void Gap_Doc_Referenced_By_Manifest_Exists()
    {
        var gapDoc = ReadManifest().RootElement.GetProperty("counts").GetProperty("gapDoc").GetString()!;
        File.Exists(Path.Combine(RepoRoot(), gapDoc)).ShouldBeTrue($"gap doc missing: {gapDoc}");
    }

    private static JsonDocument ReadManifest()
    {
        var path = Path.Combine(RepoRoot(), "docs", "release-manifest.json");
        File.Exists(path).ShouldBeTrue("docs/release-manifest.json is missing");
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
