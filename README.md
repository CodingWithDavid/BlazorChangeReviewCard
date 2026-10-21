# ChangeReviewCard

A Blazor WebAssembly demo of a compact before-and-after review card. It presents a proposed change for review and lets the user accept it without editing the value.

## Features

- Clearly labeled original and proposed values.
- Individual acceptance with a saving state, visible failure message, and retry.
- Prevents repeated acceptance while saving, after acceptance, when another operation is busy, when the values are identical, or when no callback is provided.
- Keeps the native button in the tab order and retains focus using `aria-disabled` and a guarded click handler.
- Announces status changes through a persistent polite live region.
- Wraps long values and stacks the comparison on narrow screens.
- Includes an **Accept all** demo and a **Reset demo** button.

The Home page contains examples for filenames, imported data, HTML-looking text, identical values, and long filenames. Its save operation is simulated: it waits 1.2 seconds, and the imported-data example fails once before allowing a retry. There is no persistence API in this demo.

## Requirements

- .NET 10 SDK

## Run the app

From the repository directory:

```bash
dotnet run --project ChangeReviewCard.csproj
```

Open the local URL printed by the .NET CLI.

## Run the tests

The solution includes component and Home page tests using xUnit and bUnit:

```bash
dotnet test ChangeReviewCard.slnx
```

## Use the component

The reusable component is in `Components/ChangeReviewCard.razor`.

```razor
<ChangeReviewCard Label="Shorter filename"
                  Before="Warehouse_Expansion_Final_Approved_v3.pdf"
                  After="Warehouse_Expansion.pdf"
                  Accepted="@_accepted"
                  OnAccept="AcceptFilename" />

@code {
    private bool _accepted;

    private async Task AcceptFilename()
    {
        await SaveFilenameAsync();
        _accepted = true;
    }

    private Task SaveFilenameAsync()
    {
        // Replace with the application's persistence operation.
        return Task.CompletedTask;
    }
}
```

The parent owns persistence and the `Accepted` state. Set `Accepted` only after the save succeeds; if `OnAccept` throws, the card displays an error and allows retry. Use `Busy` to prevent acceptance during another operation, such as an Accept all operation. For lists of cards, provide stable `@key` values.

| Parameter | Type | Purpose |
| --- | --- | --- |
| `Label` | `string` | Heading for the proposed change. Defaults to `Suggested change`. |
| `Before` | `string` | Original value. |
| `After` | `string` | Proposed value. |
| `Accepted` | `bool` | Parent-owned flag indicating the save succeeded. |
| `Busy` | `bool` | Blocks acceptance while another operation is running. |
| `OnAccept` | `EventCallback` | Parent operation invoked when the user accepts. |

The component is client-side UI behavior, not authorization. Validate and authorize changes in the application's server-side persistence layer.
