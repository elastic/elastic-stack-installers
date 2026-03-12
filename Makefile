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
GIT := $(if $(filter true,$(DRY_RUN)),@echo "[DRY_RUN] git",@git)

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
	@echo "  DRY_RUN              - Set to 'true' for dry-run mode (default: false)"
	@echo ""
	@echo "Example usage:"
	@echo "  make release-major-minor CURRENT_RELEASE=9.5.0"
	@echo "  DRY_RUN=true make release-major-minor CURRENT_RELEASE=9.5.0"
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
	@echo "$(GREEN)✓ Requirements check passed$(NC)"

.PHONY: create-release-branch
create-release-branch: check-requirements
	@echo "Creating release branch $(RELEASE_BRANCH) from $(BASE_BRANCH)..."
	$(GIT) checkout $(BASE_BRANCH)
	$(GIT) pull origin $(BASE_BRANCH)
	$(GIT) checkout -b $(RELEASE_BRANCH)
	@if [ "$(DRY_RUN)" = "true" ]; then \
		echo "[DRY_RUN] git push origin $(RELEASE_BRANCH)"; \
		echo "$(YELLOW)⚠ DRY_RUN mode: Branch created locally but NOT pushed to remote$(NC)"; \
	else \
		git push origin $(RELEASE_BRANCH); \
		echo "$(GREEN)✓ Created and pushed branch $(RELEASE_BRANCH)$(NC)"; \
	fi
	@echo ""
	@echo "Next steps:"
	@echo "1. Create branch protection rules at:"
	@echo "   https://github.com/$(PROJECT_OWNER)/$(PROJECT_REPO)/settings/branch_protection_rules/new"
	@echo "2. Notify release team in #mission-control"

.PHONY: release-major-minor
release-major-minor: create-release-branch
	@echo "$(GREEN)✓ Major/minor release branch created successfully$(NC)"
