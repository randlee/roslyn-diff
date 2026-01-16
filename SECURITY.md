# Security Policy

## Supported Versions

The following versions of roslyn-diff are currently supported with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 0.5.x   | :white_check_mark: |
| < 0.5   | :x:                |

## Reporting a Vulnerability

If you discover a security vulnerability in roslyn-diff, please report it responsibly:

1. **Do not** open a public GitHub issue for security vulnerabilities.

2. **Email** the maintainers directly or use GitHub's private vulnerability reporting feature.

3. **Include** the following information in your report:
   - Description of the vulnerability
   - Steps to reproduce the issue
   - Potential impact
   - Any suggested fixes (optional)

4. **Response timeline**:
   - Initial acknowledgment: within 48 hours
   - Status update: within 7 days
   - Resolution target: within 30 days for critical issues

## Security Considerations

roslyn-diff is a code comparison tool that:

- Reads and parses source code files locally
- Does not transmit data over the network
- Does not execute the code it analyzes
- Generates HTML reports that may be opened in a browser

### HTML Report Security

When generating HTML reports:
- All user-provided file paths are HTML-encoded to prevent XSS
- JavaScript in reports is sandboxed to clipboard and UI operations only
- Reports use `encodeURIComponent()` for URL parameters

## Scope

This security policy applies to:
- The roslyn-diff CLI tool
- The RoslynDiff.Core library
- The RoslynDiff.Output library
- Generated HTML/JSON output formats

Third-party dependencies are managed via NuGet and should be kept updated.
