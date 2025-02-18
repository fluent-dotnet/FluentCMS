namespace FluentCMS.Web.UI.Components;

public partial class Pagination
{
    [Parameter]
    public long Current { get; set; }

    [Parameter]
    public EventCallback<long> CurrentChanged { get; set; }

    [Parameter]
    public bool ShowIcons { get; set; } = true;

    [Parameter]
    public long Total { get; set; }

    public IEnumerable<int> Pages()
    {
        var BOUNDARY = 1;
        var SIBLING = 1;

        var min = Math.Min(BOUNDARY, this.Total);
        var max = Math.Max(this.Total - BOUNDARY + 1, BOUNDARY + 1);

        var startPages = Enumerable.Range(1, (int)min);
        var endPages = Enumerable.Range((int)max, (int)(this.Total - max + 1));

        var siblingsStart = Math.Max(
            Math.Min(
                this.Current - SIBLING,
                this.Total - BOUNDARY - SIBLING * 2 - 1
            ),
            BOUNDARY + 2
        );

        var siblingsEnd = Math.Min(
          Math.Max(
            this.Current + SIBLING,
            BOUNDARY + SIBLING * 2 + 2
          ),
          endPages.Count() > 0 ? endPages.First() - 2 : this.Total - 1
        );

        var items = Enumerable.Range(0, 0);

        items = items.Concat(startPages);

        if (siblingsStart > BOUNDARY + 2)
        {
            items = items.Concat(new[] { 0 });
        }
        else if (BOUNDARY + 1 < this.Total - BOUNDARY)
        {
            items = items.Concat(new[] { BOUNDARY + 1 });
        }

        items = items.Concat(Enumerable.Range((int)siblingsStart, (int)(siblingsEnd - siblingsStart + 1)));

        if (siblingsEnd < this.Total - BOUNDARY - 1)
        {
            items = items.Concat(new[] { 0 });
        }
        else if (this.Total - BOUNDARY > BOUNDARY)
        {
            items = items.Concat(new[] { (int)this.Total - BOUNDARY });
        }

        items = items.Concat(endPages);

        return items;
    }

    public bool CanPrevious()
    {
        return Current > 1;
    }

    public bool CanNext()
    {
        return Current < Total;
    }

    public void GoTo(long To)
    {
        if (To == Current) return;

        Current = To;

        this.CurrentChanged.InvokeAsync(Current);
    }

    public void Next()
    {
        if (!this.CanNext()) return;

        Current++;

        this.CurrentChanged.InvokeAsync(Current);
    }

    public void Previous()
    {
        if (!this.CanPrevious()) return;

        Current--;

        this.CurrentChanged.InvokeAsync(Current);
    }
}