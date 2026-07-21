# BIMCapabilities AI Guide

# For Humans

1. Open this repository in **Cursor**, **Codex**, or **Claude Code**.
2. Ask the AI to read this file (`AGENT.md`) first.
3. Ask for help using the repository documentation.

Examples:

* What is supported?
* Help me install BIMCapabilities and run my first validation.
* Create a door standard: DR_ prefix, require FireRating, no imported CAD.
* Create a Furniture standard. Require Manufacturer. No imported CAD.
* Help me understand a validation report.
* What does Missing FireRating mean?

The AI navigates the documentation graph described below. You do **not** need to copy this file into a separate chat if the AI already has repository access.

---

# Purpose

BIMCapabilities is a **Revit 2026 add-in** that turns company BIM standards into executable validation rules and produces professional compliance reports.

This AGENT.md is the **AI navigation entry point**. It guides assistants through:

* installation and first run
* finding supported capabilities in repository documentation
* creating executable `.bimrule` files using documented templates and atom semantics
* validation in Revit
* report interpretation
* troubleshooting

BIMCapabilities is documented only for:

* **Windows** 10 or later
* **Autodesk Revit 2026**

If the user is on an unsupported platform, explain the requirement and stop before installation.

Coordinator workflows live in the sections above **For Contributors**. Do not discuss repository architecture or source code with coordinators unless they ask to build from source.

# AI Instructions

You are helping a **BIM Coordinator** use BIMCapabilities.

Assume the user does **not** know C#, .NET, GitHub, or Revit add-in development.

Assume you **have access to this repository** (Cursor, Codex, Claude Code, or equivalent).

**Your responsibilities:**

1. Installation
2. Capability discovery ("What is supported?")
3. Rule creation (produce complete `.bimrule` JSON from repository docs)
4. Rule handoff (save, load, validate)
5. Report interpretation
6. Troubleshooting

**Workflow rules:**

1. Give one step at a time for installation and first run.
2. Explain the expected result after each step.
3. Wait for user confirmation before continuing when a step changes local files or Revit setup.
4. Use **only** the installation method documented below. Do not invent alternate install paths.
5. Do not tell the user to run `dotnet build`, PowerShell build scripts, or edit `.addin` files manually.
6. Before answering capability or rule questions, **read the repository documentation** listed in [Documentation graph](#documentation-graph) and [Engine documentation index](#engine-documentation-index). Do not promise capabilities beyond those documents.
7. Before creating or approving a `.bimrule`, confirm feasibility using the [Rule feasibility workflow](#rule-feasibility-workflow).
8. When saving or naming a `.bimrule` file, follow [BIMRule file naming](#bimrule-file-naming) — use a standard information-container ID, not a descriptive sentence.
9. When the user **updates** an existing rule, increment the version (`V01` → `V02`), update `metadata.ruleId` and `metadata.ruleVersion` in the JSON, and instruct **Save As** a new file — never overwrite the previous version (ISO 19650: each version is a distinct information container).
10. **Before outputting a `.bimrule`**, explain [automatic correction limits](#automatic-correction-limits) in plain language. Do not deliver JSON alone when the rule uses `fixEnabled: true` or includes naming, imported CAD, or parameter checks — the user must know what **Apply Automatic Correction** can and cannot fix.
11. **Never hallucinate** — follow [Forbidden in `.bimrule` files](#forbidden-in-bimrule-files). If the user asks for something unsupported, say **no** and explain what is supported today. Do **not** invent syntax that looks plausible but will fail in Revit.
12. Explain to the user in **their language**; keep technical atom IDs and configuration keys in English inside JSON.

# Documentation graph

When the user asks a question, read the documents below from this repository.

| User question | Read first | Then read |
|---------------|------------|-----------|
| **What is supported?** | [docs/capabilities/CAPABILITY-REGISTRY.md](docs/capabilities/CAPABILITY-REGISTRY.md) | Engine AGENT.md files (via index below) for context |
| **How does a capability work?** | Relevant engine `AGENT.md` | Linked atom doc under `src/Engines/*/docs/atoms/` |
| **Configuration keys / JSON shape** | Atom doc for the rule-ready atomId | [docs/rules/rule-examples.md](docs/rules/rule-examples.md) |
| **Can we generate this rule?** | [src/Generation/AGENT.md](src/Generation/AGENT.md) | CAPABILITY-REGISTRY |
| **Will it execute in Revit?** | [src/Composition/AGENT.md](src/Composition/AGENT.md) | Atom docs + example rules |
| **Example executable rules** | [docs/rules/rule-examples.md](docs/rules/rule-examples.md) | Files under `samples/rules/` |
| **Save and run a rule** | [docs/rules/rule-handoff.md](docs/rules/rule-handoff.md) | [Validation](#validation) below |
| **Install the add-in** | [Installation](#installation) below | [release/README.md](release/README.md) |

```text
AGENT.md (you are here)
    │
    ├── docs/capabilities/CAPABILITY-REGISTRY.md   ← supported rule-ready capabilities
    │
    ├── src/Engines/*/AGENT.md                     ← engine summaries + atom index
    │       └── docs/atoms/*.md                    ← configuration, examples, report messages
    │
    ├── src/Generation/AGENT.md                    ← NL → .bimrule generation limits
    ├── src/Composition/AGENT.md                 ← pipeline execution feasibility
    │
    └── samples/rules/*.bimrule                    ← approved executable templates
            docs/rules/rule-handoff.md             ← save and load in Revit
```

# Engine documentation index

<!-- BEGIN GENERATED:engine-index -->
When the user asks to create a rule, assess feasibility, or interpret findings, read the relevant documents below from the repository.

| Component | Path |
|-----------|------|
| **Family Engine** | `src/Engines/FamilyEngine/AGENT.md` |
| **Parameter Engine** | `src/Engines/ParameterEngine/AGENT.md` |
| **Naming Engine** | `src/Engines/NamingEngine/AGENT.md` |
| **Report Engine** | `src/Engines/ReportEngine/AGENT.md` |
| **Rule generation (NL → .bimrule)** | `src/Generation/AGENT.md` |
| **Validation pipeline** | `src/Composition/AGENT.md` |

Each engine AGENT.md links to per-atom documentation under `docs/atoms/`.
<!-- END GENERATED:engine-index -->

## Rule feasibility workflow

1. Parse the user request into required checks (naming, parameters, imported CAD, report, categories).
2. Read [docs/capabilities/CAPABILITY-REGISTRY.md](docs/capabilities/CAPABILITY-REGISTRY.md) — confirm each check is a **Supported** rule-ready capability.
3. Read relevant atom docs (via engine AGENT.md index) — confirm configuration keys and examples.
4. Read [src/Composition/AGENT.md](src/Composition/AGENT.md) — confirm the rule can **execute** in Revit today.
5. Read the closest template in [docs/rules/rule-examples.md](docs/rules/rule-examples.md) or `samples/rules/`.
6. If feasible → produce complete `.bimrule` JSON with correct atomIds and configuration.
7. If not feasible → explain which capability is missing and point to CAPABILITY-REGISTRY for what **is** supported. **Do not invent placeholder syntax** (see [Forbidden in `.bimrule` files](#forbidden-in-bimrule-files)).

## Forbidden in `.bimrule` files

**Hallucinated rules fail at load time in Revit.** The following are **forbidden** — never output them, even if they sound reasonable.

| Forbidden | Example | Why it fails |
|-----------|---------|--------------|
| Invented `atomId` | `parameter.copy-type-name` | Not in capability registry |
| Capability on wrong `engineId` | `naming.prefix.validation` under `parameter-engine` | CapabilityUnknown error |
| Invented configuration keys | `Doors.copyTypeNameToModel` | Rejected — key not documented |
| Placeholder / expression defaults | `Model={TypeName}`, `Model=$FamilyTypeName` | Rejected in **`parameterDefaults`** — literals only |
| `{…}` tokens in **`parameterDefaults`** | `Model={TypeName}` | Rejected — use **`parameterFillRules`** instead (see below) |
| Invented root JSON fields | `ruleName`, `checks`, `categories` | DeserializationFailure |
| Invented binding values | `Model=copy-from-type` | Only `type` or `instance` allowed |

**Supported today (only these rule-ready atomIds):**

`naming.prefix.validation` · `parameter.existence` · `family.imported-cad` · `report.compliance`

**Supported configuration keys** — see [CAPABILITY-REGISTRY.md](docs/capabilities/CAPABILITY-REGISTRY.md) and atom docs. Examples:

* `{Category}.parameters`, `{Category}.parameterDefaults`, `{Category}.parameterBinding`, `{Category}.parameterFillRules`
* `{Category}.prefix`, `{Category}.prefixFix`
* `excludeImportedCad.categories`

**`parameterDefaults`** = literal text only (for example `FireRating=EI60`). To copy values from Revit during automatic correction, use **`parameterFillRules`**:

* `Model=from:FamilyTypeName` — copy the family type name when Model is empty
* `RoomName=from:FamilyName` — copy the family name
* `Mark=from:OtherParameterName` — copy another parameter on the same target

**`prefixFix`** enables automatic rename when prefix validation fails (requires `fixEnabled: true`):

* `type` — rename family **type** names only (validation checks types only — same scope as fix)
* `family` — rename family **names** only
* `both` or `all` — rename both family and type names

When `prefixFix` is **omitted**, prefix validation still checks family and type names, but **Apply Automatic Correction** does not rename — manual rename required.

If the user asks to fill Model from the family type name when empty, use `parameterFillRules` — **not** `{TypeName}` in `parameterDefaults`:

```json
"Doors.parameterFillRules": "Model=from:FamilyTypeName"
```

---

# What BIMCapabilities Is

BIMCapabilities helps BIM Coordinators:

1. Describe a company BIM standard in plain language
2. Generate an executable rule file (`.bimrule`) with repository AI assistance
3. Save the rule and validate families in the active Revit model
4. Produce a professional HTML compliance report

```text
Open Repository in Cursor / Codex / Claude Code
        ↓
AI reads AGENT.md + documentation graph
        ↓
Executable Rule (.bimrule)
        ↓
Run Validation in Revit
        ↓
Professional Compliance Report
```

**Current product capabilities:**

* Revit 2026 ribbon add-in
* **Run Validation** — execute a `.bimrule` against the active model
* HTML and JSON compliance reports
* **Apply Automatic Correction** — missing shared parameters, literal defaults, parameter fill rules, and optional prefix renames (when `fixEnabled: true` and configured)

**Current limitations:**

* Automatic correction does **not** fix imported CAD or issues outside configured parameter/naming fix rules
* Naming prefix auto-fix requires `{Category}.prefixFix` — validation-only naming rules still need manual renames
* Revit 2026 only
* Windows only

---

# Installation

When a user asks to install BIMCapabilities, use **only** this workflow.

## Release Package

The verified release ZIP in the repository is:

```text
release/BIMCapabilities-1.0.3.zip
```

Download the latest `BIMCapabilities-*.zip` from the `release/` folder. **Coordinator-Agent.md** and example rules ship inside the `BIMCapabilities/` folder in the release ZIP. Repository documentation lives in the GitHub repo.

Do **not** ask the user to build from source for normal installation.

Do **not** ask the user to edit `BIMCapabilities.addin` unless troubleshooting confirms the manifest is missing or corrupted after extraction.

## Step 1 — Get the release ZIP

If the user opened the GitHub repository:

1. Open the `release` folder.
2. Download **`BIMCapabilities-1.0.3.zip`** (or the latest version in `release/`).

If the user downloaded the full repository as a ZIP:

1. Extract the repository ZIP.
2. Locate **`release/BIMCapabilities-1.0.3.zip`** (or the latest version in `release/`).

Expected result: the release ZIP is available on the user's computer (usually in Downloads).

## Step 2 — Unblock the ZIP (Windows)

If Windows marked the file as blocked:

1. Right-click the release ZIP (e.g. `BIMCapabilities-1.0.3.zip`).
2. Open **Properties**.
3. On the **General** tab, check **Unblock** if shown.
4. Click **OK**.

Expected result: the ZIP is unblocked, or no Unblock checkbox was shown.

## Step 3 — Extract into the Revit Addins folder

**Installation method:** extract the release ZIP **directly** into the Revit 2026 Addins folder. Do not move files manually after extraction.

User-specific path:

```text
%APPDATA%\Autodesk\Revit\Addins\2026\
```

Plain-language navigation: press `Win + R`, type `%APPDATA%`, press Enter, then open:

```text
Autodesk → Revit → Addins → 2026
```

Create the `2026` folder if it does not exist.

Extract the release ZIP into that folder.

## Expected installed layout

After extraction, the Addins folder must contain:

```text
%APPDATA%\Autodesk\Revit\Addins\2026\

BIMCapabilities.addin

BIMCapabilities/
BIMCapabilities/BIMCapabilities.Launchers.Revit.dll
BIMCapabilities/BIMCapabilities.Assistant.dll
BIMCapabilities/BIMCapabilities.Composition.dll
BIMCapabilities/BIMCapabilities.Generation.dll
BIMCapabilities/BIMCapabilities.Adapters.Revit.dll
BIMCapabilities/BIMCapabilities.Engines.Family.dll
BIMCapabilities/BIMCapabilities.Engines.Parameter.dll
BIMCapabilities/BIMCapabilities.Engines.Naming.dll
BIMCapabilities/BIMCapabilities.Engines.Report.dll
BIMCapabilities/BIMCapabilities.Runtime.dll
BIMCapabilities/BIMCapabilities.Contracts.dll
BIMCapabilities/BIMCapabilities.Launchers.Revit.deps.json
BIMCapabilities/Coordinator-Agent.md
```

The add-in manifest **`BIMCapabilities.addin`** must sit directly in the `2026` Addins folder — not inside the `BIMCapabilities` subfolder. **`Coordinator-Agent.md`** must stay inside the `BIMCapabilities` subfolder — not at the Addins root.

The manifest already contains the correct relative assembly path. **Do not edit it** during normal installation.

## Step 4 — Verify installation

1. Close Revit completely if it is open.
2. Start **Revit 2026**.
3. Open any project containing families to validate.
4. Confirm the **BIMCapabilities** ribbon tab appears.
5. Click **BIMCapabilities** → **Run Validation** — a file picker should open.

Expected result: the ribbon tab appears and **Run Validation** opens a file picker.

If Revit shows **Security — Unsigned Add-In**, click **Always Load**.

---

# Rule Creation

Rules are created with **repository AI** reading the documentation graph — not by memorizing capability details in this file.

## Step 1 — Discover capabilities

When the user asks **"What is supported?"**, read [docs/capabilities/CAPABILITY-REGISTRY.md](docs/capabilities/CAPABILITY-REGISTRY.md) and summarize in coordinator language.

## Step 2 — Assess feasibility

Follow the [Rule feasibility workflow](#rule-feasibility-workflow).

## Step 3 — Produce `.bimrule` JSON

1. Read atom docs for each required capability (configuration keys, examples).
2. Adapt the closest file listed in [docs/rules/rule-examples.md](docs/rules/rule-examples.md).
3. Output complete, executable JSON.
4. Set `metadata.ruleId` using [BIMRule file naming](#bimrule-file-naming) and save the file with the same name.
5. Place each `atomId` under the correct `engineId` (`naming.prefix.validation` → `naming-engine`, `parameter.existence` → `parameter-engine`, `family.imported-cad` → `family-engine`, `report.compliance` → `report-engine`). Never nest naming capabilities inside `parameter-engine`.

## BIMRule file naming

Company BIM standards are **information containers** in ISO 19650 terms: each `.bimrule` file needs a **unique, structured identifier**, not a readable sentence. National annexes (for example BS EN ISO 19650) define delimiter-separated fields so files sort and trace consistently in a Common Data Environment (CDE). BIMCapabilities follows that principle with a compact standard ID.

**Do not use descriptive filenames** such as `Company-Doors-Windows-Room.bimrule`. Put the human-readable title in `metadata.name` and `metadata.description` instead.

### Pattern

```text
STD-{Discipline}-{Topic}-{Version}.bimrule
```

| Field | Meaning | Allowed values |
|-------|---------|----------------|
| `STD` | Company **standard** (executable BIM rule) | Fixed prefix |
| `{Discipline}` | Design discipline | `ARC` (architecture), `INT` (interior), `MEP` (mechanical/electrical/plumbing) |
| `{Topic}` | Subject of the standard | `OPENINGS` (doors + windows), `FURNITURE`, `EQUIPMENT` |
| `{Version}` | Rule release version | `V01`, `V02`, … — matches `metadata.ruleVersion` |

### Rules

1. **Filename = `metadata.ruleId`** — the saved file name (without `.bimrule`) must match `metadata.ruleId` exactly.
2. **Uppercase and hyphens only** — use ASCII letters, digits, and hyphen-minus (`U+002D`) as field separators; no spaces or underscores in the ID.
3. **Topic, not category list** — a rule covering doors, windows, and room parameters is `OPENINGS`, not `Doors-Windows-Room`.
4. **Version in metadata, not status** — revision or approval status belongs in `metadata.status`; the filename carries the rule version (`V01`).
5. **Prefer existing example IDs** — when adapting a template, keep or increment its version (for example `STD-ARC-OPENINGS-V01` → `STD-ARC-OPENINGS-V02`).

### Updating an existing rule (new version)

When the user adds or changes requirements in a rule that already exists on disk, treat this as a **new information-container revision** — do **not** overwrite the previous file.

1. Increment `{Version}` in the filename and in JSON: `metadata.ruleId` and `metadata.ruleVersion` must match (for example `V01` → `V02`, `STD-ARC-OPENINGS-V01` → `STD-ARC-OPENINGS-V02`).
2. Keep the previous `.bimrule` file unchanged so projects and audit trails can reference the approved version they were checked against.
3. When giving save instructions after an update, use **Save As** with the new ID — not “save the file” over the old name.

**Example — guide the user like this:**

```text
How to update the rule

1. Copy the entire JSON above.
2. Open Notepad (you do not need to open the old file).
3. Paste the new JSON.
4. Click File → Save As.
5. Save as: STD-ARC-OPENINGS-V02.bimrule (All Files, UTF-8).
6. Run validation in Revit using the new V02 file.
```

Do **not** end with “replace everything in STD-ARC-OPENINGS-V01.bimrule and save” — that breaks ISO 19650 version traceability.

### Examples

| Good | Why |
|------|-----|
| `STD-ARC-OPENINGS-V01.bimrule` | Architecture · openings (doors/windows) · first release |
| `STD-INT-FURNITURE-V01.bimrule` | Interior · furniture · first release |
| `STD-MEP-EQUIPMENT-V01.bimrule` | MEP · equipment · first release |

| Avoid | Why |
|-------|-----|
| `Company-Doors-Windows-Room.bimrule` | Sentence-style name; not a CDE information-container ID |
| `my-door-rule.bimrule` | Lowercase, no discipline/topic/version structure |
| `STD-ARC-DOORS-WINDOWS-V01.bimrule` | Category list instead of the `OPENINGS` topic code |

Approved reference: [samples/rules/STD-ARC-OPENINGS-V01.bimrule](samples/rules/STD-ARC-OPENINGS-V01.bimrule).

## Automatic correction limits

**Always explain this to the user before they save a rule** — especially when the rule includes naming prefix checks, imported CAD checks, or `"fixEnabled": true`.

BIMCapabilities **validates** naming, parameters, and imported CAD. **Automatic correction** (ribbon → validation dialog → **Apply Automatic Correction**) is narrower.

| Finding type | Validated? | Auto-fix today? | User action |
|--------------|------------|-----------------|-------------|
| Missing required shared parameter | Yes | **Yes** — creates parameter and applies defaults/fill rules | Use **Apply Automatic Correction** or fix manually |
| Empty parameter value | Yes | **Yes** — when `parameterDefaults` or `parameterFillRules` is configured | Use **Apply Automatic Correction** or fix manually |
| Naming prefix violation | Yes | **Yes** — when `{Category}.prefixFix` is set (`type`, `family`, or `both`) | Use **Apply Automatic Correction** or rename manually |
| Imported CAD in family | Yes | **No** | Remove imported CAD manually in the family |

When both naming and parameter fixes are configured, correction runs **naming renames first**, then parameter updates (so fill-from-type-name uses the corrected name).

**Scope alignment:** `{Category}.prefixFix` controls **both** what prefix validation reports and what automatic correction renames. Example: `Windows.prefixFix: type` with 14 types and 3 families → **14** prefix failures (family names like `M_Window-Fixed` are out of scope).

**Revit type renames:** Renames are batched per family (`EditFamily` → `LoadFamily`). When reload leaves duplicate type symbols, the launcher remaps **placed instances** to the new type, deletes superseded symbols, then continues. If remapping fails, correction stops with an error — re-test on a clean project copy.

See [Validation — Apply automatic correction](#apply-automatic-correction-val--fix--val) for the full val → fix → val workflow.

If validation fails **only** on imported CAD, or on naming without `prefixFix`, **Apply Automatic Correction** may show:

```text
No supported parameter fixes were found for the current validation findings.
```

That is expected when no fix rules apply — not a bug. Tell the user before they run validation.

When a rule mixes parameters **and** naming (for example V02 with `WD_` prefix + `MY_Room`):

1. Validation reports **all** issues.
2. Automatic correction applies configured **prefixFix** and **parameter** fixes (naming first).
3. Issues without fix configuration (for example prefix check without `prefixFix`) remain until corrected manually, then re-run validation.

You may set `"fixEnabled": true` with `prefixFix` and/or `parameterFillRules` — but **must** explain what will and will not be corrected automatically.

## Step 4 — Save and validate

Guide the user using [docs/rules/rule-handoff.md](docs/rules/rule-handoff.md), then [Validation](#validation) below.

## Shared parameters

When a rule checks required parameters, it references a shared parameter file path in `externalReferences`. A common path:

```text
D:\Company\SharedParameters.txt
```

Before validation, confirm that path exists and contains the parameters named in the rule. See atom docs for parameter-engine configuration details.

---

# Validation

## Run validation

1. Save a `.bimrule` file (see [docs/rules/rule-handoff.md](docs/rules/rule-handoff.md)).
2. Open a Revit project containing families in the rule's categories.
3. Click **BIMCapabilities** → **Run Validation**.
4. Select the `.bimrule` file.

Validation runs against the **active Revit model**.

## Apply automatic correction (val → fix → val)

When the rule sets `"fixEnabled": true` and configures fixable findings (`prefixFix`, `parameterDefaults`, and/or `parameterFillRules`):

1. **Validate** — the compliance report lists all issues (HTML + JSON).
2. In the validation dialog, click **Apply Automatic Correction** (user approval applies when `requireUserApprovalBeforeModification` is set).
3. **Re-validate** — confirm **100%** compliance on configured checks.

Correction also writes a **correction report** (`{ruleId}-correction-report.html/json`) under `%TEMP%\BIMCapabilities\`.

| Stage | Report | Typical outcome (openings sample) |
|-------|--------|----------------------------------|
| Initial validation | Compliance report | Fail — empty `Model` on door types + missing `WD_` on 14 window **types** |
| Fix | Correction report | 19 values assigned + 14 type renames |
| Re-validation | Compliance report | Pass — 0 issues |

**Fix order:** naming prefix renames **first**, then parameter creates/updates (including fill rules). Mixed rules such as `WD_` prefix + `Model=from:FamilyTypeName` rely on this order.

**API verification (no UI dialogs):** the **API Verification** ribbon command (`DirectRuleExecutionVerificationCommand`) runs validate → fix (when eligible) → re-validate through `LauncherRuleExecutionService` and `LauncherRuleFixExecutionService`.

### prefixFix scope (validation = fix)

| `prefixFix` | Prefix validation checks | Automatic rename |
|-------------|--------------------------|------------------|
| `type` | Family **type** names only | Family **type** names only |
| `family` | Family **names** only | Family **names** only |
| `both` / `all` | Family and type names | Family and type names |
| *(omitted)* | Family and type names | **None** — manual rename |

### Revit launcher fix implementation

| Fix kind | Launcher component | Behavior |
|----------|-------------------|----------|
| Type prefix rename | `LauncherNamingFixExecutor` | Batch renames per family via `EditFamily` + `LoadFamily`; reconcile duplicate symbols; remap placed instances before deleting old types |
| Parameter create/fill | `LauncherParameterFixExecutor` | Create missing shared parameters; apply `parameterDefaults`; copy empty values via `parameterFillRules` |
| Write request build | `FixPipeline` | Maps validation findings → ordered write requests; respects `PrefixFixScope` |

If a type rename fails mid-run (for example instance remapping on a placed window), earlier families in the same run may already be updated — use a clean model copy and re-run val → fix → val.

## Before validation

* A Revit project must be open.
* The model should contain families in the categories defined by the rule.
* Required shared parameter files must exist at paths referenced by the rule.
* For fix testing, prefer a **fresh project copy** — partial failed fix runs can leave duplicate type symbols.

## Report output

* An HTML compliance report opens in the default browser (validation).
* Reports are saved under `%TEMP%\BIMCapabilities\` (compliance and correction).

---

# Report Interpretation

## Report sections

**Compliance Summary** — overall compliance percentage and pass/fail counts.

**Violations** — each family that failed, with a message and severity.

**Evidence** — detailed records supporting each violation.

For capability-specific diagnostic messages, read the **Typical report messages** section in the relevant atom doc under `src/Engines/*/docs/atoms/`.

## Common findings

### Missing FireRating

A **door family** is missing the `FireRating` parameter or has no value assigned.

**Fix:** Add or populate `FireRating` on the family, then re-run validation.

### Missing AcousticRating

A **window family** is missing the `AcousticRating` parameter or has no value assigned.

**Fix:** Add or populate `AcousticRating` on the family, then re-run validation.

### Missing Manufacturer

A **furniture family** is missing the `Manufacturer` parameter or has no value assigned.

**Fix:** Add or populate `Manufacturer` on the family, then re-run validation.

### Imported CAD detected

A family contains imported CAD geometry. The standard requires native Revit geometry.

**Fix:** Remove imported CAD from the family, then re-run validation.

### Naming violation

A family or **type** name does not use the required prefix (for example `DR_` for doors, `WD_` for windows).

**Fix:** Rename manually, or use **Apply Automatic Correction** when `{Category}.prefixFix` is configured. With `prefixFix: type`, only **type names** are checked and renamed — family names (for example `M_Window-Fixed`) stay unchanged.

### Missing RoomName

A family is missing the `RoomName` parameter or has no value assigned.

**Fix:** Add or populate `RoomName`, then re-run validation.

---

# Troubleshooting

### Ribbon tab not visible

1. Confirm **Revit 2026** is installed.
2. Confirm extraction was into `%APPDATA%\Autodesk\Revit\Addins\2026\`.
3. Confirm `BIMCapabilities.addin` is directly in the `2026` folder.
4. Confirm `BIMCapabilities\BIMCapabilities.Launchers.Revit.dll` exists.
5. Restart Revit completely.

### Validation not starting

1. Confirm a Revit **project** is open.
2. Confirm the model contains relevant families.
3. Confirm the `.bimrule` file path is valid and contains valid JSON.

### Shared parameter file missing

Create the path referenced in the rule (for example `D:\Company\SharedParameters.txt`) and place the company shared parameter file there.

### Report does not open

Open the HTML file manually from `%TEMP%\BIMCapabilities\`.

### Invalid rule file

1. Confirm the file extension is `.bimrule`.
2. Compare against templates in `samples/rules/` and [docs/rules/rule-examples.md](docs/rules/rule-examples.md).
3. Read atom docs for correct atomIds and configuration keys.

---

# For Contributors

The sections below are for developers modifying the BIMCapabilities source code.

## Product Scope

**In scope:** Revit 2026 launcher, validation pipeline, automatic correction (parameter + naming prefix), compliance and correction reports.

**Out of scope:** Imported CAD auto-fix, geometry engine, non-Revit platforms.

## Documentation maintenance

Capability knowledge is **not** duplicated in this file. Update sources and run ReleaseDocs:

```powershell
dotnet run --project tools/ReleaseDocs -- update-all
```

| Source of truth | Path |
|-----------------|------|
| Rule-ready capabilities | `CapabilityCatalogDefinitions` → [docs/capabilities/CAPABILITY-REGISTRY.md](docs/capabilities/CAPABILITY-REGISTRY.md) |
| Atom semantics | `src/Engines/*/docs/atoms/*.md` |
| Engine navigation | `src/Engines/*/AGENT.md` (generated atom index) |
| Root navigation index | This file (generated engine index) |
| Example rule templates | `samples/rules/` |

## Architecture (Capabilities-First)

```text
Contracts          ← shared types, BIMRule schema, engine contracts
Engines            ← Family, Parameter, Naming, Report (→ Contracts only)
Runtime            ← orchestration skeleton (→ Contracts only)
Composition        ← ValidationPipeline, FixPipeline (→ Runtime + Engines)
Generation         ← NL → .bimrule (→ Contracts)
Assistant          ← optional in-Revit chat UI (→ Generation + Composition)
Revit Adapter      ← Revit read layer (→ Contracts)
Revit Launcher     ← Ribbon add-in (→ Adapter + Composition + Assistant)
```

## Repository Structure

| Area | Path |
|------|------|
| BIMRule contract | `src/Contracts/Rules/` |
| Validation pipeline | `src/Composition/Validation/` |
| Fix pipeline | `src/Composition/Fix/` |
| Naming fix executor (Revit) | `src/Launchers/RevitLauncher/Execution/LauncherNamingFixExecutor.cs` |
| Parameter fix executor (Revit) | `src/Launchers/RevitLauncher/Execution/LauncherParameterFixExecutor.cs` |
| Rule generation | `src/Generation/` |
| Revit launcher | `src/Launchers/RevitLauncher/` |
| Release ZIP | `release/BIMCapabilities-1.0.3.zip` (latest in `release/`) |
| Release add-in manifest | `release/BIMCapabilities.addin` |

## Build and Release

```powershell
dotnet build BIMCapabilities.sln
dotnet test BIMCapabilities.sln
dotnet run --project tools/ReleaseDocs -- update-all
.\tools\BuildReleasePackage\Build-BIMCapabilitiesRelease.ps1
```

See [release/RELEASE.md](release/RELEASE.md) for the full release workflow.

## Contributor Constraints

- Do not add new engines without architecture review.
- Do not embed business rules in launcher or adapter code — use `.bimrule` files.
- Keep tests deterministic.
- Do not duplicate capability tables or atom configuration into root AGENT.md — update registry and atom docs instead.

## Architecture Documents

See `docs/architecture/` numbered documents 31–39.
