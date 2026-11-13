# Importing from BGG

If you keep your collection on [BoardGameGeek](https://boardgamegeek.com) (BGG), you can import it instead of adding games one by one. Both actions are on the **Settings** page (`/settings`).

## Linking your BGG username

1. Type your BGG username under **BoardGameGeek username** and choose **Save**.
2. Ludum Eligo checks with BGG that the user exists. No BGG password is needed.

| Message | Meaning |
|---|---|
| "BGG username saved." | Linked |
| "The user '*name*' was not found on BGG." | Not saved; the previous username stays linked |
| "BoardGameGeek is unavailable. Try again later." | BGG could not be reached; not saved |

To unlink, clear the field and choose **Save** ("BGG username removed."). Importing is unavailable until a username is linked again.

## Importing

Choose **Import from BGG**. The import runs in the background; you can leave the page and come back.

The import:

- includes only games marked as **owned** on BGG, and no expansions;
- adds the games that are missing from your collection;
- never removes or changes games that are already there, including games that are not on BGG;
- adds a game again if you removed it here but still own it on BGG;
- never changes anything on BGG.

Only one import runs at a time. Choosing **Import from BGG** while an import is active shows that import instead.

## Statuses

The page shows your most recent import and refreshes every 3 seconds while it is active.

| Status | Shown as |
|---|---|
| Queued | "Import queued…" |
| Running | "Importing…". BGG can take a few minutes to prepare a collection. |
| Succeeded | "Import finished: *N* games added, *M* already in your collection." |
| Failed | "Import failed: *reason*". You can start a new import. |

An import fails when:

- BGG does not return the collection, for example because it is private ("BGG did not return the collection (…). It may be private or unavailable.");
- BGG is still preparing the collection after 10 minutes;
- BGG does not return the details of the games;
- the app restarted while the import was running ("The import was interrupted. Start a new import.").

A failed import adds nothing and removes nothing.

## API

| Method | Route | Response |
|---|---|---|
| GET | `/api/profile` | `200 { bggUsername }` (`null` when none is linked) |
| PUT | `/api/profile` `{ bggUsername }` | `200 { bggUsername }`; an empty value clears it. `400` when BGG does not know the user or the name is longer than 100 characters, `503` when BGG is unavailable |
| POST | `/api/import` | `202` with the queued import; `200` with the active import if one is queued or running; `409` (`code: bgg_username_required`) when no username is linked |
| GET | `/api/import/latest` | `200 { id, status, createdAt, startedAt, finishedAt, added, alreadyPresent, error }`, or `204` when there is no import |

All routes need a signed-in user. PUT and POST need the XSRF token (see [Authentication](../technical/authentication.md#antiforgery)).
