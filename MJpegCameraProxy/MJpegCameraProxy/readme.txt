-------------------------
MJpeg Camera Proxy Readme
-------------------------

The service listens on port 44456 by default.  If this is a problem, run the command-line version once, and then you can change the port in "Config.cfg".

-------------------------
Installation
-------------------------

Batch files are included to help you install, uninstall, start, and stop the service.

Install_Service.bat
Uninstall_Service.bat
Start_Service.bat
Stop_Service.bat

You may need to right click and run these batch files as an administrator.

You can also test the service as a command line application by running MJpegCameraProxyCmd.exe.

-------------------------
Configuration
-------------------------

The server has a web interface for camera and user configuration.
Once the server is running, the interface can be accessed at:

 http://ip_address:44456/admin

Default user name: admin
Default password: admin

Default port number: 44456

To change the port number, see Config.cfg which is created when the server first starts.

-------------------------
Usage
-------------------------

Each camera is available in 3 ways:

1.  As a JPEG still image 

 http://ip_address:8077/image/camera_id.jpg

2.  As a continuous stream of JPEG still images (MJPEG - not supported in all browsers):

 http://ip_address:8077/image/camera_id.mjpg

3.  With an HTML interface that refreshes the image automatically:

 http://ip_address:8077/image/camera_id.cam

-------------------------
Html Files
-------------------------

If you like, you can add custom html files to the "Html" 
subdirectory.  These files are regular Html files with the 
sole exception that the first line must be a number between 
0 and 100 to indicate the permission level required to view 
the file.

Any html file with a permission level higher than 0 will 
require authentication before it can be loaded.

Html pages are served at the application root, so to 
access the included example "all.html", you would go to:

http://localhost:8077/all.html