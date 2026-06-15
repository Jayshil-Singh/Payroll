# Fiji Enterprise Payroll System — UI Standards

**Version:** 1.0.0  
**Date:** June 2026  
**Status:** Approved  
**Owner:** Senior UI/UX Designer  

---

## 1. Design Philosophy

The Fiji Enterprise Payroll System UI must project **trust, professionalism, and ease of use**. Payroll officers deal with critical financial data — the interface must eliminate cognitive load, prevent errors, and support efficient workflows.

### Core Principles
1. **Clarity over decoration** — Every element must have a purpose
2. **Error prevention first** — Guide users before they make mistakes
3. **Efficiency for power users** — Keyboard navigation for all actions
4. **Progressive disclosure** — Show only what's needed, when it's needed
5. **Consistency** — Same patterns across all modules

---

## 2. Design System

### 2.1 Colour Palette

#### Primary Brand Colours
| Token | Hex | Usage |
|-------|-----|-------|
| `Primary` | `#1A56DB` | Primary buttons, active states, links |
| `Primary-Dark` | `#1040B0` | Button hover, pressed |
| `Primary-Light` | `#EBF1FF` | Hover backgrounds, selection highlights |
| `Primary-Foreground` | `#FFFFFF` | Text on primary background |

#### Semantic Colours
| Token | Hex | Usage |
|-------|-----|-------|
| `Success` | `#057A55` | Success messages, active status |
| `Success-Light` | `#F0FDF4` | Success background |
| `Warning` | `#B45309` | Warning messages, pending status |
| `Warning-Light` | `#FFFBEB` | Warning background |
| `Danger` | `#C81E1E` | Errors, delete actions |
| `Danger-Light` | `#FDF2F2` | Error background |
| `Info` | `#0694A2` | Informational messages |
| `Info-Light` | `#ECFEFF` | Info background |

#### Neutral Colours
| Token | Hex | Usage |
|-------|-----|-------|
| `Grey-900` | `#111827` | Primary text |
| `Grey-700` | `#374151` | Secondary text |
| `Grey-500` | `#6B7280` | Placeholder text, labels |
| `Grey-300` | `#D1D5DB` | Borders, dividers |
| `Grey-100` | `#F3F4F6` | Row hover, panel backgrounds |
| `Grey-50` | `#F9FAFB` | Page background |
| `White` | `#FFFFFF` | Card backgrounds |

---

### 2.2 Typography

**Font Family:** Segoe UI (system default on Windows — no download required)  
**Fallback:** Arial, sans-serif

| Token | Size | Weight | Line Height | Usage |
|-------|------|--------|-------------|-------|
| `Header-1` | 24px | 600 | 1.2 | Page titles |
| `Header-2` | 18px | 600 | 1.3 | Section headings |
| `Header-3` | 16px | 600 | 1.4 | Card titles, form group headers |
| `Body-Large` | 15px | 400 | 1.5 | Primary body text |
| `Body` | 14px | 400 | 1.5 | Default body text, form labels |
| `Body-Small` | 12px | 400 | 1.5 | Helper text, captions |
| `Label` | 13px | 500 | 1.4 | Form field labels |
| `Numeric` | 14px | 600 | 1.4 | All monetary values (right-aligned) |
| `Code` | 13px | 400 | 1.6 | Employee codes, reference numbers |

---

### 2.3 Spacing System (8px Grid)

| Token | Value | Usage |
|-------|-------|-------|
| `Space-1` | 4px | Icon margins, tight spacing |
| `Space-2` | 8px | Inner padding (small) |
| `Space-3` | 12px | Compact form row spacing |
| `Space-4` | 16px | Standard form row spacing |
| `Space-5` | 20px | Section padding |
| `Space-6` | 24px | Card padding |
| `Space-8` | 32px | Page section gap |
| `Space-10` | 40px | Page top padding |

---

### 2.4 Border Radius

| Token | Value | Usage |
|-------|-------|-------|
| `Radius-SM` | 4px | Input fields, tags |
| `Radius-MD` | 6px | Buttons, cards |
| `Radius-LG` | 8px | Dialogs, panels |
| `Radius-XL` | 12px | Side panels |

---

## 3. Layout

### 3.1 Shell Layout

```
┌──────────────────────────────────────────────────────────────┐
│  Title Bar: [App Logo] Fiji Payroll  [Company Selector] [User]│
├────────────┬─────────────────────────────────────────────────┤
│            │  Breadcrumb: Home > Employees > John Smith       │
│  Side Nav  ├─────────────────────────────────────────────────┤
│  (240px)   │                                                  │
│            │  Content Area (scrollable)                       │
│  ─────     │                                                  │
│  Dashboard │                                                  │
│  Company   │                                                  │
│  Employees │                                                  │
│  Payroll   │                                                  │
│  Leave     │                                                  │
│  Reports   │                                                  │
│  Settings  │                                                  │
│            │                                                  │
├────────────┴─────────────────────────────────────────────────┤
│  Status Bar: [Database: Connected] [License: 180 days] [User]│
└──────────────────────────────────────────────────────────────┘
```

### 3.2 Content Area Layout

**List/Grid View:**
```
┌─────────────────────────────────────────────────────────────┐
│  [Page Title]                    [Add New] [Import] [Export] │
│  ─────────────────────────────────────────────────────────  │
│  [Search Box]  [Filter: Status ▾] [Filter: Branch ▾] [Clear]│
│  ─────────────────────────────────────────────────────────  │
│  ┌─ DataGrid ──────────────────────────────────────────────┐│
│  │ ☐ | Code | Name | Dept | Status | Start Date | Actions ││
│  │ ─ | ──── | ──── | ──── | ────── | ────────── | ─────── ││
│  │ ☐ | E001 | John | IT   | Active | 01/01/2020 | ✏ 🗑   ││
│  └─────────────────────────────────────────────────────────┘│
│  Showing 1–25 of 142 records    [< Prev] [1] [2] [3] [Next >]│
└─────────────────────────────────────────────────────────────┘
```

**Detail/Form View:**
```
┌─────────────────────────────────────────────────────────────┐
│  ← Back to Employees   Employee: John Smith (E001)           │
│  [Tab: Personal] [Tab: Employment] [Tab: Payroll] [Tab: ...] │
│  ─────────────────────────────────────────────────────────  │
│  ┌─ Form Section ─────────────────────────────────────────┐ │
│  │  First Name*  [________________]  Last Name* [________] │ │
│  │  DOB          [__/__/____]        Gender     [   ▾   ] │ │
│  └────────────────────────────────────────────────────────┘ │
│  ─────────────────────────────────────────────────────────  │
│                    [Cancel]  [Save Changes]                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. Navigation

### 4.1 Side Navigation Structure
```
Dashboard

▾ Company
    Company Details
    Branches
    Departments
    Fiscal Calendar
    Payroll Frequency

▾ Employees
    Employee List
    New Employee
    Terminations

▾ Payroll
    Payroll Runs
    New Payroll Run
    Payslips

▾ Leave
    Leave Requests
    Leave Balances
    Holiday Calendar

▾ Compliance
    FRCS (MER)
    FNPF
    Bank Files

▾ Reports
    Payroll Reports
    Employee Reports
    Leave Reports

▾ Settings
    Configuration
    Users & Roles
    Audit Trail
    Backup
```

### 4.2 Breadcrumbs
- Always displayed below the title bar
- Clickable (all segments except current are links)
- Maximum depth: 4 levels

---

## 5. Form Standards

### 5.1 Label Placement
- Labels always **above** the input field (not inline or to the left)
- Required fields marked with red asterisk `*`
- `* Required fields` note at the top of each form section

### 5.2 Input Field Sizes
| Context | Width |
|---------|-------|
| Short (codes, dates) | 160px |
| Medium (names) | 280px |
| Long (addresses, descriptions) | 400px |
| Full-width | 100% of column |

### 5.3 Validation Display
- Inline error below the field (not in a popup)
- Red border on the invalid field
- Error icon inside field (right side)
- Error message: `Body-Small` in `Danger` colour
- Show after first blur (not on every keystroke)

### 5.4 Form Action Buttons
- **Always placed at the bottom right**
- Order: `[Cancel]` then `[Save / Submit / Confirm]`
- Primary action: `Primary` button style
- Cancel: `Secondary` (outline) button style
- Destructive actions: `Danger` button, require confirmation dialog

---

## 6. Data Grid Standards

### 6.1 Grid Behaviour
- Row alternating background: `White` and `Grey-50`
- Row hover: `Primary-Light`
- Row click = select row
- Row double-click = open detail
- Sortable columns: Show arrow indicator
- Default sort: Most recently modified first

### 6.2 Monetary Columns
- Always right-aligned
- Format: `$1,234.56` (FJD, 2 decimal places, thousands separator)
- Negative values: Red text, parentheses `($1,234.56)`

### 6.3 Column Standards
| Data Type | Alignment | Format |
|-----------|-----------|--------|
| Text | Left | — |
| Numbers | Right | `#,##0` |
| Currency | Right | `$#,##0.00` |
| Percentages | Right | `##0.00%` |
| Dates | Center | `DD/MM/YYYY` |
| Status | Center | Badge/pill |
| Actions | Center | Icon buttons |

### 6.4 Status Badges
| Status | Colour | Style |
|--------|--------|-------|
| Active / Approved / Paid | `Success` | Pill |
| Draft / Pending | `Warning` | Pill |
| Inactive / Expired | `Grey-500` | Pill |
| Terminated / Reversed | `Danger` | Pill |
| On Leave | `Info` | Pill |

---

## 7. Dialog Standards

### 7.1 Confirmation Dialogs
- Title: Clear action description (e.g., "Delete Employee?")
- Body: Explain consequence (e.g., "This will permanently remove John Smith. This cannot be undone.")
- Buttons: `[Cancel]` `[Delete]` (Delete = Danger colour)
- Always modal (blocks background)

### 7.2 Error Dialogs
- Title: "Something went wrong"
- Show user-friendly message
- Offer "Show Details" (expander for technical message — for IT support)
- Button: `[Close]` and optionally `[Report Issue]`

### 7.3 Progress Dialogs
- Show during payroll calculations and imports
- Indeterminate progress bar for short tasks
- Determinate progress bar with percentage for batch operations
- Allow cancel if operation supports it

---

## 8. Keyboard Shortcuts

| Action | Shortcut |
|--------|---------|
| New Record | `Ctrl + N` |
| Save | `Ctrl + S` |
| Cancel / Escape | `Esc` |
| Search | `Ctrl + F` |
| Print | `Ctrl + P` |
| Export | `Ctrl + E` |
| Delete | `Del` (with confirmation) |
| Navigate tabs | `Ctrl + Tab` |
| Refresh | `F5` |
| Help | `F1` |

---

## 9. Accessibility

| Requirement | Standard |
|-------------|---------|
| Colour contrast | WCAG AA (4.5:1 minimum) |
| Keyboard navigation | All actions accessible via keyboard |
| Tab order | Logical top-to-bottom, left-to-right |
| Focus indicators | Visible on all interactive elements |
| Error messages | Not communicated by colour alone |
| Font size | Minimum 12px, respects Windows display settings |

---

## 10. Empty States

Every list/grid must handle empty state gracefully:

```
[Icon — e.g., 👤 for employees]
No employees found

Add your first employee to get started, or 
adjust your search filters.

[Add Employee]  [Clear Filters]
```

---

## 11. Loading States

| Scenario | UI |
|----------|-----|
| Page loading | Full-area spinner with "Loading..." text |
| Grid loading | Skeleton rows (shimmer animation) |
| Button action in progress | Button disabled + spinner icon |
| Background save | Status bar: "Saving..." |

---

## 12. Installer & Setup Wizard UX (Phase 03)

### 12.1 Wizard Layout
```
┌──────────────────────────────────────────────────────────────┐
│  [App Logo]  Fiji Payroll — Setup Wizard                     │
├─────────────┬────────────────────────────────────────────────┤
│  Step Panel │                                                 │
│  ─────────  │  Step Title                                     │
│  ✅ Step 1  │  ─────────────────────────────────────────────  │
│  ✅ Step 2  │                                                  │
│  ▶ Step 3   │  Step content area                              │
│    Step 4   │                                                  │
│    Step 5   │                                                  │
│    Step 6   │                                                  │
│    Step 7   │                                                  │
│    Step 8   │                                                  │
│    Step 9   │                                                  │
│    Step 10  │  ─────────────────────────────────────────────  │
│             │  [Back]                         [Next / Finish] │
└─────────────┴────────────────────────────────────────────────┘
```

### 12.2 Progress States Per Step
- ✅ Green check = Completed
- ▶ Blue arrow = Current
- ⚫ Grey = Not yet reached
- ❌ Red X = Failed (with retry button)

### 12.3 Database Connection Step
- Fields: Server Name, Database Name, Auth type, Username, Password
- `[Test Connection]` button — shows real-time result
- Connection string preview (masked password)
- Retry logic: 3 attempts with 2-second delay

### 12.4 Rollback Strategy
- Each completed step is checkpointed
- If a step fails after data has been written, the wizard offers:
  - "Retry this step"
  - "Roll back to step N and try again"
  - "Exit (manual cleanup required)"

---

## 13. Responsive / Window Behaviour

| Window Size | Behaviour |
|------------|-----------|
| < 1280px wide | Side nav collapses to icons only |
| > 1600px wide | Content area centres with max-width 1400px |
| Minimum | 1024×768 |
| Recommended | 1920×1080 |

---

*Document maintained by: Senior UI/UX Designer*  
*Last updated: June 2026*
