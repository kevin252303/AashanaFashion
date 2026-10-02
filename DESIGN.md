# DESIGN.md — Aashana Fashion UI Design System (IGNEK Pulse)

**This file is the single source of truth for how the app looks.** Every AI assistant (Claude, Gemini, Codex, Copilot…) and every developer must follow it when creating or changing any screen, partial, modal or front-end script.

- Figma source: `Figma/IGNEK Pulse.fig` (exported pages in `Figma/*.png`)
- Implementation: `AashanaFashion/wwwroot/css/site.css` — all tokens and component classes live there
- App shell: `AashanaFashion/Views/Shared/_Layout.cshtml`
- If this file and the CSS ever disagree, **the CSS wins** — then update this file.

---

## 0. Non-negotiable rules

1. **Reuse, don't invent.** Build every screen from the classes in this document. Before you write new CSS, check `site.css`. If something is truly missing, add a small, token-based class to `site.css` under the matching section, and document it here.
2. **No inline styles** for colour, font, spacing, border or shadow. Inline `style` is allowed only for: column widths (`width`/`min-width` on `<th>`), dynamic values (`style="width: @pct%"` on progress bars, colour swatches), and `display:none` toggled by JS.
3. **No hard-coded hex colours** in views or JS. Use tokens (`var(--indigo-600)`) or classes (`.text-success`, `.af-badge.b-green`).
4. **One primary button per view** (`.btn-primary`, indigo). Everything else is secondary, ghost or danger.
5. **Colour = status only.** Green, amber, red, blue and purple communicate state (paid, overdue, error…). They are never decoration. Card headers are always neutral.
6. **No emoji** anywhere in UI text, buttons, options, alerts or JS messages. Use Bootstrap Icons (`<i class="bi bi-…"></i>`).
7. **Sentence case** for titles, labels, buttons and table headers (“New sales order”, not “New Sales Order”). Proper nouns and codes keep their case (GSTIN, IRN, HSN, PO).
8. **Print views are out of scope.** They keep their own standalone styling (Bootstrap 5.1) and must not use this system: `Invoice/Print`, `SalesOrder/PrintChallan`, `ReadyProduct/PrintTag`, `Purchase/Print`, `JobSlip/Print`, `JobSlip/PrintAll`, `Barcode/PrintLotTag`, `Barcode/PrintTags`, `Shared/PrintEwayBill`.
9. **Accessibility:** icon-only buttons need `title` + `aria-label`. Every input needs a `<label for>` or `aria-label`. Modals need `aria-labelledby`.
10. **Don't use the legacy classes** listed in §14 in new code.

---

## 1. Stack & assets

| Asset | Path | Notes |
|---|---|---|
| Bootstrap **5.3.3** | `~/lib/bootstrap-5.3.3/` | App pages. (`~/lib/bootstrap/` = 5.1, print views only) |
| Bootstrap Icons | `~/lib/bootstrap-icons/` | The only icon set. Use `bi bi-*` |
| Inter (self-hosted) | `~/lib/inter/inter.css` | Variable 100–900, latin + latin-ext (covers ₹) |
| Design system CSS | `~/css/site.css` | Load after Bootstrap |
| Shell JS | `~/js/site.js` | Sidebar, alert close, toasts, `window.afToast()` |

Do not add CDNs, other icon packs, other fonts or other CSS frameworks.

---

## 2. Design tokens

### 2.1 Colour

**Brand — Indigo.** `--indigo-600 #4F46E5` is the single interactive primary. Its hover is `--indigo-700 #4338CA`. Use `--indigo-50 #EEF2FF` for selected or active tints.

| Token | Hex | Use |
|---|---|---|
| `--background` | `#F1F3F5` | Page canvas behind cards |
| `--surface` | `#FFFFFF` | Cards, inputs, modals, sidebar, topbar |
| `--raised` | `#F9FAFB` | Table header row, insets, read-only fields, hover rows |
| `--border` | `#E5E7EB` | Default 1px borders and dividers |
| `--border-strong` | `#D1D5DB` | Input borders, hover borders |
| `--foreground` | `#111827` | Titles, key values, primary text in tables |
| `--secondary` | `#374151` | Body text, labels |
| `--muted` | `#6B7280` | Descriptions, captions, metadata |
| `--placeholder` | `#9CA3AF` | Placeholders, disabled, table header text |

Neutrals use the Tailwind gray scale, `--gray-50` … `--gray-900`.

**Semantic colours (status only).** Each has 50 / 100 / 200 / 500 / 600 / 700.

| Family | 600 | Meaning |
|---|---|---|
| `--success-*` | `#059669` | Paid, received, completed, online, in stock |
| `--warning-*` | `#D97706` | Pending, partial, standby, low stock, due soon |
| `--error-*` | `#DC2626` | Unpaid, overdue, failed, rejected, destructive |
| `--info-*` | `#2563EB` | Informational, confirmed, in progress |
| `--purple-*`, `--orange-*`, `--cyan-*` | — | Extra stage colours (badges only) |

### 2.2 Typography — Inter

| Token | Size | Weight | Use |
|---|---|---|---|
| `--text-display` | 32px | 700 | Rare hero numbers |
| `--text-title` | 20px | 600 | Page title (`.page-title`) |
| `--text-section` | 16px | 600 | Section titles |
| `--text-card` / `--text-body` | 14px | 600 / 400 | Card title / body text |
| `--text-sm` | 13px | 400–500 | Table cells, labels, buttons |
| `--text-xs` | 12px | 400 | Captions, helper text, sub-lines in cells |
| `--text-2xs` | 11px | 600, UPPERCASE, `.06em` | Table headers, micro-labels, info labels |

- Metric values are 24px / 700 with tabular numbers. Never use `h1`–`h6` sizes from Bootstrap directly for page structure; use the classes.
- Numbers in tables and totals: `.tabular` (already applied by `.cell-num`).
- Codes and IDs (lot no., GSTIN, IFSC, IRN, UTR): `.font-monospace`, or the `.mono-id` chip.

### 2.3 Spacing (base 4)

`--space-1 4` · `--space-2 8` · `--space-3 12` · `--space-4 16` · `--space-5 20` · `--space-6 24` · `--space-8 32` · `--space-10 40` · `--space-12 48` · `--space-16 64` (px)

- Card padding is 20px. Gaps between cards and sections are 16–24px; use Bootstrap `g-3` / `g-4` and `mb-4`.
- Form grid: `row g-3`. Page header to content: built into `.page-header`.

### 2.4 Radius, elevation, sizing

| Token | Value | Use |
|---|---|---|
| `--radius-sm` | 4px | Badges, chips |
| `--radius-control` | 5px | Inputs, selects, buttons |
| `--radius-md` | 6px | Insets, small panels |
| `--radius-lg` | 8px | Cards, tables, dropdowns |
| `--radius-xl` | 12px | Modals |
| `--shadow-raised` | subtle 1px | Cards |
| `--shadow-elevated` | — | Dropdowns, toasts, floating form bar |
| `--shadow-modal` | — | Modals |
| `--focus-ring` | 3px indigo 15% | All focusable controls |
| `--control-h` / `-sm` / `-lg` | 32 / 28 / 36px | Inputs and buttons |
| `--sidebar-w` / `--topbar-h` | 248 / 56px | Shell |

---

## 3. App shell (already built — don't rebuild it)

`_Layout.cshtml` provides:
- the light sidebar (`.sidebar`, `.sb-section`, `.sb-link`, `.sb-group`, `.sb-sub`, `.sb-sublink`);
- the topbar (breadcrumb, company switch, user menu);
- the toast stack and flash alerts;
- `<main class="page-body">`.

Views only render their content. Set the title with `ViewData["Title"] = "…";`.

**Adding a nav item:** copy an existing `sb-link` / `sb-sublink` line in `_Layout.cshtml`. Keep the `@Active(ctrl == "X")` call and the permission check around it, and use a `bi-*` icon for top-level links.

**Flash messages:** set them in the controller and **do not render them in the view**. The layout shows them.
```csharp
TempData["Success"] = "Customer saved.";   // toast, auto-hides
TempData["Info"]    = "…";                 // toast
TempData["Warning"] = "…";                 // inline alert at top of page
TempData["Error"]   = "…";                 // inline alert at top of page
```
For client-side confirmations: `window.afToast('Copied to clipboard.', 'success')`. Types: `success | info | warning | error`. Never use `alert()` for success messages.

---

## 4. Page templates (copy these)

### 4.1 Page header — every page starts with this
```html
<div class="page-header">
    <div>
        <a asp-action="Index" class="page-back"><i class="bi bi-arrow-left"></i> Customers</a>  <!-- only on child pages -->
        <h1 class="page-title">New customer</h1>
        <p class="page-sub">One sentence explaining what this page is for.</p>
    </div>
    <div class="page-actions">
        <a asp-action="Export" class="btn btn-secondary"><i class="bi bi-download"></i> Export</a>
        <a asp-action="Create" class="btn btn-primary"><i class="bi bi-plus-lg"></i> New customer</a>  <!-- primary LAST -->
    </div>
</div>
```
- Title plus a status badge (detail pages): wrap them in `<div class="d-flex align-items-center gap-2 flex-wrap"> <h1 class="page-title">SO-0001</h1> <span class="af-badge b-blue">Confirmed</span> </div>`.
- Overflow actions go in a `.dropdown` with a `btn btn-secondary btn-icon` trigger (`bi-three-dots`).

### 4.2 List / index page
```html
<!-- page-header … -->

<div class="metric-grid">
    <div class="metric-card">
        <div class="metric-label">Total orders</div>
        <div class="metric-value">@total</div>
        <div class="metric-meta success">all customer bookings</div>   <!-- meta colour: success | warning | danger | info | (none) -->
    </div>
    <!-- 3–5 cards -->
</div>

<div class="table-card">
    <form method="get" class="table-toolbar">
        <div class="toolbar-filters">
            <div class="search-field">
                <i class="bi bi-search"></i>
                <input type="text" name="search" class="af-input" placeholder="Search name, code…" value="@ViewBag.Search" />
            </div>
            <select name="status" class="af-input af-select" onchange="this.form.submit()" aria-label="Status">
                <option value="">All statuses</option>
            </select>
        </div>
        <div class="toolbar-actions">
            @if (filtered) { <a asp-action="Index" class="btn btn-ghost">Clear</a> }
            <button type="submit" class="btn btn-secondary"><i class="bi bi-funnel"></i> Filter</button>
        </div>
    </form>

    @if (!Model.Any())
    {
        <div class="empty-state">
            <div class="es-icon"><i class="bi @(filtered ? "bi-search" : "bi-people")"></i></div>
            <div class="es-title">@(filtered ? "No results found" : "No customers yet")</div>
            <p>@(filtered ? "No customers match your filters." : "Add a customer to start taking orders.")</p>
            <a asp-action="Create" class="btn btn-secondary">New customer</a>   <!-- empty-state button is SECONDARY -->
        </div>
    }
    else
    {
        <div class="table-scroll">
            <table class="af-table">
                <thead>
                    <tr>
                        <th>Customer</th>
                        <th>Status</th>
                        <th class="text-end">Amount</th>
                        <th class="text-end">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>
                            <a asp-action="Details" asp-route-id="@x.Id" class="cell-title">@x.Name</a>
                            <span class="cell-sub">@x.City</span>
                        </td>
                        <td><span class="af-badge b-green">Active</span></td>
                        <td class="cell-num">₹@x.Amount.ToString("N2")</td>
                        <td class="cell-actions">
                            <div class="row-actions">
                                <a asp-action="Edit" asp-route-id="@x.Id" class="row-action" title="Edit" aria-label="Edit"><i class="bi bi-pencil"></i></a>
                                <form asp-action="Delete" asp-route-id="@x.Id" method="post" onsubmit="return confirm('Delete this customer?')">
                                    @Html.AntiForgeryToken()
                                    <button type="submit" class="row-action danger" title="Delete" aria-label="Delete"><i class="bi bi-trash"></i></button>
                                </form>
                            </div>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
        <div class="table-footer">
            <span>Showing <strong>@Model.Count()</strong> customers</span>
        </div>
    }
</div>
```
- **Labelled filters** (dates, many fields): use `class="table-toolbar align-end"` and wrap each control in `<div class="toolbar-field"><label for="x">From</label> … </div>`.
- Table header for a card without a toolbar: `<div class="data-card-header"><div class="data-card-title">Order items</div><span class="text-xs text-muted">3 lines</span></div>`.
- Row tint for exceptions (overdue, deduction): `<tr class="row-danger">` / `<tr class="row-warning">`. For an opening-balance or disabled row, use `<tr class="row-muted">`.
- Segmented filter above a table (status tabs that reload the page): `<div class="filter-bar"><div class="segmented"><a class="segment active">All</a>…</div></div>` as the first child of `.table-card`.
- Six KPIs in one row (aging buckets, GST totals): `<div class="metric-grid cols-6">`.

### 4.3 Create / edit form page
```html
<!-- page-header with page-back -->
<div class="row">
    <div class="col-xl-8">
        <form asp-action="Create" method="post" class="data-card">
            @Html.AntiForgeryToken()
            <div class="data-card-header">
                <div>
                    <div class="data-card-title">Customer details</div>
                    <div class="data-card-sub">Fields marked <span class="req">*</span> are required.</div>
                </div>
            </div>
            <div class="data-card-body">
                <div asp-validation-summary="ModelOnly" class="af-alert af-alert-danger"></div>
                <div class="row g-3">
                    <div class="col-md-6">
                        <label asp-for="Name" class="form-label">Name <span class="req">*</span></label>
                        <input asp-for="Name" class="af-input" placeholder="e.g. Shree Krishna Textiles" />
                        <span asp-validation-for="Name" class="af-error"></span>
                    </div>
                    <div class="col-md-6">
                        <label asp-for="Gstin" class="form-label">GSTIN <span class="text-muted fw-normal">(optional)</span></label>
                        <input asp-for="Gstin" class="af-input font-monospace" />
                        <div class="form-text">15-character GST number.</div>
                    </div>
                </div>
                <div class="form-actions">
                    <a asp-action="Index" class="btn btn-secondary">Cancel</a>
                    <button type="submit" class="btn btn-primary"><i class="bi bi-check2"></i> Save customer</button>
                </div>
            </div>
        </form>
    </div>
</div>
@section Scripts { <partial name="_ValidationScriptsPartial" /> }
```
- **Long forms with several cards:** put `<div class="form-footer">…buttons…</div>` as the last child of the `<form>`. It becomes a floating bar that sticks to the bottom. Inside a card body it renders as a plain footer.
- **Multiple sections in one card:** `<div class="section-label mt-4 mb-3">Charges</div>` (small uppercase label with a rule).
- **Side summary (totals) next to a form:** a `col-lg-4` holding `<div class="data-card sticky-side">` with `.kv-row` lines and a `.bill-total`.
- Buttons: Cancel (secondary) comes first, Save (primary) last, both right-aligned.

### 4.4 Detail page
- Header: title, status badge, `.page-sub` metadata joined with ` · `, and actions.
- An optional `.af-alert` for the record's state (overdue, cancelled, pending e-invoice).
- A `.metric-grid` for the key amounts.
- Info cards:
```html
<div class="data-card">
    <div class="data-card-header"><div class="data-card-title">Customer &amp; supply</div></div>
    <div class="data-card-body">
        <div class="info-grid">              <!-- .cols-3 / .cols-4 for more columns -->
            <div><div class="info-label">GSTIN</div><div class="info-value font-monospace">24AAA…</div></div>
            <div><div class="info-label">Phone</div><div class="info-value">98250 11223</div></div>
        </div>
    </div>
</div>
```
- Key/value lists and totals:
```html
<div class="kv-row"><span class="k">Subtotal</span><span class="v">₹15,000.00</span></div>
<div class="kv-row total"><span class="k">Taxable</span><span class="v">₹14,500.00</span></div>
<div class="bill-total"><span class="k">Grand total</span><span class="v">₹15,225.00</span></div>
```
- Totals under a line-item table go in `<tfoot class="totals-foot">`. Rows use `<td colspan="5" class="text-end">Label</td><td class="v">₹…</td>`, and the last row is `<tr class="grand">`.

---

## 5. Buttons

| Class | Use |
|---|---|
| `btn btn-primary` | The single main action (Save, Create, Record payment) |
| `btn btn-secondary` | Supporting actions (Cancel, Export, Print, Filter) |
| `btn btn-ghost` | Tertiary actions (Clear, Reset, “View all”) |
| `btn btn-danger` | Destructive confirm (Delete, Cancel IRN, Confirm return) |
| `btn btn-danger-ghost` | Low-emphasis destructive |
| `btn-sm` / *(default)* / `btn-lg` | 28 / 32 / 36px |
| `btn-icon` | Square icon-only button (with `title` + `aria-label`) |
| `is-loading` | Add during async work and disable the button |
| `row-action` (+ `.danger` `.success` `.warning` `.primary`) | Icon buttons in table rows |
| `label-action` | Tiny text action beside a field label (“+ New”) inside `.label-row` |

- Icon + text: `<i class="bi bi-plus-lg"></i> New order`. The icon comes first, with one space.
- Common icons:
  - create: `bi-plus-lg`
  - edit: `bi-pencil`
  - delete: `bi-trash`
  - view: `bi-eye`
  - print: `bi-printer`
  - save: `bi-check2`
  - filter: `bi-funnel`
  - search: `bi-search`
  - back: `bi-arrow-left`
  - download: `bi-download`
  - upload: `bi-upload`
  - more: `bi-three-dots`
  - dispatch: `bi-truck`
  - money: `bi-cash`
- Don't use `btn-outline-*`, `btn-success` or `btn-info` for new work. They exist only for old workflow buttons.

---

## 6. Forms

| Element | Class |
|---|---|
| Text / number / date / textarea | `af-input` (32px). In grids, add `af-input-sm` (28px) |
| Select | `af-input af-select` (custom chevron) |
| Label | `form-label` |
| Required marker | `<span class="req">*</span>` |
| Optional hint in label | `<span class="text-muted fw-normal">(optional)</span>` |
| Help text | `<div class="form-text">…</div>` |
| Field error | `<span asp-validation-for="X" class="af-error"></span>` |
| Summary error | `<div asp-validation-summary="ModelOnly" class="af-alert af-alert-danger"></div>` (auto-hidden when valid) |
| Prefix / suffix | `<div class="input-group"><span class="input-group-text">₹</span><input class="af-input" /></div>` |
| Search box | `.search-field` > `i.bi-search` + `input.af-input` |
| Trailing icon button | `.input-with-action` > `input.af-input` + `button.input-action` |
| Switch | `<div class="form-check form-switch"><input class="form-check-input" type="checkbox" role="switch" id="x"><label class="form-check-label" for="x">…</label></div>` |
| Read-only display | `af-input` with `readonly` (styled automatically); codes add `font-monospace fw-semibold` |
| Large radio choices | `.option-card` (selected state via `:has(input:checked)`) |
| Checkbox list | `.check-list` > `.check-item` |

**Editable line-item grids** (invoice, SO and PO lines):
```html
<div class="table-scroll">  <!-- or .grid-card for a bordered grid inside a card body -->
  <table class="af-table table-edit" style="min-width: 900px;">
    <tbody><tr>
      <td><input class="af-input af-input-sm" …></td>
      <td class="cell-num text-foreground fw-semibold item-total">₹0.00</td>
      <td><button type="button" class="row-action danger remove-row" title="Remove" aria-label="Remove row"><i class="bi bi-trash"></i></button></td>
    </tr></tbody>
  </table>
</div>
```
JS that builds rows must emit the same classes. Don't use `form-control` or inline font sizes.

Don't use `form-control`, `form-select`, `form-control-sm`, `af-label` or `af-form-group` in new code. Use `af-input`, `af-select`, `form-label` and Bootstrap grid gaps instead.

---

## 7. Cards

| Class | Use |
|---|---|
| `data-card` | Standard container (white, 1px border, 8px radius) |
| `data-card-header` | Flex header: title block on the left, actions or meta on the right |
| `data-card-title` / `data-card-sub` | 14px semibold title / 12px muted subtitle |
| `data-card-body` | 20px padded body |
| `table-card` | Card that holds a toolbar + table + footer (no body padding) |
| `metric-card` (+ `.metric-label/.metric-value/.metric-meta`) | KPI tiles inside `.metric-grid` |
| `info-grid` (+ `.info-label/.info-value`) | Label/value detail sections |
| `panel` / `panel-inset` | Sub-section with border / soft grey inset |
| `quick-link-card` (`.ql-icon/.ql-content/.ql-title/.ql-desc/.ql-arrow`) | Navigation tiles on dashboards |
| `summary-card` | Selectable summary tile (`.is-current` when selected) |
| `product-card` | Image grid items |

Card headers are always white or neutral. **Never** colour a card header or add a coloured `border-start` accent.

---

## 8. Tables

- Always `<table class="af-table">` inside `.table-scroll`. Headers are styled automatically: 11px uppercase, gray-50 background.
- First column: `.cell-title` (link or name, foreground, 500) + optional `.cell-sub` (12px muted second line).
- Numbers and money: `<td class="cell-num">` (right-aligned, tabular) with the header `<th class="text-end">`.
- Codes: `<span class="mono-id">LOT-0042</span>`.
- Actions: last column `<td class="cell-actions"><div class="row-actions">…row-action icons…</div></td>`. Put at most one text button (e.g. “Dispatch”) before the icons.
- Footer: `<div class="table-footer"><span>Showing <strong>N</strong> items</span></div>`.
- Thumbnails: `<img class="thumb">`. Avatars: `.av-sm`.
- Dense grids (attendance matrix): add `table-sm`.

---

## 9. Status badges

Markup: `<span class="af-badge b-COLOR">Label</span>`. Labels are short and in sentence case. Use `@enumValue.Humanize()` to turn enum names into text.

| Class | Meaning | Examples |
|---|---|---|
| `b-green` | Done / good | Paid, Received, Dispatched (complete), Active, Online, Settled, In stock |
| `b-yellow` | Waiting / partial | Pending, Partially paid, Standby, In production, Low stock |
| `b-red` | Problem / negative | Unpaid, Overdue, Rejected, Cancelled IRN, Out of stock, Clawback |
| `b-blue` | In progress / confirmed | Confirmed, In progress, Approved |
| `b-purple` | Intermediate stage | Partially dispatched, Partially received, At dying |
| `b-indigo` | Brand/info highlight | Pricelist name, Manual inward, Option A |
| `b-cyan` / `b-orange` | Extra stage colours | IGST, Handwork |
| `b-gray` | Neutral / closed / metadata | Draft, Cancelled (closed), Category, HSN, Place of supply |

**Domain mappings (keep these consistent everywhere):**

| Domain | Mapping |
|---|---|
| Account type | Asset `b-blue` · Liability `b-orange` · Equity `b-purple` · Income `b-green` · Expense `b-yellow` |
| Journal type | Sales `b-blue` · Purchase `b-yellow` · Bank `b-cyan` · Cash `b-green` · General `b-gray` |
| Journal entry status | Posted `b-green` · Draft `b-yellow` · Cancelled `b-gray` |
| Subscription plan | Enterprise `b-purple` · Growth `b-indigo` · Starter `b-blue` · Free trial `b-gray` |
| Tenant status | Active `b-green` · Past due `b-yellow` · Suspended `b-red` · Cancelled `b-gray` |
| GST match | Matched `b-green` · Missing in 2B `b-yellow` · In 2B only `b-blue` · Mismatch `b-red` |
| Account classification, HSN, UQC, place of supply | `b-gray` |

Other indicators:
- `.status-dot`: inline dot
- `.priority-*`: dot + label
- `.tag` with `.tag-remove`: removable chips
- `.count-pill`: count inside tabs and headings

---

## 10. Feedback

**Inline alert.** The icon comes automatically from the variant.
```html
<div class="af-alert af-alert-warning">          <!-- af-alert-info | -success | -warning | -danger -->
    <div class="alert-body">
        <span class="alert-title">Commission clawback</span>
        Salesman commission on these items is deducted automatically.
    </div>
    <button type="button" class="alert-close" aria-label="Dismiss">&times;</button>   <!-- optional -->
</div>
```

**Toast:** use `TempData` (§3) or `afToast(msg, type)`. Don't hand-write `.af-toast` in views.

**Empty state:** see §4.2. It has an icon circle, a title, one sentence, and an optional **secondary** button. It is used inside the card, not as a table row with `colspan`.

**Progress:**
- Bar: `<div class="af-progress"><div class="af-progress-bar" style="width: @pct%"></div></div>`. Add `bg-success` to the bar when complete.
- Labelled row: `.progress-row` > `.pr-label` + `.af-progress` + `.pr-value`.

**Process flow:**
```html
<div class="flow">
    <span class="flow-step done"><span class="flow-num"><i class="bi bi-check"></i></span> Dying</span>
    <i class="bi bi-chevron-right flow-sep"></i>
    <span class="flow-step current"><span class="flow-num">2</span> Handwork <span class="flow-code">In progress</span></span>
    <i class="bi bi-chevron-right flow-sep"></i>
    <span class="flow-step locked"><span class="flow-num">3</span> Stitching</span>
</div>
```

**Loading:** `.skeleton` blocks; `.is-loading` on buttons.

---

## 11. Navigation & overlays

- **Tabs:** Bootstrap `.nav-tabs` (underline style). Inside a card use `.nav-tabs.card-tabs`. Put counts in `<span class="count-pill">3</span>`.
- **Segmented control:** `.segmented` > `.segment` (`.active`).
- **Dropdown:** standard Bootstrap `.dropdown-menu`. Items use `<i class="bi …"></i>` + text. Destructive items get `.text-danger`. Group with `<h6 class="dropdown-header">`.
- **Modal:**
```html
<div class="modal fade" id="payModal" tabindex="-1" aria-labelledby="payTitle" aria-hidden="true">
  <div class="modal-dialog modal-dialog-centered">
    <form class="modal-content" method="post">
      <div class="modal-header">
        <h5 class="modal-title" id="payTitle">Record payment</h5>
        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
      </div>
      <div class="modal-body"> <div class="panel-inset">…kv-rows…</div> <div class="row g-3">…fields…</div> </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
        <button type="submit" class="btn btn-primary"><i class="bi bi-check2"></i> Save receipt</button>
      </div>
    </form>
  </div>
</div>
```
Modal headers are neutral (never coloured).

---

## 11a. Finance, reports & documents

**Financial statement tables** (P&L, balance sheet, GSTR-3B): add `fin-table` to the `af-table`.
```html
<table class="af-table fin-table">
  <tbody>
    <tr class="fs-group"><td colspan="2">Cash &amp; bank</td><td class="cell-num">₹1,20,000.00</td></tr>   <!-- section header row -->
    <tr><td class="fs-indent">HDFC current account</td><td class="font-monospace text-xs text-muted">102100</td><td class="cell-num">1,20,000.00</td></tr>
    <tr class="fs-subtotal"><td colspan="2">Total revenue</td><td class="cell-num">₹21,000.00</td></tr>
    <tr class="fs-highlight"><td colspan="2">Gross profit</td><td class="cell-num">₹12,500.00</td></tr>  <!-- key intermediate result -->
  </tbody>
  <tfoot><tr class="fs-result positive"><td colspan="2">Net profit</td><td class="text-end tabular">₹12,500.00</td></tr></tfoot>  <!-- negative for a loss -->
</table>
```
- Debit and credit suffix after a balance: `₹500.00<span class="dr-cr">Dr</span>`.
- Debit/credit columns show `—` for zero.
- Put totals in `<tfoot>`. Use `<tfoot class="totals-foot">` for stacked totals (see §4.4).

**Aging colour scale** (receivables and payables). Apply it to amount text only, and only when the amount is above zero:

| Class | Bucket |
|---|---|
| `age-current` | Not due |
| `age-30` | 1–30 days |
| `age-60` | 31–60 days |
| `age-90` | 61–90 days |
| `age-90p` | 90+ days (also semibold) |

Example: `<td class="cell-num @(x.Days31To60 > 0 ? "age-60" : "text-muted")">`. Tint rows with anything 90+ using `row-danger`.

**Statements and printable documents** (statement of account, payslip-style pages that stay in the app shell):
```html
<div class="page-header d-print-none">…back link, title, Print button…</div>
<div class="data-card doc-sheet">
  <div class="doc-head">
    <div><div class="doc-company">Aashana Fashion</div><div class="text-sm text-muted">Address…</div></div>
    <div class="doc-kind"><span class="micro-label">Statement of account</span><strong>01 Jan 2026 – 02 Oct 2026</strong></div>
  </div>
  <div class="row g-3 mb-4"> <div class="col-md-6"><div class="panel-inset h-100 mb-0">…kv-rows…</div></div> … </div>
  <div class="stat-strip mb-4">
    <div><div class="info-label">Opening balance</div><div class="info-value">₹0.00</div></div>
    <div class="is-total"><div class="info-label">Balance due</div><div class="info-value">₹15,750.00</div></div>
  </div>
  <div class="grid-card"><table class="af-table">…</table></div>
</div>
```
- Hide filters and action bars when printing with `d-print-none`. The shell (sidebar and topbar) is hidden automatically.
- **Never invent fallback values for money-routing data** (bank name, account number, IFSC, GSTIN). Show `Not set` instead.

**Tabbed report inside a card** (e.g. GST return tables):
```html
<div class="table-card">
  <ul class="nav nav-tabs card-tabs" role="tablist">
    <li class="nav-item" role="presentation"><button class="nav-link active" data-bs-toggle="tab" data-bs-target="#b2b" type="button" role="tab">B2B <span class="count-pill">12</span></button></li>
  </ul>
  <div class="tab-content">
    <div class="tab-pane fade show active" id="b2b" role="tabpanel">
      <div class="data-card-header">…title + Export…</div>
      <div class="table-scroll">…</div>   <!-- or .empty-state.py-4 -->
    </div>
  </div>
</div>
```

## 11b. Plans, quotas & SaaS screens

- **Plan picker:** wrap a radio in `label.plan-card`. The selected state comes from `:has(input:checked)`, so no JS is needed. Use `plan-card compact` inside narrow forms.
```html
<label class="plan-card" for="planGrowth">
  <div class="plan-head"><span class="plan-name">Growth</span><span class="af-badge b-indigo">Popular</span></div>
  <div class="plan-price">₹6,999<small>/mo</small></div>
  <p class="plan-desc">Garment brand &amp; manufacturing factory.</p>
  <ul class="plan-features"><li><i class="bi bi-check-lg"></i> 15 ERP desk users</li></ul>
  <span class="plan-select"><input class="form-check-input" type="radio" name="newPlan" id="planGrowth" value="Growth"> Select growth</span>
</label>
```
- **Quota / usage card:** a `data-card` holding a `.progress-row`, then `.usage-figures` (three label/value cells: limit · used · left), then a `.form-actions` row with the price note and a manage link.
  - Use the bar colour as a threshold: `bg-warning` at ≥ 75% and `bg-danger` at ≥ 90%.
  - Show the header badge as `af-badge b-gray`, switching to `b-yellow` / `b-red` at the same thresholds.
- **Quota banner on list pages** (seats or workers left): `af-alert af-alert-info` normally and `af-alert-warning` when full. Put the action button inside `.alert-body` (`d-flex justify-content-between`). Hide the page's primary "Add" button when the quota is full.
- **Per-row modals** (edit a tenant or a record): render them **after** the table in their own `@foreach`, never inside a `<td>`.

## 11c. Public (signed-out) pages

Anything `[AllowAnonymous]` (sign in, free-trial sign-up, suspended notice) uses `Layout = "_LoginLayout"`, never the app shell.
- **Split layout:** `<section class="auth-brand">` (logo, eyebrow, headline, feature list, legal) plus `<main class="auth-main"><div class="auth-card">…</div></main>`.
- **Wide forms** (sign-up): `auth-card auth-card-wide`. Group the fields with `.section-label`.
- **Single message pages** (suspended, expired): only `<main class="auth-main auth-solo">` holding an `.auth-card` with an `.empty-state`, an `.af-alert` and the actions.
- **Signing out is always a POST form** with `@Html.AntiForgeryToken()`, never a link. Show it only when `User.Identity?.IsAuthenticated == true`.

---

## 12. Content & formatting rules

- **Money:** `₹@value.ToString("N2")` with no space after ₹. Negative values use the true minus `−₹500.00`. Never write `@value:N2` in Razor, because it prints a literal “:N2”.
- **Dates:** `dd MMM yyyy` for display (e.g. 02 Oct 2026). Use `dd MMM yyyy, HH:mm` with time, and `dd/MM/yyyy` in dense tables.
- **Enums:** `@status.Humanize()`, e.g. `ReadyToDispatch` → “Ready to dispatch”. It comes from `AashanaFashion.Helpers.UiExtensions`.
- **Empty values:** show `—` (em dash), not “N/A”, “null” or blank.
- **Separators in meta lines:** ` · ` (middle dot).
- **Placeholders:** example values (`e.g. GJ05AB1234`), ending with `…` when they are instructions (“Search name, code…”).
- **Counts:** pluralise (`1 order` / `3 orders`).
- **Tone:** short, plain, and action-led. Page sub-titles are one sentence.
- **Razor gotchas:**
  - Wrap arithmetic before formatting: `₹@((a + b).ToString("N2"))`. `₹@(a + b).ToString("N2")` prints a literal “.ToString(…)”.
  - Never name a C# variable `section` (or `page`, `model`, `inject`, `using`). `@section.X` is parsed as a Razor directive.
  - For repeated row markup, use a local function in the `@{ }` block (`void Lines(FinancialReportSection sec) { <tr>…</tr> }`) instead of copy-pasting.

---

## 13. JavaScript-generated markup

Scripts that build HTML (line-item rows, notices, results) must use the same classes as Razor views:
- `af-input af-input-sm`, `row-action danger`, `af-badge b-*`, and `af-alert af-alert-*` with `.alert-body`.
- Insert user or server text with `textContent` or `document.createTextNode`, never by concatenating it into `innerHTML`.
- Pass values to handlers with `data-*` attributes, not inline `onclick="fn('@Model.Name')"`.
- Toggle visibility with `.d-none`/`classList` (or `style.display` for elements that start hidden). Don't set colours, borders or fonts from JS.
- Parse `<input type="date">` values as local dates: `new Date(value + 'T00:00:00')`.

---

## 14. Legacy classes — do not use in new code

These remain only so old markup doesn't break. Replace them when you touch a file.

| Legacy | Use instead |
|---|---|
| `btn-af`, `btn-af-primary`, `btn-af-ghost`, `btn-af-sm`, `btn-af-danger` | `btn btn-primary` / `btn-secondary` / `btn-ghost` / `btn-sm` / `row-action danger` |
| `form-control`, `form-select`, `*-sm` variants | `af-input`, `af-input af-select`, `af-input-sm` |
| `af-label`, `af-form-group` | `form-label` + `row g-3` |
| `stat-card`, `.stat-val`, `.stat-lbl` | `metric-card` |
| `card` / `card-header` / `card-body` | `data-card` / `data-card-header` / `data-card-body` |
| `table table-hover`, `thead.table-light` | `af-table` |
| `badge bg-success` / `bg-*-subtle` | `af-badge b-green` etc. |
| `alert alert-*` | `af-alert af-alert-*` |
| `b-warn` | `b-yellow` |
| Per-view `TempData` alert blocks | Nothing — the layout renders flash messages |
| Per-view `<style>` blocks | Classes in `site.css` |

---

## 15. Responsive

- Mobile-first Bootstrap grid. Forms use `col-md-*`; side columns use `col-lg-4` / `col-xl-*`.
- Below 992px the sidebar becomes a drawer (handled by the layout).
- Tables never cause page-level horizontal scroll. They scroll inside `.table-scroll`.
- `.metric-grid` shows 2 per row on phones automatically. `.page-actions` wraps automatically.
- Test at **390px** and **1440px** widths.

---

## 16. Checklist before you finish a UI change

- [ ] The page starts with `.page-header`. Child pages have a `.page-back` link.
- [ ] There is exactly one `btn-primary`, placed last in its group.
- [ ] There are no inline colour/font/spacing styles, no hex colours, no emoji, and no `<style>` blocks.
- [ ] Only `af-input`/`af-select`/`form-label` are used. No `form-control`/`btn-af`/`card`/`badge bg-*`.
- [ ] Statuses use `af-badge b-*` from §9, and enums use `.Humanize()`.
- [ ] Money is `₹x.ToString("N2")`, and empty values show `—`.
- [ ] Lists have a toolbar, an empty state (secondary button) and a `.table-footer` count.
- [ ] Icon-only buttons have `title` + `aria-label`, and inputs have labels.
- [ ] Every POST form has `@Html.AntiForgeryToken()`.
- [ ] Print views were not touched.
- [ ] Report pages hide filters and action bars in print (`d-print-none`).
- [ ] Public pages use `_LoginLayout`, and sign-out is a POST form.
- [ ] Domain badges follow the mappings in §9.
- [ ] Checked at 390px and 1440px.
