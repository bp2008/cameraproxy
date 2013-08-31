-------------------------
MJpeg Camera Proxy Readme
-------------------------

The service listens on port 8077.  If this is a problem, change it in the source code and rebuild it.  Sorry!

For configuration instructions, see the other xxxxx-readme.txt files.

-------------------------
Installation
-------------------------

Batch files are included to help you install, uninstall, start, and stop the service.

Install_Service.bat
Uninstall_Service.bat
Start_Service.bat
Stop_Service.bat

Install the service, configure some cameras, then start the service.

You can also test the service as a command line application by running MJpegCameraProxyCmd.exe.

-------------------------
Usage
-------------------------

Each camera is available in 3 ways:

1.  As a JPEG still image 

 http://ip_address:8077/camera_id.jpg

2.  As a continuous stream of JPEG still images (MJPEG - not supported in all browsers):

 http://ip_address:8077/camera_id.mjpg

3.  With a simple interface wrapped around it:

 http://ip_address:8077/camera_id.cam


If you like, you can configure the included all.html with your own camera IDs and use it to view all the cameras at once:

 http://ip_address:8077/all.html


Again, for configuration instructions, see the other xxxxx-readme.txt files.