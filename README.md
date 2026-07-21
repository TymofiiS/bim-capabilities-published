# BIMCapabilities

BIMCapabilities turns your company BIM standards into executable validation rules that run inside Revit.

## For BIM Coordinators

<!-- BEGIN GENERATED:coordinator-release -->
1. [Download BIMCapabilities-1.0.7.zip](https://github.com/TymofiiS/bim-capabilities-published/raw/main/release/BIMCapabilities-1.0.7.zip)
<!-- END GENERATED:coordinator-release -->
2. **Right-click** the ZIP → **Properties** → check **Unblock** → **Apply**.
3. Extract the entire ZIP into **`%APPDATA%`** (overwrite existing files when updating).
4. Open **Revit 2024, 2025, or 2026** — the add-in loads automatically for your installed version.

### Create a BIM rule

1. Open **`Coordinator-Agent.md`** from any installed version folder, for example:
   `%APPDATA%\Autodesk\Revit\Addins\2026\BIMCapabilities\Coordinator-Agent.md`
2. Paste the entire file into **ChatGPT**, **Claude**, **Cursor**, **Codex**, or similar.
3. Ask: *What is supported?*
4. Ask: *Create a BIM rule for doors requiring MY_FireRating with default value EI60.*

### Run a BIM rule

1. Save the AI output as `MyRule.bimrule`.
2. In Revit: **BIMCapabilities → Run BIM Rule**
3. Select your `.bimrule` file.
4. Open the HTML report from the result dialog.
5. Use **Apply Automatic Correction** when the rule allows it, then run again to confirm PASS.

Full capabilities, examples, and troubleshooting are in **`Coordinator-Agent.md`** inside the ZIP (copied into each Revit version folder during installation).

## Requirements

**Windows** · **Revit 2024, 2025, or 2026** · No coding required.

## What BIMCapabilities does

```text
Download & Install (extract ZIP to %APPDATA%)
        ↓
Ask AI (Coordinator-Agent.md)
        ↓
Save BIMRule (.bimrule)
        ↓
Run BIM Rule in Revit
        ↓
HTML Compliance Report
        ↓
Apply Automatic Correction (when enabled)
```

## For Developers

Open this repository in **Cursor**, **Codex**, or **Claude Code**.

Read **[AGENT.md](AGENT.md)** first.
