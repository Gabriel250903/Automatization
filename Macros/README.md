# Automatization Macro Language

Complete guide for building macros in the **Script Code** tab of Macro Manager.

You can save, export, and import macros as `.azmacro` files to share them with others.

---

## Table of Contents

1. [Overview & Execution Concepts](#overview--execution-concepts)
2. [Target Scopes: Background vs Foreground](#target-scopes-background-vs-foreground)
3. [Trigger Modes](#trigger-modes)
4. [AutoScript Language Syntax](#autoscript-language-syntax)
   - [Basic Structure & `main:`](#basic-structure--main)
   - [Indentation & Block Rules](#indentation--block-rules)
   - [Metadata Directives](#metadata-directives)
   - [Comments & Variables](#comments--variables)
   - [Key Actions & Supported Keys](#key-actions--supported-keys)
   - [Delays & Sleep Ranges](#delays--sleep-ranges)
   - [Mouse Actions](#mouse-actions)
   - [Conditional Logic (`if` & `else:`)](#conditional-logic-if--else)
   - [Loops (`repeat` & `while`)](#loops-repeat--while)
   - [Control Flow](#control-flow)
   - [Semicolons as Statement Separators](#semicolons-as-statement-separators)
5. [Visual Builder Interface](#visual-builder-interface)
   - [Adding Actions](#adding-actions)
   - [Ordering & Controls](#ordering--controls)
   - [Syncing Visual ↔ Code](#syncing-visual--code)
6. [Interactive Screen Picker](#interactive-screen-picker)
7. [Example Scripts](#example-scripts)
8. [Syntax Cheat Sheet](#macro-language-syntax-cheat-sheet)

---

## Overview & Execution Concepts

Every macro has a few core settings:

- **Name & Description** — what shows up in your macro list
- **Trigger Hotkey** — the global hotkey that starts, toggles, or runs it
- **Run Mode** — how long or how many times it runs
- **Target Scope** — whether inputs go straight to the game window in the background or get sent to whatever window is currently focused
- **Humanize** — when turned on, adds a natural ±10% random variation to all delays so the timing doesn’t look robotic

---

## Target Scopes: Background vs Foreground

| Target Scope | In Code | What it does |
| :--- | :--- | :--- |
| **ProTanki (Background)** | `target: ProTanki` | Sends Windows messages directly to the game’s window handle (`HWND`). You can alt-tab, browse, or leave the game in the background and inputs still reach it. |
| **Global (Foreground)** | `target: Global` | Uses `SendInput` to simulate real keyboard and mouse events on whatever window currently has focus on your primary monitor. |

Aliases that also work:  
- `Background` / `Game` → ProTanki  
- `Foreground` → Global  

---

## Trigger Modes

Set these in the editor header or with metadata at the top of the script:

1. **Forever (Toggle)** — `mode: Forever` (or `Toggle` / `Infinite`)  
   Press the hotkey once to start looping forever. Press it again to stop.

2. **Timed** — `mode: Timed` (or `Duration` / `Hold`)  
   Runs in a loop for a set number of seconds, then stops automatically.

3. **Once** — `mode: Once` (or `SinglePress` / `Single`)  
   Runs the whole sequence from top to bottom one time and then returns to idle.

---

## AutoScript Language Syntax

### Basic Structure & `main:`

Every script begins with a **mandatory metadata block** followed by the entry point `main:`. Block headers must end with a newline, and all executable statements must be indented inside `main:`:

```text
macro "Quick Repair"
hotkey: "None"
mode: Forever
target: ProTanki
humanize: true

main:
    press "1" for 25ms
    sleep 100ms
```

### Indentation & Block Rules

AutoScript uses strict indentation to define code blocks (similar to Python):

- **No Same-Line Statements**: Block headers (`main:`, `if ...:`, `else:`, `repeat ...:`, and `while ...:`) must end with a newline. Placing statements on the same line as a block header (e.g. `main: sleep 50ms`) is a syntax error.
- **Indented Statements Required Inside `main:`**: All executable macro instructions **must be indented** under `main:`. Any unindented statement placed after `main:` (e.g. at column 1) is outside the function body and will be rejected with an `"Unexpected statement outside 'main:'"` compiler error.
- **No Empty Blocks**: A block header cannot be empty. Every block must contain at least one indented statement.
- **Consistent Indentation**: All statements belonging to the same block level must start at the exact same indentation column. 4 spaces or 1 tab is recommended. Mixing indentation depths on sibling statements produces an `"Inconsistent indentation"` syntax error.
- **Dedents**: Moving back to an earlier column terminates the inner block and returns execution to the enclosing block level.

### Metadata Directives

Every script requires a complete metadata header at the top of the file (before `main:`). When saved, compiled, or imported, these directives configure the macro settings:

```text
macro "Overdrive & Supply Sequence"
hotkey: "Ctrl + Alt + F1"
mode: Forever
target: ProTanki
humanize: true

main:
    press "Shift" for 40ms
    sleep 150ms
    press "1" for 30ms
```

Required directives:

- `macro "Name"` — macro display name (must not be empty)
- `hotkey: "<hotkey>"` — trigger hotkey (e.g. `"F8"`, `"Ctrl + Space"`, `"Ctrl + Alt + F1"`, `"Alt + X"`, `"MiddleButton"`, or `"None"` if unassigned)
- `mode: Forever | Timed | Once` — execution trigger mode
- `target: ProTanki | Global` — target application scope (`ProTanki` sends background Windows messages; `Global` simulates foreground input)
- `humanize: true | false` — whether to apply natural ±10% timing humanization

> [!IMPORTANT]
> All 5 metadata directives are mandatory and must have explicit values (directives cannot be left blank like `mode:` or `target:`). All directives must be placed before `main:`.

### Comments & Variables

**Comments** start with `#` or `//` and can be placed on their own line or at the end of a line:
- `#` at the start of a line or after statements is always treated as a comment (e.g. `# First comment`, `#123 step`, `// note`).
- `#` is only treated as a hex color if preceded on the same line by an assignment or comparison operator (`=`, `==`, `!=`, `(`, `,`) and followed by valid hex digits.

**Variables** let you define values once at the top of your script and reuse them:

```text
var $supplyKey = "3"
var $waitTime = 200ms
var $repeatCount = 3
var $targetColor = #00FF00
var $targetX = 960
var $targetY = 540

main:
    if pixel($targetX, $targetY) == $targetColor:
        repeat $repeatCount:
            press $supplyKey for 35ms
            sleep $waitTime
```

Variable capabilities & rules:
- Supported value types: String literals (`"1"`), Numbers (`3`), Durations (`150ms`, `1.5s`), and Hex Colors (`#FF0000`, `#00FF00`).
- You can use variables for key names (`press $key`), durations (`sleep $duration`, `for $duration`), coordinates (`click left at ($x, $y)`), colors (`if pixel(...) == $targetColor:`), and loop counts (`repeat $count:`).
- **Compile-Time Validation**: Referencing any variable that was not defined (`var $name = value`) immediately produces an `"Undefined variable '$name'"` compiler error.

### Key Actions & Supported Keys

| Syntax | What it does | Example |
| :--- | :--- | :--- |
| `press "Key"` | Press and release (default 25 ms hold) | `press "1"` |
| `press "Key" for Duration` | Hold for the given time, then release | `press "Space" for 50ms` |
| `hold "Key" for Duration` | Same as press-for (defaults to 100 ms if no duration) | `hold "W" for 1.5s` |
| `key_down "Key"` | Hold the key down until you release it or the macro stops | `key_down "W"` |
| `key_up "Key"` | Release a held key | `key_up "W"` |

Supported keys:

- **Digits**: `"0"`–`"9"` and `"Num0"`–`"Num9"` / `"NumPad0"`–`"NumPad9"`
- **Letters**: `"A"`–`"Z"` (case-insensitive)
- **Punctuation & Symbols**: `"-"`, `"+"`, `"="`, `"["`, `"]"`, `";"`, `"'"` (single quote), `","`, `"."`, `"/"`, `"\"`, ``"`"`` (backtick)
- **Numpad Math**: `"*"` / `"NumPad_Multiply"`, `"+"` / `"NumPad_Add"`, `"-"` / `"NumPad_Subtract"`, `"/"` / `"NumPad_Divide"`, `"."` / `"NumPad_Decimal"`
- **Function keys**: `"F1"`–`"F24"`
- **Standard Modifiers & Controls**: `"Space"`, `"Enter"` / `"Return"`, `"Esc"` / `"Escape"`, `"Tab"`, `"Shift"`, `"Ctrl"` / `"Control"`, `"Alt"`, `"Backspace"` / `"Back"`, `"CapsLock"` / `"Caps"`
- **Dedicated Left / Right Modifiers**: `"LShift"`, `"RShift"`, `"LCtrl"`, `"RCtrl"`, `"LAlt"`, `"RAlt"`
- **Navigation & Locks**: Arrow keys (`"Up"`, `"Down"`, `"Left"`, `"Right"`), `"Delete"` / `"Del"`, `"Insert"`, `"Home"`, `"End"`, `"PageUp"` / `"PgUp"`, `"PageDown"` / `"PgDn"`, `"PrintScreen"` / `"PrtScn"`, `"ScrollLock"`, `"Pause"`, `"NumLock"`

Duration units: `ms` (milliseconds) or `s` (seconds), like `25ms`, `150ms`, `0.5s`, `2.5s`. Spaces between numbers and units (e.g. `100 ms`, `2 s`) are also supported.

### Delays & Sleep Ranges

All sleep and wait durations require an explicit unit (`ms` or `s`). For random ranges, both minimum and maximum values must include units.

| Syntax | What it does | Example |
| :--- | :--- | :--- |
| `sleep Duration` | Exact pause | `sleep 120ms` / `sleep 2s` |
| `wait Duration` | Same as sleep | `wait 0.25s` / `wait 500ms` |
| `sleep Min ~ Max` | Random pause between two values (both require units) | `sleep 80ms ~ 140ms` / `sleep 100ms~200ms` |

> [!NOTE]
> Unitless durations such as `sleep 100` or unitless ranges like `sleep 100~200` are strictly rejected by the compiler. Always specify `ms` or `s` for both values (e.g., `sleep 100ms ~ 200ms`).

### Mouse Actions

| Syntax | What it does | Example |
| :--- | :--- | :--- |
| `click button` | Click at current cursor position | `click left` |
| `click button at (X, Y)` | Move and click | `click left at (960, 540)` |
| `click button at (X, Y) for Duration` | Click and hold for given time | `click right at (500, 300) for 40ms` |
| `double_click button at (X, Y)` | Double-click at coordinates | `double_click left at (400, 250)` |
| `mouse_down button at (X, Y)` | Press and hold button | `mouse_down left at (100, 100)` |
| `mouse_up button at (X, Y)` | Release button | `mouse_up left at (200, 200)` |
| `move to (X, Y)` | Just move cursor to coordinates | `move to (1280, 720)` |

Available buttons: `left`, `right`, `middle` (also accepts string literals, e.g. `click "left"`).

### Conditional Logic (`if` & `else:`)

Check pixel colors on screen in real time with optional fallback `else:` blocks:

```text
main:
    # Check pixel with fallback
    if pixel(450, 980) == #00FF00:
        press "2" for 20ms
        sleep 50ms
    else:
        # Not green → use repair kit
        press "1" for 30ms
        sleep 500ms
```

- Operators: `==` (matches color within tolerance), `!=` (does not match color)
- Coordinates: `(X, Y)` or variables `($x, $y)`
- Colors: 6-digit hex preceded by `#` (`#FFFFFF`, `#00FF00`, `#FFAA00`) or a color variable (`$targetColor`)
- `else:` must immediately follow an `if pixel:` block at the same indentation level and must have its own indented block.

### Loops (`repeat` & `while`)

#### 1. Repeat Count Loop
Runs an indented block a fixed number of times (or count from a variable):

```text
main:
    repeat 3:
        press "2" for 20ms
        sleep 50ms
        press "3" for 20ms
        sleep 50ms
    sleep 500ms
```

#### 2. While Condition Loop
Repeats as long as a pixel condition is satisfied or loops infinitely with `while true:`:

```text
main:
    # Loop while battle flag/health pixel remains green
    while pixel(960, 970) == #00FF00:
        press "Space" for 40ms
        sleep 100ms

    # Infinite loop inside main
    while true:
        press "1" for 25ms
        sleep 1000ms
```

Loops can be nested inside other loops, `if` checks, or `else:` blocks.

### Control Flow

`stop` ends the macro immediately and releases all currently held keys or mouse buttons.

```text
main:
    if pixel(500, 500) == #FF0000:
        stop
    press "Space"
```

### Semicolons as Statement Separators

Statements may optionally end with a semicolon `;`, and multiple statements can be placed on a single line separated by semicolons:

```text
main:
    press "1"; sleep 50ms;
    press "2"; sleep 50ms;
```

---

## Visual Builder Interface

Prefer clicking instead of typing? Switch to the **Visual Builder** tab.

### Adding Actions

Toolbar buttons let you add:

- **Key Press** — tap, hold, key down, or key up with custom hold duration
- **Delay** — static pause or min–max randomized range
- **Click** — mouse clicks with coordinates and optional hold duration
- **Check Pixel** — conditional pixel color test block
- **Loop** — repeat block with iteration count
- **Pick Point/Color** — screen coordinate and color sampler

### Ordering & Controls

- **Up / Down arrows** — reorder blocks in the list
- **Delete** — remove the selected block
- **Badges** — each block shows a live summary badge (e.g. `Press '1' (hold 25ms)`)

### Syncing Visual ↔ Code

You can freely switch between the two views at any time:

- **Visual → Code**: converts your visual blocks into clean AutoScript code.
- **Code → Visual**: parses your raw script code back into visual blocks.

---

## Interactive Screen Picker

Capturing coordinates and pixel colors is quick:

1. Click **Pick Point/Color** in either the Visual Builder or Script Code toolbar.
2. A transparent overlay appears with a real-time magnifying loupe, coordinate display `(X, Y)`, and the current hex color.
3. Hover your cursor over the target spot on your screen.
4. **Left-click** or press **Space** to lock and copy the coordinates and color.
5. Press **Escape** or **right-click** to cancel.

---

## Example Scripts

### Example 1 — Basic supply cycle

Shows metadata, `press for`, and both `sleep` / `wait` with different units. Automates battle supplies in the background window.

```text
macro "Auto Supplies"
hotkey: "F1"
mode: Forever
target: ProTanki
humanize: true

main:
    press "1" for 30ms
    sleep 120ms
    press "2" for 30ms
    sleep 120ms
    press "3" for 30ms
    sleep 120ms
    press "4" for 30ms
    sleep 200ms
    press "5" for 30ms
    sleep 120ms

    wait 30s
```

---

### Example 2 — Configurable runner with variables

Define key bindings and timings once at the top so you can tweak them in one place without touching `main:`.

```text
macro "Configurable Supply Runner"
hotkey: "Ctrl + 1"
mode: Forever
target: ProTanki
humanize: false

var $repairKey = "1"
var $armorKey = "2"
var $damageKey = "3"
var $speedKey = "4"
var $tapDuration = 35ms
var $shortDelay = 150ms
var $cycleDelay = 25s

main:
    press $armorKey for $tapDuration
    sleep $shortDelay
    press $damageKey for $tapDuration
    sleep $shortDelay
    press $speedKey for $tapDuration
    sleep $shortDelay

    sleep $cycleDelay
```

---

### Example 3 — Random delay ranges

Using random delay ranges (`~`) makes your macro timing irregular and much less predictable. Both range bounds require explicit units (`ms` or `s`).

```text
macro "Stealth Supply Trigger"
hotkey: "F3"
mode: Forever
target: ProTanki
humanize: true

main:
    press "1" for 30ms
    sleep 80ms ~ 160ms

    press "2" for 30ms
    sleep 100ms ~ 220ms

    press "3" for 30ms
    wait 0.25s ~ 0.5s

    sleep 18s ~ 22s
```

---

### Example 4 — Mouse actions (Once + Global)

Navigates UI elements in the lobby. Moves the cursor, double-clicks to open battle details, and clicks to join.

```text
macro "Auto Join Red Team"
hotkey: "None"
mode: Once
target: Global
humanize: false

main:
    move to (640, 420)
    sleep 100ms
    click left at (640, 420) for 25ms
    sleep 250ms

    double_click left at (640, 420)
    sleep 500ms

    click left at (1100, 720) for 35ms
    sleep 200ms
```

---

### Example 5 — Pixel condition

Monitors a pixel on your health bar. If the pixel is no longer green (`#00FF00`), your tank took damage and it triggers the repair kit.

```text
macro "Health Watchdog"
hotkey: "F4"
mode: Forever
target: ProTanki
humanize: false

main:
    if pixel(960, 980) != #00FF00:
        press "1" for 40ms
        sleep 5000ms

    sleep 100ms
```

---

### Example 6 — Repeat loop

Lays down 5 mines in rapid succession while briefly reversing between drops.

```text
macro "Mine Field Layer"
hotkey: "F5"
mode: Once
target: ProTanki
humanize: false

main:
    repeat 5:
        press "5" for 30ms
        sleep 150ms
        key_down "S"
        sleep 200ms
        key_up "S"
        sleep 300ms
```

---

### Example 7 — All-In-One example

Combines metadata, variables, comments, conditional checks, `stop`, sustained keys, hold durations, random delays, mouse actions, and variable-count loops into one routine:

```text
macro "ProTanki Farmer Script"
hotkey: "F6"
mode: Forever
target: ProTanki
humanize: true

var $repairKey = "1"
var $armorKey = "2"
var $damageKey = "3"
var $speedKey = "4"
var $mineKey = "5"
var $supplyTap = 30ms
var $patrolCount = 2

main:
    if pixel(960, 540) == #000000:
        stop

    if pixel(960, 970) != #00FF00:
        press $repairKey for $supplyTap
        sleep 5000ms

    press $armorKey for $supplyTap
    sleep 90ms ~ 140ms
    press $damageKey for $supplyTap
    sleep 90ms ~ 140ms
    press $speedKey for $supplyTap
    sleep 150ms

    repeat $patrolCount:
        key_down "W"
        sleep 400ms
        key_down "Left"
        sleep 200ms
        key_up "Left"
        sleep 300ms
        key_up "W"

        hold "Space" for 350ms
        sleep 200ms

        key_down "S"
        sleep 250ms
        press $mineKey for $supplyTap
        sleep 200ms
        key_up "S"
        sleep 300ms

    move to (1200, 300)
    sleep 80ms
    click left at (1200, 300) for 25ms
    sleep 150ms

    wait 2.5s
```

---

## Macro Language Syntax Cheat Sheet

| Category | Statement | Valid values | Example |
| :--- | :--- | :--- | :--- |
| **Directives** *(All 5 Required)* | `macro "Name"` | Non-empty string | `macro "My Macro"` |
| | `hotkey: "<hotkey>"` | Key combo, mouse button, or `"None"` | `hotkey: "Ctrl + Alt + F1"` |
| | `mode: Mode` | `Forever`, `Toggle`, `Timed`, `Once` | `mode: Forever` |
| | `target: Target` | `ProTanki`, `Background`, `Global` | `target: ProTanki` |
| | `humanize: Bool` | `true`, `false` | `humanize: true` |
| **Variables** | `var $name = value` | String, number, duration, `#hex` | `var $key = "1"` / `var $col = #FF0000` |
| **Keys** | `press "Key"` | Letters, digits, symbols, numpad, modifiers (`+ for Duration`) | `press "1" for 30ms` / `press "-"` |
| | `hold "Key" for Duration` | Key + duration | `hold "W" for 1.5s` |
| | `key_down "Key"` | Key name | `key_down "Space"` |
| | `key_up "Key"` | Key name | `key_up "Space"` |
| **Delays** | `sleep Duration` | Explicit unit required (`ms` or `s`) | `sleep 250ms` / `sleep 2 s` |
| | `wait Duration` | Explicit unit required (`ms` or `s`) | `wait 1.5s` / `wait 500ms` |
| | `sleep Min ~ Max` | Random range (both values require units) | `sleep 80ms ~ 160ms` / `sleep 100ms~200ms` |
| **Mouse** | `click button` | `left` / `right` / `middle` | `click left` / `click "left"` |
| | `click button at (X, Y)` | + optional `for Duration` | `click left at (960, 540)` / `click at ($x, $y)` |
| | `double_click button at (X, Y)` | Coordinates or variables | `double_click left at (500, 300)` |
| | `mouse_down / mouse_up button at (X, Y)` | Press / release button | `mouse_down left at (100, 100)` |
| | `move to (X, Y)` | Coordinates or variables | `move to (1280, 720)` / `move to ($x, $y)` |
| **Conditions** | `if pixel(X, Y) == #Hex:` | 6-digit hex or `$color` variable | `if pixel(500, 500) == #FF0000:` |
| | `if pixel(X, Y) != #Hex:` | | `if pixel(960, 950) != #00FF00:` |
| | `else:` | Follows `if pixel:` block | `else:` |
| **Loops** | `repeat Count:` | Positive integer or `$count` variable | `repeat 5:` / `repeat $count:` |
| | `while pixel(X, Y) == #Hex:` | Condition loop | `while pixel(100, 100) == #00FF00:` |
| | `while true:` | Infinite loop | `while true:` |
| **Control** | `stop` | Aborts macro execution | `stop` |
| | `;` | Statement terminator / separator | `press "1"; sleep 50ms;` |
| **Comments** | `# text` or `// text` | Any descriptive text | `# Loop start` |

