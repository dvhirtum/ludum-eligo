# Spec Delta

## Purpose

Lets a user find the game they most want to play by repeatedly choosing between two playable games from their collection, in a knockout, until one winner remains.

## ADDED Requirements

### Requirement: Start with filtering questions
Game vs game SHALL first ask the player count and available time questions defined by the game-filtering capability. The knockout SHALL only include games that remain after those filters.

#### Scenario: Knockout over playable games
- **WHEN** a user answers 4 players and "up to 60 minutes", and 9 of their 40 games fit
- **THEN** the knockout contains exactly those 9 games

#### Scenario: No game fits
- **WHEN** no game remains after the filtering questions
- **THEN** the user is told nothing fits and can change their answers

#### Scenario: Only one game fits
- **WHEN** exactly one game remains after the filtering questions
- **THEN** no matchups are shown and that game is presented as the winner

#### Scenario: Empty collection
- **WHEN** a user with an empty collection starts game vs game
- **THEN** the user is told to add games first and no questions are asked

### Requirement: Random pairing per round
At the start of each round, the system SHALL pair the games still in the knockout in random order. With an odd number of games, one game SHALL get a free pass into the next round without a matchup. A game that had a free pass in the previous round SHALL NOT get one again when another game is available.

#### Scenario: Even number of games
- **WHEN** a round starts with 8 games
- **THEN** the round consists of 4 matchups and no free pass

#### Scenario: Odd number of games
- **WHEN** a round starts with 9 games
- **THEN** the round consists of 4 matchups and one game goes to the next round with a free pass

#### Scenario: Pairing differs between runs
- **WHEN** a user runs game vs game twice with the same answers
- **THEN** the first-round matchups can differ between the two runs

### Requirement: Choose between two games
The system SHALL show one matchup at a time as two games side by side, each with its name, image, player range, and play time. The user SHALL choose one game, which advances to the next round, and the other game SHALL leave the knockout.

#### Scenario: User picks a game
- **WHEN** the user chooses "Azul" over "Splendor"
- **THEN** "Azul" advances to the next round, "Splendor" leaves the knockout, and the next matchup is shown

### Requirement: Knockout ends with one winner
The knockout SHALL continue round by round until exactly one game remains, which SHALL be shown as the winner. For n games, the user SHALL make exactly n − 1 choices.

#### Scenario: Winner reached
- **WHEN** the user makes the final choice of the last round with two games
- **THEN** the chosen game is shown as the winner

#### Scenario: Number of choices
- **WHEN** a knockout starts with 9 games
- **THEN** the winner is shown after exactly 8 choices

### Requirement: Show progress
During the knockout the system SHALL show the current round number, the number of games still in the knockout, and how many choices remain until the winner.

#### Scenario: Progress during a round
- **WHEN** the user is in the first round of a knockout with 9 games and has made 2 choices
- **THEN** the progress shows round 1, 7 games still in, and 6 choices to go

### Requirement: Undo and restart
The user SHALL be able to undo their most recent choice, repeatedly back to the first matchup, and SHALL be able to restart. Undoing SHALL restore the previous matchup with the same two games. Restarting SHALL return to the filtering questions. Progress SHALL be kept in the browser only, and reloading or leaving the page SHALL discard it.

#### Scenario: Undo a choice
- **WHEN** the user chose "Azul" over "Splendor" and then undoes
- **THEN** the "Azul" versus "Splendor" matchup is shown again and both games are back in the knockout

#### Scenario: Undo across a round boundary
- **WHEN** the user undoes the first choice of a new round
- **THEN** the last matchup of the previous round is shown again with the same two games

#### Scenario: Reload discards progress
- **WHEN** the user reloads the page during the knockout
- **THEN** game vs game starts again from the filtering questions

### Requirement: Confirm the winner
From the winner screen, the user SHALL be able to confirm the winner as the game they will play, which SHALL record it in pick history with mode "game-vs-game". Leaving or restarting without confirming SHALL record nothing.

#### Scenario: Winner confirmed
- **WHEN** the user confirms the winner "Azul"
- **THEN** "Azul" is recorded in pick history with mode "game-vs-game"

#### Scenario: Winner not confirmed
- **WHEN** the user restarts from the winner screen without confirming
- **THEN** nothing is added to pick history
