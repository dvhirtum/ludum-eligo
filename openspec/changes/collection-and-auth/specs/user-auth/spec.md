# Spec Delta

## Purpose

Lets people sign in to Ludum Eligo with their Google account so each user has their own private collection and data.

## ADDED Requirements

### Requirement: Sign in with Google
The system SHALL let a visitor sign in using a Google account. Google SHALL be the only supported identity provider. The system SHALL NOT store passwords.

#### Scenario: Successful sign-in
- **WHEN** a visitor chooses "Sign in with Google" and completes Google's consent flow
- **THEN** the visitor is signed in and returned to the application

#### Scenario: Sign-in cancelled or failed
- **WHEN** a visitor cancels Google's consent flow or Google returns an error
- **THEN** the visitor remains signed out and sees a message that sign-in did not complete

### Requirement: User created on first sign-in
The system SHALL create a user record the first time a Google account signs in, identified by the Google account's stable subject identifier, and SHALL reuse that record on later sign-ins.

#### Scenario: First sign-in creates a user
- **WHEN** a Google account that has never signed in before completes sign-in
- **THEN** a new user is created with the account's display name and an empty collection

#### Scenario: Returning user
- **WHEN** a Google account that has signed in before completes sign-in
- **THEN** the existing user and their collection are used and no new user is created

#### Scenario: Changed email or name
- **WHEN** a returning Google account signs in with a different email address or display name
- **THEN** the same user record is used and its stored display name is updated

### Requirement: Session kept in a secure cookie
The system SHALL keep a signed-in session in a cookie that is HttpOnly, Secure, and SameSite. Authentication tokens SHALL NOT be exposed to browser scripts.

#### Scenario: Session survives page reload
- **WHEN** a signed-in user reloads the application
- **THEN** the user is still signed in

#### Scenario: Tokens are not readable by scripts
- **WHEN** a script running in the application page reads cookies or storage
- **THEN** it cannot obtain the session cookie or any authentication token

### Requirement: Sign out
The system SHALL let a signed-in user sign out, which ends their session in this application.

#### Scenario: User signs out
- **WHEN** a signed-in user chooses "Sign out"
- **THEN** the session ends and later requests for user data are rejected as unauthenticated

### Requirement: Current user information
The system SHALL let the application ask who is signed in, returning the user's display name when signed in and an unauthenticated result otherwise.

#### Scenario: Signed-in user
- **WHEN** the application requests the current user while signed in
- **THEN** the system returns the user's display name

#### Scenario: Anonymous visitor
- **WHEN** the application requests the current user while not signed in
- **THEN** the system returns an unauthenticated result and the application shows the sign-in option

### Requirement: User data requires authentication
The system SHALL reject requests for user-specific data from visitors who are not signed in, and SHALL only return or change data belonging to the signed-in user.

#### Scenario: Anonymous request rejected
- **WHEN** a visitor who is not signed in requests a collection
- **THEN** the system responds with an unauthenticated error and returns no data

#### Scenario: No access to other users' data
- **WHEN** a signed-in user attempts to read or change an item that belongs to another user
- **THEN** the system responds as if the item does not exist and changes nothing
