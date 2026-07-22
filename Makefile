# Makefile for elastic-stack-installers release automation

# Configuration
PROJECT_OWNER ?= elastic
PROJECT_REPO ?= elastic-stack-installers
BASE_BRANCH ?= main

# Parse version from CURRENT_RELEASE (e.g., 9.5.0 -> 9.5)
RELEASE_BRANCH ?= $(shell echo $(CURRENT_RELEASE) | sed -E 's/^([0-9]+\.[0-9]+)\.[0-9]+$$/\1/')
PROJECT_MAJOR_VERSION ?= $(shell echo $(CURRENT_RELEASE) | cut -d. -f1)

# DRY_RUN mode - set to "true" to preview commands without executing
DRY_RUN ?= false
GIT := git

# Colors for output
GREEN := \033[0;32m
RED := \033[0;31m
YELLOW := \033[0;33m
NC := \033[0m # No Color

.PHONY: help
help:
	@echo "elastic-stack-installers release automation"
	@echo ""
	@echo "Targets:"
	@echo "  check-requirements    - Validate release requirements"
	@echo "  create-release-branch - Create and push release branch"
	@echo "  release-major-minor   - Complete major/minor release workflow"
	@echo ""
	@echo "Environment variables:"
	@echo "  CURRENT_RELEASE       - Version to release (e.g., 9.5.0)"
	@echo "  BASE_BRANCH           - Base branch (default: main)"
	@echo "  DRY_RUN               - Set to 'true' for dry-run mode (default: false)"
	@echo ""
	@echo "Example usage:"
	@echo "  make release-major-minor CURRENT_RELEASE=9.5.0"
	@echo "  make release-major-minor CURRENT_RELEASE=9.5.0 BASE_BRANCH=main"

.PHONY: check-requirements
check-requirements:
	@echo "Checking release requirements..."
	@if [ -z "$(CURRENT_RELEASE)" ]; then \
		echo "$(RED)Error: CURRENT_RELEASE not set$(NC)"; \
		echo "$(YELLOW)Usage: make release-major-minor CURRENT_RELEASE=9.5.0$(NC)"; \
		exit 1; \
	fi
	@if [ "$(PROJECT_MAJOR_VERSION)" = "8" ]; then \
		echo "$(RED)Error: 8.x releases are not supported anymore$(NC)"; \
		exit 1; \
	fi
	@echo "  $(GREEN)✓ Requirements check passed$(NC)"

.PHONY: prepare-base-branch
prepare-base-branch:
	@echo "Creating release branch $(RELEASE_BRANCH) from $(BASE_BRANCH)..."
	$(GIT) checkout --quiet $(BASE_BRANCH)
	$(GIT) pull --quiet origin $(BASE_BRANCH)

.PHONY: create-release-branch
create-release-branch: check-requirements prepare-base-branch
	@if $(GIT) ls-remote --heads origin $(RELEASE_BRANCH) | grep -q .; then \
		echo "$(YELLOW)⚠ Branch $(RELEASE_BRANCH) already exists on remote, skipping$(NC)"; \
	else \
		$(GIT) checkout --quiet -b $(RELEASE_BRANCH); \
		$(GIT) push --quiet origin $(RELEASE_BRANCH); \
		echo "  $(GREEN)✓ Created and pushed branch $(RELEASE_BRANCH)$(NC)"; \
		echo "$(GREEN)✓ Major/minor release branch created successfully$(NC)"; \
	fi

.PHONY: release-major-minor
release-major-minor: create-release-branch
