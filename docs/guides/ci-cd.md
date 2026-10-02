# CI/CD

The `Unity CI` workflow separates checks that can safely run on every pull request from licensed Unity work.

| Trigger | Jobs |
| --- | --- |
| Fork pull request to `main` | Static harness checks only. This job does not receive Unity credentials. |
| Same-repository pull request to `main` | Static checks, EditMode and PlayMode tests, the full-scene Windows build, and the `forth`-only WebGL build. |
| Push to `main` or manual dispatch from any repository branch | Static checks, EditMode and PlayMode tests, the full-scene Windows build, and the `forth`-only WebGL build. |

GameCI's [Personal license setup](https://game.ci/docs/github/builder/#personal-license) requires `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD`. Its [Professional license setup](https://game.ci/docs/github/builder/#professional-license) requires `UNITY_EMAIL`, `UNITY_PASSWORD`, and `UNITY_SERIAL`. The workflow enforces that shared shape:

- `UNITY_LICENSE` for a Personal license file, or
- `UNITY_SERIAL` for a Professional license.

The workflow validates that combination before it downloads a Unity editor image. Add the secrets in the repository's Actions secrets settings; do not place them in source control. Same-repository pull requests can receive those secrets, while fork pull requests deliberately do not run Unity jobs because GitHub does not expose repository secrets to them.

The combined EditMode and PlayMode job uses GameCI's `testMode: all`, which runs both modes and combines their results. It passes its default `githubToken` to GameCI, which creates the `Test Results` GitHub check after a completed test run. The job therefore has `checks: write` in addition to read-only contents access; no other Unity job receives that permission.

The static job runs `precompletion.ps1 -SkipUnity` with an explicit project root, covering architecture, lint, content validation, and whitespace checks without relying on PowerShell's script-root inference. Each Unity job removes unused hosted-runner toolchains and prunes Docker before GameCI pulls its editor image. This is required because the editor image can exceed the runner's free Docker space. The workflow prints `df -h` after cleanup to make capacity failures diagnosable.

The Windows build still uses every enabled scene in `ProjectSettings/EditorBuildSettings.asset` and is uploaded as `qingfeng-windows-build` without additional artifact compression. The WebGL build calls `CiBuild.PerformWebGlBuild`, which builds only `Assets/Scenes/forth.unity`, and is uploaded as `qingfeng-webgl-build`. Its committed Player settings already use disabled WebGL compression and data caching, so GitHub Pages can serve the build without custom `Content-Encoding` headers; Unity's default responsive template is retained.

## Optional GitHub Pages release

Set the repository Actions variable `DEPLOY_WEBGL` to `true` only after GitHub Pages is configured to use GitHub Actions as its source. A successful trusted push to `main`, or a manual dispatch from `main`, then uploads the WebGL artifact and deploys it through the `github-pages` environment. The deployment job alone receives `pages: write` and `id-token: write`; the rest of the workflow has read-only repository access. Leave the variable unset or set it to any other value to retain artifacts without publishing a site.
