# PR naming convention

All pull request names should follow the convention:
`<emoji> <type>: <description>`

Examples:
`✨ feat: add new control`
`📚 docs: update readme`

The table below describes the exact type - emoticon mapping.

| PR Type    | Title                   | Description                                                                                                       | Emoji |
|------------|-------------------------|-------------------------------------------------------------------------------------------------------------------| :---: |
| `feat`     | Features                | A new feature                                                                                                     |  ✨   |
| `fix`      | Bug Fixes               | A bug Fix                                                                                                         |  🐛   |
| `docs`     | Documentation           | Documentation only changes                                                                                        |  📚   |
| `refactor` | Code Refactoring        | A code change that neither fixes a bug nor adds a feature                                                         |  ♻️   |
| `test`     | Tests                   | Adding missing tests or correcting existing tests                                                                 |  🚨   |
| `config`   | Configuration           | Changes that affect configuration, build system or external dependencies (example scopes: gulp, broccoli, npm) |   🛠   |
| `ci`       | Continuous Integrations | Changes to our CI configuration files and scripts (example scopes: Travis, Circle, BrowserStack, SauceLabs)       |  🚀   |
| `chore`    | Chores                  | Other changes that don't modify src or test files                                                                 |  ⚙️   |
| `revert`   | Reverts                 | Reverts a previous commit                                                                                         |  ⏪   |
