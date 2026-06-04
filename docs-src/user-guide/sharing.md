# Sharing Collections

Collectary is multi-user, and you can share a collection you own with other users on the same
installation (or, combined with [sync](sync.md), across devices).

## How sharing works

- Every collection has an **owner** — the user who created it.
- The owner can grant other users access to it with one of two permission levels:

| Permission | What it allows |
|---|---|
| **Read** | The user can view the collection and its items, but not change anything. |
| **Write** | The user can view *and* edit the collection's items. |

A shared collection appears in the recipient's **Home** screen alongside their own collections.

## Granting and revoking access

The owner manages a collection's shares — adding a user with Read or Write, changing the level,
or revoking access entirely. Revoking access removes the collection from that user's view.

## Attribution

When multiple users can edit a collection, each change records **who** made it. This attribution
also feeds [sync](sync.md): when the same item is changed in two places, Collectary uses the change
history to detect and surface conflicts.

## Related

- [Accounts & Users](accounts.md)
- [Sync](sync.md)
