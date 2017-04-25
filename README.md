# Camera Proxy

Camera Proxy is a Windows Service which acts as a "smart" proxy for IP network cameras. Uses the .NET framework and parts of the VLC media player libraries.

The service runs a simple embedded HTTP(S) server and is compatible with practically any browser.

![Camera Proxy](http://i.imgur.com/iCzJaQN.png)

## Who is it for?

The service is for **programmers** and **very computer literate** people who want:

* a single point of access to view all their network cameras either locally or via the internet
* to provide live webcam views to others on the internet without giving them direct access to any cameras.  Many IP network cameras have serious security flaws and should not be publicly accessible.
* to reduce the network and CPU load of a camera that has multiple simultaneous viewers (the proxy service only creates one user's worth of load on the camera)

A readme file is included in the source and binary distributions to aid in initial setup, but most setup is done through the web interface.

## What cameras does it support?

* Most cameras that can provide jpeg still images or an mjpeg stream via HTTP are supported natively and very efficiently.  Http authentication is supported.
* Using the `vlc_transcode` camera type, you can add virtually any camera to the system as long as it is viewable in VLC media player.  This option uses much more CPU and memory, however.  It is best to use jpeg or mjpeg based inputs whenever possible.
* Experimental support for proxying full rtsp / h264 streams using live555 and the VLC web plugin.  Results may vary.

## What PTZ cameras can it control?

* Nearly any PTZ camera can be controlled by using **Custom PTZ Profiles**.
* A small handful of PTZ cameras have PTZ support already built in.

## Can I password protect the service?

Yes.  A simple (and optional) authentication scheme is in place.  Cameras have a permission level from 0 to 100 and so do user accounts.  If the user's permission level is less than the camera's permission level, the user cannot view the camera.

## What else can it do?

* The base functionality (not including h264 / rtsp options) now works under Linux with the Mono framework.

* The service can re-encode imagery on-the-fly to JPG, PNG, and even WebP formats at any size and quality you specify in the URL.  Note that re-encoding images requires quite a lot of CPU time.  Here is a [Guide to Image URLs](https://github.com/bp2008/cameraproxy/wiki/Guide-to-Image-URLs).

## Why?

Most software NVRs can do everything this does and a lot more.  The difference is this is **free** and **open source**.  It is also **very efficient** with CPU and memory if your cameras support jpg or mjpg transmission via HTTP, as almost no image processing will be required.

## How to Use

Please see the [Setup Guide](https://github.com/bp2008/cameraproxy/wiki/Setup-Guide)
