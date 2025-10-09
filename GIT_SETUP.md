# Git Setup Instructions

## Initial Setup

Follow these steps to push your project to GitHub:

### 1. Initialize Git Repository

Open a terminal in the project root directory and run:

```bash
cd C:\Users\aaron\source\repos\ArmaServerManager
git init
```

### 2. Add Remote Repository

```bash
git remote add origin https://github.com/itskempf/ArmaServerManager.git
```

### 3. Stage All Files

```bash
git add .
```

### 4. Create Initial Commit

```bash
git commit -m "Initial commit: ArmaServerManager v1.0.0

- Complete core services implementation
- WinUI 3 user interface
- SteamCMD integration
- Server management
- Mod management
- Preset system
- Monitoring and alerts
- Backup and restore
- Task scheduling
- Plugin system"
```

### 5. Push to GitHub

```bash
git branch -M main
git push -u origin main
```

## Verify Upload

After pushing, verify at: https://github.com/itskempf/ArmaServerManager

## Future Commits

For future changes:

```bash
# Check status
git status

# Stage changes
git add .

# Commit with message
git commit -m "Your commit message"

# Push to GitHub
git push
```

## Branch Strategy

### Main Branch
- Production-ready code
- Stable releases
- Protected branch

### Develop Branch (Optional)
```bash
git checkout -b develop
git push -u origin develop
```

### Feature Branches
```bash
git checkout -b feature/feature-name
# Make changes
git add .
git commit -m "Add: Feature description"
git push -u origin feature/feature-name
# Create pull request on GitHub
```

## Useful Commands

```bash
# View commit history
git log --oneline

# View remote URL
git remote -v

# Pull latest changes
git pull

# Create and switch to new branch
git checkout -b branch-name

# Switch branches
git checkout branch-name

# Delete local branch
git branch -d branch-name

# Delete remote branch
git push origin --delete branch-name

# Undo last commit (keep changes)
git reset --soft HEAD~1

# Discard all local changes
git reset --hard HEAD

# View differences
git diff
```

## Troubleshooting

### Authentication Issues

If you encounter authentication issues:

1. **Use Personal Access Token (PAT)**
   - Go to GitHub Settings → Developer settings → Personal access tokens
   - Generate new token with `repo` scope
   - Use token as password when prompted

2. **Configure Git Credentials**
   ```bash
   git config --global user.name "Your Name"
   git config --global user.email "your.email@example.com"
   ```

### Large Files

If you have large files (>100MB):

```bash
# Install Git LFS
git lfs install

# Track large files
git lfs track "*.zip"
git lfs track "*.exe"

# Add .gitattributes
git add .gitattributes
git commit -m "Add Git LFS tracking"
```

### Merge Conflicts

If you encounter merge conflicts:

```bash
# Pull latest changes
git pull

# Resolve conflicts in files
# Edit files to resolve conflicts

# Stage resolved files
git add .

# Complete merge
git commit -m "Resolve merge conflicts"

# Push changes
git push
```

## GitHub Repository Settings

### Recommended Settings

1. **Branch Protection** (Settings → Branches)
   - Protect `main` branch
   - Require pull request reviews
   - Require status checks to pass

2. **Issues** (Settings → Features)
   - Enable Issues
   - Use issue templates

3. **Actions** (Settings → Actions)
   - Allow all actions
   - Enable workflow permissions

4. **Pages** (Settings → Pages)
   - Optional: Enable for documentation

### Labels

Create these labels for issues:
- `bug` - Something isn't working
- `enhancement` - New feature or request
- `documentation` - Documentation improvements
- `good first issue` - Good for newcomers
- `help wanted` - Extra attention needed
- `question` - Further information requested

## Release Process

### Creating a Release

1. **Tag the version**
   ```bash
   git tag -a v1.0.0 -m "Release v1.0.0"
   git push origin v1.0.0
   ```

2. **Create Release on GitHub**
   - Go to Releases → Draft a new release
   - Select the tag
   - Add release notes
   - Attach compiled binaries
   - Publish release

### Version Numbering

Follow Semantic Versioning (SemVer):
- `MAJOR.MINOR.PATCH`
- Example: `1.0.0`
  - MAJOR: Breaking changes
  - MINOR: New features (backward compatible)
  - PATCH: Bug fixes

## Backup

Always keep a local backup before major operations:

```bash
# Create backup branch
git checkout -b backup-$(date +%Y%m%d)
git push -u origin backup-$(date +%Y%m%d)
```

---

**Ready to push to GitHub!** 🚀

Run the commands in order from step 1-5 above.
