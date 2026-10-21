using Bunit;
using Microsoft.AspNetCore.Components;
using Card = ChangeReviewCardApp.Components.ChangeReviewCard;

namespace ChangeReviewCard.Tests;

public class ChangeReviewCardTests : BunitContext
{
    private IRenderedComponent<Card> RenderCard(
        Func<Task>? onAccept = null,
        string before = "Old.pdf",
        string after = "New.pdf",
        bool accepted = false,
        bool busy = false,
        string? label = null) =>
        Render<Card>(p =>
        {
            p.Add(c => c.Before, before)
             .Add(c => c.After, after)
             .Add(c => c.Accepted, accepted)
             .Add(c => c.Busy, busy);
            if (label is not null) p.Add(c => c.Label, label);
            if (onAccept is not null)
                p.Add(c => c.OnAccept, EventCallback.Factory.Create(this, onAccept));
        });

    private static string Status(IRenderedComponent<Card> cut) =>
        cut.Find("[role=status]").TextContent.Trim();

    private static string AriaDisabled(IRenderedComponent<Card> cut) =>
        cut.Find("button").GetAttribute("aria-disabled")!;

    [Fact]
    public void Renders_labeled_original_and_proposed_values()
    {
        var cut = RenderCard(() => Task.CompletedTask, label: "Shorter filename");

        Assert.Equal("Shorter filename", cut.Find("h3").TextContent);
        var terms = cut.FindAll("dt").Select(e => e.TextContent).ToArray();
        var values = cut.FindAll("dd").Select(e => e.TextContent).ToArray();
        Assert.Equal(["Original", "Proposed"], terms);
        Assert.Equal(["Old.pdf", "New.pdf"], values);
    }

    [Fact]
    public void Uses_default_label_when_none_supplied()
    {
        var cut = RenderCard(() => Task.CompletedTask);
        Assert.Equal("Suggested change", cut.Find("h3").TextContent);
    }

    [Fact]
    public void Section_is_labelled_by_its_heading()
    {
        var cut = RenderCard(() => Task.CompletedTask);
        var headingId = cut.Find("h3").Id;

        Assert.False(string.IsNullOrEmpty(headingId));
        Assert.Equal(headingId, cut.Find("section").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Multiple_cards_have_unique_heading_ids()
    {
        var a = RenderCard(() => Task.CompletedTask);
        var b = RenderCard(() => Task.CompletedTask);
        Assert.NotEqual(a.Find("h3").Id, b.Find("h3").Id);
    }

    [Fact]
    public void Html_looking_text_is_encoded_not_rendered_as_markup()
    {
        var cut = RenderCard(() => Task.CompletedTask,
            before: "<b>Bold</b>", after: "<script>alert('x')</script>");

        Assert.Empty(cut.FindAll("dd b"));
        Assert.Empty(cut.FindAll("script"));
        Assert.Equal("<b>Bold</b>", cut.FindAll("dd")[0].TextContent);
        Assert.Equal("<script>alert('x')</script>", cut.FindAll("dd")[1].TextContent);
    }

    [Fact]
    public void Initial_state_is_enabled_with_review_prompt()
    {
        var cut = RenderCard(() => Task.CompletedTask);

        Assert.Equal("false", AriaDisabled(cut));
        Assert.Equal("Accept change", cut.Find("button").TextContent.Trim());
        Assert.Equal("Review before accepting.", Status(cut));
    }

    [Fact]
    public void Button_is_native_and_never_uses_disabled_attribute()
    {
        var cut = RenderCard(() => Task.CompletedTask, accepted: true);
        var button = cut.Find("button");

        Assert.Equal("button", button.GetAttribute("type"));
        Assert.False(button.HasAttribute("disabled"));
        Assert.Equal("true", AriaDisabled(cut));
    }

    [Fact]
    public void Status_region_is_always_present()
    {
        var cut = RenderCard(() => Task.CompletedTask);
        Assert.Single(cut.FindAll("[role=status]"));
    }

    [Fact]
    public void Accept_invokes_callback_once()
    {
        var calls = 0;
        var cut = RenderCard(() => { calls++; return Task.CompletedTask; });

        cut.Find("button").Click();

        Assert.Equal(1, calls);
    }

    [Fact]
    public void Repeated_clicks_while_saving_are_ignored_and_saving_state_shown()
    {
        var calls = 0;
        var tcs = new TaskCompletionSource();
        var cut = RenderCard(() => { calls++; return tcs.Task; });

        cut.Find("button").Click();
        cut.Find("button").Click();
        cut.Find("button").Click();

        Assert.Equal(1, calls);
        Assert.Equal("Accepting…", cut.Find("button").TextContent.Trim());
        Assert.Equal("Saving change…", Status(cut));
        Assert.Equal("true", AriaDisabled(cut));

        tcs.SetResult();
        cut.WaitForAssertion(() => Assert.Equal("false", AriaDisabled(cut)));
    }

    [Fact]
    public void Accepted_card_shows_success_and_cannot_be_accepted_again()
    {
        var calls = 0;
        var cut = RenderCard(() => { calls++; return Task.CompletedTask; }, accepted: true);

        cut.Find("button").Click();

        Assert.Equal(0, calls);
        Assert.Equal("Accepted ✓", cut.Find("button").TextContent.Trim());
        Assert.Equal("Change accepted.", Status(cut));
        Assert.Equal("true", AriaDisabled(cut));
    }

    [Fact]
    public void Parent_marking_accepted_after_save_blocks_further_accepts()
    {
        var calls = 0;
        var accepted = false;
        var cut = RenderCard(() => { calls++; accepted = true; return Task.CompletedTask; });

        cut.Find("button").Click();
        cut.Render(p => p.Add(c => c.Accepted, accepted));
        cut.Find("button").Click();

        Assert.Equal(1, calls);
        Assert.Equal("Change accepted.", Status(cut));
    }

    [Fact]
    public void Failed_save_shows_error_retains_proposal_and_allows_retry()
    {
        var calls = 0;
        var cut = RenderCard(() =>
        {
            calls++;
            if (calls == 1) throw new InvalidOperationException("boom");
            return Task.CompletedTask;
        });

        cut.Find("button").Click();

        Assert.Equal("Could not accept the change. Try again.", Status(cut));
        Assert.Contains("error", cut.Find("[role=status]").ClassList);
        Assert.Equal("New.pdf", cut.FindAll("dd")[1].TextContent);
        Assert.Equal("false", AriaDisabled(cut));

        cut.Find("button").Click();

        Assert.Equal(2, calls);
        Assert.Equal("Review before accepting.", Status(cut));
        Assert.DoesNotContain("error", cut.Find("[role=status]").ClassList);
    }

    [Fact]
    public void Async_failure_is_also_caught()
    {
        var cut = RenderCard(async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
        });

        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
            Assert.Equal("Could not accept the change. Try again.", Status(cut)));
    }

    [Fact]
    public void Identical_values_block_acceptance()
    {
        var calls = 0;
        var cut = RenderCard(() => { calls++; return Task.CompletedTask; },
            before: "Same.xlsx", after: "Same.xlsx");

        cut.Find("button").Click();

        Assert.Equal(0, calls);
        Assert.Equal("true", AriaDisabled(cut));
        Assert.Equal("No change needed.", Status(cut));
    }

    [Fact]
    public void Comparison_is_case_sensitive()
    {
        var cut = RenderCard(() => Task.CompletedTask, before: "file.PDF", after: "file.pdf");
        Assert.Equal("false", AriaDisabled(cut));
    }

    [Fact]
    public void Busy_blocks_acceptance()
    {
        var calls = 0;
        var cut = RenderCard(() => { calls++; return Task.CompletedTask; }, busy: true);

        cut.Find("button").Click();

        Assert.Equal(0, calls);
        Assert.Equal("true", AriaDisabled(cut));
        Assert.Equal("Another operation is running.", Status(cut));
    }

    [Fact]
    public void Clearing_busy_reenables_acceptance()
    {
        var cut = RenderCard(() => Task.CompletedTask, busy: true);
        cut.Render(p => p.Add(c => c.Busy, false));
        Assert.Equal("false", AriaDisabled(cut));
    }

    [Fact]
    public void Missing_callback_blocks_acceptance()
    {
        var cut = RenderCard(onAccept: null);

        cut.Find("button").Click();

        Assert.Equal("true", AriaDisabled(cut));
    }

    [Fact]
    public void Multiple_cards_maintain_independent_state()
    {
        var tcs = new TaskCompletionSource();
        var a = RenderCard(() => tcs.Task);
        var b = RenderCard(() => throw new InvalidOperationException());

        a.Find("button").Click();
        b.Find("button").Click();

        Assert.Equal("Saving change…", Status(a));
        Assert.Equal("Could not accept the change. Try again.", Status(b));

        tcs.SetResult();
        a.WaitForAssertion(() => Assert.Equal("Review before accepting.", Status(a)));
        Assert.Equal("Could not accept the change. Try again.", Status(b));
    }
}
