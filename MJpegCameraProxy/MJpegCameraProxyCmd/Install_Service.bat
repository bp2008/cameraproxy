sc create MJpegCameraProxy binPath= "%~dp0MJpegCameraProxy.exe" start= auto
sc failure MJpegCameraProxy reset= 0 actions= restart/60000/restart/60000/restart/60000
pause