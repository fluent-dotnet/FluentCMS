namespace FluentCMS.Web.UI.Components;

public partial class TabList
{
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;
}

