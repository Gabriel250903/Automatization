# Automatization

An automation tool and utility suite for ProTanki built with WPF (.NET 8).

---

## 🚀 Getting Started

### Prerequisites
* Windows 10 or 11 (64-bit)
* .NET 8.0 Desktop Runtime
* ProTanki client installed

### Usage
1. Run `Automatization.exe`.
2. If the game isn't detected automatically, click **Launch Game** or open **Settings** to point to `ProTanki.exe`.
3. When the game is running, the main window will show "Game is running!" and your hotkeys will become active.

---

## 🛠 Features

### 1. Macro Manager
Build and run custom macros using either the visual block builder or the script editor.
* **AutoScript Language:** Custom scripting syntax for key presses, holds, mouse clicks, delays, loops (`repeat`, `while`), and pixel checks (`if pixel(...) == #hex`).
* **Visual Builder:** Assemble actions without coding (Key Press, Delay, Mouse Click, Check Pixel, Loops).
* **Screen Picker:** Click to grab screen coordinates `(X, Y)` and pixel colors with a magnifying loupe.
* **Execution Modes:**
  * **Forever (Toggle):** Press the hotkey once to start looping, press again to stop.
  * **Timed:** Runs for a set amount of seconds, then stops.
  * **Once:** Runs the macro sequence one time.
* **Targeting:**
  * **ProTanki (Background):** Sends inputs directly to the game window without needing to Alt-Tab into it.
  * **Global (Foreground):** Sends real keyboard/mouse inputs to whatever window currently has focus.
* **Humanize:** Adds a slight random timing variation (±10%) to delays so actions don't happen at robotically identical intervals.
* **Import / Export:** Share macros with others using `.azmacro` files.
* **Docs:** Check the [AutoScript Language Guide](Macros/README.md) or click **Docs** in the Macro Manager.

---

### 2. Supplies Clicker (Keys 1-5)
Automatically spams battle supplies at custom intervals.
* **Supported Supplies:** Repair Kit (1), Double Armor (2), Double Damage (3), Speed Boost (4), and Mine (5).
* **Delays:** Set individual delay intervals for each supply in milliseconds.
* **Master Hotkey (`F5`):** Toggle all active supply clickers on or off with one key.
* **Chat Detection:** Automatically pauses when you press **Enter** to open chat so you don't type numbers into chat, then resumes a short buffer after closing.

---

### 3. Smart Repair Kit
Watches your health bar and automatically presses Repair Kit (1) when your health falls below a set threshold.
* **Calibration:**
  1. Click **Pick Full Color**, hover over your filled green health bar in-game, and let the timer lock it.
  2. Click **Pick Empty Color**, take damage in battle, and hover over the empty health bar background.
  3. Click **Apply Colors** to save.
* **Settings:**
  * **Health Threshold:** Percent (e.g. 40%) that triggers repair.
  * **Cooldown:** Delay between repairs (default: 5000ms) to avoid spamming.
  * **Toggle Hotkey:** `F9` toggles monitoring, `F10` opens the debug preview window.

---

### 4. Team Auto-Clicker
Clicks the Red or Blue team join buttons in the battle lobby so you can join full battles as soon as a spot opens.
* **Click Types:** Left, Right, Middle, or Double click.
* **Coordinates:** Use **Pick Coordinates** in Settings to set the join button location for your resolution.
* **Click Speed:** Change the interval between clicks in Settings.

---

### 5. Discount Calculator
Calculate crystal costs for garage upgrades and sales.
* Includes all hulls, turrets, protection modules, and paints with modification tiers (M0–M3).
* Configure discount percentages to see how many crystals you'll need during sales events.
* Side-by-side comparison mode to compare prices and upgrade paths between items.
* Calculates step costs, speed-up discounts, and total crystals needed.

---

### 6. Gold Box Timer
A floating countdown timer overlay for tracking gold box drops.
* Press `F8` in-game to spawn an on-screen countdown timer.
* Right-click the timer to pause or close it.
* Supports transparent background mode and multiple stacked timers.

---

## ⚙️ Settings & Customization

Access settings by clicking the gear icon on the main window.

### Hotkeys (Keyboard & Mouse)
* Supports all standard keys (`A-Z`, `0-9`, `F1-F24`, modifiers `Ctrl`, `Shift`, `Alt`) plus mouse buttons (**Middle Click**, **Mouse 4**, and **Mouse 5**).
* To unbind a key, select the hotkey box and press `Delete`.
* Check **Pause Hotkeys** on the main window to disable all shortcuts temporarily.

### Default Keybinds
| Action | Default Hotkey |
| :--- | :--- |
| **Supplies Toggle** | `F5` |
| **Auto Red Team** | `F6` |
| **Auto Blue Team** | `F7` |
| **Gold Box Timer** | `F8` |
| **Smart Repair Toggle** | `F9` |
| **Smart Repair Debug** | `F10` |
| **Supplies (1-5)** | `1`, `2`, `3`, `4`, `5` |

### Discord Rich Presence
* Shows your current game status on your Discord profile.
* You can customize the Client ID, details text, and state template in Settings.

### Localization
* Available in English (`en-US`), German (`de-DE`), Polish (`pl-PL`), Portuguese (`pt-BR`, `pt-PT`), Russian (`ru-RU`), Ukrainian (`uk-UA`), and Chinese (`zh-CN`). Switch anytime in Settings.

### Themes & Theme Creator
* Built-in Dark and Light themes.
* Includes a **Theme Creator** to customize your own accent colors, window backgrounds, and gradients.

### Updates & Logs
* **Auto-Updater:** Checks GitHub for new releases with one-click updates.
* **Log Viewer:** View live event logs for troubleshooting.

---

## ⚠️ Notes

* **DPI Scaling:** If coordinates or pixel detection look offset, make sure your Windows display scale is at 100% or re-pick coordinates at your current scaling.
* **Input Blocking:** The app filters out its own simulated inputs to avoid triggering hotkey loops.
