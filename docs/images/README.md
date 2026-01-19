# roslyn-diff Documentation Images

This directory contains screenshots and images used in the roslyn-diff documentation.

## Current Status

This directory is currently set up with placeholder files (`.TODO` files) that describe what screenshots need to be captured.

## Required Screenshots

### 1. `html-output-screenshot.png`
**Status**: Pending (see `html-output-screenshot.png.TODO`)

**Purpose**: Showcase the HTML report output with impact badges, caveat warnings, side-by-side diff, and navigation features.

**Source**: `/Users/randlee/Documents/github/roslyn-diff/samples/impact-demo/output.html`

### 2. `json-impact-breakdown.png`
**Status**: Pending (see `json-impact-breakdown.png.TODO`)

**Purpose**: Display the JSON output structure showing the impact classification breakdown and structured data format.

**Source**: `/Users/randlee/Documents/github/roslyn-diff/samples/impact-demo/output.json`

## How to Generate Screenshots

See the comprehensive guide in `/docs/screenshot-requirements.md` for:
- Detailed specifications for each screenshot
- Recommended dimensions and formats
- Browser/tool recommendations
- Quality standards and checklists
- Sample code for browser automation (Playwright/Puppeteer)

## Quick Start

### Option 1: Manual Screenshots

1. Open `samples/impact-demo/output.html` in Chrome/Firefox at 1920x1400 viewport
2. Capture the header + first 2-3 change sections with impact badges visible
3. Save as `html-output-screenshot.png`

4. Open `samples/impact-demo/output.json` in VS Code with JSON formatting
5. Capture metadata + summary (with impactBreakdown) + one sample change object
6. Save as `json-impact-breakdown.png`

### Option 2: Browser Automation

Use Playwright or Puppeteer to generate screenshots programmatically. See examples in `/docs/screenshot-requirements.md`.

### Option 3: Request Screenshots

If you don't have the tools to create these screenshots, open a GitHub issue requesting screenshot generation and tag it with `documentation` and `help-wanted`.

## File Naming Convention

- `html-output-screenshot.png` - Main HTML output showcase
- `json-impact-breakdown.png` - JSON output with impact data
- `whitespace-warning-screenshot.png` - (Optional) Whitespace mode warnings
- `navigation-features-screenshot.png` - (Optional) Interactive features

## Quality Requirements

All screenshots must meet these standards:
- Format: PNG (lossless)
- Max file size: < 500KB (use compression)
- Text must be sharp and readable
- No personal/sensitive information in paths
- Consistent with other documentation images

## Usage

These screenshots are referenced in:
- `README.md` - Main project overview
- `docs/output-formats.md` - Output format documentation
- `docs/usage.md` - User guide
- GitHub releases and announcements

## Contributing

When adding new screenshots:
1. Follow the specifications in `/docs/screenshot-requirements.md`
2. Optimize file size using `pngquant` or similar tools
3. Update this README with the new screenshot description
4. Remove corresponding `.TODO` placeholder files
5. Submit a pull request with the images

## Support

For questions about screenshot requirements or assistance with generation:
- Review `/docs/screenshot-requirements.md`
- Open a GitHub issue
- Contact the maintainers
