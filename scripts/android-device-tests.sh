#!/usr/bin/env bash
# Device test pass on the connected phone with a development APK.
# Checks what the editor tests cannot: cold and warm starts, the save written when the app goes
# to the background, the process being killed there, a low-memory trim, no network, a large
# system font, a rotation request, Android's own monkey and memory.
# Every step scans logcat for crashes, ANRs and Unity exceptions.
#
# The phone's save is backed up first and put back at the end, whatever happens in between.
#
# Usage: scripts/android-device-tests.sh [apk] [monkey-events]
#   apk            default: Builds/Android/Buzzfield-dev.apk (menu Buzzfield > Android > Build Dev APK)
#   monkey-events  default 5000
# Memory (PSS) is reported after the cold start and after the monkey; set BZ_PSS_GAME_MB and
# BZ_PSS_ADS_MB to fail a step over that budget. A development build adds about 50 MB.
set -uo pipefail
export MSYS_NO_PATHCONV=1

PKG=com.yilkgames.buzzfield
ACTIVITY=com.unity3d.player.UnityPlayerGameActivity
FILES=/storage/emulated/0/Android/data/$PKG/files
SAVE=buzzfield_save.json
APK=${1:-Builds/Android/Buzzfield-dev.apk}
EVENTS=${2:-5000}
GAME_MB=${BZ_PSS_GAME_MB:-}
ADS_MB=${BZ_PSS_ADS_MB:-}
OUT=${BZ_DEVICE_OUT:-Builds/Android/device-tests}
[ -f "$APK" ] || { echo "no APK at $APK; build one first (Buzzfield > Android > Build Dev APK)" >&2; exit 1; }
adb get-state >/dev/null 2>&1 || { echo "no phone connected" >&2; exit 1; }
mkdir -p "$OUT"

failures=0
pass() { echo "PASS  $1"; }
fail() { echo "FAIL  $1"; failures=$((failures + 1)); }

launch() { adb shell am start -W -n "$PKG/$ACTIVITY" 2>/dev/null | tr -d '\r' | awk -F': ' '/TotalTime/{print $2}'; }

alive() { [ -n "$(adb shell pidof "$PKG" | tr -d '\r')" ]; }

# Total PSS in MB.
pss_mb() {
    local kb
    kb=$(adb shell dumpsys meminfo "$PKG" | tr -d '\r' | awk '/TOTAL PSS:|^ *TOTAL /{for(i=1;i<=NF;i++) if($i ~ /^[0-9]+$/){print $i; exit}}')
    [ -n "$kb" ] && echo $((kb / 1024))
}

# Seconds since the epoch of the save's last write on the phone, or 0.
save_time() { adb shell stat -c %Y "$FILES/$SAVE" 2>/dev/null | tr -d '\r' || echo 0; }

# Checks a PSS reading against a budget, when one is set.
memory() {
    local step=$1 mb=$2 budget=$3
    [ -n "$mb" ] || return
    if [ -z "$budget" ]; then echo "MEM   $step: PSS $mb MB"
    elif [ "$mb" -lt "$budget" ]; then pass "$step: PSS $mb MB"
    else fail "$step: PSS $mb MB over $budget"; fi
}

# Crashes, ANRs and Unity exceptions (not warnings that mention one in a stack frame) since the
# last logcat clear; saves the log for the step.
check_log() {
    local step=$1
    adb logcat -d > "$OUT/$step.log"
    local bad
    bad=$(grep -E "FATAL EXCEPTION|ANR in $PKG|E Unity *: [A-Za-z0-9_.]*Exception: " "$OUT/$step.log" | head -5)
    if [ -n "$bad" ]; then
        fail "$step: errors in logcat ($OUT/$step.log)"
        echo "$bad" | sed 's/^/      /'
        return 1
    fi
    return 0
}

step_done() {
    local step=$1 extra=${2:-}
    if alive; then
        check_log "$step" && pass "$step (PSS $(pss_mb) MB${extra})"
    else
        adb logcat -d > "$OUT/$step.log"
        fail "$step: process not running ($OUT/$step.log)"
    fi
    adb exec-out screencap -p > "$OUT/$step.png"
}

echo "device: $(adb shell getprop ro.product.model | tr -d '\r'), Android $(adb shell getprop ro.build.version.release | tr -d '\r')"
echo "apk: $APK"

# --- save backup: the save lives in the app's external files folder, which adb can read ---
adb shell am force-stop "$PKG"
BACKUP="$OUT/save-backup"
rm -rf "$BACKUP"
mkdir -p "$BACKUP"
had_save=0
if adb shell ls "$FILES/$SAVE" >/dev/null 2>&1; then
    adb pull "$FILES/$SAVE" "$BACKUP/" >/dev/null && adb pull "$FILES/$SAVE.bak" "$BACKUP/" >/dev/null 2>&1
    if [ -s "$BACKUP/$SAVE" ]; then
        had_save=1
        echo "save backed up to $BACKUP"
    else
        echo "the save could not be copied; stopping before anything touches it" >&2
        exit 1
    fi
fi

restore() {
    adb shell am force-stop "$PKG"
    adb shell settings put system font_scale 1.0 >/dev/null
    adb shell settings put system accelerometer_rotation 1 >/dev/null
    adb shell svc wifi enable >/dev/null 2>&1
    adb shell svc data enable >/dev/null 2>&1
    [ "$had_save" = 1 ] || return
    adb push "$BACKUP/$SAVE" "$FILES/$SAVE" >/dev/null
    if [ -f "$BACKUP/$SAVE.bak" ]; then adb push "$BACKUP/$SAVE.bak" "$FILES/$SAVE.bak" >/dev/null; else adb shell rm -f "$FILES/$SAVE.bak"; fi
    if [ "$(adb exec-out cat "$FILES/$SAVE" | md5sum)" = "$(md5sum < "$BACKUP/$SAVE")" ]; then
        echo "save restored"
    else
        echo "SAVE NOT RESTORED: put $BACKUP/$SAVE back by hand ($FILES)" >&2
    fi
}
trap restore EXIT

if ! adb install -r "$APK" >/dev/null; then
    fail "install (a build signed with another key needs an uninstall first, which drops the save)"
    exit 1
fi
pass "install"

# 1. Cold start: the process comes up and draws its first frame.
adb logcat -c
ms=$(launch)
sleep 10
echo "      first frame after ${ms:-?} ms"
step_done "1-cold-start"
memory "1-memory-game" "$(pss_mb)" "$GAME_MB"
grep -q "Failed to read Firebase options" "$OUT/1-cold-start.log" && echo "NOTE  Firebase did not start: Assets/google-services.json was missing from the build"

# 2. Going home writes the save at once (mobile never relies on quitting).
before=$(save_time)
sleep 2
adb logcat -c
adb shell input keyevent KEYCODE_HOME
sleep 3
after=$(save_time)
if [ "${after:-0}" -gt "${before:-0}" ]; then pass "2-save-on-pause"; else fail "2-save-on-pause: the save was not written when the app went home"; fi

# 3. Warm start: back to the running game.
ms=$(launch)
sleep 5
echo "      resumed after ${ms:-?} ms"
step_done "3-warm-start"

# 4. Low memory: Android asks the app to trim while it stays in front.
adb logcat -c
adb shell am send-trim-memory "$PKG" RUNNING_CRITICAL >/dev/null 2>&1
sleep 3
step_done "4-trim-memory"

# 5. Killed in the background (low memory): the save written on pause brings the game back.
adb logcat -c
adb shell input keyevent KEYCODE_HOME
sleep 3
adb shell am kill "$PKG"
sleep 2
launch >/dev/null
sleep 10
step_done "5-killed-in-background"

# 6. No network: trusted time, ads and Firebase fail, the game still runs.
adb shell am force-stop "$PKG"
adb shell svc wifi disable >/dev/null 2>&1
adb shell svc data disable >/dev/null 2>&1
sleep 3
adb logcat -c
launch >/dev/null
sleep 15
step_done "6-no-network"
adb shell svc wifi enable >/dev/null 2>&1
adb shell svc data enable >/dev/null 2>&1
sleep 5

# 7. Largest usual system font scale: the game draws its own text and should not care.
adb shell am force-stop "$PKG"
adb shell settings put system font_scale 1.3
adb logcat -c
launch >/dev/null
sleep 10
step_done "7-font-scale-1.3"
adb shell settings put system font_scale 1.0

# 8. Rotation request: the game stays portrait and keeps running.
adb logcat -c
adb shell settings put system accelerometer_rotation 0
adb shell settings put system user_rotation 1
sleep 4
step_done "8-rotation"
adb shell settings put system user_rotation 0
adb shell settings put system accelerometer_rotation 1

# 9. Android's monkey: random taps and swipes only (no system keys or app switches).
adb logcat -c
monkey_out=$(adb shell monkey -p "$PKG" -s 42 --throttle 60 \
    --pct-touch 50 --pct-motion 40 --pct-trackball 0 --pct-nav 0 --pct-majornav 0 \
    --pct-syskeys 0 --pct-appswitch 0 --pct-anyevent 0 --pct-flip 0 --pct-pinchzoom 10 \
    -v "$EVENTS" 2>&1)
echo "$monkey_out" > "$OUT/9-monkey.txt"
if echo "$monkey_out" | grep -qE "// CRASH|// NOT RESPONDING|Monkey aborted"; then
    adb logcat -d > "$OUT/9-monkey.log"
    fail "9-monkey: crash or ANR ($OUT/9-monkey.txt, $OUT/9-monkey.log)"
    echo "$monkey_out" | grep -m1 -A3 -E "// CRASH|// NOT RESPONDING" | sed 's/^/      /'
else
    # The monkey may have left the game behind an ad or the store sheet; bring it to the front.
    launch >/dev/null
    sleep 3
    step_done "9-monkey-$EVENTS-events"
fi

# 10. Memory after all of it, ads included.
memory "10-memory-after-monkey" "$(pss_mb)" "$ADS_MB"

echo
[ "$failures" = 0 ] && echo "all device steps passed" || echo "$failures step(s) failed; logs and screenshots in $OUT"
exit "$failures"
