# Quality Assurance Checklist

Use this checklist before recording the final demonstration and submitting the project.

| ID | Test | Expected result |
|---|---|---|
| QA-01 | Register a new valid user | Account is created and user is signed in |
| QA-02 | Register the same username again | Friendly duplicate-username error is shown |
| QA-03 | Sign in with an incorrect password | Generic invalid-credentials message is shown |
| QA-04 | Open `/matches` while signed out | Sign-in-required UI is shown |
| QA-05 | Play horizontal, vertical, and diagonal wins | Correct winner is detected |
| QA-06 | Fill a column | Its drop button becomes disabled |
| QA-07 | Select New game | Board, winner, move history, and save state reset |
| QA-08 | Save a completed game while signed in | New record appears in Match History |
| QA-09 | Add a manual match with invalid move count | Validation prevents save and explains the error |
| QA-10 | Edit a saved match | Updated values are persisted |
| QA-11 | Delete a saved match | Record is removed from history |
| QA-12 | Sign out and sign in again | Saved records remain associated with the same account |
| QA-13 | Test at mobile, tablet, and desktop widths | Layout remains usable without clipped controls |
| QA-14 | Run Lighthouse Accessibility | Resolve critical accessibility findings |
| QA-15 | Run axe DevTools | Resolve critical/serious accessibility findings |

## Recommended final evidence

Record final Lighthouse and axe results in Trello or the submission notes, and capture screenshots after fixes. Also verify the deployed URL in a private/incognito browser session.
