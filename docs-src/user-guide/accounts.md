# Accounts & Users

Collectary is multi-user: each person has their own account with their own collections, and
collections can be [shared](sharing.md) between users.

## Registering

When you first run the app — or whenever you want to add another user — you create an account with:

- **Username** — used to log in.
- **Display name** — shown in the UI and on shared collections.
- **Password** — see security note below.
- **Email** (optional) — if you fill it in, it has to look like a real address (e.g.
  `you@example.com`); leave it blank to skip it entirely.

## Logging in

On launch you log in with your username and password. The logged-in user determines which
collections you see: your own, plus any that other users have shared with you.

A failed login looks and takes the same whether the username exists or not, so the sign-in screen
can't be used to fish for which usernames are registered.

## Changing your password

You can change your password from your account at any time. You'll be asked for your **current**
password first — changing it only succeeds if that matches, so someone who walks up to an unlocked
session can't silently swap your password without knowing the old one. The new password is then
re-hashed with a fresh random salt.

## Password security

!!! info "Your password is never stored in a readable form"
    Passwords are hashed with **PBKDF2-HMAC-SHA512** using a per-user random salt, and the
    iteration count and algorithm are stored alongside the hash. Collectary never stores or logs
    your plaintext password, and never stores anything reversible. Verifying a login re-runs the
    same hash and compares — the original password cannot be recovered from what's on disk.

## How users relate to collections

- Every collection has an **owner** (the user who created it).
- The owner can [share](sharing.md) a collection with other users, granting **Read** or **Write**
  access.
- Edits are attributed to the user who made them, which also feeds into [sync](sync.md) conflict
  handling.
