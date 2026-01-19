# Screenshot Requirements for roslyn-diff Documentation

This document specifies the requirements for screenshots to be used in the roslyn-diff documentation, particularly for showcasing the HTML and JSON output features including impact badges, caveat warnings, and visual diff presentation.

## Overview

The roslyn-diff tool generates rich HTML and JSON outputs with advanced features including:
- Impact classification badges (Breaking Public API, Breaking Internal API, Non-Breaking, Formatting Only)
- Caveat warnings for potentially problematic changes
- Side-by-side diff visualization
- Collapsible change sections with navigation
- Syntax highlighting for C# code
- Summary statistics with change type breakdown

## Required Screenshots

### 1. HTML Output Screenshot (`html-output-screenshot.png`)

**Purpose**: Showcase the HTML report's visual interface with all key features visible.

**Source File**: `/Users/randlee/Documents/github/roslyn-diff/samples/impact-demo/output.html`

**What to Capture**:

#### Primary Elements (Must Be Visible)
1. **Header Section**:
   - Page title showing file comparison ("Diff: old.cs → new.cs")
   - Timestamp
   - Summary statistics badges showing:
     - Total changes count
     - Additions (green badge)
     - Deletions (red badge)
     - Modifications (yellow badge)
     - Impact breakdown (Breaking Public API, Breaking Internal API, Non-Breaking, Formatting Only)

2. **Impact Badges**:
   - At least 2-3 change sections visible showing impact indicator badges:
     - "BREAKING PUBLIC API" badge (red background)
     - Ideally show contrast between breaking and non-breaking changes

3. **Change Sections**:
   - Collapsible change headers showing:
     - Change type badge (Modified, Added, Removed)
     - Symbol kind (Namespace, Class, Method, Field, etc.)
     - Symbol name
     - Line number locations
     - Impact indicator badge
   - At least one expanded change section showing the side-by-side diff

4. **Side-by-Side Diff View**:
   - Left side (old code) with red highlighting for deletions
   - Right side (new code) with green highlighting for additions
   - Line numbers visible on both sides
   - Syntax highlighting for C# keywords, types, and comments

5. **Navigation Panel** (if visible in viewport):
   - Fixed navigation buttons at bottom-right:
     - Up arrow (scroll to top)
     - Previous/Next change navigation
     - Expand/Collapse all buttons

6. **Caveat Warnings** (Optional but highly desired):
   - Yellow warning box showing caveats like:
     - "Parameter rename may break callers using named arguments"
     - "Private member rename may break reflection or serialization"

#### Recommended Dimensions
- **Width**: 1920px (full HD width)
- **Height**: 1200-1400px (capture enough to show multiple change sections)
- **Format**: PNG with good compression
- **DPI**: 72-96 DPI (web standard)

#### Recommended Viewport/Section
- Scroll to show the **first 2-3 change sections** with at least one expanded
- Ensure the header with summary statistics is visible at the top
- If possible, capture a section showing both breaking and non-breaking impact badges
- Include at least one caveat warning if present in the visible area

#### Browser Recommendations
- Chrome or Firefox (latest version)
- Zoom level: 100% (no zoom)
- Window width: At least 1920px to show full side-by-side diff
- Disable browser extensions that might inject UI elements

#### Specific Sections to Highlight
From `/samples/impact-demo/output.html`, good candidate sections:
- The namespace "Samples.ImpactDemo" change (shows BREAKING PUBLIC API badge)
- The class "PaymentService" change (shows multiple nested changes)
- Field changes showing _merchantId → _merchantIdentifier (shows impact classification)
- Method changes showing impact badges and potential caveat warnings

### 2. JSON Impact Breakdown Screenshot (`json-impact-breakdown.png`)

**Purpose**: Show the JSON output's impact classification features and structured data format.

**Source File**: `/Users/randlee/Documents/github/roslyn-diff/samples/impact-demo/output.json`

**What to Capture**:

#### Primary Elements (Must Be Visible)
1. **Metadata Section**:
   ```json
   {
     "$schema": "roslyn-diff-output-v2",
     "metadata": {
       "version": "0.8.0+...",
       "timestamp": "2026-01-19T...",
       "mode": "roslyn",
       "options": {
         "includeContent": true,
         "contextLines": 3,
         "includeNonImpactful": false
       }
     }
   }
   ```

2. **Summary Section with Impact Breakdown**:
   ```json
   "summary": {
     "totalChanges": 16,
     "additions": 5,
     "deletions": 5,
     "modifications": 6,
     "renames": 0,
     "moves": 0,
     "impactBreakdown": {
       "breakingPublicApi": 16,
       "breakingInternalApi": 0,
       "nonBreaking": 0,
       "formattingOnly": 0
     }
   }
   ```

3. **Sample Change Object** showing impact field:
   ```json
   {
     "type": "modified",
     "kind": "namespace",
     "name": "Samples.ImpactDemo",
     "impact": "breakingPublicApi",
     "location": { ... },
     "oldLocation": { ... },
     "content": "...",
     "children": [...]
   }
   ```

#### Recommended Dimensions
- **Width**: 1200-1600px
- **Height**: 1000-1200px (capture metadata + summary + 1-2 change objects)
- **Format**: PNG with good compression

#### Recommended Tools
- **VS Code**: Open the JSON file with pretty-print formatting enabled
  - Use a dark theme (e.g., Dark+ or One Dark Pro) for better contrast
  - Font: Consolas, Monaco, or Fira Code at 12-14pt
  - Enable JSON syntax highlighting
- **Browser DevTools**: Open in browser console with formatting
- **Dedicated JSON Viewer**: Use a tool like `jq` with color output or online JSON formatter

#### Specific Content to Highlight
- The `impactBreakdown` section showing the distribution of change impacts
- At least one complete change object showing the `impact` field
- The `$schema` showing version v2 (indicating impact feature support)
- The `includeNonImpactful: false` option showing filtering capability

### 3. Additional Optional Screenshots

#### Whitespace Warning Display (`whitespace-warning-screenshot.png`)
**Purpose**: Show whitespace mode warnings in HTML output (if available in test files)

**What to Capture**:
- A change section with a yellow whitespace warning box
- Warning text explaining the whitespace issue
- Example: "Indentation changed (whitespace-significant in Python/YAML)"

**Dimensions**: 1200x600px (focused crop on warning section)

#### Navigation and Interaction (`navigation-features-screenshot.png`)
**Purpose**: Highlight the interactive navigation features

**What to Capture**:
- The fixed navigation panel at bottom-right
- Hover states on navigation buttons (if possible)
- Change sections in both expanded and collapsed states
- Copy buttons ("JSON" and "Diff" copy options)

**Dimensions**: 1920x1080px

## Screenshot Creation Guidelines

### For All Screenshots

1. **Quality Standards**:
   - Use PNG format for lossless quality
   - Ensure text is sharp and readable
   - Use appropriate compression (pngquant or similar)
   - Target file size: < 500KB per screenshot

2. **Consistency**:
   - Use the same browser/tool for all screenshots
   - Maintain consistent zoom level (100%)
   - Use consistent window dimensions
   - Apply the same color profile

3. **Content Clarity**:
   - Ensure syntax highlighting is visible
   - Make sure all text is legible at documentation viewing size
   - Avoid truncated or cut-off elements
   - Show complete UI components (no partial buttons/badges)

4. **Annotations** (Optional):
   - Consider adding red boxes or arrows to highlight key features
   - Use non-intrusive annotations that don't obscure content
   - Maintain professional appearance

### Browser Automation Options

If generating screenshots programmatically, consider:

1. **Playwright** (Node.js/Python):
   ```javascript
   const { chromium } = require('playwright');

   (async () => {
     const browser = await chromium.launch();
     const page = await browser.newPage();
     await page.setViewportSize({ width: 1920, height: 1400 });
     await page.goto('file:///path/to/output.html');
     await page.screenshot({ path: 'html-output-screenshot.png' });
     await browser.close();
   })();
   ```

2. **Puppeteer** (Node.js):
   ```javascript
   const puppeteer = require('puppeteer');

   (async () => {
     const browser = await puppeteer.launch();
     const page = await browser.newPage();
     await page.setViewport({ width: 1920, height: 1400 });
     await page.goto('file:///path/to/output.html');
     await page.screenshot({ path: 'html-output-screenshot.png' });
     await browser.close();
   })();
   ```

3. **Manual Screenshot Tools**:
   - **macOS**: Cmd+Shift+4 (select area) or Cmd+Shift+3 (full screen)
   - **Windows**: Snipping Tool or Win+Shift+S
   - **Linux**: gnome-screenshot, Spectacle, or Flameshot

## File Locations

All screenshots should be saved to:
- **Directory**: `/Users/randlee/Documents/github/roslyn-diff/docs/images/`
- **Naming Convention**:
  - `html-output-screenshot.png` - Main HTML output showcase
  - `json-impact-breakdown.png` - JSON structure with impact data
  - `whitespace-warning-screenshot.png` - Optional whitespace warnings
  - `navigation-features-screenshot.png` - Optional navigation UI

## Usage in Documentation

Once created, these screenshots will be referenced in:
- `README.md` - Main project documentation
- `docs/output-formats.md` - Format-specific documentation
- `docs/usage.md` - User guide examples
- GitHub Wiki pages (if applicable)
- Release notes and blog posts

## Quality Checklist

Before finalizing screenshots, verify:

- [ ] All required elements are visible and clear
- [ ] Text is readable at expected viewing size
- [ ] Colors accurately represent the actual UI
- [ ] No personal/sensitive information visible in paths or content
- [ ] File size is optimized (< 500KB)
- [ ] Image dimensions match recommendations
- [ ] Format is PNG (lossless)
- [ ] Syntax highlighting is properly displayed
- [ ] Impact badges are clearly visible with correct colors
- [ ] At least one caveat warning is shown (if applicable)

## Revision History

- **2026-01-19**: Initial requirements document created
  - Defined specifications for HTML and JSON screenshots
  - Added browser automation options
  - Specified impact demo sample files as source
