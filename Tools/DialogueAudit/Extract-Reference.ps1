Add-Type -AssemblyName PresentationCore,WindowsBase
$media = New-Object System.Windows.Media.MediaPlayer
$media.ScrubbingEnabled = $true
$media.Open([uri]'C:/Users/mo_an/Videos/NVIDIA/Desktop/Desktop 2026.10.26 - 17.56.55.01.mp4')
function Pump([int]$milliseconds) {
 $until = [DateTime]::UtcNow.AddMilliseconds($milliseconds)
 while ([DateTime]::UtcNow -lt $until) {
  $frame = New-Object System.Windows.Threading.DispatcherFrame
  [System.Windows.Threading.Dispatcher]::CurrentDispatcher.BeginInvoke([System.Windows.Threading.DispatcherPriority]::Background,[System.Windows.Threading.DispatcherOperationCallback]{param($f) $f.Continue=$false; return $null},$frame) | Out-Null
  [System.Windows.Threading.Dispatcher]::PushFrame($frame)
  Start-Sleep -Milliseconds 20
 }
}
$media.Play(); Pump 1500; $media.Pause()
Write-Output "Video $($media.NaturalVideoWidth)x$($media.NaturalVideoHeight) Duration=$($media.NaturalDuration)"
foreach($seconds in @(0.5,2,4,6)) {
 $media.Position=[TimeSpan]::FromSeconds($seconds); Pump 700
 $visual=New-Object System.Windows.Media.DrawingVisual
 $ctx=$visual.RenderOpen(); $ctx.DrawVideo($media,(New-Object System.Windows.Rect(0,0,1280,720))); $ctx.Close()
 $bmp=New-Object System.Windows.Media.Imaging.RenderTargetBitmap(1280,720,96,96,[System.Windows.Media.PixelFormats]::Pbgra32)
 $bmp.Render($visual)
 $encoder=New-Object System.Windows.Media.Imaging.PngBitmapEncoder
 $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bmp))
 $stream=[IO.File]::Create((Join-Path (Get-Location) "Tools/DialogueAudit/reference-$seconds.png")); $encoder.Save($stream); $stream.Close()
}
$media.Close()

