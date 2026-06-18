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
- **Expert** — every background, text, border, sidebar, danger, and warning colour the app uses. The
  **warning** colour is the amber used for non-blocking notes such as the "a collection with this name
  already exists" hint, kept distinct from the red danger colour.

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

Whichever you pick, *beside* never crowds the input on a small screen. As the editor gets narrow —
on a phone, or when you shrink the window — the labels move above their inputs so they stay
full-width and the form reads as a clean single column instead of a crushed side-by-side squeeze.
Widen it again and the labels slide back beside their inputs. So you can leave it on *beside* and
still get a comfortable editor everywhere.

## Audio

When the app can record and play sound (the desktop and Android apps), an **Audio** section lets you
choose which **microphone** Audio Note fields record from and which **playback device** they play back
through. Both start on **System default**, so they follow whatever your operating system is using —
pick a specific device only if you want to override that. The choice is app-wide and is used the next
time you record or play a clip; the browser build has no audio, so the section is hidden there.

On Android the lists name each device by its role — *Phone microphone*, *Phone speaker*, *Earpiece*,
wired and Bluetooth devices by their own names — and collapse the duplicate entries the platform
otherwise reports for a single built-in device.

## Language

Collectary is available in English and German. Pick a language and the UI updates immediately — no
restart.

## Sync

Settings is also where you configure syncing: the shared folder, auto-sync and its interval, and how
long deletions are remembered. See [Sync](sync.md).

## Account

A **Switch profile** button returns you to the profile screen without closing the app — the same as
the button in the top-right of the main window. See [Profiles](accounts.md).

## About

The **About** section at the bottom shows the **version** of Collectary you're running — something
like `0.1.203`. The number climbs by one with every change that lands in the project, so it's the
quickest way to tell whether you're on the latest build. If you ever report a problem, quoting this
version helps pin down exactly which build you saw it on.

## Remembered automatically

Collectary also remembers smaller things — whether the sidebar was expanded, how you sized the
panes — so the app looks the way you left it.
