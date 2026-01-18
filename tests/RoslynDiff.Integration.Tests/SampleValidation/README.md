# Sample Validation

This folder contains integration tests that validate sample data consistency across different output formats in RoslynDiff.

## Purpose

The tests in this directory ensure that:
- Sample data produces consistent results across all supported output formats (text, JSON, HTML, Markdown)
- Output formats maintain semantic accuracy and structural integrity
- Changes to the tool don't introduce regressions in output quality

## Organization

Tests are organized by validation type:
- Format consistency tests
- Content accuracy tests
- Cross-format comparison tests

## Usage

These tests leverage utilities from `RoslynDiff.TestUtilities` project including:
- Parsers for each output format
- Validators for semantic correctness
- Comparers for cross-format consistency checks
