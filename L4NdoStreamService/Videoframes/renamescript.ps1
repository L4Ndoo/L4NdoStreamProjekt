for ($index = 0 ; $index -le 299 ; $index++){
    $frame = $index + 1
    $num = 299 - $index
    $num = $num.toString()
    $num = $num.PadLeft(4, '0')
    $oldname = "colorshift_$num" + "_Frame $frame" + ".jpg"
    $newname = "colorshift_$num" + ".jpg"
    Rename-Item -Path $oldname -NewName $newname
}
Write-Host "Completed"