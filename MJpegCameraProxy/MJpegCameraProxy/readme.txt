-------------------------
MJpeg Camera Proxy Readme
-------------------------

The service listens on port 44456 by default.  If this is a problem, run the command-line version once, and then you can change the port in "Config.cfg".

The service also uses web sockets for Dahua and Hikvision PTZ cameras now.  This port is 44454 by default.

The service can optionally use https and wss (web socket secure).  Just configure their ports in Config.cfg and restart the service.

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

 http://ip_address:44456/image/camera_id.jpg

2.  As a continuous stream of JPEG still images (MJPEG - not supported in all browsers):

 http://ip_address:44456/image/camera_id.mjpg

3.  With an HTML interface that refreshes the image automatically:

 http://ip_address:44456/image/camera_id.cam

-------------------------
Html Files
-------------------------

If you like, you can add files to the "www" or "www_public"
subdirectories.

Files in the "www_public" directory will be made available
to anyone, even users who have not authenticated.

Files in the "www" directory will require authentication 
with a permission level you set in the admin interface.

To access the included example "all.html", you would go to:

http://localhost:44456/all.html