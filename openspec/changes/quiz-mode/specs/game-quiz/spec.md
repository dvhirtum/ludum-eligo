# Spec Delta

## Purpose

Guides a user to a game from their collection by asking a few questions. Player count and time remove games that don't fit, and preference questions rank the rest, ending with a top pick and up to two runners-up.

## ADDED Requirements

### Requirement: Quiz starts with filtering questions
The quiz SHALL first ask the player count and available time questions defined by the game-filtering capability, and SHALL apply those filters before any preference question is asked.

#### Scenario: Quiz started
- **WHEN** a signed-in user with games in their collection starts the quiz
- **THEN** the first question asks how many people are playing, and the second asks how much time is available

#### Scenario: Only one game left after filtering
- **WHEN** exactly one game remains after the filtering questions
- **THEN** the quiz skips the preference questions and shows that game as the top pick

#### Scenario: No game left after filtering
- **WHEN** no game remains after the filtering questions
- **THEN** the quiz tells the user nothing fits and lets them change their filtering answers

### Requirement: Preference questions
After filtering, the quiz SHALL ask preference questions in this order, each with a "no preference" option:
- cooperative or competitive
- light, medium or heavy complexity
- what kind of game, offering up to 4 categories most common among the remaining games

#### Scenario: Cooperative or competitive asked
- **WHEN** the remaining games include both cooperative and competitive games
- **THEN** the quiz asks whether the user wants a cooperative or a competitive game

#### Scenario: Category options from remaining games
- **WHEN** the quiz asks what kind of game the user wants
- **THEN** the options are the up to 4 categories that occur most often among the remaining games, plus "no preference"

### Requirement: Preferences rank without removing
A preference answer SHALL add one point to each remaining game that matches it and SHALL NOT remove any game. "No preference" and unknown game metadata SHALL add no points.

These rules define a match:
- A game is cooperative when its mechanics include "Cooperative Game", and competitive otherwise.
- Complexity is light below weight 2.0, medium from 2.0 to below 3.0, and heavy from 3.0.
- A category answer matches games that have that category.

#### Scenario: Matching preference scores
- **WHEN** the user answers "cooperative"
- **THEN** every remaining cooperative game gains one point and competitive games remain in the ranking without gaining a point

#### Scenario: Unknown weight
- **WHEN** the user answers "heavy" and a remaining game has unknown weight
- **THEN** that game gains no point and is not removed

### Requirement: Skip questions that do not distinguish games
The quiz SHALL skip a preference question when every remaining game would receive the same points for every answer option.

#### Scenario: All games competitive
- **WHEN** none of the remaining games is cooperative
- **THEN** the cooperative or competitive question is not asked

#### Scenario: All weights unknown or in one band
- **WHEN** every remaining game falls in the same complexity band or has unknown weight
- **THEN** the complexity question is not asked

### Requirement: Stop early when the winner is decided
The quiz SHALL stop asking questions and show results once the leading game's score exceeds the second-best score by more than the number of preference questions still to be asked.

#### Scenario: Top game cannot be overtaken
- **WHEN** the leading game is 2 points ahead and only one preference question remains
- **THEN** the quiz shows the results without asking the remaining question

### Requirement: Navigate the quiz
The user SHALL be able to go back to the previous question and change the answer, and SHALL be able to restart the quiz at any time. Quiz progress SHALL be kept in the browser only, and reloading or leaving the page SHALL discard it.

#### Scenario: Going back
- **WHEN** the user goes back to the previous question and chooses a different answer
- **THEN** the remaining questions and the ranking reflect the new answer

#### Scenario: Reload discards progress
- **WHEN** the user reloads the page during the quiz
- **THEN** the quiz starts again from the first question

### Requirement: Quiz results
The quiz SHALL end by showing the highest-scoring game as the top pick and up to two next-highest games as runners-up, each with its name and image. Games with equal scores SHALL be ordered randomly.

#### Scenario: Results with runners-up
- **WHEN** the quiz ends with 5 games remaining
- **THEN** the top pick and the two next-highest-scoring games are shown

#### Scenario: Fewer than three games remain
- **WHEN** the quiz ends with 2 games remaining
- **THEN** the top pick and one runner-up are shown

#### Scenario: Ties ordered randomly
- **WHEN** several remaining games share the highest score
- **THEN** which of them is shown as the top pick varies between quizzes with the same answers

### Requirement: Confirm the pick
From the results, the user SHALL be able to confirm any shown game as the game they will play. A confirmed pick SHALL be recorded in pick history with mode "quiz". Leaving the results without confirming SHALL record nothing.

#### Scenario: Runner-up confirmed
- **WHEN** the user confirms a runner-up instead of the top pick
- **THEN** the runner-up is recorded in pick history with mode "quiz"

#### Scenario: Results left without confirming
- **WHEN** the user restarts or leaves the quiz from the results without confirming
- **THEN** nothing is added to pick history
