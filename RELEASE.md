# Release Guide for elastic-stack-installers

This repository uses a Makefile for minimal release automation. Unlike other Elastic projects, elastic-stack-installers does NOT have version files to update - releases only require creating a release branch.

## Quick Start

### Major/Minor Release (e.g., 9.5.0)

```bash
# Create and push release branch
make release-major-minor CURRENT_RELEASE=9.5.0
```

This will:
1. Validate release requirements (blocks 8.x minor releases)
2. Checkout and pull the latest from the base branch
3. Create release branch (e.g., `9.5` from `9.5.0`)
4. Push the branch to origin

After running the command, you'll need to manually:
1. Create branch protection rules on GitHub
2. Notify the release team in #mission-control

## Environment Variables

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `CURRENT_RELEASE` | Yes | - | Version to release (e.g., 9.5.0) |
| `BASE_BRANCH` | No | `main` | Base branch to create release from |
| `PROJECT_OWNER` | No | `elastic` | GitHub organization/owner |
| `PROJECT_REPO` | No | `elastic-stack-installers` | Repository name |
| `DRY_RUN` | No | `false` | Set to `true` for dry-run mode |

## Available Targets

### `make help`
Display usage information and available targets.

```bash
make help
```

### `make check-requirements`
Validate release requirements without creating a branch.

```bash
make check-requirements CURRENT_RELEASE=9.5.0
```

This checks:
- CURRENT_RELEASE is set
- 8.x releases are blocked (deprecated)

### `make create-release-branch`
Create and push the release branch.

```bash
make create-release-branch CURRENT_RELEASE=9.5.0
```

### `make release-major-minor`
Complete major/minor release workflow (runs all the above).

```bash
make release-major-minor CURRENT_RELEASE=9.5.0
```

## DRY_RUN Mode

Test the release workflow without making changes:

```bash
# Preview what would happen
DRY_RUN=true make release-major-minor CURRENT_RELEASE=9.5.0

# Review the dry-run output
# If everything looks good, run for real:
make release-major-minor CURRENT_RELEASE=9.5.0
```

## Testing on Your Fork

To test the release automation on your fork:

```bash
# Fork the repository first, then:
cd elastic-stack-installers

# Test with dry-run first
DRY_RUN=true make release-major-minor \
  CURRENT_RELEASE=9.5.0-test \
  PROJECT_OWNER=your-username

# If dry-run looks good, test for real
make release-major-minor \
  CURRENT_RELEASE=9.5.0-test \
  PROJECT_OWNER=your-username
```

## What This Project Does NOT Do

Unlike other Elastic projects (beats, elastic-agent, etc.), elastic-stack-installers:

- **Does NOT update version files** - No version.go, version.yml, or similar files exist
- **Does NOT update documentation** - No version-specific documentation to update
- **Does NOT create pull requests** - Release branch creation is manual
- **Does NOT have patch releases** - Only major/minor releases

Versioning is managed externally via Git tags only.

## Prerequisites

Standard Unix tools (already installed on most systems):
- `git` - For branch operations
- `make` - For running the Makefile

**No special tools required** - no hub CLI, gh CLI, Python, or Go dependencies.

## Branch Naming

Release branches are automatically named based on `CURRENT_RELEASE`:

| CURRENT_RELEASE | Release Branch |
|----------------|----------------|
| 9.5.0 | 9.5 |
| 10.0.0 | 10.0 |
| 8.19.0 | 8.19 |

The branch name is automatically parsed from the major.minor portion of the version.

## Post-Release Steps

After creating the release branch:

1. **Branch Protection**: Create branch protection rules at:
   ```
   https://github.com/elastic/elastic-stack-installers/settings/branch_protection_rules/new
   ```

2. **Notification**: Notify the release team in the #mission-control Slack channel

3. **Tagging**: Create release tags as needed (manual process)

## Troubleshooting

### Error: CURRENT_RELEASE not set
Make sure to provide the CURRENT_RELEASE variable:
```bash
make release-major-minor CURRENT_RELEASE=9.5.0
```

### Error: 8.x minor releases are not supported anymore
8.x minor releases have reached end-of-life. Only 9.x and later are supported.

### Branch already exists
If the release branch already exists:
```bash
# Check existing branches
git branch -a | grep "9.5"

# Delete local branch if needed
git branch -D 9.5

# Delete remote branch if needed (use with caution!)
git push origin --delete 9.5
```

### Permission denied when pushing
Make sure you have write access to the repository:
```bash
# Check your remote
git remote -v

# Verify you're authenticated
git ls-remote origin
```

## Getting Help

For issues with the release automation:
1. Check this RELEASE.md guide
2. Run `make help` for quick reference
3. Check the Makefile source for implementation details
4. Ask in #ingest-dev or #mission-control Slack channels

## References

- [Makefile](./Makefile) - Source code for release automation
