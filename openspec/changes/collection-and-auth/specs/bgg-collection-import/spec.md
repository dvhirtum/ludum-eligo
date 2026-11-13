# Spec Delta

## Purpose

Lets a user link their BGG username and import the games they own on BGG into their Ludum Eligo collection, without the application ever changing or removing what the user curated.

## ADDED Requirements

### Requirement: Link a BGG username
The system SHALL let a signed-in user save, change, or clear an optional BGG username on their profile. Linking a username SHALL NOT require BGG credentials.

#### Scenario: Username saved
- **WHEN** a user saves the BGG username "meeple42" and BGG confirms that user exists
- **THEN** the username is stored on the user's profile

#### Scenario: Unknown username
- **WHEN** a user saves a BGG username that BGG reports does not exist
- **THEN** the username is not saved and the user is told it was not found on BGG

#### Scenario: Username cleared
- **WHEN** a user clears their BGG username
- **THEN** the profile has no BGG username and importing is unavailable until one is saved

### Requirement: Start an import
The system SHALL let a user with a linked BGG username start an import of their BGG collection. The import SHALL run in the background, and only one import per user SHALL run at a time.

#### Scenario: Import started
- **WHEN** a user with a linked BGG username starts an import
- **THEN** the system accepts the request immediately and the import runs in the background

#### Scenario: No username linked
- **WHEN** a user without a linked BGG username tries to start an import
- **THEN** the system rejects the request and asks the user to link a BGG username

#### Scenario: Import already running
- **WHEN** a user starts an import while one of their imports is still running
- **THEN** no second import is started and the user sees the status of the running import

### Requirement: Import only owned base games
The import SHALL include only games the BGG user has marked as owned, and SHALL exclude expansions.

#### Scenario: Wishlist and expansions skipped
- **WHEN** the BGG collection contains owned games, wishlist-only games, and owned expansions
- **THEN** only the owned games that are not expansions are imported

### Requirement: Import only adds games
The import SHALL add owned BGG games that are missing from the user's collection. It SHALL NOT remove or change any game already in the collection, and SHALL NOT write anything to BGG.

#### Scenario: New games added
- **WHEN** the BGG collection contains games that are not in the user's collection
- **THEN** those games are added to the user's collection

#### Scenario: Existing games untouched
- **WHEN** the BGG collection contains games already in the user's collection
- **THEN** those games remain in the collection exactly once

#### Scenario: Games missing from BGG are kept
- **WHEN** the user's collection contains a game that is not in their BGG collection
- **THEN** the game stays in the user's collection after the import

#### Scenario: Previously removed game re-added
- **WHEN** a user removed a game locally that is still owned on BGG and runs an import again
- **THEN** the game is added back to their collection

### Requirement: Import status and result
The system SHALL let the user see the status of their most recent import (queued, running, succeeded, or failed) and, when finished, how many games were added and how many were already present.

#### Scenario: Import in progress
- **WHEN** a user checks the import status while BGG is still preparing their collection
- **THEN** the status is shown as running

#### Scenario: Import succeeded
- **WHEN** an import finishes
- **THEN** the user sees that it succeeded, the number of games added, and the number already present

#### Scenario: Import failed
- **WHEN** BGG keeps refusing or the collection cannot be retrieved within 10 minutes
- **THEN** the import is marked as failed with a reason, no partial removal has happened, and the user can start a new import

#### Scenario: Private BGG collection
- **WHEN** BGG returns an error because the user's collection is private or unavailable
- **THEN** the import is marked as failed with a message explaining that BGG did not return the collection
