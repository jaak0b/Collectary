# Live Demo

The app below runs **entirely in your browser** — it's the real application compiled to WebAssembly,
not a video or mock-up. Create a collection, add a few items, click around.

!!! warning "First load is large and slow"
    The first time you open this page, the browser downloads the .NET runtime plus the rendering and
    database engines compiled to WebAssembly — tens of megabytes. Later loads are cached.

!!! note "Demo data is temporary"
    The browser build uses in-memory storage, so anything you create is wiped on refresh. For a real
    collection, [install the desktop app](user-guide/getting-started.md).

<div style="position: relative; width: 100%; height: 80vh; min-height: 600px; border: 1px solid var(--md-default-fg-color--lightest); border-radius: 4px; overflow: hidden;">
  <iframe src="../app/index.html"
          title="Collectary running in WebAssembly"
          style="position: absolute; inset: 0; width: 100%; height: 100%; border: 0;"
          loading="lazy"></iframe>
</div>

[Open the demo in its own tab :material-open-in-new:](../app/index.html){ .md-button .md-button--primary target="_blank" }

## What works in the browser

Browsing the UI, creating collections, defining fields, and adding items all work as on the desktop.
Because there's no native filesystem or server, two things differ:

- **Storage is in-memory** — data resets on refresh.
- **Sync and image-on-disk features** aren't meaningful in the sandbox.

See [Getting Started](user-guide/getting-started.md) for the full experience.
