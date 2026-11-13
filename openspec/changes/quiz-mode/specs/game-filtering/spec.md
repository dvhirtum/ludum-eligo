# Spec Delta

## Purpose

Narrows a user's collection to the games that can actually be played right now, based on how many people are playing and how much time they have. Every picking mode uses it first.

## ADDED Requirements

### Requirement: Player count question
The system SHALL ask how many people are playing, accepting a whole number from 1 to 12, where 12 means "12 or more".

#### Scenario: Player count answered
- **WHEN** the user answers that 4 people are playing
- **THEN** the player count used for filtering is 4

#### Scenario: Out-of-range answer
- **WHEN** a player count below 1 or above 12 is submitted
- **THEN** the system rejects the answer as invalid

### Requirement: Available time question
The system SHALL ask how much time is available, with the options "up to 30 minutes", "up to 60 minutes", "up to 90 minutes", "up to 2 hours", and "no limit".

#### Scenario: Time answered
- **WHEN** the user answers "up to 60 minutes"
- **THEN** the available time used for filtering is 60 minutes

### Requirement: Filter by player count
A game SHALL remain only if the player count lies within its minimum and maximum players. A game with unknown player counts SHALL remain. A player count of "12 or more" SHALL keep games whose maximum is 12 or higher.

#### Scenario: Game supports the player count
- **WHEN** 4 players are playing and a game supports 2 to 5 players
- **THEN** the game remains

#### Scenario: Game does not support the player count
- **WHEN** 5 players are playing and a game supports 1 to 4 players
- **THEN** the game is removed

#### Scenario: Unknown player count
- **WHEN** a game has no known player range
- **THEN** the game remains regardless of the player count

### Requirement: Filter by available time
A game SHALL remain only if its minimum play time is no more than the available time. When the minimum is unknown, its maximum play time SHALL be used instead. When both are unknown, or the answer is "no limit", the game SHALL remain.

#### Scenario: Game fits the time
- **WHEN** 60 minutes are available and a game plays in 45 to 90 minutes
- **THEN** the game remains

#### Scenario: Game too long
- **WHEN** 30 minutes are available and a game's minimum play time is 60 minutes
- **THEN** the game is removed

#### Scenario: No time limit
- **WHEN** the available time is "no limit"
- **THEN** no game is removed because of its play time

### Requirement: Filtering result
The system SHALL report the games that remain after both filters are applied. Only games in the signed-in user's own collection SHALL be considered.

#### Scenario: Some games remain
- **WHEN** both filters have been applied and 7 of 40 games remain
- **THEN** the result contains exactly those 7 games from the user's collection

#### Scenario: No games remain
- **WHEN** no game in the collection passes both filters
- **THEN** the user is told that no game fits and can change their answers

#### Scenario: Empty collection
- **WHEN** a user with an empty collection starts a picking mode
- **THEN** the user is told to add games first and no questions are asked
