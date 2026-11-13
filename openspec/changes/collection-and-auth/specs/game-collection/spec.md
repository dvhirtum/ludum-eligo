# Spec Delta

## Purpose

Lets each user maintain their own collection of board games from the BGG catalog, which the picking modes use as the pool of games to choose from.

## ADDED Requirements

### Requirement: View own collection
The system SHALL let a signed-in user view the games in their collection, showing for each game its name, image, player count range, and play time range, sorted by name.

#### Scenario: Collection with games
- **WHEN** a signed-in user opens their collection
- **THEN** every game in their collection is listed with its name, image, player count range, and play time range, sorted by name

#### Scenario: Empty collection
- **WHEN** a signed-in user with no games opens their collection
- **THEN** the application shows that the collection is empty and offers to search BGG or import from BGG

### Requirement: Filter collection by name
The system SHALL let a user narrow the displayed collection by typing part of a game's name.

#### Scenario: Filter matches some games
- **WHEN** a user types "ticket" in the collection filter
- **THEN** only games whose name contains "ticket", ignoring case, are shown

### Requirement: Add a game from BGG
The system SHALL let a signed-in user add a game found through BGG search to their collection. Games SHALL only be added from BGG.

#### Scenario: Game added
- **WHEN** a user adds a game from the BGG search results
- **THEN** the game appears in their collection with its catalog metadata

#### Scenario: Game already in collection
- **WHEN** a user adds a game that is already in their collection
- **THEN** the collection still contains that game exactly once and the user is told it was already there

#### Scenario: Metadata cannot be fetched
- **WHEN** a user adds a game that is not yet in the catalog and BGG is unavailable
- **THEN** the game is not added and the user is told to try again later

### Requirement: Remove a game
The system SHALL let a signed-in user remove a game from their collection. Removing a game SHALL NOT affect other users' collections or the shared catalog.

#### Scenario: Game removed
- **WHEN** a user removes a game from their collection
- **THEN** the game no longer appears in their collection

#### Scenario: Other users unaffected
- **WHEN** a user removes a game that another user also owns
- **THEN** the other user's collection still contains that game

### Requirement: Collections are private
A user's collection SHALL only be visible to and changeable by that user.

#### Scenario: Separate collections
- **WHEN** two users each add different games
- **THEN** each user sees only the games they added or imported
