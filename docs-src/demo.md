# Live Demo

The Collectary app below runs **entirely in your browser** — it is the real application compiled
to WebAssembly, not a video or a mock-up. Click around, create a collection, add a few items.

!!! warning "First load is large and slow"
    The first time you open this page the browser downloads the .NET runtime plus the rendering
    and database engines compiled to WebAssembly — tens of megabytes. Give it a moment; subsequent
    loads are cached and much faster.

!!! note "Demo data is temporary"
    The in-browser build uses **in-memory storage**, so anything you create here is wiped when you
    refresh or close the tab. It is a try-it sandbox, not a place to keep a real collection — for
    that, [install the desktop app](user-guide/getting-started.md).

<div style="position: relative; width: 100%; height: 80vh; min-height: 600px; border: 1px solid var(--md-default-fg-color--lightest); border-radius: 4px; overflow: hidden;">
  <iframe src="../app/index.html"
          title="Collectary running in WebAssembly"
          style="position: absolute; inset: 0; width: 100%; height: 100%; border: 0;"
          loading="lazy"></iframe>
</div>

[Open the demo in its own tab :material-open-in-new:](../app/index.html){ .md-button .md-button--primary target="_blank" }

## What works in the browser

The browser build is a preview of the UI. Because there is no native filesystem or server behind
it, a few things behave differently from the desktop app:

- **Storage is in-memory and non-persistent** — data resets on refresh.
- **Sync and image-on-disk features** are not meaningful in the sandbox (they need a real
  filesystem or backend).

Everything to do with browsing the UI, creating collections, defining fields, and adding items
works as it does on the desktop. See [Getting Started](user-guide/getting-started.md) for the full
experience.
