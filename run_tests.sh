#!/bin/bash

# Archistrateia Test Suite
# Usage: ./run_tests.sh [phase] [--ai-output] [--show-failures-only] [--coverage] [--coverage-output path]
# 
# Phases:
#   all (default) - Run all test phases
#   nunit - Run only NUnit unit tests
#   scenes - Run only Godot scene tests  
#   ui - Run only UI integration tests
#
# Output Modes:
#   --ai-output - Optimized output for AI interpretation (JSON-like format)
#   --show-failures-only - Show detailed output only for failing tests

PHASE="all"
AI_OUTPUT=false
SHOW_FAILURES_ONLY=false
COVERAGE=false
COVERAGE_OUTPUT="TestResults/coverage/coverage.xml"
OVERALL_EXIT=0

DEFAULT_GODOT_BIN="/Applications/Godot_mono.app/Contents/MacOS/Godot"
if [ -n "${GODOT_BIN:-}" ]; then
    GODOT_BIN="$GODOT_BIN"
elif [ -x "$DEFAULT_GODOT_BIN" ]; then
    GODOT_BIN="$DEFAULT_GODOT_BIN"
elif command -v godot >/dev/null 2>&1; then
    GODOT_BIN="$(command -v godot)"
else
    GODOT_BIN="$DEFAULT_GODOT_BIN"
fi

NUNIT_PASSED=0
NUNIT_FAILED=0
NUNIT_TOTAL=0

SCENE_PASSED=0
SCENE_FAILED=0
SCENE_TOTAL=0

UI_PASSED=0
UI_FAILED=0
UI_TOTAL=0

usage() {
    echo "Usage: ./run_tests.sh [phase] [--ai-output] [--show-failures-only] [--coverage] [--coverage-output path]"
    echo "Phases: all (default), nunit, scenes, ui"
    echo "Coverage: --coverage (NUnit phase only), --coverage-output <path>"
}

extract_metric() {
    local output="$1"
    local label="$2"
    echo "$output" | sed -n "s/.*${label}[[:space:]]*\\([0-9][0-9]*\\).*/\\1/p" | tail -1
}

is_integer() {
    [[ "$1" =~ ^[0-9]+$ ]]
}

# Parse arguments
while [ $# -gt 0 ]; do
    case "$1" in
        --ai-output)
            AI_OUTPUT=true
            ;;
        --show-failures-only)
            SHOW_FAILURES_ONLY=true
            ;;
        --coverage)
            COVERAGE=true
            ;;
        --coverage-output)
            if [ $# -lt 2 ]; then
                echo "Missing value for --coverage-output" >&2
                usage
                exit 2
            fi
            COVERAGE_OUTPUT="$2"
            shift
            ;;
        nunit|scenes|ui|all)
            PHASE="$1"
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage
            exit 2
            ;;
    esac
    shift
done

ensure_godot_available() {
    if [ ! -x "$GODOT_BIN" ]; then
        if [ "$AI_OUTPUT" = true ]; then
            echo "ERROR: Godot executable not found. Set GODOT_BIN or install Godot Mono."
        else
            echo "Godot executable not found at: $GODOT_BIN"
            echo "Set GODOT_BIN to your Godot binary path."
        fi
        OVERALL_EXIT=1
        return 1
    fi
    return 0
}

if [ "$AI_OUTPUT" = true ]; then
    echo "TEST_SUITE_START"
    echo "PHASE: $PHASE"
    echo "BUILD_STATUS: STARTING"
else
    echo "=== Archistrateia Test Suite ==="
    echo "Phase: $PHASE"
    echo "Building project..."
fi

dotnet build > /dev/null 2>&1
BUILD_RESULT=$?

if [ $BUILD_RESULT -ne 0 ]; then
    if [ "$AI_OUTPUT" = true ]; then
        echo "BUILD_STATUS: FAILED"
        echo "ERROR: Compilation failed"
        echo "TEST_SUITE_END"
    else
        echo "Build failed. Please fix compilation errors first."
    fi
    exit 1
fi

if [ "$AI_OUTPUT" = true ]; then
    echo "BUILD_STATUS: SUCCESS"
    echo "TESTS_STARTING"
else
    echo "Build successful. Running tests..."
    echo ""
fi

run_nunit_tests() {
    local command_exit

    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_1_START: NUnit Unit Tests"
    else
        echo "=== PHASE 1: NUnit Unit Tests ==="
        echo "Testing game logic, calculations, and class functionality..."
    fi
    
    if ! ensure_godot_available; then
        NUNIT_PASSED=0
        NUNIT_FAILED=1
        NUNIT_TOTAL=0
        return
    fi

    # Capture output and parse results
    if [ "$COVERAGE" = true ]; then
        local coverage_dir
        coverage_dir="$(dirname "$COVERAGE_OUTPUT")"
        mkdir -p "$coverage_dir"

        if ! command -v dotnet-coverage >/dev/null 2>&1; then
            NUNIT_PASSED=0
            NUNIT_FAILED=1
            NUNIT_TOTAL=0
            OVERALL_EXIT=1
            if [ "$AI_OUTPUT" = true ]; then
                echo "PHASE_1_RESULTS: PASSED=0 FAILED=1 TOTAL=0"
                echo "PHASE_1_STATUS: ERROR"
                echo "ERROR: dotnet-coverage is required for --coverage"
                echo "PHASE_1_END"
            else
                echo "❌ Coverage requested but dotnet-coverage was not found."
                echo "Install it with: dotnet tool install --global dotnet-coverage"
                echo ""
            fi
            return
        fi

        OUTPUT=$(dotnet-coverage collect "$GODOT_BIN --headless --quit-after 20 --main-scene res://Scenes/NUnitTestScene.tscn" -f xml -o "$COVERAGE_OUTPUT" 2>&1)
    else
        OUTPUT=$("$GODOT_BIN" --headless --quit-after 20 --main-scene res://Scenes/NUnitTestScene.tscn 2>&1)
    fi
    command_exit=$?
    NUNIT_PASSED=$(extract_metric "$OUTPUT" "Passed:")
    NUNIT_FAILED=$(extract_metric "$OUTPUT" "Failed:")
    NUNIT_TOTAL=$(extract_metric "$OUTPUT" "Total Tests:")

    if [ $command_exit -ne 0 ] || ! is_integer "$NUNIT_PASSED" || ! is_integer "$NUNIT_FAILED" || ! is_integer "$NUNIT_TOTAL"; then
        NUNIT_PASSED=0
        NUNIT_FAILED=1
        NUNIT_TOTAL=0
        OVERALL_EXIT=1
        if [ "$AI_OUTPUT" = true ]; then
            echo "PHASE_1_RESULTS: PASSED=0 FAILED=1 TOTAL=0"
            echo "PHASE_1_STATUS: ERROR"
            echo "PHASE_1_END"
        else
            echo "❌ NUnit phase did not produce parseable results."
            echo "$OUTPUT" | tail -20
            echo ""
        fi
        return
    fi
    
    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_1_RESULTS: PASSED=$NUNIT_PASSED FAILED=$NUNIT_FAILED TOTAL=$NUNIT_TOTAL"
        if [ "$COVERAGE" = true ] && [ -f "$COVERAGE_OUTPUT" ]; then
            echo "PHASE_1_COVERAGE_REPORT: $COVERAGE_OUTPUT"
        fi
        if [ "$NUNIT_FAILED" -eq 0 ]; then
            echo "PHASE_1_STATUS: SUCCESS"
        else
            echo "PHASE_1_STATUS: FAILED"
            OVERALL_EXIT=1
        fi
        echo "PHASE_1_END"
    elif [ "$SHOW_FAILURES_ONLY" = true ] && [ "$NUNIT_FAILED" -gt 0 ]; then
        OVERALL_EXIT=1
        echo "=== FAILING TESTS DETAILS ==="
        echo "$OUTPUT" | grep -E "(FAIL|Failed|Exception|Error|✗)" -A 3 -B 1
        echo ""
        echo "=== SUMMARY ==="
        echo "$OUTPUT" | tail -5
        echo ""
    elif [ "$SHOW_FAILURES_ONLY" = true ]; then
        echo "✅ All NUnit tests passed - no failures to show"
        if [ "$COVERAGE" = true ] && [ -f "$COVERAGE_OUTPUT" ]; then
            echo "Coverage report: $COVERAGE_OUTPUT"
        fi
        echo ""
    else
        echo "$OUTPUT" | tail -10
        if [ "$COVERAGE" = true ] && [ -f "$COVERAGE_OUTPUT" ]; then
            echo "Coverage report: $COVERAGE_OUTPUT"
        fi
        echo ""
    fi
}

run_scene_tests() {
    local command_exit

    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_2_START: Godot Scene Tests"
    else
        echo "=== PHASE 2: Godot Scene Tests ==="
        echo "Testing actual scene loading, UI initialization, and scene transitions..."
    fi
    
    # Capture output and parse results
    if ! ensure_godot_available; then
        SCENE_PASSED=0
        SCENE_FAILED=1
        SCENE_TOTAL=0
        return
    fi

    OUTPUT=$("$GODOT_BIN" --headless --quit-after 25 --main-scene res://Scenes/GodotSceneTestScene.tscn 2>&1)
    command_exit=$?

    SCENE_PASSED=$(extract_metric "$OUTPUT" "Passed:")
    SCENE_FAILED=$(extract_metric "$OUTPUT" "Failed:")
    SCENE_TOTAL=$(extract_metric "$OUTPUT" "Total Tests:")
    
    # Extract specific issues
    UI_MANAGER_ISSUE=$(echo "$OUTPUT" | grep -c "UI Manager not found" 2>/dev/null | head -1 || echo "0")
    MAP_GEN_ISSUE=$(echo "$OUTPUT" | grep -c "Map generation failed" 2>/dev/null | head -1 || echo "0")
    SCENE_LOAD_ISSUE=$(echo "$OUTPUT" | grep -c "Main_Scene_Loads_Successfully\\|Main Scene Loads Successfully" 2>/dev/null | head -1 || echo "0")
    # Ensure we have valid numbers
    UI_MANAGER_ISSUE=${UI_MANAGER_ISSUE:-0}
    MAP_GEN_ISSUE=${MAP_GEN_ISSUE:-0}
    SCENE_LOAD_ISSUE=${SCENE_LOAD_ISSUE:-0}
    
    if [ $command_exit -ne 0 ] || ! is_integer "$SCENE_PASSED" || ! is_integer "$SCENE_FAILED" || ! is_integer "$SCENE_TOTAL"; then
        SCENE_PASSED=0
        SCENE_FAILED=1
        SCENE_TOTAL=0
        OVERALL_EXIT=1
        if [ "$AI_OUTPUT" = true ]; then
            echo "PHASE_2_RESULTS: PASSED=0 FAILED=1 TOTAL=0"
            echo "PHASE_2_ISSUES: UI_MANAGER_NOT_FOUND=$UI_MANAGER_ISSUE MAP_GENERATION_FAILED=$MAP_GEN_ISSUE SCENE_LOADED=0"
            echo "PHASE_2_STATUS: ERROR"
            echo "PHASE_2_END"
        else
            echo "❌ Scene phase did not produce parseable results."
            echo "$OUTPUT" | tail -20
            echo ""
        fi
        return
    fi

    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_2_RESULTS: PASSED=$SCENE_PASSED FAILED=$SCENE_FAILED TOTAL=$SCENE_TOTAL"
        echo "PHASE_2_ISSUES: UI_MANAGER_NOT_FOUND=$UI_MANAGER_ISSUE MAP_GENERATION_FAILED=$MAP_GEN_ISSUE SCENE_LOADED=$SCENE_LOAD_ISSUE"
        if [ "$SCENE_FAILED" -gt 0 ]; then
            echo "PHASE_2_STATUS: FAILED_WITH_ISSUES"
            OVERALL_EXIT=1
        else
            echo "PHASE_2_STATUS: SUCCESS"
        fi
        echo "PHASE_2_END"
    elif [ "$SHOW_FAILURES_ONLY" = true ] && [ "$SCENE_FAILED" -gt 0 ]; then
        OVERALL_EXIT=1
        echo "=== FAILING SCENE TESTS DETAILS ==="
        echo "$OUTPUT" | grep -E "(FAIL|Failed|Exception|Error|✗)" -A 3 -B 1
        echo ""
        echo "=== SUMMARY ==="
        echo "$OUTPUT" | tail -10
        echo ""
    elif [ "$SHOW_FAILURES_ONLY" = true ]; then
        echo "✅ All scene tests passed - no failures to show"
        echo ""
    else
        echo "$OUTPUT" | tail -20
        echo ""
    fi
}

run_ui_tests() {
    local command_exit

    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_3_START: UI Integration Tests"
    else
        echo "=== PHASE 3: UI Integration Tests ==="
        echo "Testing UI components, interactions, and integration..."
    fi
    
    # Capture output and parse results
    if ! ensure_godot_available; then
        UI_PASSED=0
        UI_FAILED=1
        UI_TOTAL=0
        return
    fi

    OUTPUT=$("$GODOT_BIN" --headless --quit-after 20 --main-scene res://Scenes/UITestScene.tscn 2>&1)
    command_exit=$?
    UI_PASSED=$(extract_metric "$OUTPUT" "Passed:")
    UI_FAILED=$(extract_metric "$OUTPUT" "Failed:")
    UI_TOTAL=$(extract_metric "$OUTPUT" "Total Tests:")

    if [ $command_exit -ne 0 ] || ! is_integer "$UI_PASSED" || ! is_integer "$UI_FAILED" || ! is_integer "$UI_TOTAL"; then
        UI_PASSED=0
        UI_FAILED=1
        UI_TOTAL=0
        OVERALL_EXIT=1
        if [ "$AI_OUTPUT" = true ]; then
            echo "PHASE_3_RESULTS: PASSED=0 FAILED=1 TOTAL=0"
            echo "PHASE_3_STATUS: ERROR"
            echo "PHASE_3_END"
        else
            echo "❌ UI phase did not produce parseable results."
            echo "$OUTPUT" | tail -20
            echo ""
        fi
        return
    fi
    
    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_3_RESULTS: PASSED=$UI_PASSED FAILED=$UI_FAILED TOTAL=$UI_TOTAL"
        if [ "$UI_FAILED" -eq 0 ]; then
            echo "PHASE_3_STATUS: SUCCESS"
        else
            echo "PHASE_3_STATUS: FAILED"
            OVERALL_EXIT=1
        fi
        echo "PHASE_3_END"
    elif [ "$SHOW_FAILURES_ONLY" = true ] && [ "$UI_FAILED" -gt 0 ]; then
        OVERALL_EXIT=1
        echo "=== FAILING UI TESTS DETAILS ==="
        echo "$OUTPUT" | grep -E "(FAIL|Failed|Exception|Error|✗)" -A 3 -B 1
        echo ""
        echo "=== SUMMARY ==="
        echo "$OUTPUT" | tail -10
        echo ""
    elif [ "$SHOW_FAILURES_ONLY" = true ]; then
        echo "✅ All UI tests passed - no failures to show"
        echo ""
    else
        echo "$OUTPUT" | tail -10
        echo ""
    fi
}

case $PHASE in
    "nunit")
        run_nunit_tests
        if [ "$AI_OUTPUT" = true ]; then
            echo "TEST_SUITE_END"
        fi
        exit $OVERALL_EXIT
        ;;
    "scenes")
        run_scene_tests
        if [ "$AI_OUTPUT" = true ]; then
            echo "TEST_SUITE_END"
        fi
        exit $OVERALL_EXIT
        ;;
    "ui")
        run_ui_tests
        if [ "$AI_OUTPUT" = true ]; then
            echo "TEST_SUITE_END"
        fi
        exit $OVERALL_EXIT
        ;;
    "all"|*)
        run_nunit_tests
        run_scene_tests
        run_ui_tests
        
        if [ "$AI_OUTPUT" = true ]; then
            # Calculate overall summary
            TOTAL_PASSED=$((NUNIT_PASSED + SCENE_PASSED + UI_PASSED))
            TOTAL_FAILED=$((NUNIT_FAILED + SCENE_FAILED + UI_FAILED))
            TOTAL_TESTS=$((NUNIT_TOTAL + SCENE_TOTAL + UI_TOTAL))
            
            echo "OVERALL_RESULTS: PASSED=$TOTAL_PASSED FAILED=$TOTAL_FAILED TOTAL=$TOTAL_TESTS"
            if [ "$TOTAL_FAILED" -eq 0 ] && [ "$OVERALL_EXIT" -eq 0 ]; then
                echo "OVERALL_STATUS: SUCCESS"
            else
                echo "OVERALL_STATUS: FAILED_WITH_ISSUES"
                OVERALL_EXIT=1
            fi
            echo "COVERAGE: NUNIT_LOGIC SCENE_INTEGRATION UI_COMPONENTS"
            echo "TEST_SUITE_END"
        else
            echo "=== Test Suite Complete ==="
            echo "All test phases completed. Check output above for results."
            echo ""
            echo "Test Coverage Summary:"
            echo "- Phase 1: Game logic, calculations, pathfinding, movement"
            echo "- Phase 2: Scene loading, UI initialization, scene transitions"
            echo "- Phase 3: UI components, interactions, scrolling, zoom controls"
            echo ""
            echo "This comprehensive test suite covers both NUnit-testable logic"
            echo "and Godot-specific scene functionality that requires the engine."
            echo ""
                        echo "To run individual phases:"
                        echo "  ./run_tests.sh nunit   - Run only NUnit tests"
                        echo "  ./run_tests.sh scenes  - Run only Godot scene tests"
                        echo "  ./run_tests.sh ui      - Run only UI integration tests"
                        echo "  ./run_tests.sh nunit --coverage --coverage-output TestResults/coverage/coverage.xml"
                        echo "  ./run_tests.sh all --ai-output  - AI-optimized output format"
                        echo "  ./run_tests.sh --show-failures-only  - Show detailed output only for failing tests"
        fi
        ;;
esac

exit $OVERALL_EXIT
