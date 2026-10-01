# Connect Four Match Tracker

A .NET 10 Blazor web application that lets two people play Connect Four on one device and lets authenticated users keep a private match history.

## Course requirements covered

- .NET Blazor web application
- User registration, login, and logout with ASP.NET Core cookie authentication
- Password hashing with ASP.NET Core `PasswordHasher<TUser>`
- CRUD functionality for match records
  - Create: save a finished game or add a match manually
  - Read: view private match history
  - Update: edit a saved match
  - Delete: delete a saved match
- Server-side validation plus browser validation on account forms
- Error handling with user-friendly messages and production exception handling
- Responsive design for desktop, tablet, and mobile
- Accessibility features: labels, semantic headings, focus states, live status messages, keyboard-accessible controls, and non-color piece labels
- Persistent JSON storage with per-user data isolation
- No third-party NuGet packages required

## Run locally

Requirements: .NET 10 SDK.

```powershell
dotnet restore
dotnet run
```

Open the `https://localhost:...` address printed in the terminal.

## First-use test

1. Create an account from **Create account**.
2. Play a game until a win or draw.
3. Select **Save match**.
4. Open **Match History**.
5. Edit the record, save the changes, and then test Delete.
6. Use **Add Match** to verify manual creation and validation.
7. Sign out and verify that Match History requires authentication.

## Data storage

Runtime data is stored in `App_Data/users.json` and `App_Data/matches.json`. These files are ignored by Git so passwords hashes and personal match data aren't committed.

For a cloud host with a persistent disk, set the `DataDirectory` configuration value or environment variable to the mounted data directory. Example:

```text
DataDirectory=/var/data/connectfour
```

## Quality assurance before submission

Run these checks against the deployed application:

- Lighthouse: Performance, Accessibility, Best Practices
- axe DevTools: accessibility scan
- Responsive layouts at phone, tablet, and desktop widths
- Registration/login/logout
- Unauthorized access to `/matches`
- Create/read/update/delete match records
- Win detection, draw detection, full columns, and New Game reset
- Invalid form values and duplicate usernames

Record the final Lighthouse/axe results in your project notes or Trello board.

## Deployment with Docker

A `Dockerfile` is included. Any provider that supports Docker and exposes port 8080 can run the application.

Build locally if Docker is available:

```powershell
docker build -t connect-four .
docker run --rm -p 8080:8080 connect-four
```

Then open `http://localhost:8080`.

## Project presentation outline

For a 5–7 minute group demonstration, show: target audience and purpose, responsive UI, registration/login, gameplay, automatic result saving, Match History CRUD, validation/error handling, accessibility testing, Trello/GitHub workflow, and the deployed site.
