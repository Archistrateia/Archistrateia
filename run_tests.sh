#!/bin/bash

# Archistrateia Test Suite
# Usage: ./run_tests.sh [phase] [--ai-output]
# 
# Phases:
#   all (default) - Run all test phases
#   nunit - Run only NUnit unit tests
#   scenes - Run only Godot scene tests  
#   ui - Run only UI integration tests
#
# Output Modes:
#   --ai-output - Optimized output for AI interpretation (JSON-like format)

PHASE=${1:-all}
AI_OUTPUT=false

# Check for AI output mode
if [[ "$1" == "--ai-output" ]] || [[ "$2" == "--ai-output" ]]; then
    AI_OUTPUT=true
    if [[ "$1" == "--ai-output" ]]; then
        PHASE=${2:-all}
    fi
fi

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
    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_1_START: NUnit Unit Tests"
    else
        echo "=== PHASE 1: NUnit Unit Tests ==="
        echo "Testing game logic, calculations, and class functionality..."
    fi
    
    # Capture output and parse results
    OUTPUT=$(/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --quit-after 20 --main-scene res://Scenes/NUnitTestScene.tscn 2>&1)
    NUNIT_PASSED=$(echo "$OUTPUT" | grep -o "Passed: [0-9]*" | cut -d' ' -f2)
    NUNIT_FAILED=$(echo "$OUTPUT" | grep -o "Failed: [0-9]*" | cut -d' ' -f2)
    NUNIT_TOTAL=$(echo "$OUTPUT" | grep -o "Total Tests: [0-9]*" | cut -d' ' -f3)
    
    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_1_RESULTS: PASSED=$NUNIT_PASSED FAILED=$NUNIT_FAILED TOTAL=$NUNIT_TOTAL"
        if [ "$NUNIT_FAILED" -eq 0 ]; then
            echo "PHASE_1_STATUS: SUCCESS"
        else
            echo "PHASE_1_STATUS: FAILED"
        fi
        echo "PHASE_1_END"
    else
        echo "$OUTPUT" | tail -10
        echo ""
    fi
}

run_scene_tests() {
    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_2_START: Godot Scene Tests"
    else
        echo "=== PHASE 2: Godot Scene Tests ==="
        echo "Testing actual scene loading, UI initialization, and scene transitions..."
    fi
    
    # Capture output and parse results
    OUTPUT=$(/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --quit-after 25 --main-scene res://Scenes/GodotSceneTestScene.tscn 2>&1)
    
    # Count test results
    SCENE_PASSED=$(echo "$OUTPUT" | grep -c "✓ PASS" || echo "0")
    SCENE_FAILED=$(echo "$OUTPUT" | grep -c "✗ FAIL" || echo "0")
    SCENE_TOTAL=$((SCENE_PASSED + SCENE_FAILED))
    
    # Extract specific issues
    UI_MANAGER_ISSUE=$(echo "$OUTPUT" | grep -c "UI Manager not found" || echo "0")
    MAP_GEN_ISSUE=$(echo "$OUTPUT" | grep -c "Map generation failed" || echo "0")
    SCENE_LOAD_ISSUE=$(echo "$OUTPUT" | grep -c "Main Scene Loads Successfully" || echo "0")
    
    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_2_RESULTS: PASSED=$SCENE_PASSED FAILED=$SCENE_FAILED TOTAL=$SCENE_TOTAL"
        echo "PHASE_2_ISSUES: UI_MANAGER_NOT_FOUND=$UI_MANAGER_ISSUE MAP_GENERATION_FAILED=$MAP_GEN_ISSUE SCENE_LOADED=$SCENE_LOAD_ISSUE"
        if [ "$SCENE_FAILED" -gt 0 ]; then
            echo "PHASE_2_STATUS: FAILED_WITH_ISSUES"
        else
            echo "PHASE_2_STATUS: SUCCESS"
        fi
        echo "PHASE_2_END"
    else
        echo "$OUTPUT" | tail -20
        echo ""
    fi
}

run_ui_tests() {
    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_3_START: UI Integration Tests"
    else
        echo "=== PHASE 3: UI Integration Tests ==="
        echo "Testing UI components, interactions, and integration..."
    fi
    
    # Capture output and parse results
    OUTPUT=$(/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --quit-after 20 --main-scene res://Scenes/UITestScene.tscn 2>&1)
    UI_PASSED=$(echo "$OUTPUT" | grep -o "Passed: [0-9]*" | cut -d' ' -f2)
    UI_FAILED=$(echo "$OUTPUT" | grep -o "Failed: [0-9]*" | cut -d' ' -f2)
    UI_TOTAL=$(echo "$OUTPUT" | grep -o "Total Tests: [0-9]*" | cut -d' ' -f3)
    
    if [ "$AI_OUTPUT" = true ]; then
        echo "PHASE_3_RESULTS: PASSED=$UI_PASSED FAILED=$UI_FAILED TOTAL=$UI_TOTAL"
        if [ "$UI_FAILED" -eq 0 ]; then
            echo "PHASE_3_STATUS: SUCCESS"
        else
            echo "PHASE_3_STATUS: FAILED"
        fi
        echo "PHASE_3_END"
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
        ;;
    "scenes")
        run_scene_tests
        if [ "$AI_OUTPUT" = true ]; then
            echo "TEST_SUITE_END"
        fi
        ;;
    "ui")
        run_ui_tests
        if [ "$AI_OUTPUT" = true ]; then
            echo "TEST_SUITE_END"
        fi
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
            if [ "$TOTAL_FAILED" -eq 0 ]; then
                echo "OVERALL_STATUS: SUCCESS"
            else
                echo "OVERALL_STATUS: FAILED_WITH_ISSUES"
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
            echo "  ./run_tests.sh --ai-output  - AI-optimized output format"
        fi
        ;;
esac 