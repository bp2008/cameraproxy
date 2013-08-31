Camera Setup

Cameras are defined each in their own file in the "Cams" 
subdirectory.  The file name is the camera ID, and each 
line of the file content specifies:

***********************************************************


Line 1: the type of camera (jpg or mjpg)
Line 2: the imagery URL
Line 3: the user name (or blank if not applicable)
Line 4: the password (or blank if not applicable)
Line 5: Camera privacy.  0 to indicate public or 1 to 
indicate private which means you must log in to see 
the camera
Line 6: PTZ type.  Only a two camera types are currently 
supported: "LoftekCheap" (Loftek and Apexis outdoor PTZ 
cameras, typically less than $200) and "Dahua" (Dahua 
cameras are typically $1000 or greater)


************************************************************

See the included examples for specific usage details.

Note that the dahua cameras do not provide their own jpeg 
imagery, so the dahua configuration files are written to 
obtain jpeg images from a Blue Iris installation instead 
of directly from the camera.
