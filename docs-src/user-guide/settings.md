# Settings

The Settings screen controls how Collectary looks and behaves. Changes are saved to
`%APPDATA%\Collectary\preferences.json` and restored next time you open the app.

## Appearance

Everything here updates live — no restart.

### Theme and style

Pick a **color theme**: Light, Dark, Nord, Dracula, Solarized (light and dark), Catppuccin, Gruvbox,
One Dark, High Contrast, or Graphite — a soft shades-of-grey dark theme with a blurple accent. The
**style** dropdown sets the overall shape — Windows 11, Flat, or
Classic — which controls corners and control sizes.

### Customize colors

**Customize colors** layers your own palette on top of the chosen theme. Two modes:

- **Easy** — the five colours that matter most: accent, window background, surfaces/cards, main
  text, and sidebar. Changing the accent derives matching hover and pressed shades automatically.
- **Expert** — every background, text, border, sidebar, and danger colour the app uses.

The moment you change a colour or the accent, a small **Custom (based on …)** note appears under
the colour-theme dropdown. It's there to remind you that what you're looking at is your own tweaked
version of a built-in theme, not the theme as it ships — the dropdown still shows the theme you
started from. Built-in themes always stay pristine: if you pick a different one from the dropdown
while you have customizations, the app first asks whether to **discard** them, so a stray click can't
quietly wipe a palette you spent time on. **Reset colors** clears your tweaks and returns to the
chosen theme straight away.

### Field label position

The app-wide default for where field labels sit in the item editor: **beside**, **above**, or
**adaptive** (beside for single-column collections, above once a collection uses more). Any
collection can override this; collections left on *Inherit* follow this setting.

## Audio

When the app can record and play sound (the desktop and Android apps), an **Audio** section lets you
choose which **microphone** Audio Note fields record from and which **playback device** they play back
through. Both start on **System default**, so they follow whatever your operating system is using —
pick a specific device only if you want to override that. The choice is app-wide and is used the next
time you record or play a clip; the browser build has no audio, so the section is hidden there.

## Language

Collectary is available in English and German. Pick a language and the UI updates immediately — no
restart.

## Sync

Settings is also where you configure syncing: the shared folder, auto-sync and its interval, and how
long deletions are remembered. See [Sync](sync.md).

## Account

A **Switch profile** button returns you to the profile screen without closing the app — the same as
the button in the top-right of the main window. See [Profiles](accounts.md).

## Remembered automatically

Collectary also remembers smaller things — whether the sidebar was expanded, how you sized the
panes — so the app looks the way you left it.
