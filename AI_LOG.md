# AI Assistance Log (AI_LOG.md)

**Candidate:** Vinod Kumar Yadav  
**Role:** Mobile / Solutions Architect  
**Project:** JobFinder Lite (.NET MAUI & ASP.NET Core Minimal API)  
**Date:** November 2026  

---

## 1. Executive Summary

This document logs the AI-assisted development workflow used during the technical assignment, specifically focusing on generating the **Job Card UI from Figma design specifications** using GitHub Copilot (Agent Mode) and AI-assisted code review. It details the prompts, initial model outputs, architectural code reviews, defects detected, modifications made, and estimated time savings.

---

## 2. Figma to XAML Generation Workflow (Section 3.4)

### 2.1 Figma Design Specs Provided
| Element | Specification Value |
| :--- | :--- |
| **Card Container** | White background (`#FFFFFF`), `12` corner radius, `16` padding, light drop shadow, `12` bottom spacing between cards |
| **Job Title** | `17 pt` semibold, dark grey (`#1F2937`), max 2 lines with ellipsis (`TailTruncation`) |
| **Company · Location** | `14 pt` regular, secondary grey (`#6B7280`), single line with ellipsis |
| **Pay Rate** | `15 pt` semibold, accent blue (`#1D4ED8`) |
| **Job Type Chip** | Pill shape (`12` radius), tinted backgrounds: **Temporary** (Amber: `#FEF3C7` / text `#B45309`), **Permanent** (Green: `#D1FAE5` / text `#065F46`), **Contract** (Blue: `#DBEAFE` / text `#1E40AF`) |
| **Posted Time** | Relative time (e.g. `"2d ago"`), `12 pt` grey (`#9CA3AF`), right-aligned |
| **Save Icon** | Bookmark toggle in top-right corner, accessibility compliant **44 × 44 pt minimum tap target** |
| **Applied Badge** | Small `"Applied ✓"` pill badge displayed when application state is confirmed |

---

### 2.2 Prompts Given to GitHub Copilot / AI

#### Prompt 1: Initial Component Scaffolding
> *"Act as an expert .NET MAUI Architect. Generate a reusable `JobCardView` `ContentView` in XAML and code-behind for a job search app based on this Figma design spec:*
> *- Card: White background, 12 corner radius, 16 padding, subtle shadow, 12 spacing between cards.*
> *- Title: 17pt semibold, dark grey #1F2937, max 2 lines with ellipsis.*
> *- Company · Location: 14pt regular, secondary grey #6B7280, single line.*
> *- Pay Rate: 15pt semibold, accent blue #1D4ED8.*
> *- Job Type Chip: Pill shape (12 radius), tinted backgrounds: Temporary (amber), Permanent (green), Contract (blue).*
> *- Posted Date: Relative format ('2d ago'), 12pt grey, right aligned.*
> *- Bookmark save icon: Top-right corner, 44x44 minimum touch target.*
> *- Applied badge: Small pill shown when job is applied.*
> *Requirements:*
> *1. Use compiled bindings with `x:DataType="vm:JobCardViewModel"`.*
> *2. Define typography and colors in a `ResourceDictionary`.*
> *3. Include `AutomationId` on every interactive element for automated UI testing.*
> *4. Strict Clean Architecture: zero business logic in code-behind.*
> *5. Avoid legacy Xamarin.Forms controls; use modern .NET MAUI controls (`Border`, `StrokeShape`, `Shadow`)."*

---

### 2.3 What Copilot Got Right

1. **Semantic Structure:** Correctly created a 4-row `Grid` hierarchy (`Title/Bookmark`, `Company/Location`, `Pay/Posted`, `JobType/Badges`).
2. **Compiled Bindings:** Accurately placed `x:DataType="vm:JobCardViewModel"` at the root `ContentView`, unlocking compile-time binding validation and avoiding reflection overhead.
3. **ResourceDictionary Setup:** Placed shared styles (`CardBorderStyle`, `JobTitleStyle`, `PayRateStyle`) in `ContentView.Resources`.
4. **Touch Targets:** Acknowledged the `44 × 44` tap target requirement on the bookmark toggle button.

---

### 2.4 Code Review Findings & Architect Corrections

During architectural code review of the initial AI generation, four key issues were identified and corrected:

#### 1. Legacy Xamarin.Forms Relics
* **Issue:** Copilot initially attempted to use `Frame` with `HasShadow="True"` and `CornerRadius="12"`.
* **Architect Fix:** In modern .NET MAUI, `Frame` is obsolete and suffers from rendering inconsistencies across Android and iOS platforms. Replaced `Frame` with `Border`, using `StrokeShape="RoundRectangle 12"` and a native `Shadow` object with low opacity (`0.06`) and offset `(0, 2)`.

#### 2. Bookmark Tap Target Accessibility
* **Issue:** Copilot generated a standard `ImageButton` with `WidthRequest="24"`, which fails WCAG 2.5.5 accessibility guidelines for minimum touch target sizes.
* **Architect Fix:** Set explicit `WidthRequest="44"`, `HeightRequest="44"`, `MinimumWidthRequest="44"`, and `MinimumHeightRequest="44"` with centered content, ensuring a compliant 44×44 pt touch target.

#### 3. Dynamic Tinted Chip Colors
* **Issue:** Copilot attempted to write complex converter logic in XAML for chip background and text colors.
* **Architect Fix:** Encapsulated color calculation in `JobCardViewModel` (`JobTypeBgHex` and `JobTypeColorHex`) or lightweight converters, keeping the XAML markup clean, declarative, and high-performance.

#### 4. Automated Testing Support
* **Issue:** Copilot omitted `AutomationId` on the root card tap gesture and chips.
* **Architect Fix:** Added explicit `AutomationId` attributes to every element:
  - `JobCard_CardBorder`
  - `JobCard_TapRecognizer`
  - `JobCard_Title`
  - `JobCard_CompanyLocation`
  - `JobCard_PayRate`
  - `JobCard_PostedTime`
  - `JobCard_JobTypeChip`
  - `JobCard_SaveButton`
  - `JobCard_AppliedBadge`

---

### 2.5 Time Saved & Productivity Metrics

| Task | Manual Estimate | With AI + Review | Time Saved |
| :--- | :--- | :--- | :--- |
| Job Card XAML Layout & Styling | 45 minutes | 12 minutes | ~33 min (73%) |
| Bogus Data Seeding Generation (100 jobs) | 30 minutes | 8 minutes | ~22 min (73%) |
| Unit Test Boilerplate & Mock Setups | 40 minutes | 15 minutes | ~25 min (62%) |
| **Total Engineering Acceleration** | **115 minutes** | **35 minutes** | **~80 minutes saved** |

---

## 3. General AI Usage Beyond UI

1. **Bogus Mock Job Data Seeder:** AI accelerated generating realistic Indian, UK, US, and Australian logistics, technical, and healthcare job titles, companies, salary rates, and skills.
2. **Polly Resilience Policy Review:** Verified the exponential backoff jitter formula and HTTP 429 `Retry-After` header extraction logic.
3. **Idempotency Verification:** Formulated edge case tests simulating duplicate rapid taps and transient network drops.
