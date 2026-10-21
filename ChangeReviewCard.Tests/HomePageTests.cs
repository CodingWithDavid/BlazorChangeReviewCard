using Bunit;
using ChangeReviewCardApp.Pages;

namespace ChangeReviewCard.Tests;

public class HomePageTests : BunitContext
{
    [Fact]
    public void Renders_all_demo_cards()
    {
        var cut = Render<Home>();
        Assert.Equal(5, cut.FindAll("section.review").Count);
    }

    [Fact]
    public void Accept_all_reports_failure_then_succeeds_on_retry()
    {
        var cut = Render<Home>();
        string BulkStatus() => cut.Find(".toolbar [role=status]").TextContent.Trim();

        cut.Find(".toolbar button").Click();
        cut.WaitForAssertion(
            () => Assert.StartsWith("1 change(s) could not be accepted", BulkStatus()),
            TimeSpan.FromSeconds(15));

        cut.Find(".toolbar button").Click();
        cut.WaitForAssertion(
            () => Assert.Equal("All pending changes accepted.", BulkStatus()),
            TimeSpan.FromSeconds(15));

        var statuses = cut.FindAll("section.review [role=status]")
            .Select(s => s.TextContent.Trim()).ToList();
        Assert.Equal(4, statuses.Count(s => s == "Change accepted."));
        Assert.Single(statuses, s => s == "No change needed.");
        Assert.Equal("true", cut.Find(".toolbar button").GetAttribute("aria-disabled"));
    }
}
