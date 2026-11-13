# Spec Delta

## Purpose

Provides a shared catalog of board games sourced from BoardGameGeek (BGG), so every game has consistent metadata that collections and picking modes can rely on.

## ADDED Requirements

### Requirement: Search BGG for games
The system SHALL let a signed-in user search BGG for board games by name and SHALL return matching games with their BGG id, name, and year published.

#### Scenario: Matching games found
- **WHEN** a signed-in user searches for "Azul"
- **THEN** the results include games whose name matches "Azul", each with its BGG id, name, and year published

#### Scenario: No matches
- **WHEN** a signed-in user searches for a name that matches no BGG game
- **THEN** the system returns an empty result and the application shows that nothing was found

#### Scenario: Search term too short
- **WHEN** a signed-in user searches with fewer than 3 characters
- **THEN** the system does not query BGG and asks for a longer search term

#### Scenario: BGG unavailable during search
- **WHEN** BGG cannot be reached or rejects the request
- **THEN** the system returns an error stating that BGG is unavailable and the user can retry later

### Requirement: Catalog entries hold BGG metadata
Each catalog game SHALL be identified by its BGG id and SHALL hold its name, year published, image, minimum and maximum players, minimum and maximum play time, average weight, mechanics, and categories as provided by BGG.

#### Scenario: Game added to the catalog
- **WHEN** a game is added to the catalog from BGG
- **THEN** its catalog entry holds all listed metadata fields that BGG provides for it

#### Scenario: Missing metadata from BGG
- **WHEN** BGG provides no value for a field, such as weight for an unrated game
- **THEN** the catalog entry stores that field as unknown rather than as zero

### Requirement: Catalog is shared and cached
The catalog SHALL hold one entry per BGG game shared by all users. When a game already in the catalog is needed, the system SHALL use the cached entry instead of fetching it from BGG again.

#### Scenario: Second user adds the same game
- **WHEN** a user adds a game that is already in the catalog because another user added it
- **THEN** the existing catalog entry is used and BGG is not queried for that game

### Requirement: Catalog metadata is refreshed
The system SHALL refresh a catalog entry's metadata from BGG when the entry is older than 30 days and it is next fetched or used in an import.

#### Scenario: Stale entry refreshed
- **WHEN** a catalog entry last refreshed more than 30 days ago is used in an add or import
- **THEN** the system fetches the game's metadata from BGG and updates the entry

#### Scenario: Refresh fails
- **WHEN** refreshing a stale entry fails because BGG is unavailable
- **THEN** the existing cached metadata is kept and the operation that needed it continues

### Requirement: Respect BGG usage limits
The system SHALL identify itself to BGG with a registered application token and SHALL limit its request rate to BGG so that requests are not rejected for exceeding usage limits.

#### Scenario: Many games fetched at once
- **WHEN** the system needs metadata for many games, such as during a large import
- **THEN** it requests them in batches and spaces the requests out rather than sending them all at once
