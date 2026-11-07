# make_sfx.ps1
# Synthesizes placeholder PawStorm sound effects as 16-bit PCM mono 22050 Hz WAV files.
# Output folder: <project root>\Audio  (this script lives in <project root>\Docs\tools)
# Run: powershell -NoProfile -ExecutionPolicy Bypass -File Docs\tools\make_sfx.ps1

$ErrorActionPreference = 'Stop'
[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::InvariantCulture
$Inv = [System.Globalization.CultureInfo]::InvariantCulture

$SampleRate = 22050
$PeakLimit  = 0.6
$TwoPi      = 2.0 * [Math]::PI
$Rng        = New-Object System.Random 4242
$OutDir     = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'Audio'

# Generic oscillator. $Freq and $Env are scriptblocks taking t (seconds) and returning Hz / 0..1 envelope.
function Synth {
    param(
        [double]$Seconds,
        [scriptblock]$Freq,
        [scriptblock]$Env,
        [string]$Wave = 'sine',
        [double]$Amp = 0.5,
        [double]$NoiseAmt = 0.0
    )
    $n = [int][Math]::Round($Seconds * $SampleRate)
    $out = [double[]]::new($n)
    $phase = 0.0
    for ($i = 0; $i -lt $n; $i++) {
        $t = $i / $SampleRate
        $f = [double](& $Freq $t)
        $phase += $TwoPi * $f / $SampleRate
        if ($Wave -eq 'square') {
            $osc = 1.0
            if ([Math]::Sin($phase) -lt 0) { $osc = -1.0 }
        } else {
            $osc = [Math]::Sin($phase)
        }
        if ($NoiseAmt -gt 0) {
            $osc = $osc * (1.0 - $NoiseAmt) + $NoiseAmt * ($Rng.NextDouble() * 2.0 - 1.0)
        }
        $e = [double](& $Env $t)
        $out[$i] = $Amp * $e * $osc
    }
    return ,$out
}

# Short fade in/out to avoid clicks, then clamp peak to $PeakLimit.
function Finish-Samples {
    param([double[]]$S)
    $n = $S.Length
    $fadeIn  = [int](0.004 * $SampleRate)
    $fadeOut = [int](0.008 * $SampleRate)
    for ($i = 0; $i -lt [Math]::Min($fadeIn, $n); $i++) { $S[$i] = $S[$i] * ($i / $fadeIn) }
    for ($i = 0; $i -lt [Math]::Min($fadeOut, $n); $i++) {
        $j = $n - 1 - $i
        $S[$j] = $S[$j] * ($i / $fadeOut)
    }
    $peak = 0.0
    foreach ($v in $S) { if ([Math]::Abs($v) -gt $peak) { $peak = [Math]::Abs($v) } }
    if ($peak -gt $PeakLimit) {
        $k = $PeakLimit / $peak
        for ($i = 0; $i -lt $n; $i++) { $S[$i] = $S[$i] * $k }
    }
    return ,$S
}

# Writes a canonical 44-byte RIFF/WAVE header followed by 16-bit little-endian PCM data.
function Write-Wav {
    param([string]$Path, [double[]]$Samples)
    $dataBytes = $Samples.Length * 2
    $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    try {
        $bw = New-Object System.IO.BinaryWriter($fs)
        $bw.Write([System.Text.Encoding]::ASCII.GetBytes('RIFF'))
        $bw.Write([int](36 + $dataBytes))
        $bw.Write([System.Text.Encoding]::ASCII.GetBytes('WAVE'))
        $bw.Write([System.Text.Encoding]::ASCII.GetBytes('fmt '))
        $bw.Write([int]16)                   # fmt chunk size (PCM)
        $bw.Write([int16]1)                  # audio format: PCM
        $bw.Write([int16]1)                  # channels: mono
        $bw.Write([int]$SampleRate)
        $bw.Write([int]($SampleRate * 2))    # byte rate
        $bw.Write([int16]2)                  # block align
        $bw.Write([int16]16)                 # bits per sample
        $bw.Write([System.Text.Encoding]::ASCII.GetBytes('data'))
        $bw.Write([int]$dataBytes)
        foreach ($s in $Samples) {
            $v = [Math]::Round($s * 32767.0)
            if ($v -gt 32767) { $v = 32767 }
            if ($v -lt -32768) { $v = -32768 }
            $bw.Write([int16]$v)
        }
        $bw.Flush()
    } finally {
        $fs.Close()
    }
}

# --- Effects ---------------------------------------------------------------

# fish: 0.15s bright two-tone blip, 880 -> 1320 Hz
function Make-Fish {
    return (Synth -Seconds 0.15 -Freq { param($t) if ($t -lt 0.075) { 880.0 } else { 1320.0 } } -Env { param($t) [Math]::Exp(-$t * 18.0) } -Amp 0.5)
}

# jump: 0.2s sine sweep 300 -> 600 Hz, fade out
function Make-Jump {
    return (Synth -Seconds 0.2 -Freq { param($t) 300.0 + 1500.0 * $t } -Env { param($t) 1.0 - $t / 0.2 } -Amp 0.5)
}

# spin: 0.5s rising sweep 150 -> 700 Hz with slight noise
function Make-Spin {
    return (Synth -Seconds 0.5 -Freq { param($t) 150.0 + 1100.0 * $t } -Env { param($t) [Math]::Min(1.0, $t / 0.03) * (1.0 - $t / 0.5) } -Amp 0.4 -NoiseAmt 0.15)
}

# pounce: 0.25s fast falling-then-rising chirp (900 -> 300 in 0.10s, then 300 -> 1000 in 0.15s)
function Make-Pounce {
    return (Synth -Seconds 0.25 -Freq { param($t) if ($t -lt 0.10) { 900.0 - 6000.0 * $t } else { 300.0 + 4666.67 * ($t - 0.10) } } -Env { param($t) [Math]::Min(1.0, $t / 0.005) * (1.0 - $t / 0.25) } -Amp 0.5)
}

# hurt: 0.35s falling square sweep 500 -> 120 Hz, low volume
function Make-Hurt {
    return (Synth -Seconds 0.35 -Freq { param($t) 500.0 - 1085.71 * $t } -Env { param($t) [Math]::Min(1.0, $t / 0.005) * (1.0 - $t / 0.35) } -Wave 'square' -Amp 0.2)
}

# hit: 0.2s noise burst with exponential decay
function Make-Hit {
    $n = [int][Math]::Round(0.2 * $SampleRate)
    $out = [double[]]::new($n)
    for ($i = 0; $i -lt $n; $i++) {
        $t = $i / $SampleRate
        $out[$i] = 0.55 * ($Rng.NextDouble() * 2.0 - 1.0) * [Math]::Exp(-$t * 25.0)
    }
    return ,$out
}

# checkpoint: 0.4s three ascending notes 523, 659, 784 Hz (each ~0.1333s)
function Make-Checkpoint {
    return (Synth -Seconds 0.4 `
        -Freq { param($t) if ($t -lt 0.133333) { 523.0 } elseif ($t -lt 0.266667) { 659.0 } else { 784.0 } } `
        -Env { param($t) $tl = $t % 0.133333; [Math]::Min(1.0, $tl / 0.005) * [Math]::Max(0.0, 1.0 - $tl / 0.133333) * [Math]::Exp(-$tl * 6.0) } `
        -Amp 0.4)
}

# core: 0.9s arpeggio 523, 659, 784, 1047 Hz (each 0.225s) with decay
function Make-Core {
    return (Synth -Seconds 0.9 `
        -Freq { param($t) if ($t -lt 0.225) { 523.0 } elseif ($t -lt 0.45) { 659.0 } elseif ($t -lt 0.675) { 784.0 } else { 1047.0 } } `
        -Env { param($t) $tl = $t % 0.225; [Math]::Min(1.0, $tl / 0.004) * [Math]::Max(0.0, 1.0 - $tl / 0.225) * [Math]::Exp(-$tl * 7.0) } `
        -Amp 0.45)
}

# --- Main ------------------------------------------------------------------

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$effects = @(
    @{ Name = 'fish.wav';       Make = { Make-Fish } },
    @{ Name = 'jump.wav';       Make = { Make-Jump } },
    @{ Name = 'spin.wav';       Make = { Make-Spin } },
    @{ Name = 'pounce.wav';     Make = { Make-Pounce } },
    @{ Name = 'hurt.wav';       Make = { Make-Hurt } },
    @{ Name = 'hit.wav';        Make = { Make-Hit } },
    @{ Name = 'checkpoint.wav'; Make = { Make-Checkpoint } },
    @{ Name = 'core.wav';       Make = { Make-Core } }
)

foreach ($fx in $effects) {
    $samples = Finish-Samples -S ([double[]](& $fx.Make))
    $path = Join-Path $OutDir $fx.Name
    Write-Wav -Path $path -Samples $samples
    $peak = 0.0
    foreach ($v in $samples) { if ([Math]::Abs($v) -gt $peak) { $peak = [Math]::Abs($v) } }
    Write-Output ("Wrote {0} samples={1} peak={2}" -f $path, $samples.Length.ToString($Inv), $peak.ToString('0.000', $Inv))
}

# Verify
foreach ($fx in $effects) {
    $p = Join-Path $OutDir $fx.Name
    if (-not (Test-Path -LiteralPath $p)) { throw "Missing output: $p" }
    $len = (Get-Item -LiteralPath $p).Length
    if ($len -le 1024) { throw "Output too small: $p ($len bytes)" }
    Write-Output ("OK {0} {1} bytes" -f $fx.Name, $len.ToString($Inv))
}
