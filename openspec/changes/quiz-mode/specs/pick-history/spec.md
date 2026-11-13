# Spec Delta

## Purpose

Keeps a per-user record of the games chosen in the picking modes, so users can look back at what they picked before.

## ADDED Requirements

### Requirement: Record a confirmed pick
The system SHALL record a pick when a picking mode confirms it, storing the user, the game, the mode that produced it ("quiz" or "game-vs-game"), and the time of the pick. Only games in the user's collection SHALL be accepted.

#### Scenario: Pick recorded
- **WHEN** a user confirms "Azul" at the end of a quiz
- **THEN** a history entry for "Azul" with mode "quiz" and the current time is stored for that user

#### Scenario: Game not in collection
- **WHEN** a pick is submitted for a game that is not in the user's collection
- **THEN** the system rejects it and stores nothing

#### Scenario: Unknown mode
- **WHEN** a pick is submitted with a mode other than "quiz" or "game-vs-game"
- **THEN** the system rejects it and stores nothing

### Requirement: List previous picks
The system SHALL let a signed-in user view their own picks, newest first, each showing the game's name, image, mode, and date. The list SHALL be shown in pages of 50.

#### Scenario: History shown
- **WHEN** a user with three picks opens their history
- **THEN** all three are listed, newest first, with game name, image, mode, and date

#### Scenario: More than one page
- **WHEN** a user has 60 picks and opens their history
- **THEN** the 50 newest are shown and the user can load the remaining 10

#### Scenario: No picks yet
- **WHEN** a user with no picks opens their history
- **THEN** the page states that no games have been picked yet and links to the quiz

### Requirement: History survives collection changes
A history entry SHALL remain and keep showing its game after the game is removed from the user's collection.

#### Scenario: Picked game later removed
- **WHEN** a user removes a game from their collection that appears in their history
- **THEN** the history still lists that pick with the game's name and image

### Requirement: History is private and display only
A user's history SHALL only be visible to that user. In this version, history SHALL NOT affect which games any picking mode offers or how it ranks them.

#### Scenario: Other users cannot see history
- **WHEN** a user opens their history
- **THEN** only their own picks are listed

#### Scenario: Recent pick not penalised
- **WHEN** a user picked a game yesterday and takes the quiz again with the same answers
- **THEN** that game is scored exactly as if it had never been picked
