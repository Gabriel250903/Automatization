# Automatization - ProTanki Automation Tool

Welcome to **Automatization**, a automation suite designed for *ProTanki*. This tool provides features ranging from powerup automation to Smart Health monitoring and Gold Box detection.

---

## 🚀 Getting Started

### Prerequisites
*   Windows 10 or 11 (64-bit recommended).
*   .NET 8.0 Desktop Runtime.
*   ProTanki client installed.

### Launching
1.  Open `Automatization.exe`.
2.  If the game path is not automatically detected, click **Launch** or go to **Settings** to browse for your game executable (`ProTanki.exe`).
3.  Once the game is running, the tool status will change to "Game is running!" and features will become available.

---

## 🛠 Features & Usage

### 1. Powerup Automation (1-5 Keys)
Automatically triggers powerups at regular intervals.
*   **Usage:**
    *   Use the **Main Window** to toggle individual powerups (`Repair Kit`, `Double Armor`, `Double Damage`, `Speed Boost`, `Mine`).
    *   **Sliders:** Adjust the delay (in milliseconds) between activations for each powerup.
    *   **Global Toggle Hotkey (Default: F5):** Turns ALL powerups ON or OFF instantly.
*   **Chat Safety:**
    *   The tool automatically detects when you press **Enter** to open the in-game chat.
    *   Powerups are **paused** while you type to prevent accidental activation.
    *   When you close the chat (Enter/Escape), there is a **500ms buffer** before powerups resume to ensure no keys are typed into the chat.

### 2. Smart Repair Kit
Intelligent health monitoring that uses a Repair Kit only when your health drops below a certain percentage.
*   **Access:** Click the **Smart Repair** button on the main screen.
*   **Setup (Calibration - Needed only if the color detection doesn't work as expected):**
    1.  **Pick Full Color:** Click "Pick Full Color", verify you have full health in-game, and hover your mouse over the **full** part of your health bar. Wait for the countdown.
    2.  **Pick Empty Color:** Click "Pick Empty Color", take damage, and hover over the **empty** background of the health bar.
    3.  **Apply Colors:** Click "Apply Colors" to save these settings.
*   **Configuration:**
    *   **Threshold:** Set the health percentage (e.g., 40%) at which the repair kit triggers.
    *   **Cooldown:** Set the minimum time (ms) between triggers. Recommended & Default: 5000ms to avoid issues & spamming.
    *   **Key:** Select the keybind for Repair Kit (Default: 1).

### 3. Auto Gold Box Detection
Automatically detects the "Gold Box will be dropped soon" notification and starts a timer.
*   **Mechanism:** Uses Optical Character Recognition (OCR) to read the screen for the text "Gold box will be dropped soon" and color detection for the orange notification text.
*   **Auto-Timer:** When detected, a visual **Timer Window** automatically appears on your screen, counting down 40 seconds (standard Gold Box drop time).
*   **Settings:** You can enable/disable this feature in the Settings menu. If disabled or failed to detect the text, you can still use the manual timer hotkey to spawn the timer on your screen.

### 4. Team Auto-Clicker
Automatically joins battles by clicking the Red or Blue team button repeatedly.
*   **Usage:**
    1.  Select **Click Type** (Left, Right, Middle, Double).
    2.  Click **Auto Red Team** or **Auto Blue Team**.
    3.  The tool will click the configured screen coordinates repeatedly until stopped.
*   **Configuration:** Go to **Settings** > **Pick Coordinates** to set the exact screen location of the "Join" buttons for your resolution.

---

## ⚙️ Configuration (Settings Window)

Access by clicking the **Settings** (gear) icon.

### General
*   **Game Process Name:** Name of the game window/process (Default: `ProTanki`). *Admin unlockable.*
*   **Game Executable Path:** Manually browse for the game `.exe` if auto-detection fails.
*   **Click Speed:** Global speed for the Team Auto-Clicker (ms between clicks).
*   **Smart Repair Kit FPS:** Screen capture framerate (Default: 60 fps).

### Coordinates
*   **Red/Blue Team (X, Y):** Use the **Pick** buttons to interactively select screen locations for the auto-clicker.

### Keybinds (Hotkeys)
Customize the keys used for automation.
*   **Global Toggle:** Master switch for powerups (Default: F5).
*   **Red/Blue Team:** Hotkeys to toggle clickers (Default: F6 / F7).
*   **Gold Box Timer:** Hotkey to spawn a manual timer (Default: F8).
*   **Powerup Keys:** Remap which key sends "Repair Kit", "Double Damage", etc. (Default: 1-5).
*   **Smart Repair Keys:** Hotkeys for toggling monitoring and debugging (Default: F9/F10)

**Note:** All inputs support standard keyboard keys (QWERTY keyboard layout)

### Theming
*   **Presets:** Choose between Light and Dark themes.
*   **Theme Creator:** Create your own custom look!
    *   Solid, Gradient, or Image backgrounds.
    *   Custom colors for Text, Buttons, Accents, and Windows.
    *   Saved themes appear in the dropdown list.

### Updates
*   **Check for Updates:** The tool connects to the GitHub repository to check for new releases.
*   **Auto-Update:** Downloads and installs updates automatically if available.

---

## 📝 Logs (Log Viewer)
Access by clicking **Logs** in the Settings window.
*   Displays a feed of application events.
*   Useful for verifying if features are working or debugging issues.

---

## ⚠️ Troubleshooting & Notes

*   **Admin Privileges:** Some features (like simulating input or capturing screens) require a password. To get the password, please DM Noizy on discord - noizy3345.
*   **Anti-Virus:** If the tool is blocked, add an exclusion folder for the application.
*   **Screen Scaling:** If coordinates (Auto-Clicker) or detection (Smart Repair) seem off, ensure your Windows Display Scale is set to 100% or re-calibrate your coordinates.
