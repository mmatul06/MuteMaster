## MuteMaster v1.0.0

First public release of MuteMaster — a lightweight Windows utility for global mic and speaker mute control.

### Features
- System-wide microphone and speaker mute via hotkeys
- Customizable always-on-top overlay with real-time mic level bars
- Push to talk mode with low-level keyboard hook
- Auto-hide overlay with OSD-style flash on toggle
- Separate mute/unmute sounds (built-in or custom)
- Snap-to-corner overlay positioning with offset controls
- Multi-monitor support
- System tray icon with live mute status
- Light and dark theme
- Import/export settings as JSON
- Autostart with Windows
- Single portable .exe — no installer needed

### Requirements
- Windows 10 or Windows 11 (64-bit)
- No additional runtime required (self-contained)

### Installation
1. Download `MuteMaster-v1.0.0-win-x64.zip`
2. Extract anywhere
3. Run `MuteMaster.exe`
```

6. Drag your `.zip` file into the **Attach binaries** section
7. Click **Publish release**

---

**Step 4 — Update your GitHub repo description**

Go to your repo → click the ⚙️ gear next to About → add:
- Description (already done earlier)
- Website: `https://3mdesignsolutions.com`
- Topics: already added earlier

Also upload the `README.md` we created to your repo root if you haven't already — it becomes the homepage of your repo.

---

**Step 5 — Optional publishing platforms**

If you want broader reach beyond GitHub:

- **winget** (Windows Package Manager) — submit via github.com/microsoft/winget-pkgs. Takes a few days to be approved. Users can then install with `winget install MuteMaster`
- **Softpedia / MajorGeeks** — free software directories with large audiences. Submit via their upload forms
- **Your website** — add a download page at 3mdesignsolutions.com linking to the GitHub release

---

**One thing to do before publishing** — update the LICENSE file copyright from `mmatul06` to your full name. You can edit it directly on GitHub. Change line 3 to:
```
Copyright (c) 2026 Muhtasim Mahbub
